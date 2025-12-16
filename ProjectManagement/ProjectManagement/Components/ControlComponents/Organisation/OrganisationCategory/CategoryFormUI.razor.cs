using Application.Feature.Organisation.OrganisationCategory.Commands;
using Domain.DTO.Category;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Organisation;

namespace ProjectManagement.Components.ControlComponents.Organisation.OrganisationCategory
{
    public partial class CategoryFormUI
    {
        [Parameter] public ListOrganisationCategoryDTO OrganisationCategory { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }

        private ListOrganisationCategoryDTO PostOffer { get; set; } = new();
        private bool IsLoading { get; set; }

        protected override void OnParametersSet()
        {
            PostOffer = new ListOrganisationCategoryDTO();
            OrganisationCategory.CopyPropertiesTo(PostOffer);
        }

        private async Task HandleSubmitAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                bool result;

                if (PostOffer.Id == 0)
                {
                    PostOrganisationCategoryDTO entity = new();
                    PostOffer.CopyPropertiesTo(entity);
                    result = await MicroBus.Send(new CreateOrganisationCategoryCommand(entity)) > 0;
                }
                else
                {
                    PutOrganisationCategoryDTO entity = new();
                    PostOffer.CopyPropertiesTo(entity);
                    result = await MicroBus.Send(new UpdateOrganisationCategoryCommand(entity));
                }

                MHD.Notifications(PostOffer.Id == 0 ? ToastType.Add : ToastType.Update, result);
                await Callback.InvokeAsync(result);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
