using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.Base.Organisation
{
    public class UnderContactOrganisationBase
    {
        [JsonIgnore] public bool CommentIsVisible;

        // Neither name field is [Required] on its own — the contact form enforces
        // "minst ett namnfält" (first OR last name) with a Swedish message instead.
        [MaxLength(60, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(60, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string LastName { get; set; } = string.Empty;
        // Email/phone are optional and free-form; [EmailAddress]/[Phone] rejected empty strings
        // and produced English errors — the contact form validates email in Swedish when filled.
        [MaxLength(250, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Telefone { get; set; } = string.Empty;

        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Mobile { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Department { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public UnderContactStatus Status { get; set; }

        public string GetFullName()
        {
            return FirstName + " " + LastName;
        }
    }
    public enum UnderContactStatus
    {
        Aktive, Resigned, OutOfService, Retired, Vacation
    }

    /// <summary>Swedish display labels for <see cref="UnderContactStatus"/> (enum members are kept for
    /// backward-compatible serialization; only the shown text is translated).</summary>
    public static class UnderContactStatusLabels
    {
        public static string ToSwedish(this UnderContactStatus status) => status switch
        {
            UnderContactStatus.Aktive => "Aktiv",
            UnderContactStatus.Resigned => "Slutat",
            UnderContactStatus.OutOfService => "Ej i tjänst",
            UnderContactStatus.Retired => "Pensionerad",
            UnderContactStatus.Vacation => "Semester",
            _ => status.ToString()
        };
    }
}
