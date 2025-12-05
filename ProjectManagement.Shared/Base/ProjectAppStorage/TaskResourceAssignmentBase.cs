using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Project;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.Base.ProjectAppStorage
{
    public class TaskResourceAssignmentBase
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        public double CapWaste { get; set; } = 1;
        public double? BaseCost { get; set; } = 0;
        public bool Uncontrollable { get; set; } = false;
        public List<RoleDTO> CapRole { get; set; } = [];
        public List<string> Formulas { get; set; } = [];
    }
}
