using ReimburseFlow.Application.Abstractions.Persistence;
using ReimburseFlow.Application.Common.Exceptions;
using ReimburseFlow.Application.Common.Models;

namespace ReimburseFlow.Application.Reimbursements.Commands.Approve;

public sealed class ApproveReimbursementHandler(
    IReimbursementRepository reimbursements,
    IUnitOfWork unitOfWork)
{
    public async Task<ReimbursementDto> HandleAsync(
        ApproveReimbursementCommand command,
        CancellationToken cancellationToken = default)
    {
        var reimbursement = await reimbursements.GetByIdAsync(command.ReimbursementId, cancellationToken)
            ?? throw new NotFoundException($"Reimbursement '{command.ReimbursementId}' was not found.");

        reimbursement.Approve();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ReimbursementDto.FromEntity(reimbursement);
    }
}
