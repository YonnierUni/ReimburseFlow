using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReimburseFlow.Infrastructure.Persistence.Models;

namespace ReimburseFlow.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRequestConfiguration : IEntityTypeConfiguration<IdempotencyRequest>
{
    public void Configure(EntityTypeBuilder<IdempotencyRequest> builder)
    {
        builder.HasKey(request => request.Key);

        builder.Property(request => request.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(request => request.ReimbursementId)
            .IsRequired();

        builder.Property(request => request.CreatedAt)
            .IsRequired();

        builder.HasOne<ReimburseFlow.Domain.Entities.Reimbursement>()
            .WithMany()
            .HasForeignKey(request => request.ReimbursementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
