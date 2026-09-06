using Microsoft.EntityFrameworkCore;
using ReimburseFlow.Application.Abstractions.Persistence;
using ReimburseFlow.Infrastructure.Persistence.Models;

namespace ReimburseFlow.Infrastructure.Persistence.Repositories;

public sealed class IdempotencyRepository(ReimburseFlowDbContext dbContext) : IIdempotencyRepository
{
    public async Task<Guid?> FindReimbursementIdAsync(string key, CancellationToken cancellationToken)
    {
        var request = await dbContext.IdempotencyRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(request => request.Key == key, cancellationToken);

        return request?.ReimbursementId;
    }

    public async Task AddAsync(
        string key,
        Guid reimbursementId,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        await dbContext.IdempotencyRequests.AddAsync(
            new IdempotencyRequest(key, reimbursementId, createdAt),
            cancellationToken);
    }
}
