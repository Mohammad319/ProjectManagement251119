using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.DTO.Offer;

namespace ProjectManagement.Client.Pages.Calculation.Table
{
    public static class CalculationToolbarTextHelper
    {
        public static string BuildOfferBadgeText(ListOfferDTO? offer)
        {
            if (offer == null)
                return string.Empty;

            var path = BuildCategoryPath(offer);
            var parts = new List<string> { ResourceLoc.offer };

            if (!string.IsNullOrWhiteSpace(path))
                parts.Add($": {ResourceLoc.category} {path}");

            if (!string.IsNullOrWhiteSpace(offer.Organisation))
                parts.Add($"{ResourceApp.organisation} {offer.Organisation}");

            return string.Join(' ', parts).Trim();
        }

        private static string BuildCategoryPath(ListOfferDTO offer)
        {
            var parts = new[] { offer.Category, offer.SubCategory }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            return parts.Length == 0 ? string.Empty : string.Join(" > ", parts);
        }
    }
}
