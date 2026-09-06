using Microsoft.EntityFrameworkCore;
using ReimburseFlow.Domain.Entities;
using ReimburseFlow.Infrastructure.Persistence.Models;

namespace ReimburseFlow.Infrastructure.Persistence;

public sealed class ReimburseFlowDbContext(DbContextOptions<ReimburseFlowDbContext> options) : DbContext(options)
{
    public DbSet<Reimbursement> Reimbursements => Set<Reimbursement>();

    public DbSet<IdempotencyRequest> IdempotencyRequests => Set<IdempotencyRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReimburseFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
