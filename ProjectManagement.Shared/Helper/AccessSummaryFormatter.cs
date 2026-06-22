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
    /// <para>
    /// Kolumnen visar en KORT sammanfattning (inga långa namnlistor); detaljer visas i
    /// delningsdialogen vid klick. Filtret är grupperat: Åtkomsttyp, Åtkomstnivå, Personer,
    /// Avdelningar. <see cref="ProjectTags"/>/<see cref="CalcTags"/> returnerar exakt de
    /// filtervärden en rad matchar – de måste därför vara identiska med filteralternativens värden.
    /// </para>
    /// </summary>
    public static class AccessSummaryFormatter
    {
        // ── Filtergrupp: Åtkomsttyp ──────────────────────────────────────
        /// <summary>Projekt: ingen extra delning, endast tillgängligt via projektets avdelning.</summary>
        public const string ProjectTypeDepartmentOnly = "Endast avdelningsåtkomst";
        /// <summary>Kalkyl: ingen extra delning, endast tillgänglig via projektet.</summary>
        public const string CalcTypeProjectOnly = "Endast projektåtkomst";
        /// <summary>Projektet/kalkylen har extra delning till personer eller avdelningar.</summary>
        public const string TypeShared = "Extra delad";
        /// <summary>Delat men mottagaren har bara tillgång till vissa kalkyler.</summary>
        public const string TypeLimited = "Begränsad kalkylåtkomst";
        /// <summary>Kalkyl: privat (syns bara för ägare/Admin).</summary>
        public const string CalcTypePrivate = "Privat";

        // ── Filtergrupp: Åtkomstnivå ─────────────────────────────────────
        public const string LevelEdit = "Kan ändra";
        public const string LevelView = "Kan visa";

        // ── Filtergruppsrubriker ─────────────────────────────────────────
        public const string GroupAccessType = "Åtkomsttyp";
        public const string GroupAccessLevel = "Åtkomstnivå";
        public const string GroupPersons = "Personer";
        public const string GroupDepartments = "Avdelningar";

        /// <summary>Systemroll-etikett (för andra sammanhang än åtkomstnivå).</summary>
        public static string RoleLabel(string? role) => role switch
        {
            PMRolesConst.Tenant.Viewer => "Visare",
            PMRolesConst.Tenant.Manger => "Användare",
            PMRolesConst.Tenant.Admin  => "Admin",
            _ => string.IsNullOrWhiteSpace(role) ? "—" : role!
        };

        /// <summary>Åtkomstnivå i delningens språk: "Kan ändra" / "Kan visa". Samma som delningsdialogen.</summary>
        public static string AccessLevelLabel(string? role) => role switch
        {
            PMRolesConst.Tenant.Manger => LevelEdit,
            PMRolesConst.Tenant.Admin  => LevelEdit,
            _ => LevelView
        };

        /// <summary>Mottagarens filtervärde: "Person: Namn" eller "Avdelning: Namn".</summary>
        public static string RecipientFilterValue(ProjectAccessRecipientDTO r) =>
            r.Type == ProjectShareRecipientType.Department
                ? $"Avdelning: {r.Name}"
                : $"Person: {r.Name}";

        // Kort sammanfattning av mottagare: "1 person", "2 personer · 1 avdelning", "Produktion".
        private static string RecipientCountText(IReadOnlyCollection<ProjectAccessRecipientDTO> recips)
        {
            int persons = recips.Count(r => r.Type == ProjectShareRecipientType.User);
            var depts = recips.Where(r => r.Type == ProjectShareRecipientType.Department).ToList();

            var parts = new List<string>();
            if (persons > 0)
                parts.Add(persons == 1 ? "1 person" : $"{persons} personer");

            if (depts.Count == 1 && persons == 0)
                parts.Add(depts[0].Name);                       // ensam avdelning ⇒ visa namnet
            else if (depts.Count >= 1)
                parts.Add(depts.Count == 1 ? "1 avdelning" : $"{depts.Count} avdelningar");

            return parts.Count > 0 ? string.Join(" · ", parts) : "delad";
        }

        private static bool IsLimited(int shareableCalcCount, IEnumerable<ProjectAccessRecipientDTO> recips) =>
            shareableCalcCount > 0 && recips.Any(r => r.CalcCount < shareableCalcCount);

        // ════════════════════════════ Projektlistan ════════════════════════════

        /// <summary>True när projektet har extra delning (utöver normal avdelningsåtkomst).</summary>
        public static bool ProjectIsShared(ProjectAccessSummaryDTO? access, bool fallbackIsShared) =>
            access is not null ? access.Recipients.Count > 0 : fallbackIsShared;

        /// <summary>
        /// Kompakt sammanfattning utan långa namn: "Avdelningsåtkomst", "Delad · 2 personer",
        /// "Delad · Produktion", "Delad · 2 personer · 1 avdelning" eller "Begränsad · 3/5 kalkyler".
        /// </summary>
        public static string ProjectSummaryText(ProjectAccessSummaryDTO? access, bool fallbackIsShared)
        {
            if (access is null)
                return fallbackIsShared ? "Delad" : "Avdelningsåtkomst";

            var recips = access.Recipients;
            if (recips.Count == 0)
                return access.ViaDepartment ? "Avdelningsåtkomst" : "—";

            if (IsLimited(access.ShareableCalcCount, recips))
            {
                // Show the LEAST-covered recipient. When any recipient is limited this is always
                // below the total, so the label never reads as a contradictory "n/n kalkyler".
                int min = recips.Min(r => r.CalcCount);
                return $"Begränsad · {min}/{access.ShareableCalcCount} kalkyler";
            }

            return $"Delad · {RecipientCountText(recips)}";
        }

        /// <summary>Filtervärden raden matchar: åtkomsttyp, åtkomstnivå och mottagare.</summary>
        public static IEnumerable<string> ProjectTags(ProjectAccessSummaryDTO? access, bool fallbackIsShared)
        {
            bool shared = ProjectIsShared(access, fallbackIsShared);
            yield return shared ? TypeShared : ProjectTypeDepartmentOnly;

            if (access is null)
                yield break;

            if (shared && IsLimited(access.ShareableCalcCount, access.Recipients))
                yield return TypeLimited;
            if (access.Recipients.Any(r => r.Role == PMRolesConst.Tenant.Manger)) yield return LevelEdit;
            if (access.Recipients.Any(r => r.Role == PMRolesConst.Tenant.Viewer)) yield return LevelView;
            foreach (var r in access.Recipients)
                yield return RecipientFilterValue(r);
        }

        // ════════════════════════════ Kalkyllistan ════════════════════════════

        /// <summary>True när kalkylen ingår i extra projektdelning (utöver normal projektåtkomst).</summary>
        public static bool CalcIsShared(CalculationAccessSummaryDTO? access) =>
            access is not null && access.Recipients.Count > 0;

        /// <summary>
        /// Kompakt sammanfattning: "Privat", "Via projekt", "Delad · 2 personer", "Delad · Produktion".
        /// </summary>
        public static string CalcSummaryText(CalculationAccessSummaryDTO? access, bool isPrivate)
        {
            if (isPrivate)
                return "Privat";
            if (access is null)
                return "Via projekt";

            var recips = access.Recipients;
            if (recips.Count == 0)
                return access.ViaProject ? "Via projekt" : "Delad";

            return $"Delad · {RecipientCountText(recips)}";
        }

        /// <summary>Filtervärden raden matchar: åtkomsttyp, åtkomstnivå och mottagare.</summary>
        public static IEnumerable<string> CalcTags(CalculationAccessSummaryDTO? access, bool isPrivate)
        {
            if (isPrivate)
            {
                yield return CalcTypePrivate;
                yield break;
            }

            bool shared = CalcIsShared(access);
            yield return shared ? TypeShared : CalcTypeProjectOnly;
            if (access is null)
                yield break;

            // Projektdelning väljer alltid specifika kalkyler ⇒ en delad kalkyl är begränsad kalkylåtkomst.
            if (shared) yield return TypeLimited;
            if (access.Recipients.Any(r => r.Role == PMRolesConst.Tenant.Manger)) yield return LevelEdit;
            if (access.Recipients.Any(r => r.Role == PMRolesConst.Tenant.Viewer)) yield return LevelView;
            foreach (var r in access.Recipients)
                yield return RecipientFilterValue(r);
        }
    }
}
