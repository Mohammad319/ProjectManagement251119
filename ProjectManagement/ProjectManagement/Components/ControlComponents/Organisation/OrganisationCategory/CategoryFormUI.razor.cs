using Application.Feature.Organisation.OrganisationCategory.Commands;
using Domain.Entities.Organisation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.DTO.Organisation;

namespace ProjectManagement.Components.ControlComponents.Organisation.OrganisationCategory
{
    public partial class CategoryFormUI
    {
        [Parameter] public OrganisationCategoryEntity Offer { get; set; } = new();
        OrganisationCategoryEntity PostOffer { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }
        bool IsLoading = false;

        protected override void OnInitialized()
        {
            Offer.CopyPropertiesTo(PostOffer);
        }
        private async Task HandleSubmitAsync()
        {
            IsLoading = true;
            bool result = false;
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
    }
}
