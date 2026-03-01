using ProjectManagement.Shared.Base.Calculation;
using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.Calculation
{

    public class TenderAttributeListPostDTO : AttributeNameTenderBase
    {
        public Dictionary<int, double> TendersValues { get; set; } = new();

    }
    public class TenderAttributeListDTO : AttributeNameTenderBase
    {
        public int Id { get; set; }

    }
    public class TenderAttributePostDTO
    {
        public string Note { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
    public class TenderPostDTO
    {
        public Dictionary<int, double> AttributesValue { get; set; } = new();
        public string Note { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }


    public class TenderAttributeValuesListDTO
    {
        public List<TenderAttributeListDTO> Attributes { get; set; } = [];
        public List<TenderListDTO> Tenders { get; set; } = [];
    }
    public class TenderListDTO : TenderBase
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Company { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;

        public List<ValuesList> Values { get; set; } = [];
    }
    public class ValuesList
    {
        //public int TenderID { get; set; }
        public int AttributeID { get; set; }
        public double Values { get; set; }

    }
    public class TenderDetailsDTO : TenderBase
    {
        public int Id { get; set; }
        public string Company { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
    }
}
