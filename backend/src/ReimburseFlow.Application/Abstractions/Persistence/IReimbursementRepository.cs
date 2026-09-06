using ReimburseFlow.Domain.Entities;
using ReimburseFlow.Domain.Enums;

namespace ReimburseFlow.Application.Abstractions.Persistence;

public interface IReimbursementRepository
{
    Task<Reimbursement?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByEmployeeAndReceiptAsync(
        string employeeId,
        string receiptNumber,
        CancellationToken cancellationToken);

    Task AddAsync(Reimbursement reimbursement, CancellationToken cancellationToken);

    Task<IReadOnlyList<Reimbursement>> ListAsync(
        ReimbursementStatus? status,
        ExpenseCategory? category,
        CancellationToken cancellationToken);
}
