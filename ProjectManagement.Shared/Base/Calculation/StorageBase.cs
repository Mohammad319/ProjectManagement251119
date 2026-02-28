using ProjectManagement.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.Base.Calculation
{
    public class StorageBase
    {
        public string StorageValue { get; set; } = string.Empty;
        public CalculationItemType StorageType { get; set; } = default!;
        public StorageSort StorageSort { get; set; } = default!;
        public AuthorityStorage StorageLevel { get; set; } = default!;
        public int UserId { get; set; }
        public int DepartmentId { get; set; }
    }
}
