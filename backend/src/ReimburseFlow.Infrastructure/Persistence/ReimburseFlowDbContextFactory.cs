using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReimburseFlow.Infrastructure.Persistence;

public sealed class ReimburseFlowDbContextFactory : IDesignTimeDbContextFactory<ReimburseFlowDbContext>
{
    public ReimburseFlowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReimburseFlowDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=ReimburseFlow;Trusted_Connection=True;TrustServerCertificate=True;");

        return new ReimburseFlowDbContext(optionsBuilder.Options);
    }
}
