using Microsoft.EntityFrameworkCore;
using ReimburseFlow.Application.Abstractions.Persistence;
using ReimburseFlow.Domain.Entities;
using ReimburseFlow.Domain.Enums;

namespace ReimburseFlow.Infrastructure.Persistence.Repositories;

public sealed class ReimbursementRepository(ReimburseFlowDbContext dbContext) : IReimbursementRepository
{
    public Task<Reimbursement?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Reimbursements
            .SingleOrDefaultAsync(reimbursement => reimbursement.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByEmployeeAndReceiptAsync(
        string employeeId,
        string receiptNumber,
        CancellationToken cancellationToken)
    {
        return dbContext.Reimbursements
            .AsNoTracking()
            .AnyAsync(
                reimbursement => reimbursement.EmployeeId == employeeId
                    && reimbursement.ReceiptNumber == receiptNumber,
                cancellationToken);
    }

    public async Task AddAsync(Reimbursement reimbursement, CancellationToken cancellationToken)
    {
        await dbContext.Reimbursements.AddAsync(reimbursement, cancellationToken);
    }

    public async Task<IReadOnlyList<Reimbursement>> ListAsync(
        ReimbursementStatus? status,
        ExpenseCategory? category,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Reimbursements.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(reimbursement => reimbursement.Status == status.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(reimbursement => reimbursement.Category == category.Value);
        }

        return await query
            .OrderByDescending(reimbursement => reimbursement.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
