namespace ReimburseFlow.Application.Common.Exceptions;

public sealed class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException(string message)
        : base(message)
    {
    }

    public IdempotencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
