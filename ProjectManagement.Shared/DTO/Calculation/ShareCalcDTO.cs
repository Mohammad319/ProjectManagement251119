namespace ProjectManagement.Shared.DTO.Calculation
{
    public class PostShareCalcDTO
    {
        public int DepartmentId { get; set; }
        public int? UserId { get; set; }
        public int CalculationId { get; set; }
        public bool Tap1 { get; set; }
        public bool Tap2 { get; set; }
        public bool Tap3 { get; set; }
        public bool Tap4 { get; set; }
        public bool Tap5 { get; set; }
        public bool Tap6 { get; set; }
    }
    public class UpdateShareCalcDTO
    {
        public int Id { get; set; }
        public bool Tap1 { get; set; }
        public bool Tap2 { get; set; }
        public bool Tap3 { get; set; }
        public bool Tap4 { get; set; }
        public bool Tap5 { get; set; }
        public bool Tap6 { get; set; }
    }
    public class ListShareCalcDTO
    {
        public int Id { get; set; }
        public string Department { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public int? UserId { get; set; }
        public bool Tap1 { get; set; }
        public bool Tap2 { get; set; }
        public bool Tap3 { get; set; }
        public bool Tap4 { get; set; }
        public bool Tap5 { get; set; }
        public bool Tap6 { get; set; }
    }
}
