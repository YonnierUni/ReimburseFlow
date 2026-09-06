using ReimburseFlow.Application.Abstractions.Persistence;
using ReimburseFlow.Application.Common.Exceptions;
using ReimburseFlow.Application.Common.Models;

namespace ReimburseFlow.Application.Reimbursements.Queries.GetById;

public sealed class GetReimbursementByIdHandler(IReimbursementRepository reimbursements)
{
    public async Task<ReimbursementDto> HandleAsync(
        GetReimbursementByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var reimbursement = await reimbursements.GetByIdAsync(query.ReimbursementId, cancellationToken)
            ?? throw new NotFoundException($"Reimbursement '{query.ReimbursementId}' was not found.");

        return ReimbursementDto.FromEntity(reimbursement);
    }
}
