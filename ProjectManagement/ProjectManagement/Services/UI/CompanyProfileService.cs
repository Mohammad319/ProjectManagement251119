using Domain.Entities.Company;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Services.UI;

/// <summary>Företagsprofilen som UI:t arbetar mot. Logotyper levereras som data-URL:er.</summary>
public sealed record CompanyProfileDto(
    bool Exists,
    string CompanyName,
    string? OrgNumber,
    bool OrgNumberLocked,
    string? VatNumber,
    string? Phone,
    string? Email,
    string? Website,
    string? Address,
    string? PostalCode,
    string? City,
    string? Country,
    string? LogoDataUrl,
    bool HasPreviousLogo,
    string? PreviousLogoDataUrl,
    string? ReportHeaderText,
    string? ReportFooterText,
    string? CalcReportDefaultText,
    string? TenderReportDefaultText,
    string? SelfInspectionReportDefaultText,
    string? DisclaimerText,
    DateTimeOffset? NextNameChangeAllowedAtUtc,
    int LogoChangesLast30Days,
    DateTimeOffset? NextLogoChangeAllowedAtUtc);

public sealed record CompanyBasicsInput(
    string CompanyName,
    string? OrgNumber,
    string? VatNumber,
    string? Phone,
    string? Email,
    string? Website,
    string? Address,
    string? PostalCode,
    string? City,
    string? Country);

public sealed record CompanyReportTextsInput(
    string? ReportHeaderText,
    string? ReportFooterText,
    string? CalcReportDefaultText,
    string? TenderReportDefaultText,
    string? SelfInspectionReportDefaultText,
    string? DisclaimerText);

public sealed record CompanyProfileResult(bool Succeeded, string? Error = null)
{
    public static CompanyProfileResult Ok() => new(true);
    public static CompanyProfileResult Fail(string error) => new(false, error);
}

/// <summary>Företagsuppgifter som rapporter/PDF-exporter stämplar in i sidhuvud/sidfot.</summary>
public sealed record CompanyReportInfo(
    string CompanyName,
    string? OrgNumber,
    string? Phone,
    string? Email,
    string? Website,
    string? AddressLine,
    string? LogoDataUrl,
    string? ReportHeaderText,
    string? ReportFooterText,
    string? DisclaimerText);

public interface ICompanyProfileService
{
    Task<CompanyProfileDto> GetAsync(CancellationToken ct = default);

    /// <summary>Sparar grunduppgifter. Skapar profilen vid första sparningen. Hanterar
    /// namnbytesregeln (max 1/30 dagar) och det låsta organisationsnumret.</summary>
    Task<CompanyProfileResult> SaveBasicsAsync(CompanyBasicsInput input, CancellationToken ct = default);

    Task<CompanyProfileResult> SaveReportTextsAsync(CompanyReportTextsInput input, CancellationToken ct = default);

    /// <summary>Byter logotyp (max 3 byten per 30 dagar; PNG/JPG, max 2 MB).</summary>
    Task<CompanyProfileResult> ChangeLogoAsync(byte[] data, string contentType, CancellationToken ct = default);

    /// <summary>Återställer föregående logotyp. Räknas och loggas som ett logotypbyte.</summary>
    Task<CompanyProfileResult> RestorePreviousLogoAsync(CancellationToken ct = default);

    /// <summary>Företagsuppgifter för rapporter/PDF, eller null om ingen profil finns.</summary>
    Task<CompanyReportInfo?> GetReportInfoAsync(CancellationToken ct = default);
}

public sealed class CompanyProfileService(
    IDbContextFactoryTenant dbFactory,
    AuthenticationStateProvider authStateProvider,
    IUserManagementAuditService auditService,
    ILogger<CompanyProfileService> logger) : ICompanyProfileService
{
    public const int MaxLogoBytes = 2 * 1024 * 1024;
    public const int NameChangeCooldownDays = 30;
    public const int MaxLogoChangesPer30Days = 3;

    public const string NameChangeBlockedMessage =
        "Företagsnamnet kan bara ändras en gång per 30 dagar. Kontakta support om ändringen är brådskande.";
    public const string LogoChangeBlockedMessage =
        "Logotypen kan ändras max 3 gånger per 30 dagar. Kontakta support om ändringen är brådskande.";
    public const string OrgNumberLockedMessage =
        "Organisationsnummer är låst efter registrering. Kontakta support om det behöver ändras.";
    private const string NotAdminMessage = "Endast administratörer kan ändra företagsprofilen.";

    public async Task<CompanyProfileDto> GetAsync(CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var entity = await context.CompanyProfiles.AsNoTracking().FirstOrDefaultAsync(ct);
        return ToDto(entity);
    }

    public async Task<CompanyProfileResult> SaveBasicsAsync(CompanyBasicsInput input, CancellationToken ct = default)
    {
        if (!await IsTenantAdminAsync())
            return CompanyProfileResult.Fail(NotAdminMessage);

        var newName = input.CompanyName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(newName))
            return CompanyProfileResult.Fail("Företagsnamn måste anges.");

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var entity = await context.CompanyProfiles.FirstOrDefaultAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var isNew = entity is null;

        if (entity is null)
        {
            entity = CompanyProfileEntity.Create();
            context.CompanyProfiles.Add(entity);
        }

        var oldName = entity.CompanyName;
        var nameChanged = !isNew
            && !string.IsNullOrWhiteSpace(oldName)
            && !string.Equals(oldName, newName, StringComparison.Ordinal);

        if (nameChanged)
        {
            var nextAllowed = entity.NameLastChangedAtUtc?.AddDays(NameChangeCooldownDays);
            if (nextAllowed.HasValue && nextAllowed.Value > now)
            {
                await WriteAuditAsync("company.name-change-blocked",
                    $"Försök att ändra företagsnamn från \"{oldName}\" till \"{newName}\" stoppades av 30-dagarsregeln.",
                    oldName, succeeded: false, ct);
                return CompanyProfileResult.Fail(NameChangeBlockedMessage);
            }

            entity.ChangeName(newName, now);
        }
        else if (string.IsNullOrWhiteSpace(oldName))
        {
            // Första namnsättningen startar inte 30-dagarsspärren.
            entity.SetInitialName(newName);
        }

        // Organisationsnummer: sätts fritt så länge det är olåst, därefter endast via support.
        var requestedOrgNumber = input.OrgNumber?.Trim();
        var orgWasLocked = entity.OrgNumberIsLocked;
        if (!entity.TrySetOrgNumber(requestedOrgNumber))
        {
            await WriteAuditAsync("company.orgnumber-change-blocked",
                $"Försök att ändra låst organisationsnummer ({entity.OrgNumber}) blockerades.",
                entity.CompanyName, succeeded: false, ct);
            return CompanyProfileResult.Fail(OrgNumberLockedMessage);
        }

        entity.UpdateContact(input.VatNumber, input.Phone, input.Email, input.Website,
            input.Address, input.PostalCode, input.City, input.Country);

        await context.SaveChangesAsync(ct);

        if (isNew)
        {
            await WriteAuditAsync("company.profile-created",
                $"Företagsprofil skapades för \"{entity.CompanyName}\".", entity.CompanyName, true, ct);
        }
        else if (nameChanged)
        {
            await WriteAuditAsync("company.name-changed",
                $"Företagsnamn ändrades från \"{oldName}\" till \"{newName}\".", newName, true, ct);
        }

        if (!orgWasLocked && entity.OrgNumberIsLocked)
        {
            await WriteAuditAsync("company.orgnumber-set",
                $"Organisationsnummer registrerades: {entity.OrgNumber}. Numret är nu låst.",
                entity.CompanyName, true, ct);
        }

        if (!isNew && !nameChanged)
        {
            await WriteAuditAsync("company.contact-updated",
                "Företagets kontaktuppgifter uppdaterades.", entity.CompanyName, true, ct);
        }

        return CompanyProfileResult.Ok();
    }

    public async Task<CompanyProfileResult> SaveReportTextsAsync(CompanyReportTextsInput input, CancellationToken ct = default)
    {
        if (!await IsTenantAdminAsync())
            return CompanyProfileResult.Fail(NotAdminMessage);

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var entity = await context.CompanyProfiles.FirstOrDefaultAsync(ct);
        if (entity is null)
            return CompanyProfileResult.Fail("Spara företagets grunduppgifter först.");

        entity.UpdateReportTexts(input.ReportHeaderText, input.ReportFooterText,
            input.CalcReportDefaultText, input.TenderReportDefaultText,
            input.SelfInspectionReportDefaultText, input.DisclaimerText);
        await context.SaveChangesAsync(ct);

        await WriteAuditAsync("company.report-texts-updated",
            "Företagets rapporttexter uppdaterades.", entity.CompanyName, true, ct);
        return CompanyProfileResult.Ok();
    }

    public async Task<CompanyProfileResult> ChangeLogoAsync(byte[] data, string contentType, CancellationToken ct = default)
    {
        if (!await IsTenantAdminAsync())
            return CompanyProfileResult.Fail(NotAdminMessage);

        var validation = ValidateLogo(data, contentType);
        if (validation is not null)
            return CompanyProfileResult.Fail(validation);

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var entity = await context.CompanyProfiles.FirstOrDefaultAsync(ct);
        if (entity is null)
            return CompanyProfileResult.Fail("Spara företagets grunduppgifter först.");

        var now = DateTimeOffset.UtcNow;
        if (entity.LogoChangesWithin30Days(now).Count >= MaxLogoChangesPer30Days)
        {
            await WriteAuditAsync("company.logo-change-blocked",
                "Försök att byta logotyp stoppades av gränsen 3 byten per 30 dagar.",
                entity.CompanyName, succeeded: false, ct);
            return CompanyProfileResult.Fail(LogoChangeBlockedMessage);
        }

        entity.SetLogo(data, contentType, now);
        await context.SaveChangesAsync(ct);

        await WriteAuditAsync("company.logo-updated",
            "Företagslogotyp uppdaterades.", entity.CompanyName, true, ct);
        return CompanyProfileResult.Ok();
    }

    public async Task<CompanyProfileResult> RestorePreviousLogoAsync(CancellationToken ct = default)
    {
        if (!await IsTenantAdminAsync())
            return CompanyProfileResult.Fail(NotAdminMessage);

        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var entity = await context.CompanyProfiles.FirstOrDefaultAsync(ct);
        if (entity is null || entity.PreviousLogoData is not { Length: > 0 })
            return CompanyProfileResult.Fail("Det finns ingen tidigare logotyp att återställa.");

        var now = DateTimeOffset.UtcNow;
        if (entity.LogoChangesWithin30Days(now).Count >= MaxLogoChangesPer30Days)
        {
            await WriteAuditAsync("company.logo-change-blocked",
                "Försök att återställa logotyp stoppades av gränsen 3 byten per 30 dagar.",
                entity.CompanyName, succeeded: false, ct);
            return CompanyProfileResult.Fail(LogoChangeBlockedMessage);
        }

        entity.RestorePreviousLogo(now);
        await context.SaveChangesAsync(ct);

        await WriteAuditAsync("company.logo-restored",
            "Företagslogotyp återställdes till föregående version.", entity.CompanyName, true, ct);
        return CompanyProfileResult.Ok();
    }

    public async Task<CompanyReportInfo?> GetReportInfoAsync(CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var entity = await context.CompanyProfiles.AsNoTracking().FirstOrDefaultAsync(ct);
        if (entity is null || string.IsNullOrWhiteSpace(entity.CompanyName))
            return null;

        var addressParts = new[]
        {
            entity.Address,
            string.Join(' ', new[] { entity.PostalCode, entity.City }.Where(s => !string.IsNullOrWhiteSpace(s))),
            entity.Country
        }.Where(s => !string.IsNullOrWhiteSpace(s));

        return new CompanyReportInfo(
            entity.CompanyName,
            entity.OrgNumber,
            entity.Phone,
            entity.Email,
            entity.Website,
            string.Join(", ", addressParts) is { Length: > 0 } line ? line : null,
            ToDataUrl(entity.LogoData, entity.LogoContentType),
            entity.ReportHeaderText,
            entity.ReportFooterText,
            entity.DisclaimerText);
    }

    /// <summary>PNG/JPG, max 2 MB, med enkel magic-byte-kontroll så innehållet matchar typen.</summary>
    internal static string? ValidateLogo(byte[]? data, string? contentType)
    {
        if (data is not { Length: > 0 })
            return "Ingen fil valdes.";
        if (data.Length > MaxLogoBytes)
            return "Logotypen får vara högst 2 MB.";

        var type = contentType?.Trim().ToLowerInvariant();
        var isPng = type == "image/png"
            && data.Length > 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47;
        var isJpeg = (type == "image/jpeg" || type == "image/jpg")
            && data.Length > 3 && data[0] == 0xFF && data[1] == 0xD8;

        return isPng || isJpeg ? null : "Endast PNG- och JPG-bilder stöds som logotyp.";
    }

    private async Task<bool> IsTenantAdminAsync()
    {
        try
        {
            var authState = await authStateProvider.GetAuthenticationStateAsync();
            return authState.User.IsInRole(PMRolesConst.Tenant.Admin);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not resolve authentication state for company profile change.");
            return false;
        }
    }

    private async Task WriteAuditAsync(string action, string details, string? companyName, bool succeeded, CancellationToken ct)
        => await auditService.WriteAsync(
            action,
            targetUserId: null,
            targetAuthId: null,
            targetDisplayName: string.IsNullOrWhiteSpace(companyName) ? "Företagsprofil" : companyName,
            details: details,
            succeeded: succeeded,
            ct: ct);

    private static CompanyProfileDto ToDto(CompanyProfileEntity? entity)
    {
        if (entity is null)
        {
            return new CompanyProfileDto(
                Exists: false,
                CompanyName: string.Empty,
                OrgNumber: null, OrgNumberLocked: false,
                VatNumber: null, Phone: null, Email: null, Website: null,
                Address: null, PostalCode: null, City: null, Country: null,
                LogoDataUrl: null, HasPreviousLogo: false, PreviousLogoDataUrl: null,
                ReportHeaderText: null, ReportFooterText: null,
                CalcReportDefaultText: null, TenderReportDefaultText: null,
                SelfInspectionReportDefaultText: null, DisclaimerText: null,
                NextNameChangeAllowedAtUtc: null,
                LogoChangesLast30Days: 0,
                NextLogoChangeAllowedAtUtc: null);
        }

        var now = DateTimeOffset.UtcNow;
        var logoChanges = entity.LogoChangesWithin30Days(now);
        var nextNameChange = entity.NameLastChangedAtUtc?.AddDays(NameChangeCooldownDays);

        return new CompanyProfileDto(
            Exists: true,
            CompanyName: entity.CompanyName,
            OrgNumber: entity.OrgNumber,
            OrgNumberLocked: entity.OrgNumberIsLocked,
            VatNumber: entity.VatNumber,
            Phone: entity.Phone,
            Email: entity.Email,
            Website: entity.Website,
            Address: entity.Address,
            PostalCode: entity.PostalCode,
            City: entity.City,
            Country: entity.Country,
            LogoDataUrl: ToDataUrl(entity.LogoData, entity.LogoContentType),
            HasPreviousLogo: entity.PreviousLogoData is { Length: > 0 },
            PreviousLogoDataUrl: ToDataUrl(entity.PreviousLogoData, entity.PreviousLogoContentType),
            ReportHeaderText: entity.ReportHeaderText,
            ReportFooterText: entity.ReportFooterText,
            CalcReportDefaultText: entity.CalcReportDefaultText,
            TenderReportDefaultText: entity.TenderReportDefaultText,
            SelfInspectionReportDefaultText: entity.SelfInspectionReportDefaultText,
            DisclaimerText: entity.DisclaimerText,
            NextNameChangeAllowedAtUtc: nextNameChange > now ? nextNameChange : null,
            LogoChangesLast30Days: logoChanges.Count,
            NextLogoChangeAllowedAtUtc: logoChanges.Count >= MaxLogoChangesPer30Days
                ? logoChanges[0].AddDays(30)
                : null);
    }

    private static string? ToDataUrl(byte[]? data, string? contentType)
        => data is { Length: > 0 } && !string.IsNullOrWhiteSpace(contentType)
            ? $"data:{contentType};base64,{Convert.ToBase64String(data)}"
            : null;
}
