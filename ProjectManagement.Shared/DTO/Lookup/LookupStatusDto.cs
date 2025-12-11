using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectManagement.Shared.DTO.Lookup
{
    public class LookupStatusDto
    {
        public int Id { get; set; }
        public int SortOrder { get; set; }
        public string Name { get; set; }
        public string Color { get; set; } = "#00ff00";
        public bool IsVisible { get; private set; } = true;

    }
}
