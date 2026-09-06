using ReimburseFlow.Domain.Enums;
using ReimburseFlow.Domain.Exceptions;

namespace ReimburseFlow.Domain.Entities;

public sealed class Reimbursement
{
    public Reimbursement(
        string employeeId,
        string receiptNumber,
        DateOnly expenseDate,
        ExpenseCategory category,
        string description,
        decimal amount)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            throw new DomainException("EmployeeId is required.");
        }

        if (string.IsNullOrWhiteSpace(receiptNumber))
        {
            throw new DomainException("ReceiptNumber is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Description is required.");
        }

        if (!Enum.IsDefined(category))
        {
            throw new DomainException("Expense category is invalid.");
        }

        if (amount <= 0)
        {
            throw new DomainException("Amount must be greater than zero.");
        }

        Id = Guid.NewGuid();
        EmployeeId = employeeId.Trim();
        ReceiptNumber = receiptNumber.Trim();
        ExpenseDate = expenseDate;
        Category = category;
        Description = description.Trim();
        Amount = amount;
        Status = ReimbursementStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string EmployeeId { get; private set; }

    public string ReceiptNumber { get; private set; }

    public DateOnly ExpenseDate { get; private set; }

    public ExpenseCategory Category { get; private set; }

    public string Description { get; private set; }

    public decimal Amount { get; private set; }

    public ReimbursementStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public void Approve()
    {
        EnsurePending();

        Status = ReimbursementStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        EnsurePending();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Rejection reason is required.");
        }

        Status = ReimbursementStatus.Rejected;
        RejectionReason = reason.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != ReimbursementStatus.Pending)
        {
            throw new DomainException("Only pending reimbursements can change status.");
        }
    }
}
