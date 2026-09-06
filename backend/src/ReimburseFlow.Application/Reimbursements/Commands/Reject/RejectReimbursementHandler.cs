using ReimburseFlow.Application.Abstractions.Persistence;
using ReimburseFlow.Application.Common.Exceptions;
using ReimburseFlow.Application.Common.Models;

namespace ReimburseFlow.Application.Reimbursements.Commands.Reject;

public sealed class RejectReimbursementHandler(
    IReimbursementRepository reimbursements,
    IUnitOfWork unitOfWork)
{
    public async Task<ReimbursementDto> HandleAsync(
        RejectReimbursementCommand command,
        CancellationToken cancellationToken = default)
    {
        var reimbursement = await reimbursements.GetByIdAsync(command.ReimbursementId, cancellationToken)
            ?? throw new NotFoundException($"Reimbursement '{command.ReimbursementId}' was not found.");

        reimbursement.Reject(command.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ReimbursementDto.FromEntity(reimbursement);
    }
}
