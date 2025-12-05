using TaskResourceBlueprints.Entities.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskResourceBlueprints.Entities
{
    public class TaskUnitGroup
    {

        public int Id { get; set; }

        public string? DisplayName { get; set; }
        public List<string> Keys { get; set; } = [];
        public List<TaskDefinition> Tasks { get; set; } = [];
    }
}
