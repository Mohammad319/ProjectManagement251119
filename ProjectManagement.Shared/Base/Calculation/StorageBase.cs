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
        public CalculationItemType StorageType { get; set; }
        public StorageSort StorageSort { get; set; }
        public AuthorityStorage StorageLevel { get; set; }
        public int UserId { get; set; }
        public int DepartmentId { get; set; }
    }
}