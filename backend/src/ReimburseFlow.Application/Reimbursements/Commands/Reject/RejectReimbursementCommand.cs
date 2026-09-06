namespace ReimburseFlow.Application.Reimbursements.Commands.Reject;

public sealed record RejectReimbursementCommand(Guid ReimbursementId, string Reason);
