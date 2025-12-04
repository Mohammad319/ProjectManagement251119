using ProjectManagement.Shared.DTO.Offer;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Shared.MVVM.Offer
{
    public class ListOfferMVVM : ListOfferDTO
    {
        [JsonIgnore]
        public bool ShowComment { get; set; }
    }

    public class ListOfferCalcInfoMVVM: ListOfferCalcInfo
    {

    }
}
