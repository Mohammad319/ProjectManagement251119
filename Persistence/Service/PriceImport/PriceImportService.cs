using Application.Feature.PriceImport;
using Domain.Entities.PriceLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Factory;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Persistence.Service.PriceImport;

public sealed class PriceImportService(
    IDbContextFactoryTenant dbFactory,
    IPriceImportExtractionClient extractionClient,
    IPriceImportAiExtractionService aiExtractionService,
    IOptions<PriceImportStorageOptions> storageOptions,
    ILogger<PriceImportService> logger) : IPriceImportService
{
    public async Task<IReadOnlyList<PriceImportJobListItemDto>> GetImportJobsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.PriceImportJobs
            .AsNoTracking()
            .Where(x => x.TenantId == db.TenantId)
            .OrderByDescending(x => x.StartedAt)
            .Select(x => new PriceImportJobListItemDto(
                x.Id,
                x.SourceFileName,
                x.SupplierName,
                x.FileType,
                x.Status,
                x.TotalCandidates,
                x.ReadyCount,
                x.ReviewCount,
                x.ErrorCount,
                x.ApprovedCount,
                x.StartedAt,
                x.CompletedAt))
            .ToListAsync(ct);
    }

    public async Task<PriceImportJobDetailsDto?> GetImportJobDetailsAsync(Guid jobId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var job = await db.PriceImportJobs
            .AsNoTracking()
            .Where(x => x.Id == jobId && x.TenantId == db.TenantId)
            .Select(x => new
            {
                x.Id,
                x.SourceFileName,
                x.SourceFilePath,
                x.SupplierName,
                x.FileType,
                x.Status,
                x.TotalCandidates,
                x.ReadyCount,
                x.ReviewCount,
                x.ErrorCount,
                x.ApprovedCount,
                x.StartedAt,
                x.CompletedAt
            })
            .FirstOrDefaultAsync(ct);

        if (job is null)
            return null;

        var candidates = await db.PriceImportCandidates
            .AsNoTracking()
            .Where(x => x.ImportJobId == jobId && x.TenantId == db.TenantId)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.Name)
            .Select(x => new PriceImportCandidateDto(
                x.Id,
                x.Status,
                x.ArticleNumber,
                x.ProductCode,
                x.Name,
                x.Description,
                x.CategoryName,
                x.BasePrice,
                x.DiscountPercent,
                x.NetPrice,
                x.Unit,
                x.Currency,
                x.SupplierName,
                x.Confidence,
                x.SourceText,
                x.ErrorMessage))
            .ToListAsync(ct);

        return new PriceImportJobDetailsDto(
            job.Id,
            job.SourceFileName,
            job.SourceFilePath,
            job.SupplierName,
            job.FileType,
            job.Status,
            job.TotalCandidates,
            job.ReadyCount,
            job.ReviewCount,
            job.ErrorCount,
            job.ApprovedCount,
            job.StartedAt,
            job.CompletedAt,
            candidates);
    }

    public async Task<IReadOnlyList<PriceListListItemDto>> GetPriceListsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == db.TenantId)
            .OrderByDescending(x => x.ImportedAt ?? x.CreatedAt)
            .Select(x => new PriceListListItemDto(
                x.Id,
                x.Name,
                x.SupplierName,
                x.Currency,
                x.ValidFrom,
                x.ValidTo,
                x.SourceFileName,
                x.CreatedAt,
                x.ImportedAt,
                x.IsActive,
                db.PriceListItems.Count(i => i.TenantId == db.TenantId && i.PriceListId == x.Id)))
            .ToListAsync(ct);
    }

    public async Task<PriceListDetailsDto?> GetPriceListDetailsAsync(Guid priceListId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var priceList = await db.PriceLists
            .AsNoTracking()
            .Where(x => x.Id == priceListId && x.TenantId == db.TenantId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.SupplierName,
                x.Currency,
                x.ValidFrom,
                x.ValidTo,
                x.SourceFileName,
                x.SourceFilePath,
                x.SourceFileHash,
                x.CreatedAt,
                x.ImportedAt,
                x.IsActive,
                x.Note
            })
            .FirstOrDefaultAsync(ct);

        if (priceList is null)
            return null;

        var items = await db.PriceListItems
            .AsNoTracking()
            .Where(x => x.PriceListId == priceListId && x.TenantId == db.TenantId)
            .OrderBy(x => x.CategoryName)
            .ThenBy(x => x.Name)
            .Select(x => new PriceListItemDto(
                x.Id,
                x.ArticleNumber,
                x.ProductCode,
                x.Name,
                x.Description,
                x.CategoryName,
                x.ClassificationPath,
                x.BasePrice,
                x.DiscountPercent,
                x.NetPrice,
                x.Unit,
                x.Currency,
                x.SupplierName,
                x.ConsumptionFactor,
                x.WastePercent,
                x.SourceFileName,
                x.SourcePageNumber,
                x.SourceSheetName,
                x.SourceCellRange,
                x.SourceText,
                x.Confidence,
                x.CreatedAt,
                x.IsActive,
                x.UserNote))
            .ToListAsync(ct);

        return new PriceListDetailsDto(
            priceList.Id,
            priceList.Name,
            priceList.SupplierName,
            priceList.Currency,
            priceList.ValidFrom,
            priceList.ValidTo,
            priceList.SourceFileName,
            priceList.SourceFilePath,
            priceList.SourceFileHash,
            priceList.CreatedAt,
            priceList.ImportedAt,
            priceList.IsActive,
            priceList.Note,
            items);
    }

    public async Task<bool> UpdatePriceListAsync(PriceListUpdateDto priceListUpdate, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var updated = await db.PriceLists
            .Where(x => x.Id == priceListUpdate.Id && x.TenantId == db.TenantId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Name, Truncate(priceListUpdate.Name, 300) ?? "")
                .SetProperty(x => x.SupplierName, Truncate(NormalizeOptional(priceListUpdate.SupplierName), 300))
                .SetProperty(x => x.Currency, Truncate(string.IsNullOrWhiteSpace(priceListUpdate.Currency) ? "SEK" : priceListUpdate.Currency.Trim(), 10)!)
                .SetProperty(x => x.ValidFrom, priceListUpdate.ValidFrom)
                .SetProperty(x => x.ValidTo, priceListUpdate.ValidTo)
                .SetProperty(x => x.IsActive, priceListUpdate.IsActive)
                .SetProperty(x => x.Note, NormalizeOptional(priceListUpdate.Note)), ct);

        return updated > 0;
    }

    public async Task<bool> SetPriceListActiveAsync(Guid priceListId, bool isActive, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var updated = await db.PriceLists
            .Where(x => x.Id == priceListId && x.TenantId == db.TenantId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsActive, isActive), ct);

        return updated > 0;
    }

    public async Task<bool> UpdatePriceListItemAsync(PriceListItemUpdateDto itemUpdate, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var updated = await db.PriceListItems
            .Where(x => x.Id == itemUpdate.Id && x.TenantId == db.TenantId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ArticleNumber, Truncate(NormalizeOptional(itemUpdate.ArticleNumber), 150))
                .SetProperty(x => x.ProductCode, Truncate(NormalizeOptional(itemUpdate.ProductCode), 150))
                .SetProperty(x => x.Name, Truncate(itemUpdate.Name, 500) ?? "")
                .SetProperty(x => x.Description, NormalizeOptional(itemUpdate.Description))
                .SetProperty(x => x.CategoryName, NormalizeOptional(itemUpdate.CategoryName))
                .SetProperty(x => x.ClassificationPath, NormalizeOptional(itemUpdate.ClassificationPath))
                .SetProperty(x => x.BasePrice, itemUpdate.BasePrice)
                .SetProperty(x => x.DiscountPercent, itemUpdate.DiscountPercent)
                .SetProperty(x => x.NetPrice, itemUpdate.NetPrice)
                .SetProperty(x => x.Unit, Truncate(NormalizeOptional(itemUpdate.Unit), 50))
                .SetProperty(x => x.Currency, Truncate(string.IsNullOrWhiteSpace(itemUpdate.Currency) ? "SEK" : itemUpdate.Currency.Trim(), 10)!)
                .SetProperty(x => x.SupplierName, Truncate(NormalizeOptional(itemUpdate.SupplierName), 300))
                .SetProperty(x => x.ConsumptionFactor, itemUpdate.ConsumptionFactor)
                .SetProperty(x => x.WastePercent, itemUpdate.WastePercent)
                .SetProperty(x => x.IsActive, itemUpdate.IsActive)
                .SetProperty(x => x.UserNote, NormalizeOptional(itemUpdate.UserNote)), ct);

        return updated > 0;
    }

    public async Task<bool> SetPriceListItemActiveAsync(Guid itemId, bool isActive, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var updated = await db.PriceListItems
            .Where(x => x.Id == itemId && x.TenantId == db.TenantId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsActive, isActive), ct);

        return updated > 0;
    }

    public async Task<Guid> CreateManualTestJobAsync(
        string? supplierName = null,
        string? sourceFileName = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var supplier = string.IsNullOrWhiteSpace(supplierName) ? "Test Supplier" : supplierName.Trim();
        var fileName = string.IsNullOrWhiteSpace(sourceFileName) ? "TestPrices.pdf" : sourceFileName.Trim();

        var job = new PriceImportJob
        {
            Id = Guid.NewGuid(),
            TenantId = db.TenantId,
            SupplierName = supplier,
            SourceFileName = fileName,
            SourceFilePath = $"manual-test/{fileName}",
            SourceFileHash = $"manual-test-{Guid.NewGuid():N}",
            FileType = PriceImportFileType.PdfText,
            Status = PriceImportJobStatus.ReadyForReview,
            StartedAt = DateTime.UtcNow
        };

        job.Candidates.AddRange(
        [
            new PriceImportCandidate
            {
                Id = Guid.NewGuid(),
                TenantId = db.TenantId,
                ImportJobId = job.Id,
                ArticleNumber = "VA-100",
                ProductCode = "PIPE-110",
                Name = "PVC pipe 110 mm",
                Description = "Manual test candidate for approved price flow.",
                CategoryName = "VA material",
                BasePrice = 128.50m,
                DiscountPercent = 10m,
                NetPrice = 115.65m,
                Unit = "m",
                Currency = "SEK",
                SupplierName = supplier,
                PageNumber = 1,
                SourceText = "VA-100 PVC pipe 110 mm 128.50 SEK/m discount 10%",
                Confidence = 0.9400m,
                Status = PriceImportCandidateStatus.Approved,
                CreatedAt = DateTime.UtcNow
            },
            new PriceImportCandidate
            {
                Id = Guid.NewGuid(),
                TenantId = db.TenantId,
                ImportJobId = job.Id,
                ArticleNumber = "BET-210",
                ProductCode = "CONC-C25",
                Name = "Concrete C25/30",
                CategoryName = "Concrete",
                BasePrice = 1425m,
                NetPrice = 1425m,
                Unit = "m3",
                Currency = "SEK",
                SupplierName = supplier,
                PageNumber = 2,
                SourceText = "BET-210 Concrete C25/30 1425 SEK/m3",
                Confidence = 0.7200m,
                Status = PriceImportCandidateStatus.NeedsReview,
                CreatedAt = DateTime.UtcNow
            },
            new PriceImportCandidate
            {
                Id = Guid.NewGuid(),
                TenantId = db.TenantId,
                ImportJobId = job.Id,
                Name = "Unclear row from PDF",
                Currency = "SEK",
                SupplierName = supplier,
                PageNumber = 3,
                SourceText = "Extracted text row could not be mapped to a reliable price.",
                Confidence = 0.1800m,
                Status = PriceImportCandidateStatus.Error,
                ErrorMessage = "Missing product name or price in extracted text.",
                CreatedAt = DateTime.UtcNow
            }
        ]);

        RefreshJobCounts(job);
        db.PriceImportJobs.Add(job);
        await db.SaveChangesAsync(ct);
        return job.Id;
    }

    public async Task<PriceImportStartResultDto> StartImportFromUploadedFileAsync(
        Stream fileStream,
        string fileName,
        long fileSize,
        string? contentType,
        string? supplierName,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        var safeFileName = SanitizeFileName(Path.GetFileName(fileName));
        if (string.IsNullOrWhiteSpace(safeFileName))
            throw new InvalidOperationException("File name is required.");

        var extension = Path.GetExtension(safeFileName);
        var allowedExtensions = GetAllowedExtensions();
        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException($"Filtypen stöds inte. Tillåtna filtyper är: {string.Join(", ", allowedExtensions)}");

        var maxFileSize = storageOptions.Value.MaxFileSizeBytes <= 0
            ? 20 * 1024 * 1024
            : storageOptions.Value.MaxFileSizeBytes;

        if (fileSize <= 0)
            throw new InvalidOperationException("File is empty.");

        if (fileSize > maxFileSize)
            throw new InvalidOperationException($"File is too large. Maximum size is {maxFileSize / 1024 / 1024} MB.");

        if (!await extractionClient.CheckHealthAsync(ct))
        {
            logger.LogWarning(
                "Price import blocked because extraction service is unavailable. FileName={FileName}, FileSize={FileSize}",
                safeFileName,
                fileSize);

            return new PriceImportStartResultDto(
                Guid.Empty,
                false,
                "Extraktionstjänsten är inte tillgänglig. Kontrollera att Python-tjänsten körs.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var jobId = Guid.NewGuid();
        var rootPath = ResolveStorageRoot();
        var jobFolder = Path.Combine(rootPath, db.TenantId.ToString(CultureInfo.InvariantCulture), jobId.ToString("N"));
        Directory.CreateDirectory(jobFolder);

        var savedFileName = $"{jobId:N}_{safeFileName}";
        var savedPath = Path.GetFullPath(Path.Combine(jobFolder, savedFileName));
        if (!savedPath.StartsWith(Path.GetFullPath(jobFolder), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid file path.");

        await using (var output = File.Create(savedPath))
        {
            await fileStream.CopyToAsync(output, ct);
        }

        var fileHash = await ComputeSha256Async(savedPath, ct);
        var duplicateImportExists = await db.PriceImportJobs
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == db.TenantId && x.SourceFileHash == fileHash, ct);

        var duplicatePriceListExists = await db.PriceLists
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == db.TenantId && x.SourceFileHash == fileHash, ct);

        if (duplicateImportExists || duplicatePriceListExists)
        {
            TryDeleteFileAndEmptyFolder(savedPath);
            logger.LogWarning(
                "Duplicate price import blocked. TenantId={TenantId}, FileName={FileName}, FileSize={FileSize}, Hash={SourceFileHash}",
                db.TenantId,
                safeFileName,
                fileSize,
                fileHash);

            return new PriceImportStartResultDto(
                Guid.Empty,
                false,
                "This file appears to have been imported before.");
        }

        var now = DateTime.UtcNow;
        var job = new PriceImportJob
        {
            Id = jobId,
            TenantId = db.TenantId,
            SupplierName = NormalizeOptional(supplierName),
            SourceFileName = safeFileName,
            SourceFilePath = savedPath,
            SourceFileHash = fileHash,
            FileType = DetectFileType(extension),
            StartedAt = now,
            Status = PriceImportJobStatus.Analyzing
        };

        db.PriceImportJobs.Add(job);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Started price import. ImportJobId={ImportJobId}, TenantId={TenantId}, FileName={FileName}, FileType={FileType}, FileSize={FileSize}",
            job.Id,
            db.TenantId,
            safeFileName,
            job.FileType,
            fileSize);

        try
        {
            await using var extractionStream = File.OpenRead(savedPath);
            var extraction = await extractionClient.ExtractTextAsync(
                extractionStream,
                safeFileName,
                contentType,
                ct);

            if (extraction is null)
                throw new InvalidOperationException("Extraction service returned no result.");

            if (!extraction.Ok)
                throw new InvalidOperationException(ShortError(string.Join("; ", extraction.Errors)));

            var candidates = BuildCandidatesFromExtraction(extraction, job, db.TenantId);
            if (candidates.Count == 0)
            {
                candidates.Add(new PriceImportCandidate
                {
                    Id = Guid.NewGuid(),
                    TenantId = db.TenantId,
                    ImportJobId = job.Id,
                    Name = "No extractable text",
                    Currency = "SEK",
                    SupplierName = job.SupplierName,
                    SourceText = "The extraction service completed, but no text rows or blocks were returned.",
                    Confidence = 0.1000m,
                    Status = PriceImportCandidateStatus.NeedsReview,
                    CreatedAt = DateTime.UtcNow
                });
            }

            db.PriceImportCandidates.AddRange(candidates);
            job.Status = PriceImportJobStatus.ReadyForReview;
            job.CompletedAt = DateTime.UtcNow;
            ApplyCounts(job, candidates);

            await db.SaveChangesAsync(ct);
            logger.LogInformation(
                "Price import ready for review. ImportJobId={ImportJobId}, TenantId={TenantId}, Candidates={CandidateCount}, Status={Status}",
                job.Id,
                db.TenantId,
                candidates.Count,
                job.Status);

            return new PriceImportStartResultDto(job.Id, true, "Import created successfully.");
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            job.Status = PriceImportJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = "Extraction timed out. Please try again with a smaller file or restart the extraction service.";
            await db.SaveChangesAsync(ct);

            logger.LogWarning(
                ex,
                "Price import extraction timed out. ImportJobId={ImportJobId}, TenantId={TenantId}, FileName={FileName}",
                job.Id,
                db.TenantId,
                safeFileName);

            return new PriceImportStartResultDto(job.Id, false, job.ErrorMessage);
        }
        catch (Exception ex)
        {
            job.Status = PriceImportJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = ShortError(ToUserSafeImportError(ex));
            await db.SaveChangesAsync(ct);

            logger.LogWarning(
                ex,
                "Price import failed. ImportJobId={ImportJobId}, TenantId={TenantId}, FileName={FileName}, Status={Status}, Error={ErrorMessage}",
                job.Id,
                db.TenantId,
                safeFileName,
                job.Status,
                job.ErrorMessage);

            return new PriceImportStartResultDto(job.Id, false, job.ErrorMessage);
        }
    }

    public async Task<bool> UpdateCandidateStatusAsync(
        Guid candidateId,
        PriceImportCandidateStatus status,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var candidate = await db.PriceImportCandidates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == candidateId && x.TenantId == db.TenantId, ct);

        if (candidate is null)
            return false;

        var newStatus = status == PriceImportCandidateStatus.Approved && !CanApproveCandidate(candidate, out _)
            ? PriceImportCandidateStatus.NeedsReview
            : status;

        var updated = await db.PriceImportCandidates
            .Where(x => x.Id == candidateId && x.TenantId == db.TenantId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, newStatus), ct);

        if (updated == 0)
            return false;

        await RefreshJobCountsAsync(db, candidate.ImportJobId, ct);
        return true;
    }

    public async Task<bool> UpdateCandidateAsync(PriceImportCandidateUpdateDto candidateUpdate, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var candidate = await db.PriceImportCandidates
            .FirstOrDefaultAsync(x => x.Id == candidateUpdate.Id && x.TenantId == db.TenantId, ct);

        if (candidate is null)
            return false;

        candidate.ArticleNumber = NormalizeOptional(candidateUpdate.ArticleNumber);
        candidate.ProductCode = NormalizeOptional(candidateUpdate.ProductCode);
        candidate.Name = TruncateName(candidateUpdate.Name) ?? "";
        candidate.Description = NormalizeOptional(candidateUpdate.Description);
        candidate.CategoryName = NormalizeOptional(candidateUpdate.CategoryName);
        candidate.BasePrice = candidateUpdate.BasePrice;
        candidate.DiscountPercent = candidateUpdate.DiscountPercent;
        candidate.NetPrice = candidateUpdate.NetPrice;
        candidate.Unit = NormalizeOptional(candidateUpdate.Unit);
        candidate.Currency = string.IsNullOrWhiteSpace(candidateUpdate.Currency) ? "SEK" : candidateUpdate.Currency.Trim();
        candidate.SupplierName = NormalizeOptional(candidateUpdate.SupplierName);
        candidate.SourceText = NormalizeOptional(candidateUpdate.SourceText);

        if (candidate.Status == PriceImportCandidateStatus.Approved && !CanApproveCandidate(candidate, out _))
            candidate.Status = PriceImportCandidateStatus.NeedsReview;

        await db.SaveChangesAsync(ct);
        await RefreshJobCountsAsync(db, candidate.ImportJobId, ct);
        return true;
    }

    public async Task<int> BulkUpdateCandidateStatusAsync(
        IReadOnlyCollection<Guid> candidateIds,
        PriceImportCandidateStatus status,
        CancellationToken ct = default)
    {
        if (candidateIds.Count == 0)
            return 0;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var ids = candidateIds.Distinct().ToList();
        var candidates = await db.PriceImportCandidates
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.TenantId == db.TenantId)
            .ToListAsync(ct);

        var changed = 0;
        var jobIds = new HashSet<Guid>();
        foreach (var candidate in candidates)
        {
            if (status == PriceImportCandidateStatus.Approved && !CanApproveCandidate(candidate, out _))
            {
                candidate.Status = PriceImportCandidateStatus.NeedsReview;
            }
            else
            {
                candidate.Status = status;
            }

            var updated = await db.PriceImportCandidates
                .Where(x => x.Id == candidate.Id && x.TenantId == db.TenantId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, candidate.Status), ct);

            changed += updated;
            jobIds.Add(candidate.ImportJobId);
        }

        foreach (var jobId in jobIds)
            await RefreshJobCountsAsync(db, jobId, ct);

        return changed;
    }

    public async Task<int> ApproveAllReadyCandidatesAsync(Guid jobId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var candidates = await db.PriceImportCandidates
            .AsNoTracking()
            .Where(x => x.ImportJobId == jobId && x.TenantId == db.TenantId && x.Status == PriceImportCandidateStatus.Ready)
            .ToListAsync(ct);

        var approved = 0;
        foreach (var candidate in candidates)
        {
            if (!CanApproveCandidate(candidate, out _))
            {
                await db.PriceImportCandidates
                    .Where(x => x.Id == candidate.Id && x.TenantId == db.TenantId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, PriceImportCandidateStatus.NeedsReview), ct);
                continue;
            }

            approved += await db.PriceImportCandidates
                .Where(x => x.Id == candidate.Id && x.TenantId == db.TenantId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, PriceImportCandidateStatus.Approved), ct);
        }

        await RefreshJobCountsAsync(db, jobId, ct);
        return approved;
    }

    public Task<bool> ApproveCandidateAsync(Guid candidateId, CancellationToken ct = default)
        => ApproveCandidateValidatedAsync(candidateId, ct);

    public Task<bool> IgnoreCandidateAsync(Guid candidateId, CancellationToken ct = default)
        => UpdateCandidateStatusAsync(candidateId, PriceImportCandidateStatus.Ignored, ct);

    private async Task<bool> ApproveCandidateValidatedAsync(Guid candidateId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var candidate = await db.PriceImportCandidates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == candidateId && x.TenantId == db.TenantId, ct);

        if (candidate is null)
            return false;

        var status = CanApproveCandidate(candidate, out _)
            ? PriceImportCandidateStatus.Approved
            : PriceImportCandidateStatus.NeedsReview;

        var updated = await db.PriceImportCandidates
            .Where(x => x.Id == candidateId && x.TenantId == db.TenantId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, status), ct);

        if (updated == 0)
            return false;

        await RefreshJobCountsAsync(db, candidate.ImportJobId, ct);
        return true;
    }

    public async Task<Guid?> CreatePriceListFromApprovedCandidatesAsync(Guid jobId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var job = await db.PriceImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == jobId && x.TenantId == db.TenantId, ct);
        if (job is null)
            return null;

        var approvedCandidates = await db.PriceImportCandidates
            .AsNoTracking()
            .Where(x => x.ImportJobId == jobId && x.TenantId == db.TenantId && x.Status == PriceImportCandidateStatus.Approved)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        if (approvedCandidates.Count == 0)
            return null;

        var now = DateTime.UtcNow;
        var priceListInfo = await db.PriceLists
            .AsNoTracking()
            .Where(x => x.TenantId == db.TenantId
                && x.SourceFileHash == job.SourceFileHash
                && x.SourceFileHash != null)
            .Select(x => new
            {
                x.Id
            })
            .FirstOrDefaultAsync(ct);

        var priceListId = priceListInfo?.Id ?? Guid.NewGuid();
        if (priceListInfo is null)
        {
            db.PriceLists.Add(new PriceList
            {
                Id = priceListId,
                TenantId = db.TenantId,
                Name = $"{job.SupplierName ?? "Imported"} - {job.SourceFileName}",
                SupplierName = job.SupplierName,
                Currency = approvedCandidates.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Currency))?.Currency ?? "SEK",
                SourceFileName = job.SourceFileName,
                SourceFilePath = job.SourceFilePath,
                SourceFileHash = job.SourceFileHash,
                CreatedAt = now,
                ImportedAt = now,
                IsActive = true,
                Note = "Created from approved smart price import candidates."
            });
        }

        var existingItems = await db.PriceListItems
            .AsNoTracking()
            .Where(x => x.TenantId == db.TenantId && x.PriceListId == priceListId)
            .Select(x => new
            {
                x.ArticleNumber,
                x.ProductCode,
                x.Name,
                x.BasePrice,
                x.NetPrice,
                x.Unit
            })
            .ToListAsync(ct);

        var newItems = new List<PriceListItem>();
        foreach (var candidate in approvedCandidates)
        {
            var duplicate = existingItems.Any(x => IsSamePriceListItem(x.ArticleNumber, x.ProductCode, x.Name, x.BasePrice, x.NetPrice, x.Unit, candidate))
                || newItems.Any(x => IsSamePriceListItem(x.ArticleNumber, x.ProductCode, x.Name, x.BasePrice, x.NetPrice, x.Unit, candidate));

            if (!duplicate)
                newItems.Add(CreatePriceListItemFromCandidate(candidate, job, priceListId, db.TenantId, now));
        }

        if (newItems.Count > 0)
            db.PriceListItems.AddRange(newItems);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(
                ex,
                "Concurrency error while converting approved candidates. ImportJobId={ImportJobId}, TenantId={TenantId}, PriceListId={PriceListId}",
                jobId,
                db.TenantId,
                priceListId);

            return null;
        }

        await db.PriceImportJobs
            .Where(x => x.Id == jobId && x.TenantId == db.TenantId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, PriceImportJobStatus.Completed)
                .SetProperty(x => x.CompletedAt, now), ct);

        await db.PriceImportCandidates
            .Where(x => x.ImportJobId == jobId && x.TenantId == db.TenantId && x.Status == PriceImportCandidateStatus.Approved)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, PriceImportCandidateStatus.Imported), ct);

        await RefreshJobCountsAsync(db, jobId, ct);

        logger.LogInformation(
            "Converted approved candidates to price list. ImportJobId={ImportJobId}, TenantId={TenantId}, PriceListId={PriceListId}, CreatedItems={CreatedItems}",
            jobId,
            db.TenantId,
            priceListId,
            newItems.Count);

        return priceListId;
    }

    public async Task<PriceImportAiRunResultDto> RunAiExtractionForJobAsync(Guid importJobId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var job = await db.PriceImportJobs.FirstOrDefaultAsync(x => x.Id == importJobId && x.TenantId == db.TenantId, ct);
        if (job is null)
            return new PriceImportAiRunResultDto(false, 0, 0, 0, "Import job was not found.");

        var sourceCandidates = await db.PriceImportCandidates
            .Where(x => x.ImportJobId == importJobId
                && x.TenantId == db.TenantId
                && (!string.IsNullOrWhiteSpace(x.SourceText) || !string.IsNullOrWhiteSpace(x.RawJson)))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        var blocks = sourceCandidates
            .Select(x => new AiPriceExtractionBlock
            {
                BlockId = $"candidate-{x.Id:N}",
                PageNumber = x.PageNumber,
                SheetName = x.SheetName,
                CellRange = x.CellRange,
                Text = !string.IsNullOrWhiteSpace(x.SourceText) ? x.SourceText! : x.RawJson!
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .Take(120)
            .ToList();

        if (blocks.Count == 0)
            return new PriceImportAiRunResultDto(false, 0, 0, 0, "No source text is available for AI extraction.");

        var request = new AiPriceExtractionRequest
        {
            ImportJobId = job.Id,
            TenantId = db.TenantId,
            SupplierName = job.SupplierName,
            SourceFileName = job.SourceFileName,
            FileType = job.FileType.ToString(),
            Blocks = blocks
        };

        var aiResult = await aiExtractionService.ExtractPricesAsync(request, ct);
        if (!aiResult.Ok)
        {
            var message = aiResult.Errors.Count > 0
                ? string.Join("; ", aiResult.Errors.Take(3))
                : "AI extraction failed.";

            logger.LogWarning(
                "AI extraction returned no usable result for ImportJobId={ImportJobId}. Blocks={BlockCount}. Errors={ErrorCount}",
                importJobId,
                blocks.Count,
                aiResult.Errors.Count);

            return new PriceImportAiRunResultDto(false, blocks.Count, aiResult.Items.Count, 0, ShortError(message));
        }

        var existingCandidates = await db.PriceImportCandidates
            .AsNoTracking()
            .Where(x => x.ImportJobId == importJobId)
            .Where(x => x.TenantId == db.TenantId)
            .Select(x => new
            {
                x.Name,
                x.BasePrice,
                x.NetPrice,
                x.Unit
            })
            .ToListAsync(ct);

        var newCandidates = new List<PriceImportCandidate>();
        foreach (var item in aiResult.Items)
        {
            var candidate = BuildCandidateFromAiItem(item, job, db.TenantId);
            if (candidate is null)
                continue;

            var candidatePrice = candidate.BasePrice ?? candidate.NetPrice;
            var duplicateExists = existingCandidates.Any(x =>
                    SameText(x.Name, candidate.Name)
                    && SameText(x.Unit, candidate.Unit)
                    && (x.BasePrice ?? x.NetPrice) == candidatePrice)
                || newCandidates.Any(x =>
                    SameText(x.Name, candidate.Name)
                    && SameText(x.Unit, candidate.Unit)
                    && (x.BasePrice ?? x.NetPrice) == candidatePrice);

            if (!duplicateExists)
                newCandidates.Add(candidate);
        }

        if (newCandidates.Count > 0)
            db.PriceImportCandidates.AddRange(newCandidates);

        job.Status = PriceImportJobStatus.ReadyForReview;
        job.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await RefreshJobCountsAsync(db, importJobId, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "AI extraction saved candidates for ImportJobId={ImportJobId}. Blocks={BlockCount}, Items={ItemCount}, Created={CreatedCount}",
            importJobId,
            blocks.Count,
            aiResult.Items.Count,
            newCandidates.Count);

        return new PriceImportAiRunResultDto(
            true,
            blocks.Count,
            aiResult.Items.Count,
            newCandidates.Count,
            $"AI extraction completed. Created {newCandidates.Count} new candidates.");
    }

    private static PriceListItem CreatePriceListItemFromCandidate(
        PriceImportCandidate candidate,
        PriceImportJob job,
        Guid priceListId,
        int tenantId,
        DateTime now)
    {
        return new PriceListItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PriceListId = priceListId,
            ArticleNumber = candidate.ArticleNumber,
            ProductCode = candidate.ProductCode,
            Name = candidate.Name,
            Description = candidate.Description,
            CategoryName = candidate.CategoryName,
            BasePrice = candidate.BasePrice,
            DiscountPercent = candidate.DiscountPercent,
            NetPrice = candidate.NetPrice,
            Unit = candidate.Unit,
            Currency = candidate.Currency,
            SupplierName = candidate.SupplierName ?? job.SupplierName,
            SourceFileName = job.SourceFileName,
            SourcePageNumber = candidate.PageNumber,
            SourceSheetName = candidate.SheetName,
            SourceCellRange = candidate.CellRange,
            SourceText = candidate.SourceText,
            Confidence = candidate.Confidence,
            CreatedAt = now,
            IsActive = true
        };
    }

    private static bool IsSamePriceListItem(
        string? articleNumber,
        string? productCode,
        string name,
        decimal? basePrice,
        decimal? netPrice,
        string? unit,
        PriceImportCandidate candidate)
    {
        return SameText(articleNumber, candidate.ArticleNumber)
            && SameText(productCode, candidate.ProductCode)
            && SameText(name, candidate.Name)
            && basePrice == candidate.BasePrice
            && netPrice == candidate.NetPrice
            && SameText(unit, candidate.Unit);
    }

    public async Task<PriceImportDeleteResultDto> DeleteImportJobAsync(Guid jobId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var job = await db.PriceImportJobs
            .FirstOrDefaultAsync(x => x.Id == jobId && x.TenantId == db.TenantId, ct);

        if (job is null)
            return new PriceImportDeleteResultDto(false, "Import job was not found.");

        var sourcePath = job.SourceFilePath;
        var sourceFileName = job.SourceFileName;
        var status = job.Status;

        db.PriceImportJobs.Remove(job);
        await db.SaveChangesAsync(ct);

        var fileDeleted = TryDeleteStoredImportFile(sourcePath);

        logger.LogInformation(
            "Deleted price import job. ImportJobId={ImportJobId}, TenantId={TenantId}, FileName={FileName}, Status={Status}, FileDeleted={FileDeleted}",
            jobId,
            db.TenantId,
            sourceFileName,
            status,
            fileDeleted);

        return new PriceImportDeleteResultDto(
            true,
            fileDeleted
                ? "Import job and stored file were deleted."
                : "Import job was deleted. Stored file was not found or could not be deleted.");
    }

    private static void RefreshJobCounts(PriceImportJob job)
    {
        job.TotalCandidates = job.Candidates.Count;
        job.ReadyCount = job.Candidates.Count(x => x.Status == PriceImportCandidateStatus.Ready);
        job.ReviewCount = job.Candidates.Count(x => x.Status == PriceImportCandidateStatus.NeedsReview);
        job.ErrorCount = job.Candidates.Count(x => x.Status == PriceImportCandidateStatus.Error);
        job.ApprovedCount = job.Candidates.Count(x => x.Status == PriceImportCandidateStatus.Approved);
    }

    private static PriceImportCandidate? BuildCandidateFromAiItem(
        AiExtractedPriceItem item,
        PriceImportJob job,
        int tenantId)
    {
        var name = TruncateName(item.Name);
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (!item.BasePrice.HasValue && !item.NetPrice.HasValue)
            return null;

        var confidence = Math.Clamp(item.Confidence, 0m, 1m);
        var price = item.BasePrice ?? item.NetPrice;
        var hasValidPrice = price is > 0m;
        var hasUnit = !string.IsNullOrWhiteSpace(item.Unit);
        var ready = confidence >= 0.85m && hasValidPrice && hasUnit;

        return new PriceImportCandidate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ImportJobId = job.Id,
            ArticleNumber = NormalizeOptional(item.ArticleNumber),
            ProductCode = NormalizeOptional(item.ProductCode),
            Name = name,
            Description = NormalizeOptional(item.Description),
            CategoryName = NormalizeOptional(item.CategoryName),
            BasePrice = item.BasePrice,
            DiscountPercent = item.DiscountPercent,
            NetPrice = item.NetPrice,
            Unit = NormalizeOptional(item.Unit),
            Currency = string.IsNullOrWhiteSpace(item.Currency) ? "SEK" : item.Currency.Trim(),
            SupplierName = NormalizeOptional(item.SupplierName) ?? job.SupplierName,
            SourceText = NormalizeOptional(item.SourceText),
            RawJson = NormalizeOptional(item.SourceBlockId),
            Confidence = confidence,
            Status = ready ? PriceImportCandidateStatus.Ready : PriceImportCandidateStatus.NeedsReview,
            ErrorMessage = hasValidPrice ? NormalizeOptional(item.Warning) : "AI returned a missing, zero, or negative price.",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static bool SameText(string? left, string? right)
        => string.Equals(
            NormalizeOptional(left) ?? "",
            NormalizeOptional(right) ?? "",
            StringComparison.OrdinalIgnoreCase);

    private static bool CanApproveCandidate(PriceImportCandidate candidate, out string message)
    {
        if (string.IsNullOrWhiteSpace(candidate.Name))
        {
            message = "Name is required before approval.";
            return false;
        }

        var price = candidate.BasePrice ?? candidate.NetPrice;
        if (!price.HasValue)
        {
            message = "BasePrice or NetPrice is required before approval.";
            return false;
        }

        if (price.Value <= 0)
        {
            message = "Price must be greater than zero before approval.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(candidate.Unit))
        {
            message = "Unit is required before approval.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(candidate.Currency))
        {
            message = "Currency is required before approval.";
            return false;
        }

        message = "";
        return true;
    }

    private HashSet<string> GetAllowedExtensions()
    {
        var configured = storageOptions.Value.AllowedExtensions;
        var extensions = configured.Length == 0
            ? [".xlsx", ".xls", ".docx", ".pdf"]
            : configured;

        return extensions
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().StartsWith('.') ? x.Trim() : "." + x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(name))
            return "";

        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (invalidChars.Contains(ch))
            {
                builder.Append('_');
                continue;
            }

            builder.Append(char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' or ' ' ? ch : '_');
        }

        var safe = Regex.Replace(builder.ToString(), "_{2,}", "_").Trim(' ', '.');
        return string.IsNullOrWhiteSpace(safe) ? "price-import-file" : safe;
    }

    private static void TryDeleteFileAndEmptyFolder(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);

            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                Directory.Delete(folder);
        }
        catch
        {
            // Best-effort cleanup only; the import is still blocked by hash.
        }
    }

    private static bool TryDeleteStoredImportFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
                return false;

            File.Delete(fullPath);

            var folder = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                Directory.Delete(folder);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ToUserSafeImportError(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => "Python extraction service is not available. Please make sure the service is running.",
            JsonException => "Extraction service returned invalid JSON.",
            IOException => "The uploaded file could not be read or saved.",
            _ when ex.Message.Contains("No connection could be made", StringComparison.OrdinalIgnoreCase)
                => "Python extraction service is not available. Please make sure the service is running.",
            _ when ex.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase)
                => "Python extraction service is not available. Please make sure the service is running.",
            _ => ex.Message
        };
    }

    private string ResolveStorageRoot()
    {
        var configured = !string.IsNullOrWhiteSpace(storageOptions.Value.RootPath)
            ? storageOptions.Value.RootPath
            : storageOptions.Value.StorageRoot;

        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "App_Data", "PriceImports"));
    }

    private static PriceImportFileType DetectFileType(string extension) => extension.ToLowerInvariant() switch
    {
        ".xlsx" or ".xls" => PriceImportFileType.Excel,
        ".docx" => PriceImportFileType.Word,
        ".pdf" => PriceImportFileType.PdfText,
        _ => PriceImportFileType.Unknown
    };

    private static async Task<string> ComputeSha256Async(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static List<PriceImportCandidate> BuildCandidatesFromExtraction(
        PriceTextExtractionResult extraction,
        PriceImportJob job,
        int tenantId)
    {
        if (extraction.Sheets.Count > 0)
            return BuildExcelCandidates(extraction.Sheets, job, tenantId);

        return BuildTextCandidates(extraction.Pages, job, tenantId);
    }

    private static List<PriceImportCandidate> BuildExcelCandidates(
        List<ExtractedSheet> sheets,
        PriceImportJob job,
        int tenantId)
    {
        var candidates = new List<PriceImportCandidate>();

        foreach (var sheet in sheets)
        {
            if (sheet.Rows.Count == 0)
                continue;

            var headerMatch = FindHeaderRow(sheet.Rows);
            var headers = headerMatch?.Headers ?? [];
            var map = headerMatch?.Map ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var firstDataRow = headerMatch is null ? 0 : headerMatch.RowIndex + 1;

            for (var rowIndex = firstDataRow; rowIndex < sheet.Rows.Count; rowIndex++)
            {
                var row = sheet.Rows[rowIndex];
                if (IsMostlyEmpty(row))
                    continue;

                var sourceText = BuildRowSourceText(headers, row);
                var articleNumber = GetMappedValue(row, map, "article");
                var name = GetMappedValue(row, map, "name");
                var basePrice = TryParseDecimal(GetMappedValue(row, map, "basePrice"));
                var netPrice = TryParseDecimal(GetMappedValue(row, map, "netPrice"));
                var discountPercent = TryParseDecimal(GetMappedValue(row, map, "discount"));
                var unit = GetMappedValue(row, map, "unit");
                var supplierName = GetMappedValue(row, map, "supplier") ?? job.SupplierName;
                var categoryName = GetMappedValue(row, map, "category");
                var hasPrice = basePrice.HasValue || netPrice.HasValue;
                var hasImportantText = IsImportantReviewText(row, sourceText);

                if (string.IsNullOrWhiteSpace(name) && !hasPrice && string.IsNullOrWhiteSpace(articleNumber))
                {
                    if (!hasImportantText)
                        continue;

                    name = TruncateName(sourceText);
                }

                if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(articleNumber))
                    name = articleNumber;

                if (string.IsNullOrWhiteSpace(name))
                    name = TruncateName(sourceText);

                var hasName = !string.IsNullOrWhiteSpace(name);
                var ready = hasName && basePrice.HasValue && !string.IsNullOrWhiteSpace(unit);
                var confidence = ready ? 0.7000m : hasName || hasPrice || !string.IsNullOrWhiteSpace(articleNumber) ? 0.4500m : 0.2500m;

                candidates.Add(new PriceImportCandidate
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ImportJobId = job.Id,
                    ArticleNumber = articleNumber,
                    Name = TruncateName(name) ?? "Excel row",
                    CategoryName = categoryName,
                    BasePrice = basePrice,
                    DiscountPercent = discountPercent,
                    NetPrice = netPrice,
                    Unit = unit,
                    Currency = "SEK",
                    SupplierName = supplierName,
                    SheetName = sheet.SheetName,
                    CellRange = $"Row {rowIndex + 1}",
                    SourceText = sourceText,
                    Confidence = confidence,
                    Status = ready ? PriceImportCandidateStatus.Ready : PriceImportCandidateStatus.NeedsReview,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return candidates;
    }

    private sealed record ExcelHeaderMatch(
        int RowIndex,
        int Score,
        List<string> Headers,
        Dictionary<string, int> Map);

    private static ExcelHeaderMatch? FindHeaderRow(List<List<string>> rows)
    {
        ExcelHeaderMatch? best = null;
        var scanCount = Math.Min(rows.Count, 15);

        for (var rowIndex = 0; rowIndex < scanCount; rowIndex++)
        {
            var row = rows[rowIndex];
            if (IsMostlyEmpty(row))
                continue;

            var map = BuildHeaderMap(row);
            var score = map.Count;
            if (score == 0)
                continue;

            if (best is null || score > best.Score)
                best = new ExcelHeaderMatch(rowIndex, score, row, map);
        }

        return best is { Score: >= 2 } ? best : null;
    }

    private static Dictionary<string, int> BuildHeaderMap(List<string> headers)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < headers.Count; i++)
        {
            var normalized = NormalizeHeader(headers[i]);
            if (MatchesAny(normalized, "artnr", "artikelnr", "artikelnummer", "artikelnr", "artikelno", "artikelid", "articlenumber", "sku", "varunummer"))
                result.TryAdd("article", i);
            else if (MatchesAny(normalized, "name", "namn", "benamning", "produktnamn", "productname", "artikel", "beskrivning", "description"))
                result.TryAdd("name", i);
            else if (MatchesAny(normalized, "pris", "price", "listpris", "bruttopris", "unitprice", "prissek", "rekpris", "rekommenderatpris", "baseprice"))
                result.TryAdd("basePrice", i);
            else if (MatchesAny(normalized, "nettopris", "netto", "netprice", "prisefterrabatt"))
                result.TryAdd("netPrice", i);
            else if (MatchesAny(normalized, "unit", "enhet", "enh", "me", "mattenhet"))
                result.TryAdd("unit", i);
            else if (MatchesAny(normalized, "rabatt", "rabattprocent", "rabatt%", "discount", "discountprocent", "discount%"))
                result.TryAdd("discount", i);
            else if (MatchesAny(normalized, "leverantor", "supplier"))
                result.TryAdd("supplier", i);
            else if (MatchesAny(normalized, "kategori", "category"))
                result.TryAdd("category", i);
        }

        return result;
    }

    private static List<PriceImportCandidate> BuildTextCandidates(
        List<ExtractedTextPage> pages,
        PriceImportJob job,
        int tenantId)
    {
        var candidates = new List<PriceImportCandidate>();

        foreach (var page in pages)
        {
            foreach (var chunk in SplitTextIntoChunks(page.Text))
            {
                candidates.Add(new PriceImportCandidate
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ImportJobId = job.Id,
                    Name = TruncateName(chunk) ?? "Text block",
                    Currency = "SEK",
                    SupplierName = job.SupplierName,
                    PageNumber = page.PageNumber,
                    SourceText = chunk,
                    Confidence = 0.2500m,
                    Status = PriceImportCandidateStatus.NeedsReview,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return candidates;
    }

    private static IEnumerable<string> SplitTextIntoChunks(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var blocks = Regex.Split(text.Trim(), @"(\r?\n){2,}")
            .Select(x => x.Trim())
            .Where(x => x.Length > 0);

        foreach (var block in blocks)
        {
            if (block.Length <= 800)
            {
                yield return block;
                continue;
            }

            for (var i = 0; i < block.Length; i += 800)
                yield return block.Substring(i, Math.Min(800, block.Length - i)).Trim();
        }
    }

    private static string BuildRowSourceText(List<string> headers, List<string> row)
    {
        var parts = new List<string>();
        var max = Math.Max(headers.Count, row.Count);

        for (var i = 0; i < max; i++)
        {
            var header = i < headers.Count && !string.IsNullOrWhiteSpace(headers[i]) ? headers[i] : $"Column {i + 1}";
            var value = i < row.Count ? row[i] : "";
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add($"{header}: {value}");
        }

        return string.Join(" | ", parts);
    }

    private static string? GetMappedValue(List<string> row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var index))
            return null;

        if (index < 0 || index >= row.Count)
            return null;

        return NormalizeOptional(row[index]);
    }

    private static bool IsMostlyEmpty(List<string> row)
    {
        if (row.Count == 0)
            return true;

        var nonEmpty = row.Count(value => !string.IsNullOrWhiteSpace(value));
        return nonEmpty == 0 || nonEmpty <= Math.Max(1, row.Count / 5) && row.Count > 8;
    }

    private static bool IsImportantReviewText(List<string> row, string sourceText)
    {
        var nonEmpty = row.Count(value => !string.IsNullOrWhiteSpace(value));
        return nonEmpty >= 2 || sourceText.Length >= 30;
    }

    private static decimal? TryParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim()
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("SEK", "", StringComparison.OrdinalIgnoreCase);

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out var current))
            return current;

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant))
            return invariant;

        normalized = normalized.Replace(",", ".", StringComparison.Ordinal);
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var fallback)
            ? fallback
            : null;
    }

    private static string NormalizeHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return value.Trim()
            .ToLowerInvariant()
            .Replace("ä", "a", StringComparison.Ordinal)
            .Replace("å", "a", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.Ordinal)
            .Replace(".", "", StringComparison.Ordinal)
            .Replace("_", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal)
            .Replace(" ", "", StringComparison.Ordinal);
    }

    private static bool MatchesAny(string value, params string[] candidates)
        => candidates.Any(candidate => string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase));

    private static string? TruncateName(string? value)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
            return null;

        return normalized.Length <= 80 ? normalized : normalized[..80];
    }

    private static string? Truncate(string? value, int maxLength)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
            return null;

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ShortError(string? message)
    {
        var normalized = string.IsNullOrWhiteSpace(message) ? "Import failed." : message.Trim();
        return normalized.Length <= 1000 ? normalized : normalized[..1000];
    }

    private static void ApplyCounts(PriceImportJob job, IReadOnlyCollection<PriceImportCandidate> candidates)
    {
        job.TotalCandidates = candidates.Count;
        job.ReadyCount = candidates.Count(x => x.Status == PriceImportCandidateStatus.Ready);
        job.ReviewCount = candidates.Count(x => x.Status == PriceImportCandidateStatus.NeedsReview);
        job.ErrorCount = candidates.Count(x => x.Status == PriceImportCandidateStatus.Error);
        job.ApprovedCount = candidates.Count(x => x.Status == PriceImportCandidateStatus.Approved);
    }

    private static async Task RefreshJobCountsAsync(
        Persistence.Context.ShardingSingleDbContext db,
        Guid jobId,
        CancellationToken ct)
    {
        var counts = await db.PriceImportCandidates
            .Where(x => x.ImportJobId == jobId && x.TenantId == db.TenantId)
            .GroupBy(x => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Ready = g.Count(x => x.Status == PriceImportCandidateStatus.Ready),
                Review = g.Count(x => x.Status == PriceImportCandidateStatus.NeedsReview),
                Error = g.Count(x => x.Status == PriceImportCandidateStatus.Error),
                Approved = g.Count(x => x.Status == PriceImportCandidateStatus.Approved)
            })
            .FirstOrDefaultAsync(ct);

        var total = counts?.Total ?? 0;
        var ready = counts?.Ready ?? 0;
        var review = counts?.Review ?? 0;
        var error = counts?.Error ?? 0;
        var approved = counts?.Approved ?? 0;

        await db.PriceImportJobs
            .Where(x => x.Id == jobId && x.TenantId == db.TenantId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.TotalCandidates, total)
                .SetProperty(x => x.ReadyCount, ready)
                .SetProperty(x => x.ReviewCount, review)
                .SetProperty(x => x.ErrorCount, error)
                .SetProperty(x => x.ApprovedCount, approved), ct);
    }
}
