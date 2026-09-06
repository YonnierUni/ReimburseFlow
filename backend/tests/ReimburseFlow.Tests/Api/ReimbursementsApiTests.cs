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

    private static object CreateRequest() => new
    {
        employeeId = "employee-api",
        receiptNumber = "receipt-api",
        expenseDate = "2026-09-05",
        category = "Food",
        description = "Client lunch",
        amount = 25.50m
    };

    private sealed record ProblemDetailsResponse(int? Status, string? TraceId);
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
