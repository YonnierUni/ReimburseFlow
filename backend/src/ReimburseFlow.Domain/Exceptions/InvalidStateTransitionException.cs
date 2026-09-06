namespace ReimburseFlow.Domain.Exceptions;

public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string message)
        : base(message)
    {
    }
}
