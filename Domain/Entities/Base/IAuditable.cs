using Domain.Entities.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Base
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        int? DeletedBy { get; set; }
    }

    public interface IAuditable
    {
        DateTime CreatedAt { get; set; }
        int? CreatedBy { get; set; }
        DateTime? UpdatedAt { get; set; }
        int? UpdatedBy { get; set; }
    }
    public interface IHasRowVersion
    {
        byte[] RowVersion { get; set; }
    }

    public abstract class AuditableEntity<TKey> : BaseEntity<TKey>, IAuditable
    {
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        [JsonIgnore]
        public UserEntity? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        [JsonIgnore]
        public UserEntity? UpdatedByUser { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
    public abstract class AuditableSoftDeletableEntity<TKey>
    : AuditableEntity<TKey>, ISoftDeletable
    {
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
        [JsonIgnore]
        public UserEntity? DeletedByUser { get; set; }
    }

}
