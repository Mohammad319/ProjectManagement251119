using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.Base.AppTenant
{
    public class LogBase
    {
        public string Message { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public string Exception { get; set; } = string.Empty;
        public string Properties { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        public int? TenantID { get; set; }
    }
}
