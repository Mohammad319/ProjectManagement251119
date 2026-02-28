using ProjectManagement.Shared.Base.AppTenant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.DTO.App.Dataloader
{
        public class TaskResourceListDTO
        {
            public int Id { get; set; }
            public int TaskId { get; set; }
            public int ResourceId { get; set; }
            public double ChangeFactor1 { get; set; } = 1;
            public double ChangeFactor2 { get; set; } = 1;
            public double CapWaste { get; set; } = 1;
            public double? BaseCost { get; set; } = 0;
            public bool Uncontrollable { get; set; } = false;
            public List<RoleDTO> CapRole { get; set; } = [];
        public List<string> Equations { get; set; } = [];

        public List<int> ConditionsIDs { get; set; } = [];
        }
    
}
