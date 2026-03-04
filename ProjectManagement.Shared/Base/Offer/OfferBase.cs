using System;

namespace ProjectManagement.Shared.Base.Offer
{
    public class OfferBase
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string Comment { get; set; } = string.Empty;
    }
}
