using Domain.Entities.Base;
using Domain.Entities.Organisation;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Offer;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

        private OfferEntity() { }

        public OfferEntity(int resourceId, int? organisationId, OfferData metadata, string? comment)
        {
            ResourceId = resourceId;
            OrganisationId = organisationId;
            Metadata = NormalizeMetadata(metadata);
            Comment = NormalizeComment(comment);
            Date = DateTime.UtcNow;
        }

        public void Update(int? organisationId, OfferData metadata, string? comment)
        {
            OrganisationId = organisationId;
            Metadata = NormalizeMetadata(metadata);
            Comment = NormalizeComment(comment);
            Date = DateTime.UtcNow;
        }

        public void SetBaseCost(decimal baseCost)
        {
            Metadata.BaseCost = Math.Round(baseCost, 2, MidpointRounding.AwayFromZero);
            Metadata.Normalize();
        }

        private static OfferData NormalizeMetadata(OfferData? metadata)
        {
            metadata ??= new OfferData();
            metadata.Normalize();
            return metadata;
        }

        private static string? NormalizeComment(string? comment)
            => string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }
}
