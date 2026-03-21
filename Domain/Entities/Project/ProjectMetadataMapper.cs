using Domain.Helper;
using ProjectManagement.Shared.DTO.Project;
using DomainMetadataCloneHelper = Domain.Helper.MetadataCloneHelper;

namespace Domain.Entities.Project
{
    internal static class ProjectMetadataMapper
    {
        public static ProjectData Build(PostProjectDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return Build(dto.Data);
        }

        public static ProjectData Build(ProjectData? metadata)
        {
            metadata ??= new ProjectData();

            return new ProjectData
            {
                Procurement = metadata.Procurement,
                ProjectManager = Normalize(metadata.ProjectManager),
                Notes = DomainMetadataCloneHelper.CloneStrings(metadata.Notes),
                ClientsContactPersonTender = Normalize(metadata.ClientsContactPersonTender),
                Address = DomainMetadataCloneHelper.CloneAddresses(metadata.Address),
                ClientsManager = Normalize(metadata.ClientsManager),
                Contacts = DomainMetadataCloneHelper.CloneContacts(metadata.Contacts),
                Designer = Normalize(metadata.Designer),
                Developer = Normalize(metadata.Developer),
                Inspector = Normalize(metadata.Inspector),
                OverviewInfoProject = Normalize(metadata.OverviewInfoProject),
                Responsibles = DomainMetadataCloneHelper.CloneStrings(metadata.Responsibles),
                Supervisor = Normalize(metadata.Supervisor)
            };
        }

        private static string Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
