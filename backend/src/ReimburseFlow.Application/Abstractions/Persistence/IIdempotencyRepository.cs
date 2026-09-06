namespace ReimburseFlow.Application.Abstractions.Persistence;

public interface IIdempotencyRepository
{
    Task<Guid?> FindReimbursementIdAsync(string key, CancellationToken cancellationToken);

    Task AddAsync(
        string key,
        Guid reimbursementId,
        DateTime createdAt,
        CancellationToken cancellationToken);
}
