namespace ReimburseFlow.Infrastructure.Persistence.Models;

public sealed class IdempotencyRequest
{
    public IdempotencyRequest(string key, Guid reimbursementId, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(key));
        }

        if (reimbursementId == Guid.Empty)
        {
            throw new ArgumentException("ReimbursementId is required.", nameof(reimbursementId));
        }

        Key = key.Trim();
        ReimbursementId = reimbursementId;
        CreatedAt = createdAt;
    }

    private IdempotencyRequest()
    {
    }

    public string Key { get; private set; } = null!;

    public Guid ReimbursementId { get; private set; }

    public DateTime CreatedAt { get; private set; }
}
