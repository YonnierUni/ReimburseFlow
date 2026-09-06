using Microsoft.Data.SqlClient;

namespace ReimburseFlow.Infrastructure.Persistence;

internal static class SqlServerConstraintClassifier
{
    public const string IdempotencyConstraint = "PK_IdempotencyRequests";
    public const string BusinessDuplicateConstraint = "UX_Reimbursements_EmployeeId_ReceiptNumber";

    public static bool IsUniqueViolation(SqlException exception)
    {
        return exception.Number is 2601 or 2627;
    }

    public static bool IsIdempotencyViolation(string errorMessage)
    {
        return errorMessage.Contains(IdempotencyConstraint, StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("IdempotencyRequests.Key", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsBusinessDuplicateViolation(string errorMessage)
    {
        return errorMessage.Contains(BusinessDuplicateConstraint, StringComparison.OrdinalIgnoreCase);
    }
}
