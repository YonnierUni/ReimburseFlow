using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ReimburseFlow.Domain.Entities;
using ReimburseFlow.Domain.Enums;
using ReimburseFlow.Infrastructure.Persistence;
using ReimburseFlow.Infrastructure.Persistence.Models;

namespace ReimburseFlow.Tests.Persistence;

public class PersistenceModelTests
{
    private static readonly DbContextOptions<ReimburseFlowDbContext> Options =
        new DbContextOptionsBuilder<ReimburseFlowDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ReimburseFlowTests;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

    [Fact]
    public void Reimbursement_EmployeeAndReceiptNumberIndex_ShouldBeUnique()
    {
        var entityType = GetEntityType<Reimbursement>();
        var index = entityType.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Reimbursement.EmployeeId), nameof(Reimbursement.ReceiptNumber)]));

        Assert.True(index.IsUnique);
        Assert.Equal("UX_Reimbursements_EmployeeId_ReceiptNumber", index.GetDatabaseName());
    }

    [Fact]
    public void Reimbursement_RowVersion_ShouldBeConcurrencyToken()
    {
        var property = GetProperty<Reimbursement>(nameof(Reimbursement.RowVersion));

        Assert.True(property.IsConcurrencyToken);
    }

    [Fact]
    public void Reimbursement_RowVersion_ShouldUseDatabaseGeneratedRowVersion()
    {
        var property = GetProperty<Reimbursement>(nameof(Reimbursement.RowVersion));

        Assert.Equal("rowversion", property.GetColumnType());
        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
    }

    [Fact]
    public void Reimbursement_Amount_ShouldUseDecimal18Scale2()
    {
        var property = GetProperty<Reimbursement>(nameof(Reimbursement.Amount));

        Assert.Equal("decimal(18,2)", property.GetColumnType());
    }

    [Fact]
    public void IdempotencyRequest_Key_ShouldBeUniquePrimaryKey()
    {
        var entityType = GetEntityType<IdempotencyRequest>();
        var primaryKey = entityType.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(IdempotencyRequest.Key), primaryKey!.Properties.Single().Name);
        Assert.Equal(200, entityType.FindProperty(nameof(IdempotencyRequest.Key))!.GetMaxLength());
    }

    [Fact]
    public void IdempotencyRequest_WithValidValues_ShouldTrimKey()
    {
        var reimbursementId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var request = new IdempotencyRequest("  request-key  ", reimbursementId, createdAt);

        Assert.Equal("request-key", request.Key);
        Assert.Equal(reimbursementId, request.ReimbursementId);
        Assert.Equal(createdAt, request.CreatedAt);
    }

    [Fact]
    public void IdempotencyRequest_WithEmptyKey_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new IdempotencyRequest("  ", Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void IdempotencyRequest_WithEmptyReimbursementId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new IdempotencyRequest("request-key", Guid.Empty, DateTime.UtcNow));
    }

    [Fact]
    public void Reimbursement_Enums_ShouldUseStringConversion()
    {
        var entityType = GetEntityType<Reimbursement>();
        var category = entityType.FindProperty(nameof(Reimbursement.Category));
        var status = entityType.FindProperty(nameof(Reimbursement.Status));

        Assert.Equal("nvarchar(50)", category!.GetColumnType());
        Assert.Equal("nvarchar(50)", status!.GetColumnType());
    }

    private static IEntityType GetEntityType<TEntity>()
        where TEntity : class
    {
        return new ReimburseFlowDbContext(Options).Model.FindEntityType(typeof(TEntity))!;
    }

    private static IProperty GetProperty<TEntity>(string propertyName)
        where TEntity : class
    {
        return GetEntityType<TEntity>().FindProperty(propertyName)!;
    }
}
