using BlazorMHD.UI.Core.DesignSystem;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.Repositories;

namespace ProjectManagement.Client.Helper
{
    /// <summary>
    /// Gemensam, säker regelhantering för "Ta bort projekt" som delas av
    /// projektlistan (<c>ProjectsUI</c>) och vänsterträdet (<c>FoldersTree</c>).
    /// <para>
    /// Ett projekt får bara tas bort om det är tomt: inga kalkyler (aktiva eller
    /// arkiverade) och inga anbud/anbudsgivare. Annars visas en informationsdialog
    /// som förklarar varför borttagning inte är tillåten. "Arkivera projekt" finns
    /// kvar som säkert alternativ för projekt med innehåll/historik.
    /// </para>
    /// </summary>
    public static class ProjectDeleteHelper
    {
        public const string DeleteLabel = "Ta bort projekt";
        public const string DeleteCssClass = "text-red-600 dark:text-red-400";

        private const string BlockedTitle = "Projektet kan inte tas bort";

        /// <summary>
        /// Utvärderar borttagningsreglerna och visar antingen en informationsdialog
        /// (blockerad) eller en bekräftelsedialog. <paramref name="onConfirmedAsync"/>
        /// körs först när användaren bekräftat att ett tomt projekt ska tas bort.
        /// </summary>
        public static async Task RequestDeleteAsync(
            ComponentBase owner,
            IUnitOfWorkRepository repo,
            MhdServices mhd,
            IClientLogger logger,
            ListProjectMVVM project,
            bool canManage,
            Func<Task> onConfirmedAsync)
        {
            // Behörighet
            if (!canManage)
            {
                mhd.MessageOk(BlockedTitle, "Du saknar behörighet att ta bort detta projekt.", MhdState.Warning);
                return;
            }

            bool hasCalculations;
            bool hasBids;
            try
            {
                // Kalkyler – både aktiva och arkiverade räknas som innehåll.
                var active = await repo.Calculation.GetAsync(project.Id, isArchived: false) ?? [];
                var archived = await repo.Calculation.GetAsync(project.Id, isArchived: true) ?? [];
                hasCalculations = active.Count > 0 || archived.Count > 0;

                // Anbud/anbudsgivare.
                var bids = await repo.ProjectBid.GetViewAsync(project.Id);
                hasBids = (bids?.Bids?.Count ?? 0) > 0;
            }
            catch (Exception ex)
            {
                await logger.ErrorAsync("Checking project contents before delete failed", ex: ex);
                mhd.MessageOk(BlockedTitle,
                    "Det gick inte att kontrollera projektets innehåll. Försök igen.",
                    MhdState.Danger);
                return;
            }

            // Projekt med kalkyler får inte tas bort i första version. Detta täcker även
            // låsta/historiska kalkyler och kontrakts-/produktionskedjor, eftersom de alla
            // är kalkyler – arkivera projektet i stället.
            if (hasCalculations)
            {
                mhd.MessageOk(BlockedTitle,
                    "Projektet kan inte tas bort eftersom det innehåller kalkyler. Arkivera projektet i stället, eller flytta/ta bort kalkylerna först.",
                    MhdState.Warning);
                return;
            }

            // Projekt med anbudsdata får inte tas bort i första version.
            if (hasBids)
            {
                mhd.MessageOk(BlockedTitle,
                    "Projektet kan inte tas bort eftersom det innehåller anbudsdata. Arkivera projektet i stället.",
                    MhdState.Warning);
                return;
            }

            // Tomt projekt – bekräftelsedialog krävs innan borttagning.
            ShowConfirmation(owner, mhd, project, onConfirmedAsync);
        }

        private static void ShowConfirmation(
            ComponentBase owner, MhdServices mhd, ListProjectMVVM project, Func<Task> onConfirmedAsync)
        {
            var model = new MhdDialogModel
            {
                Title = "Ta bort projekt?",
                State = MhdState.Danger,
                Size = MhdDialogSize.Medium,
                CloseOnOverlayClick = false,
                Content = builder =>
                {
                    builder.OpenElement(0, "p");
                    builder.AddAttribute(1, "class", "text-sm leading-relaxed text-slate-700 dark:text-slate-300");
                    builder.AddContent(2, $"Du håller på att ta bort projektet \"{project.Name}\". Denna åtgärd kan inte ångras. Vill du fortsätta?");
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
