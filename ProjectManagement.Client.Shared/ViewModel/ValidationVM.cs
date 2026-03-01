using ProjectManagement.Shared.Base.Application;
using System;
using System.Collections.Generic;

namespace ProjectManagement.Client.Shared.ViewModel
{
    public class ValidNumberVM
    {
        public int? Min { get; set; }
        public int? Max { get; set; } = 1000;
        public int? Default { get; set; }
    }
    public class ValidTextVM
    {
        public int? Min { get; set; }
        public int? Max { get; set; } = 500;
        public string Default { get; set; } = string.Empty;
    }
    public class ValidDateVM
    {
        public static string GetHTMLFormat(AttributeType type)
        {
            if (type == AttributeType.DateTime) return "datetime-local";
            else if (type == AttributeType.Date) return "date";
            else return "time";
        }
        public DateTime? Min { get; set; }
        public DateTime? Max { get; set; }
        public DateTime? Default { get; set; }
    }
    public class ValidBoolVM
    {
        public bool? Default { get; set; }
    }
    public class ValidSelectVM
    {
        public int Min { get; set; } = 0;
        public int Max { get; set; } = 1;
        public int DefaultIndex { get; set; } = 0;
        public List<string> Values { get; set; } = [];
    }
}
