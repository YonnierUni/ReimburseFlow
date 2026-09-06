using Microsoft.AspNetCore.Mvc;
using ReimburseFlow.Api.Models;
using ReimburseFlow.Application.Common.Models;
using ReimburseFlow.Application.Reimbursements.Commands.Approve;
using ReimburseFlow.Application.Reimbursements.Commands.Create;
using ReimburseFlow.Application.Reimbursements.Commands.Reject;
using ReimburseFlow.Application.Reimbursements.Queries.GetById;
using ReimburseFlow.Application.Reimbursements.Queries.GetList;
using ReimburseFlow.Domain.Enums;

namespace ReimburseFlow.Api.Controllers;

[ApiController]
[Route("api/reimbursements")]
public sealed class ReimbursementsController(
    CreateReimbursementHandler createHandler,
    ApproveReimbursementHandler approveHandler,
    RejectReimbursementHandler rejectHandler,
    GetReimbursementByIdHandler getByIdHandler,
    GetReimbursementsHandler getListHandler,
    ILogger<ReimbursementsController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ReimbursementDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ReimbursementDto>> Create(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreateReimbursementRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "The Idempotency-Key header is required.",
                Instance = HttpContext.Request.Path
            };
            problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
            return BadRequest(problemDetails);
        }

        var result = await createHandler.HandleAsync(
            new CreateReimbursementCommand(
                request.EmployeeId,
                request.ReceiptNumber,
                request.ExpenseDate,
                request.Category,
                request.Description,
                request.Amount,
                idempotencyKey),
            cancellationToken);

        logger.LogInformation("Reimbursement created or replayed: {ReimbursementId}", result.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReimbursementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReimbursementDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await getByIdHandler.HandleAsync(
            new GetReimbursementByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReimbursementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReimbursementDto>>> GetList(
        [FromQuery] ReimbursementStatus? status,
        [FromQuery] ExpenseCategory? category,
        CancellationToken cancellationToken)
    {
        var result = await getListHandler.HandleAsync(
            new GetReimbursementsQuery(status, category),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ReimbursementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReimbursementDto>> Approve(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await approveHandler.HandleAsync(
            new ApproveReimbursementCommand(id),
            cancellationToken);

        logger.LogInformation("Reimbursement approved: {ReimbursementId}", id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ReimbursementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReimbursementDto>> Reject(
        Guid id,
        [FromBody] RejectReimbursementRequest request,
        CancellationToken cancellationToken)
    {
        var result = await rejectHandler.HandleAsync(
            new RejectReimbursementCommand(id, request.Reason),
            cancellationToken);

        logger.LogInformation("Reimbursement rejected: {ReimbursementId}", id);
        return Ok(result);
    }
}
