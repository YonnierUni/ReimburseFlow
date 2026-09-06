using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReimburseFlow.Application.Abstractions.Persistence;
using ReimburseFlow.Infrastructure.Persistence;
using ReimburseFlow.Infrastructure.Persistence.Repositories;

namespace ReimburseFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<ReimburseFlowDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IReimbursementRepository, ReimbursementRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
