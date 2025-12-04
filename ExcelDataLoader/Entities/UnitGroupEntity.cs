using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectImportHub.Entities
{
    public class UnitGroupEntity
    {
        public int Id { get; set; }
        public string? DisplayName { get; set; }
        public List<string> Keys { get; set; } = [];
        public List<ProjectTaskEntity> Tasks { get; set; } = [];
    }
}
