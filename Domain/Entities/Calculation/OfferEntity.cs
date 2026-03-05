using Domain.Entities.Base;
using Domain.Entities.Organisation;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Offer;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System;

namespace Domain.Entities.Calculation
{
    [Index(nameof(TenantId), nameof(ResourceId))]
    public sealed class OfferEntity : AuditableEntity<int>
    {
        public DateTime Date { get; private set; } = DateTime.UtcNow;

        [MaxLength(FieldLengths.Comment)]
        public string? Comment { get; private set; }

        private OfferData? _metadata;
        public OfferData Metadata
        {
            get => _metadata ??= new OfferData();
            private set => _metadata = value;
        }

        public int? OrganisationId { get; private set; }

        [JsonIgnore]
        public OrganisationEntity? Organisation { get; private set; }

        public int ResourceId { get; private set; }

        [JsonIgnore]
        public ResourceEntity Resource { get; private set; } = null!;

        private OfferEntity() { } // EF

        public OfferEntity(
            int resourceId,
            int? organisationId,
            OfferData metadata,
            string? comment)
        {
            ResourceId = resourceId;
            OrganisationId = organisationId;
            Metadata = metadata;
            
            Metadata.Normalize();
Comment = comment;
            Date = DateTime.UtcNow;
        }

        public void Update(
            int? organisationId,
            OfferData metadata,
            string? comment)
        {
            OrganisationId = organisationId;
            Metadata = metadata;
            
            Metadata.Normalize();
Comment = comment;
            Date = DateTime.UtcNow;
        }

        public void SetBaseCost(decimal baseCost)
        {
            Metadata.BaseCost = Math.Round(baseCost, 2, MidpointRounding.AwayFromZero);
        }
    }
}