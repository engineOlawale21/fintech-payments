using FintechPayments.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(user => user.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320).IsRequired();
        builder.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
        builder.Property(user => user.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(user => user.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(user => user.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(user => user.NormalizedEmail).IsUnique().HasDatabaseName("ux_users_normalized_email");
    }
}
