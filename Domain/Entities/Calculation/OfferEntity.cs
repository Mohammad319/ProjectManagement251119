using Domain.Entities.Base;
using Domain.Entities.Organisation;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Offer;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    [Index(nameof(TenantId), nameof(ResourceId))]
    public sealed class OfferEntity : IntBaseEntity
    {
        /// <summary>
        /// تاريخ العرض.
        /// </summary>
        public DateTime Date { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// تعليق اختياري على العرض.
        /// </summary>
        [MaxLength(FieldLengths.Comment)]
        public string? Comment { get; set; }

        private OfferData? _metadata;
        public OfferData Metadata
        {
            get => _metadata ??= new OfferData();
            set => _metadata = value;
        }

        /// <summary>
        /// المنظمة المقدِّمة للعرض (اختيارية).
        /// </summary>
        public int? OrganisationId { get; set; }

        [JsonIgnore]
        public OrganisationEntity? Organisation { get; set; }

        /// <summary>
        /// المورد المرتبط بهذا العرض (إجباري).
        /// </summary>
        public int ResourceId { get; set; }

        [JsonIgnore]
        public ResourceEntity Resource { get; set; } = null!;
    }
}
