using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ReimburseFlow.Application.Common.Exceptions;
using ReimburseFlow.Domain.Exceptions;

namespace ReimburseFlow.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            InvalidStateTransitionException =>
                (StatusCodes.Status409Conflict, "Conflict"),
            DomainException or ArgumentException =>
                (StatusCodes.Status400BadRequest, "Bad Request"),
            NotFoundException =>
                (StatusCodes.Status404NotFound, "Not Found"),
            ConflictException or ConcurrencyConflictException or IdempotencyConflictException =>
                (StatusCodes.Status409Conflict, "Conflict"),
            _ =>
                (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing request {TraceId}", httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogWarning("Request failed with status {StatusCode}: {Detail}", statusCode, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
