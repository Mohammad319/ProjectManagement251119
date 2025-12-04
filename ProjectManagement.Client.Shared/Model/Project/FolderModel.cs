using ProjectManagement.Shared.Base.Project;
using System;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Shared.Model.Project
{
    public class FolderModel : FolderBase
    {
        public Guid Id { get; set; }

        [JsonIgnore]
        public bool ShowProjects { get; set; } = false;

        [JsonIgnore]
        public bool Loading { get; set; } = false;
    }
}
