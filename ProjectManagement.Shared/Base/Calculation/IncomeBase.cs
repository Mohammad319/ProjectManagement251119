using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Shared.Base.Calculation
{
    public class IncomeBase
    {
        public int Year { get; set; }
        public string? Description { get; set; }
        public IncomeSubType SubType { get; set; } = IncomeSubType.Grunddel;
        public decimal Q1 { get; set; }
        public decimal Q2 { get; set; }
        public decimal Q3 { get; set; }
        public decimal Q4 { get; set; }
    }
}
