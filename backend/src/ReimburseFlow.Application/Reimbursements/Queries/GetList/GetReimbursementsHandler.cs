using ReimburseFlow.Application.Abstractions.Persistence;
using ReimburseFlow.Application.Common.Models;

namespace ReimburseFlow.Application.Reimbursements.Queries.GetList;

public sealed class GetReimbursementsHandler(IReimbursementRepository reimbursements)
{
    public async Task<IReadOnlyList<ReimbursementDto>> HandleAsync(
        GetReimbursementsQuery query,
        CancellationToken cancellationToken = default)
    {
        var items = await reimbursements.ListAsync(query.Status, query.Category, cancellationToken);

        return items.Select(ReimbursementDto.FromEntity).ToArray();
    }
}
