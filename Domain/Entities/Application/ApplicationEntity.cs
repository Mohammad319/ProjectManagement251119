using Domain.Entities.Base;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Application;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Application
{
    public class RowEntity : RowBase
    {
        public List<AttributeBase> Attributes { get; set; } = [];
    }

    public class ApplicationDataEntity : ApplicationDataBase
    {
        public List<RowEntity> Rows { get; set; } = [];
    }

    /// <summary>
    /// Application (form) created by a department.
    /// </summary>
    public sealed class ApplicationEntity : ApplicationBase, IDataKeyFilterReadOnly
    {
        [Key]
        public int Id { get; set; }

        private ApplicationDataEntity _data = new();

        public ApplicationDataEntity Data
        {
            get => _data;
            set => _data = value ?? new ApplicationDataEntity();
        }

        public int DepartmentId { get; set; }

        [JsonIgnore]
        public DepartmentEntity Department { get; set; } = null!;

        [JsonIgnore]
        public int TenantId { get; set; }

        public static ApplicationEntity Create(
            int departmentId,
            bool isVisible,
            int userId,
            string? name,
            ApplicationDataEntity? data)
        {
            return new ApplicationEntity
            {
                DepartmentId = departmentId,
                IsVisible = isVisible,
                UserId = userId,
                Name = NormalizeName(name),
                Data = data ?? new ApplicationDataEntity(),
                LastUpdate = DateTime.UtcNow
            };
        }

        public void UpdateFrom(ApplicationEntity source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            DepartmentId = source.DepartmentId;
            IsVisible = source.IsVisible;
            UserId = source.UserId;
            Name = NormalizeName(source.Name);
            Data = source.Data ?? new ApplicationDataEntity();
            LastUpdate = DateTime.UtcNow;
        }

        public void TouchUtc()
        {
            LastUpdate = DateTime.UtcNow;
        }

        private static string NormalizeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Application name is required.");

            return name.Trim();
        }
    }
}
