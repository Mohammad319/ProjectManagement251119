using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.DTO.ProjectAppStorage
{
    public class ProjectTaskFilterDto
    {
        public string NameOrCode { get; set; } = string.Empty;
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
    }
}
