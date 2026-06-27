using Domain.Entities.Base;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Base.Application;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Application
{
    public class ApplicationValuesEntity : ApplicationValuesBase, IDataKeyFilterReadOnly
    {
        // Id بدون [Key] — EF يكتشفه تلقائياً بالاسم
        public int Id { get; set; }

        public int CalculationId { get; set; }

        [JsonIgnore]
        public CalculationEntity Calculation { get; set; } = null!;

        public int ApplicationId { get; set; }

        public ApplicationEntity Application { get; set; } = null!;

        [JsonIgnore]
        public int TenantId { get; set; }

        public static ApplicationValuesEntity Create(
            int calculationId,
            int applicationId,
            int userId,
            string? name,
            string? responsible,
            ApplicationValuesData? data)
        {
            return new ApplicationValuesEntity
            {
                CalculationId = calculationId,
                ApplicationId = applicationId,
                UserId = userId,
                Name = NormalizeRequired(name, "Application value name"),
                Responsible = NormalizeOptional(responsible) ?? string.Empty,
                Data = data ?? new ApplicationValuesData(),
                LastUpdate = DateTime.UtcNow
            };
        }

        public void UpdateFrom(ApplicationValuesEntity source)
        {
            ArgumentNullException.ThrowIfNull(source);

            UserId = source.UserId;
            Name = NormalizeRequired(source.Name, "Application value name");
            Responsible = NormalizeOptional(source.Responsible) ?? string.Empty;
            Data = source.Data ?? new ApplicationValuesData();
            LastUpdate = DateTime.UtcNow;
        }

        private static string NormalizeRequired(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ValidationException($"{fieldName} is required.");

            return value.Trim();
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
