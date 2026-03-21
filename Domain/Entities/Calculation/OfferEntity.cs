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
            private set => _metadata = CloneMetadata(value);
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
            Metadata = metadata;
            Comment = NormalizeComment(comment);
            Date = DateTime.UtcNow;
        }

        public void Update(int? organisationId, OfferData metadata, string? comment)
        {
            OrganisationId = organisationId;
            Metadata = metadata;
            Comment = NormalizeComment(comment);
            Date = DateTime.UtcNow;
        }

        public OfferData GetMetadataSnapshot()
            => CloneMetadata(_metadata);

        public void UpdateMetadata(Action<OfferData> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            var snapshot = GetMetadataSnapshot();
            update(snapshot);
            Metadata = snapshot;
        }

        public void SetBaseCost(decimal baseCost)
        {
            UpdateMetadata(m => m.BaseCost = baseCost);
        }

        private static OfferData CloneMetadata(OfferData? metadata)
        {
            metadata ??= new OfferData();

            var copy = new OfferData
            {
                Comment = metadata.Comment ?? string.Empty,
                Contact = metadata.Contact ?? string.Empty,
                Cost = metadata.Cost,
                BaseCost = metadata.BaseCost
            };

            copy.Normalize();
            return copy;
        }

        private static string? NormalizeComment(string? comment)
            => string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }
}
