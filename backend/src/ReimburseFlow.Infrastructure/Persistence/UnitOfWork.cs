using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReimburseFlow.Application.Abstractions.Persistence;
using ReimburseFlow.Application.Common.Exceptions;

namespace ReimburseFlow.Infrastructure.Persistence;

public sealed class UnitOfWork(ReimburseFlowDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "The reimbursement was changed by another operation.",
                exception);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            var constraintMessage = exception.GetBaseException().Message;

            if (SqlServerConstraintClassifier.IsIdempotencyViolation(constraintMessage))
            {
                throw new IdempotencyConflictException(
                    "Another operation registered this idempotency key first.",
                    exception);
            }

            if (SqlServerConstraintClassifier.IsBusinessDuplicateViolation(constraintMessage))
            {
                throw new ConflictException(
                    "A reimbursement already exists for this employee and receipt.",
                    exception);
            }

            throw new ConflictException(
                "The persistence operation violated a unique constraint.",
                exception);
        }
    }

    public void DiscardPendingChanges()
    {
        foreach (var entry in dbContext.ChangeTracker.Entries()
                     .Where(entry => entry.State == EntityState.Added))
        {
            entry.State = EntityState.Detached;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.GetBaseException() is SqlException sqlException
            && SqlServerConstraintClassifier.IsUniqueViolation(sqlException);
    }
}
