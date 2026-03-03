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
            // Tenant databases use the singular table name generated from DbSet<User>.
            // Mapping explicitly avoids runtime failures like "Invalid object name 'Users'".
            builder.ToTable("User");

            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.FirstName).HasMaxLength(30);
            builder.Property(u => u.LastName).HasMaxLength(30);
        }
    }

}
