using Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Base
{
    public interface IAuditable
    {
        DateTime CreatedAt { get; set; }
        int CreatedBy { get; set; }
        DateTime? UpdatedAt { get; set; }
        int? UpdatedBy { get; set; }
    }
    public abstract class AuditableEntity<TKey> : BaseEntity<TKey>, IAuditable
    {
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
