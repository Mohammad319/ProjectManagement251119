using System.Collections.Generic;
using System.Linq;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Shared.Helper
{
    /// <summary>
    /// Ren (UI-fri) formatering och klassificering för "Åtkomst"-kolumnen i både projekt- och
    /// kalkyllistan. Bryts ut hit så att samma regler kan återanvändas och enhetstestas utan att
    /// instansiera Blazor-komponenter. Strängarna är medvetet svenska (UI-text).
    /// </summary>
    public static class AccessSummaryFormatter
    {
        // ── Filterkategorier: projektlistan ──────────────────────────────
        public const string ProjectTagViaDepartment = "Via avdelning";
        public const string ProjectTagShared        = "Delad";
        public const string ProjectTagLimited        = "Begränsad kalkylåtkomst";
        public const string ProjectTagNotShared      = "Ej extra delad";

        // ── Filterkategorier: kalkyllistan ───────────────────────────────
        public const string CalcTagViaProject = "Via projekt";
        public const string CalcTagShared     = "Delad";
        public const string CalcTagPrivate    = "Privat";
        public const string CalcTagLimited    = "Begränsad";

        // ── Gemensamma behörighetskategorier ─────────────────────────────
        public const string PermManagerTag = "Behörighet: Användare";
        public const string PermViewerTag  = "Behörighet: Visare";

        public static string RoleLabel(string? role) => role switch
        {
            PMRolesConst.Tenant.Viewer => "Visare",
            PMRolesConst.Tenant.Manger => "Användare",
            PMRolesConst.Tenant.Admin  => "Admin",
            _ => string.IsNullOrWhiteSpace(role) ? "—" : role!
        };

        /// <summary>Etikett för en mottagare i filterlistan: "Användare: Namn" / "Visare: Namn" / "Avdelning: Namn".</summary>
        public static string RecipientFilterTag(ProjectAccessRecipientDTO r) =>
            r.Type == ProjectShareRecipientType.Department
                ? $"Avdelning: {r.Name}"
                : $"{RoleLabel(r.Role)}: {r.Name}";

        // ════════════════════════════ Projektlistan ════════════════════════════

        /// <summary>True när projektet har extra delning (utöver normal avdelningsåtkomst).</summary>
        public static bool ProjectIsShared(ProjectAccessSummaryDTO? access, bool fallbackIsShared) =>
            access is not null ? access.Recipients.Count > 0 : fallbackIsShared;

        /// <summary>
        /// Kompakt sammanfattning: "Via avdelning", "Delad", "Delad · 3/5 kalkyler",
        /// "Användare 2 · Visare 1" eller "Visare: Namn" (vid få mottagare).
        /// </summary>
        public static string ProjectSummaryText(ProjectAccessSummaryDTO? access, bool fallbackIsShared)
        {
            if (access is null)
                return fallbackIsShared ? ProjectTagShared : ProjectTagViaDepartment;

            var recips = access.Recipients;
            if (recips.Count == 0)
                return access.ViaDepartment ? ProjectTagViaDepartment : "—";

            string calcSuffix = string.Empty;
            if (access.ShareableCalcCount > 0 && recips.Any(r => r.CalcCount < access.ShareableCalcCount))
                calcSuffix = $" · {recips.Max(r => r.CalcCount)}/{access.ShareableCalcCount} kalkyler";

            if (recips.Count == 1)
                return $"{RoleLabel(recips[0].Role)}: {recips[0].Name}{calcSuffix}";
            if (recips.Count == 2)
                return $"{recips[0].Name} · {recips[1].Name}{calcSuffix}";

            int mgr = recips.Count(r => r.Role == PMRolesConst.Tenant.Manger);
            int vis = recips.Count(r => r.Role == PMRolesConst.Tenant.Viewer);
            var parts = new List<string>();
            if (mgr > 0) parts.Add($"Användare {mgr}");
            if (vis > 0) parts.Add($"Visare {vis}");
            return (parts.Count > 0 ? string.Join(" · ", parts) : ProjectTagShared) + calcSuffix;
        }

        /// <summary>Kategorimärken för filtrering: åtkomsttyp, behörighet och mottagare.</summary>
        public static IEnumerable<string> ProjectTags(ProjectAccessSummaryDTO? access, bool fallbackIsShared)
        {
            bool shared = ProjectIsShared(access, fallbackIsShared);
            yield return shared ? ProjectTagShared : ProjectTagNotShared;

            if (access is null)
            {
                if (!fallbackIsShared) yield return ProjectTagViaDepartment;
                yield break;
            }

            if (access.ViaDepartment) yield return ProjectTagViaDepartment;
            if (access.ShareableCalcCount > 0 && access.Recipients.Any(r => r.CalcCount < access.ShareableCalcCount))
                yield return ProjectTagLimited;
            if (access.Recipients.Any(r => r.Role == PMRolesConst.Tenant.Manger)) yield return PermManagerTag;
            if (access.Recipients.Any(r => r.Role == PMRolesConst.Tenant.Viewer)) yield return PermViewerTag;
            foreach (var r in access.Recipients)
                yield return RecipientFilterTag(r);
        }

        // ════════════════════════════ Kalkyllistan ════════════════════════════

        /// <summary>True när kalkylen ingår i extra projektdelning (utöver normal projektåtkomst).</summary>
        public static bool CalcIsShared(CalculationAccessSummaryDTO? access) =>
            access is not null && access.Recipients.Count > 0;

        /// <summary>
        /// Kompakt sammanfattning: "Via projekt", "Delad", "Delad · Visare 2",
        /// "Delad · Användare 1 · Visare 2" eller "Endast ägare/Admin" (privat).
        /// </summary>
        public static string CalcSummaryText(CalculationAccessSummaryDTO? access, bool isPrivate)
        {
            if (isPrivate)
                return "Endast ägare/Admin";
            if (access is null)
                return CalcTagViaProject;

            var recips = access.Recipients;
            if (recips.Count == 0)
                return access.ViaProject ? CalcTagViaProject : CalcTagShared;

            int mgr = recips.Count(r => r.Role == PMRolesConst.Tenant.Manger);
            int vis = recips.Count(r => r.Role == PMRolesConst.Tenant.Viewer);
            var parts = new List<string>();
            if (mgr > 0) parts.Add($"Användare {mgr}");
            if (vis > 0) parts.Add($"Visare {vis}");
            return parts.Count > 0 ? $"{CalcTagShared} · {string.Join(" · ", parts)}" : CalcTagShared;
        }

        /// <summary>Kategorimärken för filtrering: åtkomsttyp, behörighet och mottagare.</summary>
        public static IEnumerable<string> CalcTags(CalculationAccessSummaryDTO? access, bool isPrivate)
        {
            if (isPrivate)
            {
                yield return CalcTagPrivate;
                yield break;
            }

            bool shared = CalcIsShared(access);
            yield return shared ? CalcTagShared : CalcTagViaProject;
            if (access is null)
                yield break;

            if (access.ViaProject) yield return CalcTagViaProject;
            // Projektdelning väljer alltid specifika kalkyler ⇒ en delad kalkyl är begränsad kalkylåtkomst.
            if (shared) yield return CalcTagLimited;
            if (access.Recipients.Any(r => r.Role == PMRolesConst.Tenant.Manger)) yield return PermManagerTag;
            if (access.Recipients.Any(r => r.Role == PMRolesConst.Tenant.Viewer)) yield return PermViewerTag;
            foreach (var r in access.Recipients)
                yield return RecipientFilterTag(r);
        }
    }
}
