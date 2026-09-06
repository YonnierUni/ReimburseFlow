using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReimburseFlow.Domain.Entities;

namespace ReimburseFlow.Infrastructure.Persistence.Configurations;

public sealed class ReimbursementConfiguration : IEntityTypeConfiguration<Reimbursement>
{
    public void Configure(EntityTypeBuilder<Reimbursement> builder)
    {
        builder.HasKey(reimbursement => reimbursement.Id);

        builder.Property(reimbursement => reimbursement.EmployeeId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(reimbursement => reimbursement.ReceiptNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(reimbursement => reimbursement.ExpenseDate)
            .IsRequired();

        builder.Property(reimbursement => reimbursement.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(reimbursement => reimbursement.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(reimbursement => reimbursement.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(reimbursement => reimbursement.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(reimbursement => reimbursement.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(reimbursement => reimbursement.CreatedAt)
            .IsRequired();

        builder.Property(reimbursement => reimbursement.UpdatedAt);

        builder.Property(reimbursement => reimbursement.RowVersion)
            .IsRowVersion();

        builder.HasIndex(reimbursement => new
        {
            reimbursement.EmployeeId,
            reimbursement.ReceiptNumber
        })
        .IsUnique()
        .HasDatabaseName("UX_Reimbursements_EmployeeId_ReceiptNumber");
    }
}
