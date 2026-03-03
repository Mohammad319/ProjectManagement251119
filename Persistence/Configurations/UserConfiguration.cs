using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Persistence.Configurations
{

    internal sealed class UserConfiguration : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            // Tenant databases store users in [dbo].[User].
            // Set both table + schema explicitly so EF does not rely on login default schema.
            // This prevents runtime failures like: "Invalid object name 'User'".
            builder.ToTable("User", "dbo");

            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.FirstName).HasMaxLength(30);
            builder.Property(u => u.LastName).HasMaxLength(30);
        }
    }

}
