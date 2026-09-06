using ReimburseFlow.Domain.Enums;

namespace ReimburseFlow.Api.Models;

public sealed record CreateReimbursementRequest(
    string EmployeeId,
    string ReceiptNumber,
    DateOnly ExpenseDate,
    ExpenseCategory Category,
    string Description,
    decimal Amount);
