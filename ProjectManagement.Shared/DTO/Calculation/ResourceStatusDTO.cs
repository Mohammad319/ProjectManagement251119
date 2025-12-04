using ProjectManagement.Shared.Base.Calculation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class PostResourceStatusDTO : StatusResourceBase
    {
        public bool IsVisible { get; set; } = true;
    }

}
