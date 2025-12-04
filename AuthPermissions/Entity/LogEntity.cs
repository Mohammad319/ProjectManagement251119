using ProjectManagement.Shared.Base.AppTenant;
using System.ComponentModel.DataAnnotations;

namespace AuthPermissions.Entity
{
    public class LogEntity : LogBase
    {
        public int Id { get; set; }
    }
}
