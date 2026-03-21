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

        private PostOrganisationCategoryDTO PostOffer { get; set; } = new();
        private bool IsLoading { get; set; }
        private int? LastCategoryId;
        private ListOrganisationCategoryDTO? LastCategoryReference;

        protected override void OnParametersSet()
        {
            var category = OrganisationCategory ?? new();
            var currentId = category.Id;
            var sameReference = ReferenceEquals(LastCategoryReference, category);
            if (sameReference && LastCategoryId == currentId)
                return;

            PostOffer = new PostOrganisationCategoryDTO
            {
                Name = category.Name,
                CategoryId = category.ParentCategoryId
            };

            LastCategoryId = currentId;
            LastCategoryReference = category;
        }

        private async Task Cancel()
        {
            if (Callback.HasDelegate)
                await Callback.InvokeAsync(false);
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
                    result = await Dispatcher.Send(
                        new CreateOrganisationCategoryCommand(PostOffer)) > 0;
                }
                else
                {
                    PutOrganisationCategoryDTO entity = new();
                    PostOffer.CopyPropertiesTo(entity);

                    result = await Dispatcher.Send(
                        new UpdateOrganisationCategoryCommand(entity));
                }

                MHD.Notifications(
                    OrganisationCategory.Id == 0 ? ToastType.Add : ToastType.Update,
                    result);

                if (Callback.HasDelegate)
                    await Callback.InvokeAsync(result);
            }
            finally
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
