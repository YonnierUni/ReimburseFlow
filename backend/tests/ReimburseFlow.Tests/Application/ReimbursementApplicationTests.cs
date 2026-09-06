using ReimburseFlow.Application.Abstractions.Persistence;
using ReimburseFlow.Application.Common.Exceptions;
using ReimburseFlow.Application.Common.Models;
using ReimburseFlow.Application.Reimbursements.Commands.Approve;
using ReimburseFlow.Application.Reimbursements.Commands.Create;
using ReimburseFlow.Application.Reimbursements.Commands.Reject;
using ReimburseFlow.Application.Reimbursements.Queries.GetById;
using ReimburseFlow.Application.Reimbursements.Queries.GetList;
using ReimburseFlow.Domain.Entities;
using ReimburseFlow.Domain.Enums;
using ReimburseFlow.Domain.Exceptions;
using ReimburseFlow.Infrastructure.Persistence;

namespace ReimburseFlow.Tests.Application;

public class ReimbursementApplicationTests
{
    [Fact]
    public async Task Create_ValidRequest_CreatesPendingReimbursementAndIdempotencyRecord()
    {
        var context = new FakePersistence();
        var handler = context.CreateHandler();

        var result = await handler.HandleAsync(CreateCommand());

        Assert.Equal(ReimbursementStatus.Pending, result.Status);
        Assert.Single(context.Reimbursements);
        Assert.Equal(result.Id, context.IdempotencyByKey["request-key"]);
        Assert.Equal(1, context.SaveChangesCount);
    }

    [Fact]
    public async Task Create_SameIdempotencyKey_ReturnsExistingWithoutCreatingAnother()
    {
        var context = new FakePersistence();
        var handler = context.CreateHandler();
        var first = await handler.HandleAsync(CreateCommand());

        var retry = await handler.HandleAsync(CreateCommand(description: "Retry payload"));

        Assert.Equal(first.Id, retry.Id);
        Assert.Single(context.Reimbursements);
        Assert.Equal(1, context.SaveChangesCount);
    }

    [Fact]
    public async Task Create_IdempotencyRace_ReturnsTheReimbursementCreatedByTheWinningRequest()
    {
        var context = new FakePersistence();
        var winningReimbursement = context.AddExisting();
        context.IdempotencyLookupResults.Enqueue(null);
        context.IdempotencyLookupResults.Enqueue(winningReimbursement.Id);
        context.SaveException = new IdempotencyConflictException("Idempotency key was already registered.");

        var result = await context.CreateHandler().HandleAsync(CreateCommand());

        Assert.Equal(winningReimbursement.Id, result.Id);
        Assert.Equal(1, context.SaveChangesCount);
        Assert.Equal(1, context.DiscardPendingChangesCount);
    }

    [Fact]
    public async Task Create_IdempotencyRace_WhenKeyIsStillMissing_ThrowsConflict()
    {
        var context = new FakePersistence
        {
            SaveException = new IdempotencyConflictException("Idempotency key was already registered.")
        };
        context.IdempotencyLookupResults.Enqueue(null);
        context.IdempotencyLookupResults.Enqueue(null);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            context.CreateHandler().HandleAsync(CreateCommand()));

        Assert.Contains("winning request was not found", exception.Message);
    }

    [Fact]
    public async Task Create_IdempotencyRace_WhenKeyReferencesMissingReimbursement_ThrowsConflict()
    {
        var context = new FakePersistence
        {
            SaveException = new IdempotencyConflictException("Idempotency key was already registered.")
        };
        context.IdempotencyLookupResults.Enqueue(null);
        context.IdempotencyLookupResults.Enqueue(Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            context.CreateHandler().HandleAsync(CreateCommand()));

        Assert.Contains("reimbursement that could not be found", exception.Message);
    }

    [Fact]
    public async Task Create_DuplicateEmployeeAndReceipt_ThrowsConflict()
    {
        var context = new FakePersistence();
        var handler = context.CreateHandler();
        await handler.HandleAsync(CreateCommand());

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.HandleAsync(CreateCommand(idempotencyKey: "another-key")));
    }

    [Fact]
    public async Task Create_NullEmployeeId_UsesDomainValidationInsteadOfNullReferenceException()
    {
        var context = new FakePersistence();

        var exception = await Assert.ThrowsAsync<ReimburseFlow.Domain.Exceptions.DomainException>(() =>
            context.CreateHandler().HandleAsync(CreateCommand(employeeId: null!)));

        Assert.Equal("EmployeeId is required.", exception.Message);
    }

    [Fact]
    public async Task Create_NullReceiptNumber_UsesDomainValidationInsteadOfNullReferenceException()
    {
        var context = new FakePersistence();

        var exception = await Assert.ThrowsAsync<ReimburseFlow.Domain.Exceptions.DomainException>(() =>
            context.CreateHandler().HandleAsync(CreateCommand(receiptNumber: null!)));

        Assert.Equal("ReceiptNumber is required.", exception.Message);
    }

    [Fact]
    public async Task Create_BusinessDuplicatePersistenceConflict_RemainsConflict()
    {
        var context = new FakePersistence
        {
            SaveException = new ConflictException("The persistence operation violated a unique constraint.")
        };
        context.IdempotencyLookupResults.Enqueue(null);

        await Assert.ThrowsAsync<ConflictException>(() =>
            context.CreateHandler().HandleAsync(CreateCommand()));
    }

    [Fact]
    public void ConstraintClassifier_DistinguishesIdempotencyAndBusinessDuplicate()
    {
        Assert.True(SqlServerConstraintClassifier.IsIdempotencyViolation(
            "Violation of PRIMARY KEY constraint 'PK_IdempotencyRequests'."));
        Assert.False(SqlServerConstraintClassifier.IsBusinessDuplicateViolation(
            "Violation of PRIMARY KEY constraint 'PK_IdempotencyRequests'."));
        Assert.True(SqlServerConstraintClassifier.IsBusinessDuplicateViolation(
            "Cannot insert duplicate key in object 'Reimbursements' with unique index 'UX_Reimbursements_EmployeeId_ReceiptNumber'."));
        Assert.False(SqlServerConstraintClassifier.IsIdempotencyViolation(
            "Cannot insert duplicate key in object 'Reimbursements' with unique index 'UX_Reimbursements_EmployeeId_ReceiptNumber'."));
    }

    [Fact]
    public async Task Create_UsesOneSaveForReimbursementAndIdempotency()
    {
        var context = new FakePersistence();

        await context.CreateHandler().HandleAsync(CreateCommand());

        Assert.Equal(1, context.SaveChangesCount);
    }

    [Fact]
    public async Task Approve_PendingReimbursement_ReturnsApprovedDto()
    {
        var context = new FakePersistence();
        var reimbursement = context.AddExisting();

        var result = await new ApproveReimbursementHandler(context, context)
            .HandleAsync(new ApproveReimbursementCommand(reimbursement.Id));

        Assert.Equal(ReimbursementStatus.Approved, result.Status);
        Assert.Equal(1, context.SaveChangesCount);
    }

    [Fact]
    public async Task Approve_MissingReimbursement_ThrowsNotFound()
    {
        var context = new FakePersistence();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ApproveReimbursementHandler(context, context)
                .HandleAsync(new ApproveReimbursementCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task Approve_InvalidDomainTransition_RemainsDomainException()
    {
        var context = new FakePersistence();
        var reimbursement = context.AddExisting();
        reimbursement.Approve();

        await Assert.ThrowsAsync<InvalidStateTransitionException>(() =>
            new ApproveReimbursementHandler(context, context)
                .HandleAsync(new ApproveReimbursementCommand(reimbursement.Id)));
    }

    [Fact]
    public async Task Reject_PendingReimbursement_ReturnsRejectedDtoWithReason()
    {
        var context = new FakePersistence();
        var reimbursement = context.AddExisting();

        var result = await new RejectReimbursementHandler(context, context)
            .HandleAsync(new RejectReimbursementCommand(reimbursement.Id, "Not readable"));

        Assert.Equal(ReimbursementStatus.Rejected, result.Status);
        Assert.Equal("Not readable", result.RejectionReason);
    }

    [Fact]
    public async Task Reject_MissingReimbursement_ThrowsNotFound()
    {
        var context = new FakePersistence();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new RejectReimbursementHandler(context, context)
                .HandleAsync(new RejectReimbursementCommand(Guid.NewGuid(), "Reason")));
    }

    [Fact]
    public async Task Reject_InvalidReason_RemainsDomainException()
    {
        var context = new FakePersistence();
        var reimbursement = context.AddExisting();

        await Assert.ThrowsAsync<ReimburseFlow.Domain.Exceptions.DomainException>(() =>
            new RejectReimbursementHandler(context, context)
                .HandleAsync(new RejectReimbursementCommand(reimbursement.Id, " ")));
    }

    [Fact]
    public async Task GetById_ReturnsDto()
    {
        var context = new FakePersistence();
        var reimbursement = context.AddExisting();

        var result = await new GetReimbursementByIdHandler(context)
            .HandleAsync(new GetReimbursementByIdQuery(reimbursement.Id));

        Assert.Equal(reimbursement.Id, result.Id);
        Assert.Equal(reimbursement.EmployeeId, result.EmployeeId);
    }

    [Fact]
    public async Task GetById_MissingReimbursement_ThrowsNotFound()
    {
        var context = new FakePersistence();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetReimbursementByIdHandler(context)
                .HandleAsync(new GetReimbursementByIdQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task GetList_FiltersByStatus()
    {
        var context = new FakePersistence();
        var pending = context.AddExisting();
        var approved = context.AddExisting();
        approved.Approve();

        var result = await new GetReimbursementsHandler(context)
            .HandleAsync(new GetReimbursementsQuery(Status: ReimbursementStatus.Pending));

        Assert.Single(result);
        Assert.Equal(pending.Id, result[0].Id);
    }

    [Fact]
    public async Task GetList_FiltersByCategory()
    {
        var context = new FakePersistence();
        context.AddExisting(ExpenseCategory.Food);
        var transportation = context.AddExisting(ExpenseCategory.Transportation);

        var result = await new GetReimbursementsHandler(context)
            .HandleAsync(new GetReimbursementsQuery(Category: ExpenseCategory.Transportation));

        Assert.Single(result);
        Assert.Equal(transportation.Id, result[0].Id);
    }

    [Fact]
    public async Task Approve_WhenPersistenceReportsConcurrency_ThrowsSemanticException()
    {
        var context = new FakePersistence { SaveException = new ConcurrencyConflictException("Conflict") };
        var reimbursement = context.AddExisting();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            new ApproveReimbursementHandler(context, context)
                .HandleAsync(new ApproveReimbursementCommand(reimbursement.Id)));
    }

    private static CreateReimbursementCommand CreateCommand(
        string description = "Client lunch",
        string idempotencyKey = "request-key",
        string employeeId = "employee-123",
        string receiptNumber = "receipt-456") => new(
            employeeId,
            receiptNumber,
            new DateOnly(2026, 9, 5),
            ExpenseCategory.Food,
            description,
            125.50m,
            idempotencyKey);

    private sealed class FakePersistence : IReimbursementRepository, IIdempotencyRepository, IUnitOfWork
    {
        public List<Reimbursement> Reimbursements { get; } = [];

        public Dictionary<string, Guid> IdempotencyByKey { get; } = [];

        public int SaveChangesCount { get; private set; }

        public int DiscardPendingChangesCount { get; private set; }

        public Exception? SaveException { get; set; }

        public Queue<Guid?> IdempotencyLookupResults { get; } = [];

        private List<Reimbursement> PendingReimbursements { get; } = [];

        private Dictionary<string, Guid> PendingIdempotency { get; } = [];

        public CreateReimbursementHandler CreateHandler() =>
            new(this, this, this);

        public Reimbursement AddExisting(ExpenseCategory category = ExpenseCategory.Food)
        {
            var reimbursement = new Reimbursement(
                "employee-" + Reimbursements.Count,
                "receipt-" + Reimbursements.Count,
                new DateOnly(2026, 9, 5),
                category,
                "Existing expense",
                10m);
            Reimbursements.Add(reimbursement);
            return reimbursement;
        }

        public Task<Reimbursement?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Reimbursements.SingleOrDefault(reimbursement => reimbursement.Id == id));

        public Task<bool> ExistsByEmployeeAndReceiptAsync(
            string employeeId,
            string receiptNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult(Reimbursements.Concat(PendingReimbursements).Any(reimbursement =>
                reimbursement.EmployeeId == employeeId && reimbursement.ReceiptNumber == receiptNumber));

        public Task AddAsync(Reimbursement reimbursement, CancellationToken cancellationToken)
        {
            PendingReimbursements.Add(reimbursement);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Reimbursement>> ListAsync(
            ReimbursementStatus? status,
            ExpenseCategory? category,
            CancellationToken cancellationToken)
        {
            IEnumerable<Reimbursement> result = Reimbursements.Concat(PendingReimbursements);

            if (status.HasValue)
            {
                result = result.Where(reimbursement => reimbursement.Status == status.Value);
            }

            if (category.HasValue)
            {
                result = result.Where(reimbursement => reimbursement.Category == category.Value);
            }

            return Task.FromResult<IReadOnlyList<Reimbursement>>(result.ToArray());
        }

        public Task<Guid?> FindReimbursementIdAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult(IdempotencyLookupResults.Count > 0
                ? IdempotencyLookupResults.Dequeue()
                : IdempotencyByKey.TryGetValue(key, out var id) ? id : (Guid?)null);

        public Task AddAsync(
            string key,
            Guid reimbursementId,
            DateTime createdAt,
            CancellationToken cancellationToken)
        {
            PendingIdempotency.Add(key, reimbursementId);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;

            if (SaveException is not null)
            {
                throw SaveException;
            }

            Reimbursements.AddRange(PendingReimbursements);
            PendingReimbursements.Clear();
            foreach (var pair in PendingIdempotency)
            {
                IdempotencyByKey.Add(pair.Key, pair.Value);
            }

            PendingIdempotency.Clear();

            return Task.FromResult(1);
        }

        public void DiscardPendingChanges()
        {
            DiscardPendingChangesCount++;
            PendingReimbursements.Clear();
            PendingIdempotency.Clear();
        }
    }
}
