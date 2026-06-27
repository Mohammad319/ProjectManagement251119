using System;
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
        public const string ProjectTypeDepartmentOnly = "Avdelningsåtkomst";
        /// <summary>Kalkyl: synlig via normal avdelningsåtkomst, inte via extra projektdelning.</summary>
        public const string CalcTypeDepartmentAccess = "Avdelningsåtkomst";
        /// <summary>Kalkyl: ingår i projektets delning.</summary>
        public const string CalcTypeViaProject = "Via projekt";
        /// <summary>Kalkyl: projektet är delat, men kalkylen ingår inte i projektdelningen.</summary>
        public const string CalcTypeNotViaProject = "Ej via projekt";
        /// <summary>Bakåtkompatibelt alias för äldre kod/tester.</summary>
        public const string CalcTypeProjectOnly = CalcTypeDepartmentAccess;
        /// <summary>Projektet är delat med personer eller avdelningar utöver den normala åtkomsten.</summary>
        public const string TypeShared = "Delat";
        /// <summary>Projektet är delat, men bara vissa kalkyler ingår i delningen.</summary>
        public const string ProjectTypeLimited = "Delvis delat";
        /// <summary>Äldre kalkyl/filter-begrepp. Projektlistan använder <see cref="ProjectTypeLimited"/>.</summary>
        public const string TypeLimited = "Delvis delad";
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

        // ── Tooltip ──────────────────────────────────────────────────────
        /// <summary>Max antal mottagarrader som visas per grupp i tooltipen innan resten sammanfattas.</summary>
        public const int TooltipMaxRecipientRows = 5;

        /// <summary>
        /// Lägger till en rad per mottagare (formaterad via <paramref name="format"/>) i
        /// <paramref name="lines"/>, men aldrig fler än <see cref="TooltipMaxRecipientRows"/>.
        /// Återstoden sammanfattas på en egen rad som "+ X fler" så att långa delningslistor
        /// inte gör tooltipen orimligt lång.
        /// </summary>
        public static void AppendRecipientLines(
            List<string> lines,
            IReadOnlyList<ProjectAccessRecipientDTO> recipients,
            Func<ProjectAccessRecipientDTO, string> format)
        {
            int shown = Math.Min(recipients.Count, TooltipMaxRecipientRows);
            for (int i = 0; i < shown; i++)
                lines.Add(format(recipients[i]));

            int rest = recipients.Count - shown;
            if (rest > 0)
                lines.Add($"+ {rest} fler");
        }

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
        /// Kompakt sammanfattning utan detaljer: "Avdelningsåtkomst", "Delat" eller "Delvis delat".
        /// </summary>
        public static string ProjectSummaryText(ProjectAccessSummaryDTO? access, bool fallbackIsShared)
        {
            if (access is null)
                return fallbackIsShared ? TypeShared : "Avdelningsåtkomst";

            var recips = access.Recipients;
            if (recips.Count == 0)
                return access.ViaDepartment ? "Avdelningsåtkomst" : "—";

            if (IsLimited(access.ShareableCalcCount, recips))
                return ProjectTypeLimited;

            return TypeShared;
        }

        /// <summary>Filtervärden raden matchar i projektlistan: endast huvudstatus.</summary>
        public static IEnumerable<string> ProjectTags(ProjectAccessSummaryDTO? access, bool fallbackIsShared)
        {
            bool shared = ProjectIsShared(access, fallbackIsShared);
            if (!shared)
            {
                yield return ProjectTypeDepartmentOnly;
                yield break;
            }

            if (access is null)
            {
                yield return TypeShared;
                yield break;
            }

            if (shared && IsLimited(access.ShareableCalcCount, access.Recipients))
                yield return ProjectTypeLimited;
            else
                yield return TypeShared;
        }

        // ════════════════════════════ Kalkyllistan ════════════════════════════

        /// <summary>True när kalkylen ingår i extra projektdelning (utöver normal projektåtkomst).</summary>
        public static bool CalcIsShared(CalculationAccessSummaryDTO? access) =>
            access is not null && access.Recipients.Count > 0;

        /// <summary>
        /// Kompakt sammanfattning: "Privat", "Via projekt", "Ej via projekt" eller "Avdelningsåtkomst".
        /// </summary>
        public static string CalcSummaryText(CalculationAccessSummaryDTO? access, bool isPrivate, bool projectHasSharing = false)
        {
            if (isPrivate)
                return CalcTypePrivate;
            if (access is null)
                return projectHasSharing ? CalcTypeNotViaProject : CalcTypeDepartmentAccess;

            var recips = access.Recipients;
            if (recips.Count == 0)
                return projectHasSharing ? CalcTypeNotViaProject : CalcTypeDepartmentAccess;

            return CalcTypeViaProject;
        }

        /// <summary>Filtervärden raden matchar: kalkyllistans fyra åtkomststatusar.</summary>
        public static IEnumerable<string> CalcTags(CalculationAccessSummaryDTO? access, bool isPrivate, bool projectHasSharing = false)
        {
            if (isPrivate)
            {
                yield return CalcTypePrivate;
                yield break;
            }

            bool shared = CalcIsShared(access);
            if (shared)
                yield return CalcTypeViaProject;
            else if (projectHasSharing)
                yield return CalcTypeNotViaProject;
            else
                yield return CalcTypeDepartmentAccess;
        }
    }
}
