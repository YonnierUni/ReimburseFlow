using ReimburseFlow.Domain.Enums;

namespace ReimburseFlow.Application.Reimbursements.Queries.GetList;

public sealed record GetReimbursementsQuery(
    ReimbursementStatus? Status = null,
    ExpenseCategory? Category = null);
