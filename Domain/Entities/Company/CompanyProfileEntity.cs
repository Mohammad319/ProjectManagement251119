using Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Company;

/// <summary>
/// Företagsprofil — en rad per tenant. Håller företagets identitet (namn, organisationsnummer,
/// logotyp) samt kontaktuppgifter och standardtexter som används i rapporter och PDF-exporter.
/// Identitetsfälten är avsiktligt skyddade: organisationsnumret låses efter första registrering,
/// namn-/logotypbyten är frekvensbegränsade av CompanyProfileService och alla viktiga ändringar
/// loggas i aktivitetsloggen. TenantId sätts automatiskt av tenant-interceptorn.
/// </summary>
public sealed class CompanyProfileEntity : AuditableEntity<int>
{
    // ── Grunduppgifter ──────────────────────────────────────────────
    [MaxLength(200)] public string CompanyName { get; private set; } = string.Empty;
    /// <summary>Låst efter att det satts första gången; ändras endast via support.</summary>
    [MaxLength(30)] public string? OrgNumber { get; private set; }
    [MaxLength(30)] public string? VatNumber { get; private set; }
    [MaxLength(60)] public string? Phone { get; private set; }
    [MaxLength(200)] public string? Email { get; private set; }
    [MaxLength(300)] public string? Website { get; private set; }
    [MaxLength(300)] public string? Address { get; private set; }
    [MaxLength(20)] public string? PostalCode { get; private set; }
    [MaxLength(100)] public string? City { get; private set; }
    [MaxLength(100)] public string? Country { get; private set; }

    // ── Logotyp (nuvarande + en generation historik) ────────────────
    public byte[]? LogoData { get; private set; }
    [MaxLength(100)] public string? LogoContentType { get; private set; }
    public byte[]? PreviousLogoData { get; private set; }
    [MaxLength(100)] public string? PreviousLogoContentType { get; private set; }

    // ── Rapporttexter ───────────────────────────────────────────────
    [MaxLength(500)] public string? ReportHeaderText { get; private set; }
    [MaxLength(500)] public string? ReportFooterText { get; private set; }
    [MaxLength(2000)] public string? CalcReportDefaultText { get; private set; }
    [MaxLength(2000)] public string? TenderReportDefaultText { get; private set; }
    [MaxLength(2000)] public string? SelfInspectionReportDefaultText { get; private set; }
    [MaxLength(2000)] public string? DisclaimerText { get; private set; }

    // ── Spärrdata för frekvensbegränsning ───────────────────────────
    /// <summary>När företagsnamnet senast ändrades (styr 30-dagarsregeln, max 1 byte).</summary>
    public DateTimeOffset? NameLastChangedAtUtc { get; private set; }
    /// <summary>Semikolonseparerade UTC-tidpunkter ("o"-format) för de senaste logotypbytena;
    /// poster äldre än 30 dagar rensas löpande. Styr max 3 byten per 30 dagar.</summary>
    [MaxLength(400)] public string? LogoChangeHistory { get; private set; }

    private CompanyProfileEntity() { }

    public static CompanyProfileEntity Create() => new();

    public bool OrgNumberIsLocked => !string.IsNullOrWhiteSpace(OrgNumber);

    /// <summary>Sätter organisationsnumret första gången. Returnerar false om det redan är låst.</summary>
    public bool TrySetOrgNumber(string? orgNumber)
    {
        var normalized = Normalize(orgNumber, 30);
        if (string.IsNullOrWhiteSpace(normalized))
            return true; // inget att sätta — fortfarande olåst

        if (OrgNumberIsLocked)
            return string.Equals(OrgNumber, normalized, StringComparison.Ordinal);

        OrgNumber = normalized;
        return true;
    }

    public void ChangeName(string name, DateTimeOffset nowUtc)
    {
        CompanyName = Normalize(name, 200) ?? string.Empty;
        NameLastChangedAtUtc = nowUtc;
    }

    /// <summary>Första sättningen av namnet (vid profilskapande) — startar inte 30-dagarsspärren.</summary>
    public void SetInitialName(string name)
        => CompanyName = Normalize(name, 200) ?? string.Empty;

    public void UpdateContact(string? vatNumber, string? phone, string? email, string? website,
        string? address, string? postalCode, string? city, string? country)
    {
        VatNumber = Normalize(vatNumber, 30);
        Phone = Normalize(phone, 60);
        Email = Normalize(email, 200);
        Website = Normalize(website, 300);
        Address = Normalize(address, 300);
        PostalCode = Normalize(postalCode, 20);
        City = Normalize(city, 100);
        Country = Normalize(country, 100);
    }

    public void UpdateReportTexts(string? headerText, string? footerText, string? calcText,
        string? tenderText, string? selfInspectionText, string? disclaimerText)
    {
        ReportHeaderText = Normalize(headerText, 500);
        ReportFooterText = Normalize(footerText, 500);
        CalcReportDefaultText = Normalize(calcText, 2000);
        TenderReportDefaultText = Normalize(tenderText, 2000);
        SelfInspectionReportDefaultText = Normalize(selfInspectionText, 2000);
        DisclaimerText = Normalize(disclaimerText, 2000);
    }

    /// <summary>Byter logotyp och flyttar den nuvarande till historiken (en generation).</summary>
    public void SetLogo(byte[] data, string contentType, DateTimeOffset nowUtc)
    {
        if (LogoData is { Length: > 0 })
        {
            PreviousLogoData = LogoData;
            PreviousLogoContentType = LogoContentType;
        }

        LogoData = data;
        LogoContentType = Normalize(contentType, 100);
        RegisterLogoChange(nowUtc);
    }

    /// <summary>Byter tillbaka till föregående logotyp (räknas som ett logotypbyte).</summary>
    public bool RestorePreviousLogo(DateTimeOffset nowUtc)
    {
        if (PreviousLogoData is not { Length: > 0 })
            return false;

        (LogoData, PreviousLogoData) = (PreviousLogoData, LogoData);
        (LogoContentType, PreviousLogoContentType) = (PreviousLogoContentType, LogoContentType);
        RegisterLogoChange(nowUtc);
        return true;
    }

    /// <summary>UTC-tidpunkter för logotypbyten inom det rullande 30-dagarsfönstret.</summary>
    public IReadOnlyList<DateTimeOffset> LogoChangesWithin30Days(DateTimeOffset nowUtc)
        => (LogoChangeHistory ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => DateTimeOffset.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var ts) ? ts : (DateTimeOffset?)null)
            .Where(ts => ts.HasValue && ts.Value > nowUtc.AddDays(-30))
            .Select(ts => ts!.Value)
            .OrderBy(ts => ts)
            .ToList();

    private void RegisterLogoChange(DateTimeOffset nowUtc)
    {
        var recent = LogoChangesWithin30Days(nowUtc).Append(nowUtc);
        LogoChangeHistory = string.Join(';', recent.Select(ts => ts.ToString("o")));
    }

    private static string? Normalize(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
            return null;

        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }
}
