using Microsoft.Extensions.DependencyInjection;
using ReimburseFlow.Application.Reimbursements.Commands.Approve;
using ReimburseFlow.Application.Reimbursements.Commands.Create;
using ReimburseFlow.Application.Reimbursements.Commands.Reject;
using ReimburseFlow.Application.Reimbursements.Queries.GetById;
using ReimburseFlow.Application.Reimbursements.Queries.GetList;

namespace ReimburseFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateReimbursementHandler>();
        services.AddScoped<ApproveReimbursementHandler>();
        services.AddScoped<RejectReimbursementHandler>();
        services.AddScoped<GetReimbursementByIdHandler>();
        services.AddScoped<GetReimbursementsHandler>();

        return services;
    }
}
