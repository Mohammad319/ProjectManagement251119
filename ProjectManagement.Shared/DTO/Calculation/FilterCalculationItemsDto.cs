using ProjectManagement.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class FilterCalculationItemsDto
    {
        public int Status { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Unit { get; set; }
        public int Page { get; set; }
        public int AccountGroup { get; set; }
        public int Account { get; set; }
        public int ResourceTypeId { get; set; }
        public int ResourceSortId { get; set; }
        public ResourceTypesEnum? ResType { get; set; }
        public Guid? FolderID { get; set; }
        public Guid? ProjectID { get; set; }
        public int CalculationID { get; set; }
    }
}
