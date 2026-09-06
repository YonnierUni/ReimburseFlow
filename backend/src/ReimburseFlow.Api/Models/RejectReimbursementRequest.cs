using System.ComponentModel.DataAnnotations;

namespace ReimburseFlow.Api.Models;

public sealed record RejectReimbursementRequest(
    [param: Required]
    [param: MaxLength(1000)]
    string Reason);
