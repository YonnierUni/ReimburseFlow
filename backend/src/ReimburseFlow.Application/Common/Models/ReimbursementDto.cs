using ReimburseFlow.Domain.Entities;
using ReimburseFlow.Domain.Enums;

namespace ReimburseFlow.Application.Common.Models;

public sealed record ReimbursementDto(
    Guid Id,
    string EmployeeId,
    string ReceiptNumber,
    DateOnly ExpenseDate,
    ExpenseCategory Category,
    string Description,
    decimal Amount,
    ReimbursementStatus Status,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static ReimbursementDto FromEntity(Reimbursement reimbursement) => new(
        reimbursement.Id,
        reimbursement.EmployeeId,
        reimbursement.ReceiptNumber,
        reimbursement.ExpenseDate,
        reimbursement.Category,
        reimbursement.Description,
        reimbursement.Amount,
        reimbursement.Status,
        reimbursement.RejectionReason,
        reimbursement.CreatedAt,
        reimbursement.UpdatedAt);
}
