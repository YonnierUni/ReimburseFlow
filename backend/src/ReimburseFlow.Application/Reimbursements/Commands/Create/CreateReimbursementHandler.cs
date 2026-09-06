using ReimburseFlow.Application.Abstractions.Persistence;
using ReimburseFlow.Application.Common.Exceptions;
using ReimburseFlow.Application.Common.Models;
using ReimburseFlow.Domain.Entities;

namespace ReimburseFlow.Application.Reimbursements.Commands.Create;

public sealed class CreateReimbursementHandler(
    IReimbursementRepository reimbursements,
    IIdempotencyRepository idempotency,
    IUnitOfWork unitOfWork)
{
    public async Task<ReimbursementDto> HandleAsync(
        CreateReimbursementCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            throw new ArgumentException("IdempotencyKey is required.", nameof(command.IdempotencyKey));
        }

        var key = command.IdempotencyKey.Trim();
        var existingId = await idempotency.FindReimbursementIdAsync(key, cancellationToken);

        if (existingId.HasValue)
        {
            var existing = await reimbursements.GetByIdAsync(existingId.Value, cancellationToken)
                ?? throw new ConflictException("The idempotency key references a missing reimbursement.");

            return ReimbursementDto.FromEntity(existing);
        }

        if (!string.IsNullOrWhiteSpace(command.EmployeeId)
            && !string.IsNullOrWhiteSpace(command.ReceiptNumber)
            && await reimbursements.ExistsByEmployeeAndReceiptAsync(
                command.EmployeeId.Trim(),
                command.ReceiptNumber.Trim(),
                cancellationToken))
        {
            throw new ConflictException("A reimbursement already exists for this employee and receipt.");
        }

        var reimbursement = new Reimbursement(
            command.EmployeeId,
            command.ReceiptNumber,
            command.ExpenseDate,
            command.Category,
            command.Description,
            command.Amount);

        await reimbursements.AddAsync(reimbursement, cancellationToken);
        await idempotency.AddAsync(key, reimbursement.Id, reimbursement.CreatedAt, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (IdempotencyConflictException exception)
        {
            unitOfWork.DiscardPendingChanges();

            var winningId = await idempotency.FindReimbursementIdAsync(key, cancellationToken);

            if (!winningId.HasValue)
            {
                throw new ConflictException(
                    "The idempotency key conflict could not be resolved because the winning request was not found.",
                    exception);
            }

            var winningReimbursement = await reimbursements.GetByIdAsync(winningId.Value, cancellationToken);

            if (winningReimbursement is null)
            {
                throw new ConflictException(
                    "The idempotency key references a reimbursement that could not be found.",
                    exception);
            }

            return ReimbursementDto.FromEntity(winningReimbursement);
        }

        return ReimbursementDto.FromEntity(reimbursement);
    }
}
