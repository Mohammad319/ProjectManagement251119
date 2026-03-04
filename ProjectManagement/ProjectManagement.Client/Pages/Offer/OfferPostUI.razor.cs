using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.DTO.Offer;

namespace ProjectManagement.Client.Pages.Offer
{
    public partial class OfferPostUI
    {
        [Parameter] public ListOfferMVVM Offer { get; set; } = new ListOfferMVVM();
        [Parameter] public ResourceListMVVM Resource { get; set; } = new();
        PostOfferDTO PostOffer { get; set; } = new();
        bool IsLoading = false;
        List<ListDTO> Organisations = [];
        List<UnderContactOrganisationBase> Contacts = [];
        void OnChangeCompany(ChangeEventArgs e)
        {
            PostOffer.Contact = null;
            Contacts = [];
            if (e == null || string.IsNullOrEmpty(e.Value.ToString())) return;
            PostOffer.OrganisationId = int.Parse(e.Value.ToString());
            var org = Organisations.FirstOrDefault(x => x.Id == PostOffer.OrganisationId);
        }
        protected async override Task OnInitializedAsync()
        {
            PropertyCopier.CopyPropertiesTo(Offer, PostOffer);
            PostOffer.ResourceId = Resource.Id;
            Organisations = await Repo.Org.GetVisibleOrIdAsync(Offer.OrganisationId.HasValue ? Offer.OrganisationId.Value : 0);
        }
        void Change(ListOfferMVVM offer)
        {
            offer.Cost = PostOffer.Cost;
            offer.BaseCost = PostOffer.BaseCost;
            offer.Comment = PostOffer.Comment;
            offer.OrganisationId = PostOffer.OrganisationId;
            offer.Contact = PostOffer.Contact;
            offer.Organisation = PostOffer.OrganisationId.HasValue ? Organisations.FirstOrDefault(x => x.Id == PostOffer.OrganisationId)?.Name ?? string.Empty : string.Empty;
        }
        private async Task HandleSubmitAsync()
        {
            IsLoading = true;
            if (Offer.Id == 0)
            {
                ListOfferMVVM offer = new();
                Change(offer);
                offer.Id = await Repo.Offer.AddAsync(PostOffer);
                offer.Date = DateTime.Now;
                MHD.Notifications(ToastType.Add, offer.Id > 0);
                Modal.Close();
            }
            else
            {
                Change(Offer);
                bool res = await Repo.Offer.UpdateAsync(Offer.Id, PostOffer);
                MHD.Notifications(ToastType.Update, res);
                Modal.Close();
            }
        }
    }
}
