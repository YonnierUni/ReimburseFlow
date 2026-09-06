using System.ComponentModel.DataAnnotations;
using ReimburseFlow.Domain.Enums;

namespace ReimburseFlow.Api.Models;

public sealed record CreateReimbursementRequest(
    [param: Required]
    [param: MaxLength(100)]
    string EmployeeId,
    [param: Required]
    [param: MaxLength(100)]
    string ReceiptNumber,
    DateOnly ExpenseDate,
    ExpenseCategory Category,
    [param: Required]
    [param: MaxLength(1000)]
    string Description,
    decimal Amount);
