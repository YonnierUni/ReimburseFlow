using ReimburseFlow.Domain.Entities;
using ReimburseFlow.Domain.Enums;
using ReimburseFlow.Domain.Exceptions;

namespace ReimburseFlow.Tests.Domain;

public class ReimbursementTests
{
    [Fact]
    public void CreatingValidReimbursement_ShouldStartAsPending()
    {
        var reimbursement = CreateReimbursement();

        Assert.NotEqual(Guid.Empty, reimbursement.Id);
        Assert.Equal(ReimbursementStatus.Pending, reimbursement.Status);
        Assert.Null(reimbursement.RejectionReason);
        Assert.NotEqual(default, reimbursement.CreatedAt);
        Assert.Null(reimbursement.UpdatedAt);
    }

    [Fact]
    public void CreatingReimbursement_WithZeroAmount_ShouldThrow()
    {
        var exception = Assert.Throws<DomainException>(() => CreateReimbursement(amount: 0));

        Assert.Equal("Amount must be greater than zero.", exception.Message);
    }

    [Fact]
    public void CreatingReimbursement_WithNegativeAmount_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CreateReimbursement(amount: -1));
    }

    [Fact]
    public void CreatingReimbursement_WithoutEmployeeId_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CreateReimbursement(employeeId: " "));
    }

    [Fact]
    public void CreatingReimbursement_WithoutReceiptNumber_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CreateReimbursement(receiptNumber: ""));
    }

    [Fact]
    public void CreatingReimbursement_WithoutDescription_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CreateReimbursement(description: "\t"));
    }

    [Fact]
    public void CreatingReimbursement_WithInvalidCategory_ShouldThrow()
    {
        var exception = Assert.Throws<DomainException>(() => CreateReimbursement(category: (ExpenseCategory)999));

        Assert.Equal("Expense category is invalid.", exception.Message);
    }

    [Fact]
    public void CreatingReimbursement_ShouldTrimEmployeeId()
    {
        var reimbursement = CreateReimbursement(employeeId: "  employee-123  ");

        Assert.Equal("employee-123", reimbursement.EmployeeId);
    }

    [Fact]
    public void CreatingReimbursement_ShouldTrimReceiptNumber()
    {
        var reimbursement = CreateReimbursement(receiptNumber: "  receipt-456  ");

        Assert.Equal("receipt-456", reimbursement.ReceiptNumber);
    }

    [Fact]
    public void CreatingReimbursement_ShouldTrimDescription()
    {
        var reimbursement = CreateReimbursement(description: "  Client lunch  ");

        Assert.Equal("Client lunch", reimbursement.Description);
    }

    [Fact]
    public void Approve_WhenPending_ShouldSetApproved()
    {
        var reimbursement = CreateReimbursement();

        reimbursement.Approve();

        Assert.Equal(ReimbursementStatus.Approved, reimbursement.Status);
        Assert.NotNull(reimbursement.UpdatedAt);
        Assert.Null(reimbursement.RejectionReason);
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ShouldThrow()
    {
        var reimbursement = CreateReimbursement();
        reimbursement.Approve();

        Assert.Throws<InvalidStateTransitionException>(() => reimbursement.Approve());
    }

    [Fact]
    public void Approve_WhenRejected_ShouldThrow()
    {
        var reimbursement = CreateReimbursement();
        reimbursement.Reject("Missing receipt details.");

        Assert.Throws<InvalidStateTransitionException>(() => reimbursement.Approve());
    }

    [Fact]
    public void Reject_WhenPending_ShouldSetRejectedAndReason()
    {
        var reimbursement = CreateReimbursement();
        const string reason = "  Receipt is not readable.  ";

        reimbursement.Reject(reason);

        Assert.Equal(ReimbursementStatus.Rejected, reimbursement.Status);
        Assert.Equal("Receipt is not readable.", reimbursement.RejectionReason);
        Assert.NotNull(reimbursement.UpdatedAt);
    }

    [Fact]
    public void Reject_WithoutReason_ShouldThrow()
    {
        var reimbursement = CreateReimbursement();

        Assert.Throws<DomainException>(() => reimbursement.Reject("  "));
    }

    [Fact]
    public void Reject_WhenApproved_ShouldThrow()
    {
        var reimbursement = CreateReimbursement();
        reimbursement.Approve();

        Assert.Throws<InvalidStateTransitionException>(() => reimbursement.Reject("Too late."));
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ShouldThrow()
    {
        var reimbursement = CreateReimbursement();
        reimbursement.Reject("Duplicate receipt.");

        Assert.Throws<InvalidStateTransitionException>(() => reimbursement.Reject("Another reason."));
    }

    private static Reimbursement CreateReimbursement(
        string employeeId = "employee-123",
        string receiptNumber = "receipt-456",
        string description = "Client lunch",
        decimal amount = 125.50m,
        ExpenseCategory category = ExpenseCategory.Food)
    {
        return new Reimbursement(
            employeeId,
            receiptNumber,
            new DateOnly(2026, 9, 5),
            category,
            description,
            amount);
    }
}
