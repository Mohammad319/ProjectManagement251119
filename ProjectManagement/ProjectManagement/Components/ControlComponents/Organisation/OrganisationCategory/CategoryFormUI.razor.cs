using Application.Feature.Organisation.OrganisationCategory.Commands;
using DocumentFormat.OpenXml.Vml.Office;
using Domain.DTO.Category;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Organisation;

namespace ProjectManagement.Components.ControlComponents.Organisation.OrganisationCategory
{
    public partial class CategoryFormUI
    {
        [Parameter] public ListOrganisationCategoryDTO OrganisationCategory { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }

        private PostOrganisationCategoryDTO PostOffer { get; set; } = new();
        private bool IsLoading { get; set; }

        protected override void OnInitialized()
        {
            OrganisationCategory.CopyPropertiesTo(PostOffer);
            PostOffer.CategoryId = PostOffer.CategoryId;

        }

        private async Task HandleSubmitAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                bool result;

                if (OrganisationCategory.Id == 0)
                {
                    result = await MicroBus.Send(new CreateOrganisationCategoryCommand(PostOffer)) > 0;
                }
                else
                {
                    PutOrganisationCategoryDTO entity = new();
                    PostOffer.CopyPropertiesTo(entity);
                    result = await MicroBus.Send(new UpdateOrganisationCategoryCommand(entity));
                }

                MHD.Notifications(OrganisationCategory.Id == 0 ? ToastType.Add : ToastType.Update, result);
                await Callback.InvokeAsync(result);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
