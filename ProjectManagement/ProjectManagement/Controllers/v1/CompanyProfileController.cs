using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Services.UI;

namespace ProjectManagement.Server.Controllers.v1
{
    /// <summary>
    /// Företagsuppgifter för rapporter/PDF-exporter (namn, org.nr, kontakt, logotyp som data-URL
    /// och rapporttexter). Läses av reportExport.js/calcGridBehavior.js när en rapport skrivs ut
    /// eller exporteras så att företagets identitet stämplas in i dokumentet. Tenant-skopad via
    /// det globala query-filtret; read-only.
    /// </summary>
    [ApiVersion("1.0")]
    [Authorize]
    public class CompanyProfileController(ICompanyProfileService profileService) : BaseApiController
    {
        [HttpGet("report")]
        public async Task<IActionResult> GetReportInfo(CancellationToken ct)
            => Ok(await profileService.GetReportInfoAsync(ct));
    }
}
