using BlazorMHD.UI.Core.DesignSystem;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper;

namespace ProjectManagement.Client.Helper
{
    /// <summary>
    /// Gemensam, säker regelhantering för "Ta bort kalkyl" som delas av
    /// kalkyllistan (<c>ProjectsCalculationList</c>) och vänsterträdet (<c>FoldersTree</c>).
    /// <para>
    /// En kalkyl får bara tas bort om den är olåst, inte godkänd/historik och inte
    /// kopplad till en kontrakts-/produktionskedja. Annars visas en informationsdialog
    /// som förklarar varför. För kalkyler med flera versioner tas endast den valda
    /// versionen bort ("Ta bort version"); annars tas hela kalkylen bort. "Arkivera
    /// kalkyl" finns kvar som säkert alternativ.
    /// </para>
    /// </summary>
    public static class CalculationDeleteHelper
    {
        public const string DeleteCssClass = "text-red-600 dark:text-red-400";

        private const string BlockedTitle = "Kalkylen kan inte tas bort";

        /// <summary>Dynamisk etikett: "Ta bort version" vid flera versioner, annars "Ta bort kalkyl".</summary>
        public static string DeleteLabel(
            IEnumerable<ListCalculationMVVM>? projectCalculations, ListCalculationMVVM calc) =>
            CalculationVersionSelector.HasMultipleVersions(projectCalculations, calc)
                ? "Ta bort version"
                : "Ta bort kalkyl";

        public static async Task RequestDeleteAsync(
            ComponentBase owner,
            IUnitOfWorkRepository repo,
            MhdServices mhd,
            IClientLogger logger,
            ListCalculationMVVM calc,
            IReadOnlyList<ListCalculationMVVM> projectCalculations,
            bool canManage,
            Func<Task> onConfirmedAsync)
        {
            // Behörighet
            if (!canManage)
            {
                mhd.MessageOk(BlockedTitle, "Du saknar behörighet att ta bort kalkyler.", MhdState.Warning);
                return;
            }

            // Låst / godkänd / historik (skickad, vunnen eller förlorad räknas som historik).
            if (calc.IsLocked
                || calc.ApprovedAtUtc.HasValue
                || calc.CountsAsSubmittedBid
                || calc.CountsAsWonBid
                || calc.CountsAsLostBid)
            {
                mhd.MessageOk(BlockedTitle,
                    "Kalkylen kan inte tas bort eftersom den är låst eller används som historik. Arkivera kalkylen i stället.",
                    MhdState.Warning);
                return;
            }

            // Kontrakts-/produktionskedja: kalkylen är antingen själv en kontrakts-/produktions-
            // kalkyl (härledd) eller ursprung för en annan kalkyl i projektet.
            bool isDerived =
                calc.CalculationType is CalculationVersionType.Contract or CalculationVersionType.Production
                || calc.SourceCalculationId.HasValue;
            bool isOrigin = projectCalculations.Any(c => c.Id != calc.Id && c.SourceCalculationId == calc.Id);

            if (isDerived || isOrigin)
            {
                mhd.MessageOk(BlockedTitle,
                    "Kalkylen kan inte tas bort eftersom den är kopplad till en kontrakts- eller produktionskalkyl. Arkivera kalkylen i stället.",
                    MhdState.Warning);
                return;
            }

            // Tillåtet – bekräftelsedialog (versionsmedveten).
            bool isVersionDelete = CalculationVersionSelector.HasMultipleVersions(projectCalculations, calc);
            ShowConfirmation(owner, mhd, calc, isVersionDelete, onConfirmedAsync);
        }

        private static void ShowConfirmation(
            ComponentBase owner, MhdServices mhd, ListCalculationMVVM calc, bool isVersionDelete, Func<Task> onConfirmedAsync)
        {
            var title = isVersionDelete ? "Ta bort version?" : "Ta bort kalkyl?";
            var text = isVersionDelete
                ? $"Kalkylen har flera versioner. Endast version V.{calc.VersionNumber:D2} tas bort. Övriga versioner sparas. Vill du fortsätta?"
                : $"Du håller på att ta bort kalkylen \"{calc.Name}\". Denna åtgärd kan inte ångras. Vill du fortsätta?";

            var model = new MhdDialogModel
            {
                Title = title,
                State = MhdState.Danger,
                Size = MhdDialogSize.Medium,
                CloseOnOverlayClick = false,
                Content = builder =>
                {
                    builder.OpenElement(0, "p");
                    builder.AddAttribute(1, "class", "text-sm leading-relaxed text-slate-700 dark:text-slate-300");
                    builder.AddContent(2, text);
                    builder.CloseElement();
                },
                Buttons =
                {
                    new MhdDialogButtonModel
                    {
                        Text = "Avbryt",
                        State = MhdState.Secondary,
                        IsPrimary = false,
                        OnClick = EventCallback.Factory.Create(owner, () => mhd.Modal.CloseAsync())
                    },
                    new MhdDialogButtonModel
                    {
                        Text = "Ta bort",
                        State = MhdState.Danger,
                        IsPrimary = true,
                        OnClick = EventCallback.Factory.Create(owner, onConfirmedAsync)
                    }
                }
            };

            mhd.Modal.Show(model);
        }
    }
}
