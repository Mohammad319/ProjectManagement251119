using Domain.DTO.Category;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Organisation;

namespace ProjectManagement.Components.ControlComponents.Organisation.OrganisationsType
{
    public partial class OrganisationTypeFormUI : ComponentBase
    {
        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public ListOrganisationTypeDTO? CustomerGroup { get; set; }
        [Parameter] public EventCallback<ListOrganisationTypeDTO> OnValidSubmit { get; set; }

        private ListOrganisationTypeDTO EditModel = new();
        private int? LastCustomerGroupId;
        private ListOrganisationTypeDTO? LastCustomerGroupReference;

        protected override void OnParametersSet()
        {
            var currentId = CustomerGroup?.Id;
            var sameReference = ReferenceEquals(LastCustomerGroupReference, CustomerGroup);
            if (sameReference && LastCustomerGroupId == currentId)
                return;

            EditModel = new ListOrganisationTypeDTO();
            if (CustomerGroup is not null)
                PropertyCopier.CopyPropertiesTo(CustomerGroup, EditModel);

            LastCustomerGroupId = currentId;
            LastCustomerGroupReference = CustomerGroup;
        }

        private async Task SubmitAsync()
        {
            if (IsLoading) return;
            await OnValidSubmit.InvokeAsync(EditModel);
        }

        private void Close()
            => MHD.Modal.Close();
    }
}
