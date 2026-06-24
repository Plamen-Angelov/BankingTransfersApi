using BankingTransfers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingTransfers.Infrastructure.Persistence.Configurations;

public class UserProfileAccountPermissionsConfiguration : IEntityTypeConfiguration<UserProfileAccountPermissions>
{
    public void Configure(EntityTypeBuilder<UserProfileAccountPermissions> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IBAN)
            .IsRequired()
            .HasMaxLength(34);

        builder.Property(x => x.CreateTransferPermission)
            .IsRequired();

        builder.HasIndex(x => new { x.UserProfileId, x.IBAN })
            .IsUnique();
    }
}
