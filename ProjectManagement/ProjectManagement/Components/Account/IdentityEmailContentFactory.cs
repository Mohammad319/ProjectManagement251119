using System.Globalization;
using AuthPermissions.Context;

namespace ProjectManagement.Components.Account;

internal sealed class IdentityEmailContentFactory
{
    public (string Subject, string HtmlBody) CreateConfirmationLinkEmail(ApplicationUser user, string confirmationLink)
    {
        var culture = ResolveCulture();
        return culture.TwoLetterISOLanguageName == "sv"
            ? (
                "Bekräfta din e-postadress",
                $"Hej {DisplayName(user)},<br><br>Bekräfta din e-postadress genom att <a href='{confirmationLink}'>klicka här</a>.<br><br>ProjectManagement")
            : (
                "Confirm your email address",
                $"Hello {DisplayName(user)},<br><br>Please confirm your email address by <a href='{confirmationLink}'>clicking here</a>.<br><br>ProjectManagement");
    }

    public (string Subject, string HtmlBody) CreatePasswordResetLinkEmail(ApplicationUser user, string resetLink)
    {
        var culture = ResolveCulture();
        return culture.TwoLetterISOLanguageName == "sv"
            ? (
                "Återställ ditt lösenord",
                $"Hej {DisplayName(user)},<br><br>Du kan återställa ditt lösenord genom att <a href='{resetLink}'>klicka här</a>.<br><br>ProjectManagement")
            : (
                "Reset your password",
                $"Hello {DisplayName(user)},<br><br>You can reset your password by <a href='{resetLink}'>clicking here</a>.<br><br>ProjectManagement");
    }

    public (string Subject, string HtmlBody) CreatePasswordResetCodeEmail(ApplicationUser user, string resetCode)
    {
        var culture = ResolveCulture();
        return culture.TwoLetterISOLanguageName == "sv"
            ? (
                "Kod för återställning av lösenord",
                $"Hej {DisplayName(user)},<br><br>Använd följande kod för att återställa ditt lösenord: <strong>{resetCode}</strong><br><br>ProjectManagement")
            : (
                "Password reset code",
                $"Hello {DisplayName(user)},<br><br>Use the following code to reset your password: <strong>{resetCode}</strong><br><br>ProjectManagement");
    }

    private static CultureInfo ResolveCulture() =>
        CultureInfo.CurrentUICulture ?? CultureInfo.CurrentCulture ?? CultureInfo.InvariantCulture;

    private static string DisplayName(ApplicationUser user)
    {
        var fullName = string.Join(" ", new[] { user.Firstname, user.Lastname }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(fullName) ? (user.Email ?? "there") : fullName;
    }
}
