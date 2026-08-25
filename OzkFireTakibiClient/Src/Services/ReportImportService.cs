using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OzkFireTakibiClient.Src.Authorization;
using OzkFireTakibiClient.Src.Data;
using OzkFireTakibiClient.Src.Data.Entities;
using OzkFireTakibiClient.Src.Options;
using OzkFireTakibiClient.Src.ReportImports;

namespace OzkFireTakibiClient.Src.Services;

/// <summary>
/// Yangın/fire takip raporlarının çiftli (aylık kesinleşen ve kümülatif karşılaştırma) içe aktarımı,
/// doğrulanması, sürümlendirilmesi, silinmesi ve detaylı sorgulanmasını yöneten temel iş mantığı servisi.
/// </summary>
public sealed class ReportImportService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ReportImportParser parser,
    IAuthorizationService authorizationService,
    ExcuseAutomationService excuseAutomationService,
    IOptions<ReportImportOptions> options)
{
    private readonly ReportImportOptions _options = options.Value;

    /// <summary>
    /// Aylık ve kümülatif Excel dosyalarını ayrıştırarak veritabanına kaydetmeden önce önizleme ve sürüm durumunu hazırlar.
    /// </summary>
    /// <param name="monthlyFilePath">Aylık kesinleşen Excel dosyasının geçici yolu</param>
    /// <param name="monthlyOriginalFileName">Aylık dosyanın orijinal adı</param>
    /// <param name="cumulativeFilePath">Kümülatif karşılaştırma Excel dosyasının geçici yolu</param>
    /// <param name="cumulativeOriginalFileName">Kümülatif dosyanın orijinal adı</param>
    /// <param name="user">İşlemi yapan kullanıcı kimliği</param>
    /// <param name="cancellationToken">İptal belirteci</param>
    /// <returns>Önizleme ve doğrulama sonuç modeli</returns>
    public async Task<ReportPairImportPreview> PreviewPairAsync(
        string monthlyFilePath,
        string monthlyOriginalFileName,
        string cumulativeFilePath,
        string cumulativeOriginalFileName,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanImportAsync(user);
        var (monthlyReport, cumulativeReport) = await ParsePairAsync(
            monthlyFilePath,
            monthlyOriginalFileName,
            cumulativeFilePath,
            cumulativeOriginalFileName,
            cancellationToken);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reportPeriod = await dbContext.ReportPeriods
            .AsNoTracking()
            .SingleOrDefaultAsync(period =>
                period.CategorySignature == monthlyReport.CategorySignature &&
                period.EndDate == monthlyReport.EndDate,
                cancellationToken);

        var activeImports = Array.Empty<ReportImportEntity>();
        if (reportPeriod is not null)
        {
            activeImports = await dbContext.ReportImports
                .AsNoTracking()
                .Where(reportImport => reportImport.ReportPeriodId == reportPeriod.Id && reportImport.IsActive)
                .ToArrayAsync(cancellationToken);
        }

        var monthlyExisting = await FindExistingPairMemberAsync(
            dbContext,
            reportPeriod,
            monthlyReport,
            cancellationToken);
        var cumulativeExisting = await FindExistingPairMemberAsync(
            dbContext,
            reportPeriod,
            cumulativeReport,
            cancellationToken);

        return new ReportPairImportPreview
        {
            EndDate = monthlyReport.EndDate,
            MonthlyReport = CreatePairFilePreview(
                monthlyReport,
                monthlyOriginalFileName,
                monthlyExisting is not null,
                monthlyExisting is null && activeImports.Any(item => item.PeriodType == ReportPeriodType.Monthly)),
            CumulativeReport = CreatePairFilePreview(
                cumulativeReport,
                cumulativeOriginalFileName,
                cumulativeExisting is not null,
                cumulativeExisting is null && activeImports.Any(item => item.PeriodType == ReportPeriodType.Cumulative))
        };
    }

    /// <summary>
    /// Aylık ve kümülatif Excel dosyalarını tek bir atomik veritabanı işlemi (Transaction) içinde sisteme kaydeder.
    /// Eski aktif sürümleri pasifleştirir ve yeni satır verilerini ekler.
    /// </summary>
    /// <returns>İçe aktarma işlem sonucu (kaydedilen ID'ler ve değişiklik bayrakları)</returns>
    public async Task<ReportPairImportResult> ImportPairAsync(
        string monthlyFilePath,
        string monthlyOriginalFileName,
        string cumulativeFilePath,
        string cumulativeOriginalFileName,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanImportAsync(user);
        var uploadedByUserId = GetUserId(user);
        var (monthlyReport, cumulativeReport) = await ParsePairAsync(
            monthlyFilePath,
            monthlyOriginalFileName,
            cumulativeFilePath,
            cumulativeOriginalFileName,
            cancellationToken);
        var now = DateTime.UtcNow;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var reportPeriod = await dbContext.ReportPeriods
            .SingleOrDefaultAsync(period =>
                period.CategorySignature == monthlyReport.CategorySignature &&
                period.EndDate == monthlyReport.EndDate,
                cancellationToken);

        if (reportPeriod is null)
        {
            reportPeriod = new ReportPeriodEntity
            {
                CategorySignature = monthlyReport.CategorySignature,
                EndDate = monthlyReport.EndDate,
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.ReportPeriods.Add(reportPeriod);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var monthlySave = await SavePairMemberAsync(
            dbContext,
            reportPeriod,
            monthlyReport,
            monthlyOriginalFileName,
            uploadedByUserId,
            now,
            cancellationToken);
        var cumulativeSave = await SavePairMemberAsync(
            dbContext,
            reportPeriod,
            cumulativeReport,
            cumulativeOriginalFileName,
            uploadedByUserId,
            now,
            cancellationToken);

        if (!monthlySave.Changed && !cumulativeSave.Changed)
        {
            throw new ReportImportValidationException("Seçilen iki Excel de bu dönemin aktif raporlarıyla birebir aynı.");
        }

        var generatedExcuseCount = 0;
        if (monthlySave.Changed)
        {
            generatedExcuseCount = await excuseAutomationService.GenerateAsync(
                dbContext,
                monthlySave.ReportImport,
                cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return new ReportPairImportResult
        {
            ReportPeriodId = reportPeriod.Id,
            MonthlyImportId = monthlySave.ReportImport.Id,
            CumulativeImportId = cumulativeSave.ReportImport.Id,
            MonthlyReportChanged = monthlySave.Changed,
            CumulativeReportChanged = cumulativeSave.Changed,
            GeneratedExcuseCount = generatedExcuseCount
        };
    }

    /// <summary>
    /// En son yüklenen raporların geçmiş listesini (HistoryPageSize kadar) döndürür.
    /// </summary>
    public async Task<IReadOnlyList<ReportImportHistoryItem>> GetHistoryAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Rapor geçmişini görüntülemek için oturum açılmalıdır.");
        }


        var historyPageSize = Math.Clamp(_options.HistoryPageSize, 1, 200);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.ReportImports
            .AsNoTracking()
            .OrderByDescending(reportImport => reportImport.CreatedAt)
            .Take(historyPageSize)
            .Select(reportImport => new ReportImportHistoryItem
            {
                Id = reportImport.Id,
                ReportPeriodId = reportImport.ReportPeriodId,
                OriginalFileName = reportImport.OriginalFileName,
                PeriodType = reportImport.PeriodType,
                StartDate = reportImport.StartDate,
                EndDate = reportImport.EndDate,
                IsActive = reportImport.IsActive,
                TotalRowCount = reportImport.TotalRowCount,
                UploadedBy = reportImport.UploadedByUser.Name ?? reportImport.UploadedByUser.Email,
                ImportedAtUtc = reportImport.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Tanımlı rapor dönemlerini ve her döneme ait aktif aylık/kümülatif raporların durumunu getirir.
    /// </summary>
    public async Task<IReadOnlyList<ReportPeriodOverviewItem>> GetReportPeriodsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Rapor dönemlerini görüntülemek için oturum açılmalıdır.");
        }

        var historyPageSize = Math.Clamp(_options.HistoryPageSize, 1, 200);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var periods = await dbContext.ReportPeriods
            .AsNoTracking()
            .Include(period => period.Imports.Where(reportImport => reportImport.IsActive))
            .OrderByDescending(period => period.EndDate)
            .ThenBy(period => period.Id)
            .Take(historyPageSize)
            .ToListAsync(cancellationToken);

        return periods.Select(period => new ReportPeriodOverviewItem
        {
            Id = period.Id,
            EndDate = period.EndDate,
            MonthlyReport = CreatePeriodFileItem(period.Imports, ReportPeriodType.Monthly),
            CumulativeReport = CreatePeriodFileItem(period.Imports, ReportPeriodType.Cumulative)
        }).ToArray();
    }

    /// <summary>
    /// Analiz ekranı için aktif aylık raporları Excel'deki genel sonuç satırıyla birlikte getirir.
    /// </summary>
    public async Task<MonthlyAnalysisResult> GetMonthlyAnalysesAsync(
        MonthlyAnalysisFilter? filters,
        int pageNumber,
        int pageSize,
        bool includeComparison,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(user);

        pageSize = Math.Clamp(pageSize, 10, 100);
        pageNumber = Math.Max(1, pageNumber);
        var normalizedFilters = NormalizeMonthlyAnalysisFilters(filters);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var availableYears = await dbContext.ReportImports
            .AsNoTracking()
            .Where(item => item.PeriodType == ReportPeriodType.Monthly && item.IsActive)
            .Select(item => item.EndDate.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToArrayAsync(cancellationToken);

        var hasComparisonData = await dbContext.ReportPeriods
            .AsNoTracking()
            .AnyAsync(period =>
                period.Imports.Any(item => item.PeriodType == ReportPeriodType.Monthly && item.IsActive) &&
                period.Imports.Any(item => item.PeriodType == ReportPeriodType.Cumulative && item.IsActive),
                cancellationToken);

        includeComparison = includeComparison && hasComparisonData;
        if (!includeComparison)
        {
            normalizedFilters.ComparisonFilter = ReportDetailComparisonFilter.All;
        }

        var query = dbContext.ReportRows
            .AsNoTracking()
            .Include(row => row.ReportImport)
            .Where(row =>
                row.RowType == ReportRowType.General &&
                row.ReportImport.PeriodType == ReportPeriodType.Monthly &&
                row.ReportImport.IsActive);

        if (normalizedFilters.SearchText.Length > 0)
        {
            var searchText = normalizedFilters.SearchText;
            query = query.Where(row => row.ReportImport.OriginalFileName.Contains(searchText));
        }

        if (normalizedFilters.Year.HasValue)
        {
            query = query.Where(row => row.ReportImport.EndDate.Year == normalizedFilters.Year.Value);
        }

        if (normalizedFilters.Month.HasValue)
        {
            query = query.Where(row => row.ReportImport.EndDate.Month == normalizedFilters.Month.Value);
        }

        query = normalizedFilters.WasteFilter switch
        {
            ReportDetailWasteFilter.Loss => query.Where(row => row.WasteRate < 0m || row.WasteAmount < 0m),
            ReportDetailWasteFilter.NoLoss => query.Where(row =>
                (row.WasteRate == null || row.WasteRate >= 0m) &&
                (row.WasteAmount == null || row.WasteAmount >= 0m) &&
                (row.WasteRate != null || row.WasteAmount != null)),
            _ => query
        };

        if (normalizedFilters.MinimumWasteRate.HasValue)
        {
            query = query.Where(row => row.WasteRate >= normalizedFilters.MinimumWasteRate.Value);
        }

        if (normalizedFilters.MaximumWasteRate.HasValue)
        {
            query = query.Where(row => row.WasteRate <= normalizedFilters.MaximumWasteRate.Value);
        }

        if (normalizedFilters.ComparisonFilter == ReportDetailComparisonFilter.WorseThanCumulative)
        {
            query = query.Where(monthly =>
                monthly.WasteRate < 0m &&
                dbContext.ReportRows.Any(cumulative =>
                    cumulative.RowType == ReportRowType.General &&
                    cumulative.ReportImport.PeriodType == ReportPeriodType.Cumulative &&
                    cumulative.ReportImport.IsActive &&
                    cumulative.ReportImport.ReportPeriodId == monthly.ReportImport.ReportPeriodId &&
                    cumulative.WasteRate != null &&
                    monthly.WasteRate < cumulative.WasteRate));
        }

        var totalRowCount = await query.CountAsync(cancellationToken);
        var totalPageCount = Math.Max(1, (int)Math.Ceiling(totalRowCount / (double)pageSize));
        pageNumber = Math.Min(pageNumber, totalPageCount);

        var orderedQuery = normalizedFilters.Sort switch
        {
            MonthlyAnalysisSort.OldestPeriod => query
                .OrderBy(row => row.ReportImport.EndDate)
                .ThenBy(row => row.ReportImport.OriginalFileName),
            MonthlyAnalysisSort.WorstWasteRate => query
                .OrderBy(row => row.WasteRate == null)
                .ThenBy(row => row.WasteRate)
                .ThenByDescending(row => row.ReportImport.EndDate),
            MonthlyAnalysisSort.WorstWasteAmount => query
                .OrderBy(row => row.WasteAmount == null)
                .ThenBy(row => row.WasteAmount)
                .ThenByDescending(row => row.ReportImport.EndDate),
            MonthlyAnalysisSort.HighestSalesAmount => query
                .OrderBy(row => row.StoreSalesAmount == null)
                .ThenByDescending(row => row.StoreSalesAmount)
                .ThenByDescending(row => row.ReportImport.EndDate),
            _ => query
                .OrderByDescending(row => row.ReportImport.EndDate)
                .ThenBy(row => row.ReportImport.OriginalFileName)
        };

        var rows = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        IReadOnlyDictionary<long, ReportRowEntity> comparisonRows =
            new Dictionary<long, ReportRowEntity>();
        if (includeComparison && rows.Length > 0)
        {
            var reportPeriodIds = rows
                .Select(row => row.ReportImport.ReportPeriodId)
                .Distinct()
                .ToArray();
            comparisonRows = (await dbContext.ReportRows
                    .AsNoTracking()
                    .Include(row => row.ReportImport)
                    .Where(row =>
                        row.RowType == ReportRowType.General &&
                        row.ReportImport.PeriodType == ReportPeriodType.Cumulative &&
                        row.ReportImport.IsActive &&
                        reportPeriodIds.Contains(row.ReportImport.ReportPeriodId))
                    .ToArrayAsync(cancellationToken))
                .ToDictionary(row => row.ReportImport.ReportPeriodId);
        }

        return new MonthlyAnalysisResult
        {
            Filters = normalizedFilters,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRowCount = totalRowCount,
            AvailableYears = availableYears,
            HasComparisonData = hasComparisonData,
            IncludeComparison = includeComparison,
            Rows = rows.Select(row =>
            {
                comparisonRows.TryGetValue(row.ReportImport.ReportPeriodId, out var comparisonRow);
                return CreateMonthlyAnalysisItem(row, comparisonRow);
            }).ToArray()
        };
    }

    /// <summary>
    /// Belirtilen raporun detay bilgilerini, satır tipi filtrelemesini, arama kriterlerini, sayfalama ve kümülatif karşılaştırma verilerini sorgular.
    /// </summary>
    public async Task<ReportDetailResult> GetDetailAsync(
        long reportImportId,
        ReportRowType rowType,
        ReportDetailFilter? filters,
        int pageNumber,
        int pageSize,
        bool includeComparison,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(user);

        pageSize = Math.Clamp(pageSize, 10, 100);
        pageNumber = Math.Max(1, pageNumber);
        var normalizedFilters = NormalizeDetailFilters(filters, rowType);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reportImport = await dbContext.ReportImports
            .AsNoTracking()
            .Include(item => item.UploadedByUser)
            .SingleOrDefaultAsync(item => item.Id == reportImportId, cancellationToken)
            ?? throw new ReportNotFoundException(reportImportId);

        ReportImportEntity? comparisonImport = null;
        if (reportImport.PeriodType == ReportPeriodType.Monthly)
        {
            comparisonImport = await dbContext.ReportImports
                .AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.ReportPeriodId == reportImport.ReportPeriodId &&
                    item.PeriodType == ReportPeriodType.Cumulative &&
                    item.IsActive,
                    cancellationToken);
        }

        includeComparison = includeComparison && comparisonImport is not null;
        if (!includeComparison)
        {
            normalizedFilters.ComparisonFilter = ReportDetailComparisonFilter.All;
        }

        var generalRow = await dbContext.ReportRows
            .AsNoTracking()
            .SingleOrDefaultAsync(row =>
                row.ReportImportId == reportImportId && row.RowType == ReportRowType.General,
                cancellationToken);

        ReportRowEntity? comparisonGeneralRow = null;
        if (includeComparison)
        {
            comparisonGeneralRow = await dbContext.ReportRows
                .AsNoTracking()
                .SingleOrDefaultAsync(row =>
                    row.ReportImportId == comparisonImport!.Id && row.RowType == ReportRowType.General,
                    cancellationToken);
        }

        var query = dbContext.ReportRows
            .AsNoTracking()
            .Where(row => row.ReportImportId == reportImportId && row.RowType == rowType);

        if (normalizedFilters.SearchText.Length > 0)
        {
            var searchText = normalizedFilters.SearchText;
            var hasStoreNumber = int.TryParse(searchText, out var storeNumber);
            query = query.Where(row =>
                (hasStoreNumber && row.StoreNumber == storeNumber) ||
                (row.StoreName != null && row.StoreName.Contains(searchText)) ||
                (row.CategoryCode != null && row.CategoryCode.Contains(searchText)) ||
                (row.CategoryName != null && row.CategoryName.Contains(searchText)) ||
                (row.StockCode != null && row.StockCode.Contains(searchText)) ||
                (row.StockName != null && row.StockName.Contains(searchText)) ||
                (row.AlternativeName != null && row.AlternativeName.Contains(searchText)));
        }

        if (normalizedFilters.StoreText.Length > 0)
        {
            var storeText = normalizedFilters.StoreText;
            var hasStoreNumber = int.TryParse(storeText, out var storeNumber);
            query = query.Where(row =>
                (hasStoreNumber && row.StoreNumber == storeNumber) ||
                (row.StoreName != null && row.StoreName.Contains(storeText)));
        }

        if (normalizedFilters.CategoryText.Length > 0)
        {
            var categoryText = normalizedFilters.CategoryText;
            query = query.Where(row =>
                (row.CategoryCode != null && row.CategoryCode.Contains(categoryText)) ||
                (row.CategoryName != null && row.CategoryName.Contains(categoryText)));
        }

        if (normalizedFilters.ProductText.Length > 0)
        {
            var productText = normalizedFilters.ProductText;
            query = query.Where(row =>
                (row.StockCode != null && row.StockCode.Contains(productText)) ||
                (row.StockName != null && row.StockName.Contains(productText)) ||
                (row.AlternativeName != null && row.AlternativeName.Contains(productText)));
        }

        query = normalizedFilters.WasteFilter switch
        {
            ReportDetailWasteFilter.Loss => query.Where(row => row.WasteRate < 0m || row.WasteAmount < 0m),
            ReportDetailWasteFilter.NoLoss => query.Where(row =>
                (row.WasteRate == null || row.WasteRate >= 0m) &&
                (row.WasteAmount == null || row.WasteAmount >= 0m) &&
                (row.WasteRate != null || row.WasteAmount != null)),
            _ => query
        };

        if (normalizedFilters.MinimumWasteRate.HasValue)
        {
            query = query.Where(row => row.WasteRate >= normalizedFilters.MinimumWasteRate.Value);
        }

        if (normalizedFilters.MaximumWasteRate.HasValue)
        {
            query = query.Where(row => row.WasteRate <= normalizedFilters.MaximumWasteRate.Value);
        }

        if (normalizedFilters.ComparisonFilter == ReportDetailComparisonFilter.WorseThanCumulative)
        {
            var comparisonQuery = dbContext.ReportRows
                .AsNoTracking()
                .Where(row =>
                    row.ReportImportId == comparisonImport!.Id &&
                    row.RowType == rowType &&
                    row.WasteRate != null);

            query = ApplyComparisonWasteFilter(query, comparisonQuery, rowType);
        }

        var totalRowCount = await query.CountAsync(cancellationToken);
        var totalPageCount = Math.Max(1, (int)Math.Ceiling(totalRowCount / (double)pageSize));
        pageNumber = Math.Min(pageNumber, totalPageCount);

        var orderedQuery = normalizedFilters.Sort switch
        {
            ReportDetailSort.WorstWasteRate => query
                .OrderBy(row => row.WasteRate == null)
                .ThenBy(row => row.WasteRate)
                .ThenBy(row => row.SourceRowNumber),
            ReportDetailSort.WorstWasteAmount => query
                .OrderBy(row => row.WasteAmount == null)
                .ThenBy(row => row.WasteAmount)
                .ThenBy(row => row.SourceRowNumber),
            ReportDetailSort.HighestSalesAmount => query
                .OrderBy(row => row.StoreSalesAmount == null)
                .ThenByDescending(row => row.StoreSalesAmount)
                .ThenBy(row => row.SourceRowNumber),
            _ => query.OrderBy(row => row.SourceRowNumber)
        };

        var rows = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        IReadOnlyDictionary<string, ReportRowEntity> comparisonRows =
            new Dictionary<string, ReportRowEntity>(StringComparer.OrdinalIgnoreCase);
        if (includeComparison)
        {
            comparisonRows = (await dbContext.ReportRows
                    .AsNoTracking()
                    .Where(row => row.ReportImportId == comparisonImport!.Id && row.RowType == rowType)
                    .ToListAsync(cancellationToken))
                .ToDictionary(row => CreateNaturalKey(row, rowType), StringComparer.OrdinalIgnoreCase);
        }

        return new ReportDetailResult
        {
            Header = new ReportDetailHeader
            {
                Id = reportImport.Id,
                ReportPeriodId = reportImport.ReportPeriodId,
                OriginalFileName = reportImport.OriginalFileName,
                PeriodType = reportImport.PeriodType,
                StartDate = reportImport.StartDate,
                EndDate = reportImport.EndDate,
                IsActive = reportImport.IsActive,
                TotalRowCount = reportImport.TotalRowCount,
                GeneralRowCount = reportImport.GeneralRowCount,
                CategorySummaryRowCount = reportImport.CategorySummaryRowCount,
                StoreSummaryRowCount = reportImport.StoreSummaryRowCount,
                StoreCategoryRowCount = reportImport.StoreCategoryRowCount,
                ProductSummaryRowCount = reportImport.ProductSummaryRowCount,
                StoreProductRowCount = reportImport.StoreProductRowCount,
                UploadedBy = reportImport.UploadedByUser.Name ?? reportImport.UploadedByUser.Email,
                ImportedAtUtc = reportImport.CreatedAt,
                GeneralSummary = generalRow is null
                    ? null
                    : CreateDetailRowItem(generalRow, comparisonGeneralRow),
                ComparisonSource = comparisonImport is null
                    ? null
                    : new ReportComparisonSource
                    {
                        ImportId = comparisonImport.Id,
                        OriginalFileName = comparisonImport.OriginalFileName,
                        StartDate = comparisonImport.StartDate,
                        EndDate = comparisonImport.EndDate
                    }
            },
            RowType = rowType,
            Filters = normalizedFilters,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRowCount = totalRowCount,
            Rows = rows.Select(row =>
            {
                comparisonRows.TryGetValue(CreateNaturalKey(row, rowType), out var comparisonRow);
                return CreateDetailRowItem(row, comparisonRow);
            }).ToArray(),
            IncludeComparison = includeComparison
        };
    }

    /// <summary>
    /// Raporu ve tüm detay satırlarını güvenli şekilde siler.
    /// Silinen rapor aktif bir sürüm ise önceki en güncel sürümü otomatik olarak yeniden aktif eder.
    /// Dönemde başka rapor kalmazsa dönemi de temizler.
    /// </summary>
    public async Task<ReportDeleteResult> DeleteAsync(
        long reportImportId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanDeleteAsync(user);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var reportImport = await dbContext.ReportImports
            .SingleOrDefaultAsync(item => item.Id == reportImportId, cancellationToken)
            ?? throw new ReportNotFoundException(reportImportId);

        var deletedActiveVersion = reportImport.IsActive;
        ReportImportEntity? previousVersion = null;

        if (deletedActiveVersion)
        {
            reportImport.IsActive = false;
            reportImport.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            var previousVersions = dbContext.ReportImports
                .Where(item =>
                    item.Id != reportImportId &&
                    item.ReportPeriodId == reportImport.ReportPeriodId &&
                    item.PeriodType == reportImport.PeriodType);

            var activeCounterpart = await dbContext.ReportImports
                .SingleOrDefaultAsync(item =>
                    item.ReportPeriodId == reportImport.ReportPeriodId &&
                    item.PeriodType == CounterpartOf(reportImport.PeriodType) &&
                    item.IsActive,
                    cancellationToken);

            if (activeCounterpart is not null)
            {
                previousVersions = reportImport.PeriodType == ReportPeriodType.Monthly
                    ? previousVersions.Where(item => item.StartDate > activeCounterpart.StartDate)
                    : previousVersions.Where(item => item.StartDate < activeCounterpart.StartDate);
            }

            previousVersion = await previousVersions
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (previousVersion is not null)
            {
                previousVersion.IsActive = true;
                previousVersion.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);

                if (reportImport.PeriodType == ReportPeriodType.Monthly)
                {
                    await excuseAutomationService.RestoreSupersededAsync(
                        dbContext,
                        previousVersion.Id,
                        reportImport.Id,
                        cancellationToken);
                }
            }
        }

        var reportPeriodId = reportImport.ReportPeriodId;
        dbContext.ReportImports.Remove(reportImport);
        await dbContext.SaveChangesAsync(cancellationToken);

        var deletedEmptyReportPeriod = !await dbContext.ReportImports
            .AnyAsync(item => item.ReportPeriodId == reportPeriodId, cancellationToken);

        if (deletedEmptyReportPeriod)
        {
            var reportPeriod = await dbContext.ReportPeriods
                .SingleAsync(item => item.Id == reportPeriodId, cancellationToken);
            dbContext.ReportPeriods.Remove(reportPeriod);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return new ReportDeleteResult
        {
            DeletedActiveVersion = deletedActiveVersion,
            ReactivatedPreviousVersion = previousVersion is not null,
            DeletedEmptyReportPeriod = deletedEmptyReportPeriod
        };
    }

    private async Task EnsureCanImportAsync(ClaimsPrincipal user)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            user,
            resource: null,
            ReportPolicies.CanImportReports);

        if (!authorizationResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Excel raporu yüklemek için yetkiniz bulunmuyor.");
        }
    }

    private async Task EnsureCanDeleteAsync(ClaimsPrincipal user)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            user,
            resource: null,
            ReportPolicies.CanDeleteReports);

        if (!authorizationResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Rapor silmek için Admin yetkisi gereklidir.");
        }
    }

    private static MonthlyAnalysisFilter NormalizeMonthlyAnalysisFilters(MonthlyAnalysisFilter? filters)
    {
        filters ??= new MonthlyAnalysisFilter();
        var minimumWasteRate = filters.MinimumWasteRate;
        var maximumWasteRate = filters.MaximumWasteRate;
        if (minimumWasteRate.HasValue && maximumWasteRate.HasValue && minimumWasteRate > maximumWasteRate)
        {
            (minimumWasteRate, maximumWasteRate) = (maximumWasteRate, minimumWasteRate);
        }

        return new MonthlyAnalysisFilter
        {
            SearchText = NormalizeFilterText(filters.SearchText, 100),
            Year = filters.Year is >= 1 and <= 9999 ? filters.Year : null,
            Month = filters.Month is >= 1 and <= 12 ? filters.Month : null,
            WasteFilter = Enum.IsDefined(filters.WasteFilter)
                ? filters.WasteFilter
                : ReportDetailWasteFilter.All,
            ComparisonFilter = Enum.IsDefined(filters.ComparisonFilter)
                ? filters.ComparisonFilter
                : ReportDetailComparisonFilter.All,
            MinimumWasteRate = minimumWasteRate,
            MaximumWasteRate = maximumWasteRate,
            Sort = Enum.IsDefined(filters.Sort)
                ? filters.Sort
                : MonthlyAnalysisSort.NewestPeriod
        };
    }

    private static MonthlyAnalysisItem CreateMonthlyAnalysisItem(
        ReportRowEntity row,
        ReportRowEntity? comparisonRow = null) => new()
    {
        ImportId = row.ReportImportId,
        OriginalFileName = row.ReportImport.OriginalFileName,
        StartDate = row.ReportImport.StartDate,
        EndDate = row.ReportImport.EndDate,
        StoreSalesAmount = row.StoreSalesAmount,
        CostOfSales = row.CostOfSales,
        WasteRate = row.WasteRate,
        WasteQuantity = row.WasteQuantity,
        WasteAmount = row.WasteAmount,
        ProfitRate = row.ProfitRate,
        ProfitAmount = row.ProfitAmount,
        Comparison = comparisonRow is null ? null : CreateMonthlyAnalysisItem(comparisonRow)
    };

    private static ReportDetailFilter NormalizeDetailFilters(ReportDetailFilter? filters, ReportRowType rowType)
    {
        filters ??= new ReportDetailFilter();
        if (rowType == ReportRowType.General)
        {
            return new ReportDetailFilter();
        }

        var minimumWasteRate = filters.MinimumWasteRate;
        var maximumWasteRate = filters.MaximumWasteRate;
        var supportsStore = rowType is
            ReportRowType.StoreSummary or ReportRowType.StoreCategory or ReportRowType.StoreProduct;
        var supportsCategory = rowType is
            ReportRowType.CategorySummary or ReportRowType.StoreCategory or ReportRowType.StoreProduct;
        var supportsProduct = rowType is
            ReportRowType.ProductSummary or ReportRowType.StoreProduct;

        if (minimumWasteRate.HasValue && maximumWasteRate.HasValue && minimumWasteRate > maximumWasteRate)
        {
            (minimumWasteRate, maximumWasteRate) = (maximumWasteRate, minimumWasteRate);
        }

        return new ReportDetailFilter
        {
            SearchText = NormalizeFilterText(filters.SearchText, 100),
            StoreText = supportsStore ? NormalizeFilterText(filters.StoreText, 80) : string.Empty,
            CategoryText = supportsCategory ? NormalizeFilterText(filters.CategoryText, 80) : string.Empty,
            ProductText = supportsProduct ? NormalizeFilterText(filters.ProductText, 80) : string.Empty,
            WasteFilter = Enum.IsDefined(filters.WasteFilter)
                ? filters.WasteFilter
                : ReportDetailWasteFilter.All,
            ComparisonFilter = Enum.IsDefined(filters.ComparisonFilter)
                ? filters.ComparisonFilter
                : ReportDetailComparisonFilter.All,
            MinimumWasteRate = minimumWasteRate,
            MaximumWasteRate = maximumWasteRate,
            Sort = Enum.IsDefined(filters.Sort)
                ? filters.Sort
                : ReportDetailSort.SourceOrder
        };
    }

    private static IQueryable<ReportRowEntity> ApplyComparisonWasteFilter(
        IQueryable<ReportRowEntity> monthlyRows,
        IQueryable<ReportRowEntity> cumulativeRows,
        ReportRowType rowType)
    {
        monthlyRows = monthlyRows.Where(monthly => monthly.WasteRate < 0m);

        return rowType switch
        {
            ReportRowType.General => monthlyRows.Where(monthly =>
                cumulativeRows.Any(cumulative => monthly.WasteRate < cumulative.WasteRate)),
            ReportRowType.CategorySummary => monthlyRows.Where(monthly =>
                cumulativeRows.Any(cumulative =>
                    cumulative.CategoryCode == monthly.CategoryCode &&
                    monthly.WasteRate < cumulative.WasteRate)),
            ReportRowType.StoreSummary => monthlyRows.Where(monthly =>
                cumulativeRows.Any(cumulative =>
                    cumulative.StoreNumber == monthly.StoreNumber &&
                    monthly.WasteRate < cumulative.WasteRate)),
            ReportRowType.StoreCategory => monthlyRows.Where(monthly =>
                cumulativeRows.Any(cumulative =>
                    cumulative.StoreNumber == monthly.StoreNumber &&
                    cumulative.CategoryCode == monthly.CategoryCode &&
                    monthly.WasteRate < cumulative.WasteRate)),
            ReportRowType.ProductSummary => monthlyRows.Where(monthly =>
                cumulativeRows.Any(cumulative =>
                    cumulative.StockCode == monthly.StockCode &&
                    monthly.WasteRate < cumulative.WasteRate)),
            ReportRowType.StoreProduct => monthlyRows.Where(monthly =>
                cumulativeRows.Any(cumulative =>
                    cumulative.StoreNumber == monthly.StoreNumber &&
                    cumulative.CategoryCode == monthly.CategoryCode &&
                    cumulative.StockCode == monthly.StockCode &&
                    monthly.WasteRate < cumulative.WasteRate)),
            _ => monthlyRows
        };
    }

    private static string NormalizeFilterText(string? value, int maximumLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    private static void EnsureAuthenticated(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Rapor detaylarını görüntülemek için oturum açılmalıdır.");
        }
    }

    private void ValidateFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new ReportImportValidationException("Yüklenecek geçici dosya bulunamadı.");
        }

        if (fileInfo.Length == 0)
        {
            throw new ReportImportValidationException("Boş dosya yüklenemez.");
        }

        if (fileInfo.Length > _options.MaxFileSizeBytes)
        {
            throw new ReportImportValidationException(
                $"Dosya boyutu izin verilen {_options.MaxFileSizeBytes / (1024 * 1024)} MB sınırını aşıyor.");
        }
    }

    private static int GetUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException("Oturumdaki kullanıcı kimliği çözümlenemedi.");
        }

        return userId;
    }

    private static string NormalizeFileName(string originalFileName)
    {
        var fileName = Path.GetFileName(originalFileName).Trim();
        if (fileName.Length == 0)
        {
            throw new ReportImportValidationException("Dosya adı boş olamaz.");
        }

        return fileName.Length <= 260 ? fileName : fileName[..260];
    }

    private async Task<(ParsedReport MonthlyReport, ParsedReport CumulativeReport)> ParsePairAsync(
        string monthlyFilePath,
        string monthlyOriginalFileName,
        string cumulativeFilePath,
        string cumulativeOriginalFileName,
        CancellationToken cancellationToken)
    {
        ValidateFile(monthlyFilePath);
        ValidateFile(cumulativeFilePath);

        var monthlyTask = parser.ParseAsync(monthlyFilePath, monthlyOriginalFileName, cancellationToken);
        var cumulativeTask = parser.ParseAsync(cumulativeFilePath, cumulativeOriginalFileName, cancellationToken);
        await Task.WhenAll(monthlyTask, cumulativeTask);

        var monthlyReport = await monthlyTask;
        var cumulativeReport = await cumulativeTask;
        ValidateExpectedPeriodType(monthlyReport.PeriodType, ReportPeriodType.Monthly);
        ValidateExpectedPeriodType(cumulativeReport.PeriodType, ReportPeriodType.Cumulative);
        ValidatePair(monthlyReport, cumulativeReport);

        return (monthlyReport, cumulativeReport);
    }

    private static void ValidatePair(ParsedReport monthlyReport, ParsedReport cumulativeReport)
    {
        if (!string.Equals(
                monthlyReport.CategorySignature,
                cumulativeReport.CategorySignature,
                StringComparison.Ordinal))
        {
            throw new ReportImportValidationException(
                "Aylık ve kümülatif Excel'lerdeki kategori kodları aynı olmalıdır.");
        }

        if (monthlyReport.EndDate != cumulativeReport.EndDate)
        {
            throw new ReportImportValidationException(
                "Aylık ve kümülatif Excel'in dönem sonu tarihleri aynı olmalıdır.");
        }

        if (cumulativeReport.StartDate >= monthlyReport.StartDate)
        {
            throw new ReportImportValidationException(
                "Kümülatif karşılaştırma raporunun başlangıç tarihi aylık raporun başlangıç tarihinden önce olmalıdır.");
        }
    }

    private static async Task<ReportImportEntity?> FindExistingPairMemberAsync(
        AppDbContext dbContext,
        ReportPeriodEntity? reportPeriod,
        ParsedReport report,
        CancellationToken cancellationToken)
    {
        var existingImport = await dbContext.ReportImports
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.FileHash == report.FileHash, cancellationToken);

        if (existingImport is null)
        {
            return null;
        }

        if (!existingImport.IsActive)
        {
            throw new ReportImportValidationException(
                $"'{existingImport.OriginalFileName}' içeriği daha önce eski sürüm olarak kaydedilmiş. Güncel iki Excel'i seçin.");
        }

        if (reportPeriod is null ||
            existingImport.ReportPeriodId != reportPeriod.Id ||
            existingImport.PeriodType != report.PeriodType ||
            existingImport.EndDate != report.EndDate)
        {
            throw new ReportImportValidationException(
                "Seçilen Excel içeriği farklı bir rapor döneminde kayıtlı.");
        }

        return existingImport;
    }

    private static ReportPairFilePreview CreatePairFilePreview(
        ParsedReport report,
        string originalFileName,
        bool isAlreadyActive,
        bool replacesActiveVersion)
    {
        return new ReportPairFilePreview
        {
            OriginalFileName = NormalizeFileName(originalFileName),
            PeriodType = report.PeriodType,
            StartDate = report.StartDate,
            EndDate = report.EndDate,
            TotalRowCount = report.Rows.Count,
            GeneralRowCount = report.Count(ReportRowType.General),
            CategorySummaryRowCount = report.Count(ReportRowType.CategorySummary),
            StoreSummaryRowCount = report.Count(ReportRowType.StoreSummary),
            StoreCategoryRowCount = report.Count(ReportRowType.StoreCategory),
            ProductSummaryRowCount = report.Count(ReportRowType.ProductSummary),
            StoreProductRowCount = report.Count(ReportRowType.StoreProduct),
            IsAlreadyActive = isAlreadyActive,
            ReplacesActiveVersion = replacesActiveVersion
        };
    }

    private static async Task<PairMemberSaveResult> SavePairMemberAsync(
        AppDbContext dbContext,
        ReportPeriodEntity reportPeriod,
        ParsedReport report,
        string originalFileName,
        int uploadedByUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existingImport = await FindExistingPairMemberAsync(
            dbContext,
            reportPeriod,
            report,
            cancellationToken);

        if (existingImport is not null)
        {
            return new PairMemberSaveResult(existingImport, Changed: false);
        }

        var previousActiveImport = await dbContext.ReportImports
            .SingleOrDefaultAsync(item =>
                item.ReportPeriodId == reportPeriod.Id &&
                item.PeriodType == report.PeriodType &&
                item.IsActive,
                cancellationToken);

        if (previousActiveImport is not null)
        {
            previousActiveImport.IsActive = false;
            previousActiveImport.UpdatedAt = now;
        }

        var reportImport = new ReportImportEntity
        {
            ReportPeriodId = reportPeriod.Id,
            PeriodType = report.PeriodType,
            StartDate = report.StartDate,
            EndDate = report.EndDate,
            OriginalFileName = NormalizeFileName(originalFileName),
            FileHash = report.FileHash,
            IsActive = false,
            UploadedByUserId = uploadedByUserId,
            TotalRowCount = report.Rows.Count,
            GeneralRowCount = report.Count(ReportRowType.General),
            CategorySummaryRowCount = report.Count(ReportRowType.CategorySummary),
            StoreSummaryRowCount = report.Count(ReportRowType.StoreSummary),
            StoreCategoryRowCount = report.Count(ReportRowType.StoreCategory),
            ProductSummaryRowCount = report.Count(ReportRowType.ProductSummary),
            StoreProductRowCount = report.Count(ReportRowType.StoreProduct),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.ReportImports.Add(reportImport);
        await dbContext.SaveChangesAsync(cancellationToken);

        var reportRows = report.Rows.Select(row => CreateEntity(reportImport.Id, row, now)).ToArray();
        await dbContext.ReportRows.AddRangeAsync(reportRows, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        reportImport.IsActive = true;
        reportImport.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PairMemberSaveResult(reportImport, Changed: true);
    }

    private static ReportPeriodFileItem? CreatePeriodFileItem(
        IEnumerable<ReportImportEntity> imports,
        ReportPeriodType periodType)
    {
        var reportImport = imports.SingleOrDefault(item => item.PeriodType == periodType);
        return reportImport is null
            ? null
            : new ReportPeriodFileItem
            {
                ImportId = reportImport.Id,
                OriginalFileName = reportImport.OriginalFileName,
                StartDate = reportImport.StartDate,
                EndDate = reportImport.EndDate
            };
    }

    private static string CreateNaturalKey(ReportRowEntity row, ReportRowType rowType) => rowType switch
    {
        ReportRowType.General => "general",
        ReportRowType.CategorySummary => $"category:{row.CategoryCode?.Trim()}",
        ReportRowType.StoreSummary => $"store:{row.StoreNumber}",
        ReportRowType.StoreCategory => $"store-category:{row.StoreNumber}|{row.CategoryCode?.Trim()}",
        ReportRowType.ProductSummary => $"product:{row.StockCode?.Trim()}",
        ReportRowType.StoreProduct =>
            $"store-product:{row.StoreNumber}|{row.CategoryCode?.Trim()}|{row.StockCode?.Trim()}",
        _ => throw new ArgumentOutOfRangeException(nameof(rowType), rowType, null)
    };

    private static ReportDetailRowItem CreateDetailRowItem(
        ReportRowEntity row,
        ReportRowEntity? comparisonRow = null) => new()
    {
        Id = row.Id,
        SourceRowNumber = row.SourceRowNumber,
        SourceReportId = row.SourceReportId,
        SourceReportType = row.SourceReportType,
        StoreNumber = row.StoreNumber,
        StoreName = row.StoreName,
        CategoryCode = row.CategoryCode,
        CategoryName = row.CategoryName,
        StockCode = row.StockCode,
        StockName = row.StockName,
        AlternativeName = row.AlternativeName,
        CostGroupType = row.CostGroupType,
        CostGroupCode = row.CostGroupCode,
        PurchaseGroupValueFactor = row.PurchaseGroupValueFactor,
        PurchaseStockValueFactor = row.PurchaseStockValueFactor,
        OpeningQuantity = row.OpeningQuantity,
        OpeningAmount = row.OpeningAmount,
        CompanyPurchaseQuantity = row.CompanyPurchaseQuantity,
        CompanyPurchaseAmount = row.CompanyPurchaseAmount,
        WarehouseTransferInQuantity = row.WarehouseTransferInQuantity,
        WarehouseTransferInAmount = row.WarehouseTransferInAmount,
        WarehouseTransferOutQuantity = row.WarehouseTransferOutQuantity,
        WarehouseTransferOutAmount = row.WarehouseTransferOutAmount,
        StoreSalesQuantity = row.StoreSalesQuantity,
        StoreSalesAmount = row.StoreSalesAmount,
        CostOfSales = row.CostOfSales,
        WasteRate = row.WasteRate,
        WasteQuantity = row.WasteQuantity,
        WasteAmount = row.WasteAmount,
        ClosingQuantity = row.ClosingQuantity,
        ClosingAmount = row.ClosingAmount,
        ProfitAmount = row.ProfitAmount,
        ProfitRate = row.ProfitRate,
        CategoryProfitRate = row.CategoryProfitRate,
        CategoryWasteRate = row.CategoryWasteRate,
        Comparison = comparisonRow is null ? null : CreateDetailRowItem(comparisonRow)
    };

    private static void ValidateExpectedPeriodType(
        ReportPeriodType actualPeriodType,
        ReportPeriodType expectedPeriodType)
    {
        if (actualPeriodType == expectedPeriodType)
        {
            return;
        }

        throw new ReportImportValidationException(
            $"Seçilen rapor türü '{ReportDisplayNames.PeriodType(expectedPeriodType)}', " +
            $"ancak Excel '{ReportDisplayNames.PeriodType(actualPeriodType)}' olarak algılandı.");
    }

    private static ReportPeriodType CounterpartOf(ReportPeriodType periodType) => periodType switch
    {
        ReportPeriodType.Monthly => ReportPeriodType.Cumulative,
        ReportPeriodType.Cumulative => ReportPeriodType.Monthly,
        _ => throw new ArgumentOutOfRangeException(nameof(periodType), periodType, null)
    };

    private static ReportRowEntity CreateEntity(long reportImportId, ParsedReportRow row, DateTime now)
    {
        return new ReportRowEntity
        {
            ReportImportId = reportImportId,
            SourceRowNumber = row.SourceRowNumber,
            RowType = row.RowType,
            SourceReportId = row.SourceReportId,
            SourceReportType = row.SourceReportType,
            StoreNumber = row.StoreNumber,
            StoreName = row.StoreName,
            CategoryCode = row.CategoryCode,
            CategoryName = row.CategoryName,
            StockCode = row.StockCode,
            StockName = row.StockName,
            AlternativeName = row.AlternativeName,
            CostGroupType = row.CostGroupType,
            CostGroupCode = row.CostGroupCode,
            PurchaseGroupValueFactor = row.PurchaseGroupValueFactor,
            PurchaseStockValueFactor = row.PurchaseStockValueFactor,
            OpeningQuantity = row.OpeningQuantity,
            OpeningAmount = row.OpeningAmount,
            CompanyPurchaseQuantity = row.CompanyPurchaseQuantity,
            CompanyPurchaseAmount = row.CompanyPurchaseAmount,
            WarehouseTransferInQuantity = row.WarehouseTransferInQuantity,
            WarehouseTransferInAmount = row.WarehouseTransferInAmount,
            WarehouseTransferOutQuantity = row.WarehouseTransferOutQuantity,
            WarehouseTransferOutAmount = row.WarehouseTransferOutAmount,
            StoreSalesQuantity = row.StoreSalesQuantity,
            StoreSalesAmount = row.StoreSalesAmount,
            CostOfSales = row.CostOfSales,
            WasteRate = row.WasteRate,
            WasteQuantity = row.WasteQuantity,
            WasteAmount = row.WasteAmount,
            ClosingQuantity = row.ClosingQuantity,
            ClosingAmount = row.ClosingAmount,
            ProfitAmount = row.ProfitAmount,
            ProfitRate = row.ProfitRate,
            CategoryProfitRate = row.CategoryProfitRate,
            CategoryWasteRate = row.CategoryWasteRate,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private sealed record PairMemberSaveResult(ReportImportEntity ReportImport, bool Changed);
}
