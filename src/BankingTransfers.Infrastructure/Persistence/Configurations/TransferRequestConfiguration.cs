using BankingTransfers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingTransfers.Infrastructure.Persistence.Configurations;

public class TransferRequestConfiguration : IEntityTypeConfiguration<TransferRequest>
{
    public void Configure(EntityTypeBuilder<TransferRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UId)
            .IsRequired();

        builder.Property(x => x.SourceIban)
            .IsRequired()
            .HasMaxLength(34);

        builder.Property(x => x.TargetIban)
            .IsRequired()
            .HasMaxLength(34);

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.ExecutionDate)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.UId)
            .IsUnique();

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(x => x.Status);
    }
}
