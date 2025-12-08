using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Base
{
    public abstract class BaseEntity<TKey>
    {
        public TKey Id { get; set; } = default!;
    }
    public abstract class GuidBaseEntity : BaseEntity<Guid>
    {
        public GuidBaseEntity()
        {
            Id = Guid.NewGuid();
        }
    }


    public abstract class BaseEntity
    {
        public int Id { get; set; }
    }

}
