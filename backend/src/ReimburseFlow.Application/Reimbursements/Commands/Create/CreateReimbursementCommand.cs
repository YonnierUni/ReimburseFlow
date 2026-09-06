using ReimburseFlow.Domain.Enums;

namespace ReimburseFlow.Application.Reimbursements.Commands.Create;

public sealed record CreateReimbursementCommand(
    string EmployeeId,
    string ReceiptNumber,
    DateOnly ExpenseDate,
    ExpenseCategory Category,
    string Description,
    decimal Amount,
    string IdempotencyKey);
