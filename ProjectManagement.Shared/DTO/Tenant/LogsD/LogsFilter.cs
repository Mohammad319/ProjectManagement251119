using System;

namespace ProjectManagement.Shared.DTO.Tenant.Logs
{
    public class LogsFilter
    {
        public DateTime? Date1 { get; set; }
        public DateTime? Date2 { get; set; }
        public int? TenantID { get; set; }
        public string Level { get; set; } = string.Empty;

        public int Count { get; set; } = 50;
        public int PageNr { get; set; } = 1;
        //public bool HasNextPage { get; set; }
    }
}
