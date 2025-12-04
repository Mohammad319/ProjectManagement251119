using ProjectManagement.Shared.Base.Organisation;

namespace ProjectManagement.Shared.DTO.Organisation
{
    public class PostOrganisationCategoryDTO : OrganisationCategoryBase
    {
        public int? CategoryId { get; set; }
    }
    public class PutOrganisationCategoryDTO : OrganisationCategoryBase
    {
        public int Id { get; set; }
    }
}
