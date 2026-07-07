using ProjectManagement.Client.Shared.Model.Application;
using ProjectManagement.Shared.Base.Application;

namespace ProjectManagement.Client.Pages.Calculation.SelfInspection
{
    /// <summary>
    /// Progress/status for a self-inspection instance in a calculation:
    /// Ej påbörjad / Pågår / Klar plus done-count over the template's visible checkpoints.
    /// </summary>
    public static class SelfInspectionProgress
    {
        public sealed record Result(int Total, int Done, string Status, int Percent);

        public static Result For(ApplicationValuesModel item)
        {
            var answers = item.Data.Attributes;
            var rows = item.Application?.Data?.Row?.Where(r => r.IsVisible).ToList() ?? [];
            var total = rows.Count;
            var done = rows.Count(r => RowDone(r, answers));
            var anyAnswer = answers.Values.Any(v => !string.IsNullOrWhiteSpace(v));

            var status = total > 0 && done == total
                ? "Klar"
                : anyAnswer ? "Pågår" : "Ej påbörjad";

            var percent = total == 0 ? 0 : (int)Math.Round(100.0 * done / total);
            return new Result(total, done, status, percent);
        }

        // A checkpoint counts as done when its checkbox column ("Klart" or similar) is checked.
        // Rows without any checkbox column count as done when every non-computed field is filled in.
        public static bool RowDone(RowModel row, Dictionary<Guid, string> answers)
        {
            var bools = row.Attributes.Where(a => a.AttributeType == AttributeType.Bool).ToList();
            if (bools.Count > 0)
                return bools.Any(a => answers.TryGetValue(a.ID, out var v)
                                      && string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));

            var editable = row.Attributes.Where(a => !a.IsComputed).ToList();
            return editable.Count > 0
                   && editable.All(a => answers.TryGetValue(a.ID, out var v) && !string.IsNullOrWhiteSpace(v));
        }
    }
}
