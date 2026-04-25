using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using identity_service.Entities;

namespace identity_service.Data.Configurations;

public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.FirebaseUid)
            .HasColumnName("firebase_uid")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .HasDefaultValue(string.Empty);

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasMaxLength(50)
            .HasDefaultValue("customer");

        builder.Property(u => u.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(128);

        builder.Property(u => u.AvatarUrl)
            .HasColumnName("avatar_url");

        builder.Property(u => u.FcmToken)
            .HasColumnName("fcm_token");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(u => u.FirebaseUid)
            .IsUnique()
            .HasDatabaseName("idx_users_firebase_uid");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("idx_users_email");

        builder.HasIndex(u => u.FcmToken)
            .HasFilter("fcm_token IS NOT NULL")
            .HasDatabaseName("idx_users_fcm_token");
    }
}
