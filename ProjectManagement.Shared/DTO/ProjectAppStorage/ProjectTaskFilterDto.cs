using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.DTO.ProjectAppStorage
{
    public class ProjectTaskFilterDto
    {
        public string NameOrCode { get; set; } = string.Empty;

        /// <summary>
        /// Top-N normalized tokens (from TF-IDF) sent by the client.
        /// The server uses OR-Contains on Name, Code, and NormalizedTextSv.
        /// When populated, takes precedence over NameOrCode.
        /// </summary>
        public List<string> SearchTokens { get; set; } = [];

        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
    }
}
