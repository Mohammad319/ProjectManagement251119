using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using AuthPermissions.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ProjectManagement.Components.Account;

public interface IAccountNotificationEmailSender
{
    Task SendUserCreatedAsync(string email, string? fullName, string? temporaryPassword, bool passwordWasGenerated, CancellationToken ct = default);
    Task SendPasswordChangedNoticeAsync(string email, string? fullName, CancellationToken ct = default);
}

internal sealed class LocalizedIdentityEmailSender(
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    ILogger<LocalizedIdentityEmailSender> logger)
    : IEmailSender<ApplicationUser>, IAccountNotificationEmailSender
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var content = BuildConfirmationContent(confirmationLink);
        return SendEmailInternalAsync(email, content.Subject, content.HtmlBody);
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var content = BuildPasswordResetLinkContent(resetLink);
        return SendEmailInternalAsync(email, content.Subject, content.HtmlBody);
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var content = BuildPasswordResetCodeContent(resetCode);
        return SendEmailInternalAsync(email, content.Subject, content.HtmlBody);
    }

    public Task SendUserCreatedAsync(string email, string? fullName, string? temporaryPassword, bool passwordWasGenerated, CancellationToken ct = default)
    {
        var content = BuildUserCreatedContent(email, fullName, temporaryPassword, passwordWasGenerated);
        return SendEmailInternalAsync(email, content.Subject, content.HtmlBody, ct);
    }

    public Task SendPasswordChangedNoticeAsync(string email, string? fullName, CancellationToken ct = default)
    {
        var content = BuildPasswordChangedContent(fullName);
        return SendEmailInternalAsync(email, content.Subject, content.HtmlBody, ct);
    }

    private async Task SendEmailInternalAsync(string email, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var host = configuration["MailSettings:Host"];
        var from = configuration["MailSettings:Mail"];
        var password = configuration["MailSettings:Password"];
        var displayName = configuration["MailSettings:DisplayName"];
        var port = GetPort(configuration["MailSettings:Port"]);
        var enableSsl = GetEnableSsl(configuration["MailSettings:UseSsl"], port);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            logger.LogWarning("MailSettings are incomplete. Email to {Email} was skipped.", email);
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(from, string.IsNullOrWhiteSpace(displayName) ? "ProjectManagement" : displayName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(email);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = string.IsNullOrWhiteSpace(password)
            };

            if (!string.IsNullOrWhiteSpace(password))
            {
                client.Credentials = new NetworkCredential(from, password);
            }

            ct.ThrowIfCancellationRequested();
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send identity email to {Email}.", email);
        }
    }

    private (string Subject, string HtmlBody) BuildConfirmationContent(string confirmationLink)
    {
        if (IsSwedish())
        {
            return (
                "Bekräfta din e-postadress",
                BuildShell(
                    "Bekräfta din e-postadress",
                    $"Bekräfta ditt konto genom att <a href=\"{HtmlEncoder.Default.Encode(confirmationLink)}\">klicka här</a>."));
        }

        return (
            "Confirm your email",
            BuildShell(
                "Confirm your email",
                $"Please confirm your account by <a href=\"{HtmlEncoder.Default.Encode(confirmationLink)}\">clicking here</a>."));
    }

    private (string Subject, string HtmlBody) BuildPasswordResetLinkContent(string resetLink)
    {
        if (IsSwedish())
        {
            return (
                "Återställ ditt lösenord",
                BuildShell(
                    "Återställ ditt lösenord",
                    $"Du kan återställa ditt lösenord genom att <a href=\"{HtmlEncoder.Default.Encode(resetLink)}\">klicka här</a>."));
        }

        return (
            "Reset your password",
            BuildShell(
                "Reset your password",
                $"You can reset your password by <a href=\"{HtmlEncoder.Default.Encode(resetLink)}\">clicking here</a>."));
    }

    private (string Subject, string HtmlBody) BuildPasswordResetCodeContent(string resetCode)
    {
        var safeCode = HtmlEncoder.Default.Encode(resetCode);

        if (IsSwedish())
        {
            return (
                "Återställ ditt lösenord",
                BuildShell(
                    "Återställ ditt lösenord",
                    $"Använd följande kod för att återställa ditt lösenord: <strong>{safeCode}</strong>."));
        }

        return (
            "Reset your password",
            BuildShell(
                "Reset your password",
                $"Use the following code to reset your password: <strong>{safeCode}</strong>."));
    }

    private (string Subject, string HtmlBody) BuildUserCreatedContent(string email, string? fullName, string? temporaryPassword, bool passwordWasGenerated)
    {
        var safeName = HtmlEncoder.Default.Encode(string.IsNullOrWhiteSpace(fullName) ? email : fullName);
        var safeEmail = HtmlEncoder.Default.Encode(email);
        var appName = HtmlEncoder.Default.Encode(configuration["MailSettings:DisplayName"] ?? "ProjectManagement");

        if (IsSwedish())
        {
            var body = $"Hej {safeName},<br/><br/>" +
                       $"Ett konto har skapats för dig i <strong>{appName}</strong>.<br/>" +
                       $"Inloggningsadress: <strong>{safeEmail}</strong>.<br/>";

            if (passwordWasGenerated && !string.IsNullOrWhiteSpace(temporaryPassword))
            {
                body += $"Tillfälligt lösenord: <strong>{HtmlEncoder.Default.Encode(temporaryPassword)}</strong>.<br/>" +
                        "Byt gärna lösenordet direkt efter första inloggningen.<br/>";
            }
            else
            {
                body += "Lösenordet har satts av administratören. Om du inte har fått det separat, kontakta administratören.<br/>";
            }

            body += "<br/>Om du inte förväntade dig detta meddelande kan du ignorera det.";
            return ("Ditt konto har skapats", BuildShell("Ditt konto har skapats", body));
        }

        var englishBody = $"Hello {safeName},<br/><br/>" +
                          $"An account has been created for you in <strong>{appName}</strong>.<br/>" +
                          $"Sign-in email: <strong>{safeEmail}</strong>.<br/>";

        if (passwordWasGenerated && !string.IsNullOrWhiteSpace(temporaryPassword))
        {
            englishBody += $"Temporary password: <strong>{HtmlEncoder.Default.Encode(temporaryPassword)}</strong>.<br/>" +
                           "Please change it after your first sign-in.<br/>";
        }
        else
        {
            englishBody += "Your password was set by an administrator. If it was not shared separately, please contact your administrator.<br/>";
        }

        englishBody += "<br/>If you did not expect this email, you can ignore it.";
        return ("Your account has been created", BuildShell("Your account has been created", englishBody));
    }

    private (string Subject, string HtmlBody) BuildPasswordChangedContent(string? fullName)
    {
        var safeName = HtmlEncoder.Default.Encode(string.IsNullOrWhiteSpace(fullName) ? "" : fullName);
        var whenText = HtmlEncoder.Default.Encode(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm"));

        if (IsSwedish())
        {
            var greeting = string.IsNullOrWhiteSpace(safeName) ? "Hej," : $"Hej {safeName},";
            return (
                "Ditt lösenord har ändrats",
                BuildShell(
                    "Ditt lösenord har ändrats",
                    $"{greeting}<br/><br/>Vi vill meddela att lösenordet för ditt konto ändrades {whenText}.<br/>Om det inte var du ska du byta lösenord direkt och kontakta administratören."));
        }

        var englishGreeting = string.IsNullOrWhiteSpace(safeName) ? "Hello," : $"Hello {safeName},";
        return (
            "Your password has been changed",
            BuildShell(
                "Your password has been changed",
                $"{englishGreeting}<br/><br/>This is a confirmation that your account password was changed on {whenText}.<br/>If this was not you, please change it again immediately and contact your administrator."));
    }

    private string BuildShell(string title, string body)
    {
        var appName = HtmlEncoder.Default.Encode(configuration["MailSettings:DisplayName"] ?? "ProjectManagement");
        var safeTitle = HtmlEncoder.Default.Encode(title);

        return $"""
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>{safeTitle}</title>
</head>
<body style="margin:0;padding:0;background:#f8fafc;font-family:Arial,Helvetica,sans-serif;color:#0f172a;">
  <div style="max-width:640px;margin:24px auto;padding:0 16px;">
    <div style="background:#ffffff;border:1px solid #e2e8f0;border-radius:16px;overflow:hidden;box-shadow:0 8px 24px rgba(15,23,42,.08);">
      <div style="padding:24px 24px 8px 24px;background:#0f172a;color:#ffffff;font-size:14px;font-weight:700;">{appName}</div>
      <div style="padding:24px;line-height:1.65;">
        <h1 style="margin:0 0 16px 0;font-size:24px;line-height:1.3;color:#0f172a;">{safeTitle}</h1>
        <div style="font-size:15px;color:#334155;">{body}</div>
      </div>
    </div>
  </div>
</body>
</html>
""";
    }

    private bool IsSwedish()
    {
        var culture = ResolveCultureName();
        return culture.StartsWith("sv", StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveCultureName()
    {
        var cookie = httpContextAccessor.HttpContext?.Request.Cookies[".AspNetCore.Culture"];
        if (!string.IsNullOrWhiteSpace(cookie))
        {
            foreach (var part in cookie.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.StartsWith("uic=", StringComparison.OrdinalIgnoreCase))
                    return part[4..];
            }
        }

        return CultureInfo.CurrentUICulture.Name;
    }

    private static int GetPort(string? rawPort)
        => int.TryParse(rawPort, out var port) && port > 0 ? port : 587;

    private static bool GetEnableSsl(string? rawValue, int port)
        => bool.TryParse(rawValue, out var explicitValue) ? explicitValue : port != 25;
}
