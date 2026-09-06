using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReimburseFlow.Application.Abstractions.Persistence;
using ReimburseFlow.Application.Common.Exceptions;
using ReimburseFlow.Domain.Entities;
using ReimburseFlow.Domain.Enums;

namespace ReimburseFlow.Tests.Api;

public class ReimbursementsApiTests
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/reimbursements", CreateRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedAndStringEnums()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "api-request-1");

        var response = await client.PostAsJsonAsync("/api/reimbursements", CreateRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("\"category\":\"Food\"", body);
        Assert.Contains("/api/reimbursements/", response.Headers.Location?.ToString());
    }

    [Theory]
    [MemberData(nameof(ValidCreateRequestMaxLengthCases))]
    public async Task Create_WithExactMaxLengthContractFields_ReturnsCreated(object request)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/api/reimbursements", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidCreateRequestMaxLengthCases))]
    public async Task Create_WithTooLongContractFields_ReturnsValidationProblemDetails(
        string expectedField,
        object request)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/api/reimbursements", request);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, problem?.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem?.TraceId));
        Assert.NotNull(problem?.Errors);
        Assert.Contains(expectedField, problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithZeroAmount_StillUsesDomainValidation()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "zero-amount-key");

        var response = await client.PostAsJsonAsync("/api/reimbursements", CreateRequest(amount: 0));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, problem?.Status);
        Assert.Equal("Amount must be greater than zero.", problem?.Detail);
        Assert.False(string.IsNullOrWhiteSpace(problem?.TraceId));
    }

    [Fact]
    public async Task Create_WithFutureExpenseDate_ReturnsCreated()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "future-date-key");

        var response = await client.PostAsJsonAsync(
            "/api/reimbursements",
            CreateRequest(expenseDate: "2099-01-01"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        using var factory = new ApiFactory();
        var reimbursement = factory.Persistence.AddReimbursement();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/reimbursements/{reimbursement.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFoundProblemDetails()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/reimbursements/{Guid.NewGuid()}");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, problem?.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem?.TraceId));
    }

    [Fact]
    public async Task List_ReturnsOk()
    {
        using var factory = new ApiFactory();
        factory.Persistence.AddReimbursement();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reimbursements");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_InvalidStatus_ReturnsBadRequest()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reimbursements?status=Invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_InvalidCategory_ReturnsBadRequest()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reimbursements?category=Invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsAvailableInDevelopment()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Approve_ReturnsOk()
    {
        using var factory = new ApiFactory();
        var reimbursement = factory.Persistence.AddReimbursement();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/reimbursements/{reimbursement.Id}/approve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Reject_ReturnsOk()
    {
        using var factory = new ApiFactory();
        var reimbursement = factory.Persistence.AddReimbursement();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/reimbursements/{reimbursement.Id}/reject",
            new { reason = "Receipt is not readable" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Reject_WithoutReason_ReturnsBadRequest()
    {
        using var factory = new ApiFactory();
        var reimbursement = factory.Persistence.AddReimbursement();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/reimbursements/{reimbursement.Id}/reject",
            new { reason = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reject_WithExactMaxLengthReason_ReturnsOk()
    {
        using var factory = new ApiFactory();
        var reimbursement = factory.Persistence.AddReimbursement();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/reimbursements/{reimbursement.Id}/reject",
            new { reason = new string('R', 1000) });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Reject_WithTooLongReason_ReturnsValidationProblemDetails()
    {
        using var factory = new ApiFactory();
        var reimbursement = factory.Persistence.AddReimbursement();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/reimbursements/{reimbursement.Id}/reject",
            new { reason = new string('R', 1001) });
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, problem?.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem?.TraceId));
        Assert.NotNull(problem?.Errors);
        Assert.Contains("Reason", problem.Errors.Keys);
    }

    [Fact]
    public async Task Approve_WhenAlreadyApproved_ReturnsConflictProblemDetails()
    {
        using var factory = new ApiFactory();
        var reimbursement = factory.Persistence.AddReimbursement();
        reimbursement.Approve();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/reimbursements/{reimbursement.Id}/approve", null);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(409, problem?.Status);
    }

    [Fact]
    public async Task Reject_WhenAlreadyRejected_ReturnsConflictProblemDetails()
    {
        using var factory = new ApiFactory();
        var reimbursement = factory.Persistence.AddReimbursement();
        reimbursement.Reject("Already rejected.");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/reimbursements/{reimbursement.Id}/reject",
            new { reason = "Another reason" });
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(409, problem?.Status);
    }

    [Fact]
    public async Task Conflict_ReturnsConflictProblemDetails()
    {
        using var factory = new ApiFactory
        {
            Persistence = { SaveException = new ConflictException("A reimbursement already exists for this employee and receipt.") }
        };
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "conflict-key");

        var response = await client.PostAsJsonAsync("/api/reimbursements", CreateRequest());
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(409, problem?.Status);
    }

    [Fact]
    public async Task ConcurrencyConflict_ReturnsConflictProblemDetails()
    {
        using var factory = new ApiFactory
        {
            Persistence = { SaveException = new ConcurrencyConflictException("Concurrency conflict") }
        };
        var reimbursement = factory.Persistence.AddReimbursement();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/reimbursements/{reimbursement.Id}/approve", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UnexpectedException_ReturnsGenericInternalServerError()
    {
        using var factory = new ApiFactory
        {
            Persistence = { SaveException = new InvalidOperationException("secret database detail") }
        };
        var reimbursement = factory.Persistence.AddReimbursement();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/reimbursements/{reimbursement.Id}/approve", null);
        var problem = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("secret database detail", problem);
    }

    public static IEnumerable<object[]> ValidCreateRequestMaxLengthCases()
    {
        yield return [CreateRequest(employeeId: new string('E', 100), receiptNumber: "receipt-max-employee")];
        yield return [CreateRequest(employeeId: "employee-max-receipt", receiptNumber: new string('R', 100))];
        yield return [CreateRequest(employeeId: "employee-max-description", receiptNumber: "receipt-max-description", description: new string('D', 1000))];
    }

    public static IEnumerable<object[]> InvalidCreateRequestMaxLengthCases()
    {
        yield return ["EmployeeId", CreateRequest(employeeId: new string('E', 101), receiptNumber: "receipt-long-employee")];
        yield return ["ReceiptNumber", CreateRequest(employeeId: "employee-long-receipt", receiptNumber: new string('R', 101))];
        yield return ["Description", CreateRequest(employeeId: "employee-long-description", receiptNumber: "receipt-long-description", description: new string('D', 1001))];
    }

    private static object CreateRequest(
        string employeeId = "employee-api",
        string receiptNumber = "receipt-api",
        string expenseDate = "2026-09-05",
        string description = "Client lunch",
        decimal amount = 25.50m) => new
    {
        employeeId,
        receiptNumber,
        expenseDate,
        category = "Food",
        description,
        amount
    };

    private sealed record ProblemDetailsResponse(int? Status, string? Detail, string? TraceId);

    private sealed record ValidationProblemDetailsResponse(
        int? Status,
        string? TraceId,
        Dictionary<string, string[]>? Errors);
}

public sealed class ApiFactory : WebApplicationFactory<global::Program>
{
    public ApiTestPersistence Persistence { get; set; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IReimbursementRepository>();
            services.RemoveAll<IIdempotencyRepository>();
            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton(Persistence);
            services.AddSingleton<IReimbursementRepository>(Persistence);
            services.AddSingleton<IIdempotencyRepository>(Persistence);
            services.AddSingleton<IUnitOfWork>(Persistence);
        });
    }
}

public sealed class ApiTestPersistence : IReimbursementRepository, IIdempotencyRepository, IUnitOfWork
{
    private readonly List<Reimbursement> reimbursements = [];
    private readonly Dictionary<string, Guid> idempotencyKeys = [];

    public Exception? SaveException { get; set; }

    public Reimbursement AddReimbursement()
    {
        var reimbursement = new Reimbursement(
            $"employee-{reimbursements.Count}",
            $"receipt-{reimbursements.Count}",
            new DateOnly(2026, 9, 5),
            ExpenseCategory.Food,
            "Client lunch",
            25.50m);
        reimbursements.Add(reimbursement);
        return reimbursement;
    }

    public Task<Reimbursement?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(reimbursements.SingleOrDefault(reimbursement => reimbursement.Id == id));

    public Task<bool> ExistsByEmployeeAndReceiptAsync(
        string employeeId,
        string receiptNumber,
        CancellationToken cancellationToken) =>
        Task.FromResult(reimbursements.Any(reimbursement =>
            reimbursement.EmployeeId == employeeId && reimbursement.ReceiptNumber == receiptNumber));

    public Task AddAsync(Reimbursement reimbursement, CancellationToken cancellationToken)
    {
        reimbursements.Add(reimbursement);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Reimbursement>> ListAsync(
        ReimbursementStatus? status,
        ExpenseCategory? category,
        CancellationToken cancellationToken)
    {
        IEnumerable<Reimbursement> result = reimbursements;
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
        Task.FromResult(idempotencyKeys.TryGetValue(key, out var id) ? id : (Guid?)null);

    public Task AddAsync(string key, Guid reimbursementId, DateTime createdAt, CancellationToken cancellationToken)
    {
        idempotencyKeys.Add(key, reimbursementId);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (SaveException is not null)
        {
            throw SaveException;
        }

        return Task.FromResult(1);
    }

    public void DiscardPendingChanges()
    {
    }
}
