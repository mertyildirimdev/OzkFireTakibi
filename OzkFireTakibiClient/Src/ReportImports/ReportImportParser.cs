using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using OzkFireTakibiClient.Src.Data.Entities;

namespace OzkFireTakibiClient.Src.ReportImports;

public sealed partial class ReportImportParser
{
    private const string SheetName = "İCMAL";

    private static readonly string[] RequiredHeaders =
    [
        "rpr_id", "rpr_tip", "Depo No", "Depo Adı", "Kategori Kodu", "Kategori İsmi",
        "Stok Kodu", "Stok İsmi", "Alternatif İsim", "Maliyet Grup Tipi", "Maliyet Grup Kodu",
        "Satın Alma Grubu Değer Çarpanı", "Satın Alma Stok Değer Çarpanı", "Dönem Başı Miktar",
        "Dönem Başı Tutar", "Firma Alış Miktar", "Firma Alış Tutar", "Depo Sevk Alış Miktar",
        "Depo Sevk Alış Tutar", "Depo Sevk Satış Miktar", "Depo Sevk Satış Tutar",
        "Mağaza Satış Miktar", "Mağaza Satış Tutar", "Satış Maliyeti", "Fire Oranı",
        "Fire Miktarı", "Fire Tutarı", "Dönem Sonu Miktar", "Dönem Sonu Tutar", "Kar Tutar",
        "Kar Oran", "Kategori Kar Oran", "Kategori Fire Oran"
    ];

    public async Task<ParsedReport> ParseAsync(
        string filePath,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        ValidateExtension(originalFileName);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
        stream.Position = 0;

        try
        {
            using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration
            {
                FallbackEncoding = Encoding.GetEncoding(1254),
                LeaveOpen = true
            });

            MoveToReportSheet(reader);
            var headers = ReadHeaders(reader);
            var rows = ReadRows(reader, headers, cancellationToken);
            ValidateRowOrder(rows);
            ResolveProductCodes(rows);
            ValidateNaturalKeys(rows);

            var (startDate, endDate) = ResolvePeriod(rows);
            var periodType = endDate.DayNumber - startDate.DayNumber <= 31
                ? ReportPeriodType.Monthly
                : ReportPeriodType.Cumulative;

            return new ParsedReport
            {
                FileHash = hash,
                Scope = ResolveScope(rows),
                PeriodType = periodType,
                StartDate = startDate,
                EndDate = endDate,
                Rows = rows
            };
        }
        catch (ReportImportValidationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ReportImportValidationException(
                "Excel dosyası okunamadı. Dosyanın bozuk veya desteklenmeyen bir formatta olmadığını kontrol edin.",
                exception);
        }
    }

    private static void ValidateExtension(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!extension.Equals(".xls", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new ReportImportValidationException("Yalnızca .xls ve .xlsx dosyaları yüklenebilir.");
        }
    }

    private static void MoveToReportSheet(IExcelDataReader reader)
    {
        do
        {
            if (string.Equals(NormalizeText(reader.Name), SheetName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        } while (reader.NextResult());

        throw new ReportImportValidationException($"'{SheetName}' çalışma sayfası bulunamadı.");
    }

    private static IReadOnlyDictionary<string, int> ReadHeaders(IExcelDataReader reader)
    {
        if (!reader.Read())
        {
            throw new ReportImportValidationException("Excel çalışma sayfası boş.");
        }

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < reader.FieldCount; index++)
        {
            var header = NormalizeText(reader.GetValue(index));
            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            if (!headers.TryAdd(header, index))
            {
                throw new ReportImportValidationException($"Tekrarlanan kolon başlığı bulundu: '{header}'.");
            }
        }

        var missingHeaders = RequiredHeaders.Where(header => !headers.ContainsKey(header)).ToArray();
        if (missingHeaders.Length > 0)
        {
            throw new ReportImportValidationException(
                $"Eksik Excel kolonları: {string.Join(", ", missingHeaders)}.");
        }

        return headers;
    }

    private static List<ParsedReportRow> ReadRows(
        IExcelDataReader reader,
        IReadOnlyDictionary<string, int> headers,
        CancellationToken cancellationToken)
    {
        var rows = new List<ParsedReportRow>();
        var sourceRowNumber = 1;

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            sourceRowNumber++;

            if (IsEmptyRow(reader))
            {
                continue;
            }

            var reportId = GetRequiredInt(reader, headers, "rpr_id", sourceRowNumber);
            var reportType = GetRequiredText(reader, headers, "rpr_tip", sourceRowNumber);
            var rowType = ResolveRowType(reportId, reportType, sourceRowNumber);

            rows.Add(new ParsedReportRow
            {
                SourceRowNumber = sourceRowNumber,
                RowType = rowType,
                SourceReportType = reportType,
                StoreNumber = NormalizeStoreNumber(GetNullableInt(reader, headers, "Depo No", sourceRowNumber)),
                StoreName = NullIfRollup(GetText(reader, headers, "Depo Adı")),
                CategoryCode = NullIfRollup(GetText(reader, headers, "Kategori Kodu")),
                CategoryName = NullIfRollup(GetText(reader, headers, "Kategori İsmi")),
                StockCode = NullIfRollup(GetText(reader, headers, "Stok Kodu")),
                StockName = NullIfRollup(GetText(reader, headers, "Stok İsmi")),
                AlternativeName = NullIfRollup(GetText(reader, headers, "Alternatif İsim")),
                CostGroupType = NullIfRollup(GetText(reader, headers, "Maliyet Grup Tipi")),
                CostGroupCode = NullIfRollup(GetText(reader, headers, "Maliyet Grup Kodu")),
                PurchaseGroupValueFactor = GetDecimal(reader, headers, "Satın Alma Grubu Değer Çarpanı", sourceRowNumber),
                PurchaseStockValueFactor = GetDecimal(reader, headers, "Satın Alma Stok Değer Çarpanı", sourceRowNumber),
                OpeningQuantity = GetDecimal(reader, headers, "Dönem Başı Miktar", sourceRowNumber),
                OpeningAmount = GetDecimal(reader, headers, "Dönem Başı Tutar", sourceRowNumber),
                CompanyPurchaseQuantity = GetDecimal(reader, headers, "Firma Alış Miktar", sourceRowNumber),
                CompanyPurchaseAmount = GetDecimal(reader, headers, "Firma Alış Tutar", sourceRowNumber),
                WarehouseTransferInQuantity = GetDecimal(reader, headers, "Depo Sevk Alış Miktar", sourceRowNumber),
                WarehouseTransferInAmount = GetDecimal(reader, headers, "Depo Sevk Alış Tutar", sourceRowNumber),
                WarehouseTransferOutQuantity = GetDecimal(reader, headers, "Depo Sevk Satış Miktar", sourceRowNumber),
                WarehouseTransferOutAmount = GetDecimal(reader, headers, "Depo Sevk Satış Tutar", sourceRowNumber),
                StoreSalesQuantity = GetDecimal(reader, headers, "Mağaza Satış Miktar", sourceRowNumber),
                StoreSalesAmount = GetDecimal(reader, headers, "Mağaza Satış Tutar", sourceRowNumber),
                CostOfSales = GetDecimal(reader, headers, "Satış Maliyeti", sourceRowNumber),
                WasteRate = GetDecimal(reader, headers, "Fire Oranı", sourceRowNumber),
                WasteQuantity = GetDecimal(reader, headers, "Fire Miktarı", sourceRowNumber),
                WasteAmount = GetDecimal(reader, headers, "Fire Tutarı", sourceRowNumber),
                ClosingQuantity = GetDecimal(reader, headers, "Dönem Sonu Miktar", sourceRowNumber),
                ClosingAmount = GetDecimal(reader, headers, "Dönem Sonu Tutar", sourceRowNumber),
                ProfitAmount = GetDecimal(reader, headers, "Kar Tutar", sourceRowNumber),
                ProfitRate = GetDecimal(reader, headers, "Kar Oran", sourceRowNumber),
                CategoryProfitRate = GetDecimal(reader, headers, "Kategori Kar Oran", sourceRowNumber),
                CategoryWasteRate = GetDecimal(reader, headers, "Kategori Fire Oran", sourceRowNumber)
            });
        }

        if (rows.Count == 0)
        {
            throw new ReportImportValidationException("Excel dosyasında veri satırı bulunamadı.");
        }

        return rows;
    }

    private static ReportRowType ResolveRowType(int reportId, string reportType, int sourceRowNumber)
    {
        var expectedText = reportId switch
        {
            2 => "Genel Durum Grup Detaylı",
            3 => "Şubeler Genel Durum",
            4 => "Şubeler Grup Detaylı",
            5 => "Stoklar Genel",
            7 => "Şubeler Stok Detaylı",
            _ => null
        };

        if (reportId == 1)
        {
            if (!reportType.StartsWith("Genel Durum ", StringComparison.Ordinal))
            {
                throw new ReportImportValidationException(
                    $"{sourceRowNumber}. satırdaki genel durum metni beklenen formatta değil.");
            }

            return ReportRowType.General;
        }

        if (expectedText is null || !string.Equals(reportType, expectedText, StringComparison.Ordinal))
        {
            throw new ReportImportValidationException(
                $"{sourceRowNumber}. satırda bilinmeyen rapor tipi bulundu: rpr_id={reportId}, rpr_tip='{reportType}'.");
        }

        return reportId switch
        {
            2 => ReportRowType.CategorySummary,
            3 => ReportRowType.StoreSummary,
            4 => ReportRowType.StoreCategory,
            5 => ReportRowType.ProductSummary,
            7 => ReportRowType.StoreProduct,
            _ => throw new UnreachableException()
        };
    }

    private static void ValidateRowOrder(IReadOnlyList<ParsedReportRow> rows)
    {
        var expectedOrder = new[]
        {
            ReportRowType.General,
            ReportRowType.CategorySummary,
            ReportRowType.StoreSummary,
            ReportRowType.StoreCategory,
            ReportRowType.ProductSummary,
            ReportRowType.StoreProduct
        };

        var orderLookup = expectedOrder
            .Select((rowType, index) => new { rowType, index })
            .ToDictionary(x => x.rowType, x => x.index);

        var lastOrder = -1;
        foreach (var row in rows)
        {
            var currentOrder = orderLookup[row.RowType];
            if (currentOrder < lastOrder)
            {
                throw new ReportImportValidationException(
                    $"Rapor satır bloklarının sırası {row.SourceRowNumber}. satırda bozuluyor.");
            }

            lastOrder = currentOrder;
        }

        foreach (var rowType in expectedOrder)
        {
            if (!rows.Any(row => row.RowType == rowType))
            {
                throw new ReportImportValidationException($"'{rowType}' satır bloğu bulunamadı.");
            }
        }

        if (rows.Count(row => row.RowType == ReportRowType.General) != 1)
        {
            throw new ReportImportValidationException("Raporda tam olarak bir genel durum satırı olmalıdır.");
        }
    }

    private static void ResolveProductCodes(IReadOnlyList<ParsedReportRow> rows)
    {
        var detailRows = rows.Where(row => row.RowType == ReportRowType.StoreProduct).ToArray();
        foreach (var row in detailRows)
        {
            RequireKey(row.StockName, "Stok İsmi", row.SourceRowNumber);
            RequireKey(row.StockCode, "Stok Kodu", row.SourceRowNumber);
            RequireKey(row.CategoryCode, "Kategori Kodu", row.SourceRowNumber);
            if (row.StoreNumber is null)
            {
                throw new ReportImportValidationException($"{row.SourceRowNumber}. satırda Depo No boş.");
            }
        }

        var nameToCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in detailRows.GroupBy(row => row.StockName!, StringComparer.OrdinalIgnoreCase))
        {
            var codes = group.Select(row => row.StockCode!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (codes.Length != 1)
            {
                throw new ReportImportValidationException(
                    $"'{group.Key}' ürün adı birden fazla stok koduna bağlı.");
            }

            nameToCode[group.Key] = codes[0];
        }

        foreach (var group in detailRows.GroupBy(row => row.StockCode!, StringComparer.OrdinalIgnoreCase))
        {
            var signatures = group
                .Select(row => $"{row.StockName}|{row.CategoryCode}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (signatures.Length != 1)
            {
                throw new ReportImportValidationException(
                    $"'{group.Key}' stok kodu birden fazla ürün veya kategoriye bağlı.");
            }
        }

        foreach (var row in rows.Where(row => row.RowType == ReportRowType.ProductSummary))
        {
            var stockName = RequireKey(row.StockName, "Stok İsmi", row.SourceRowNumber);
            if (!nameToCode.TryGetValue(stockName, out var stockCode))
            {
                throw new ReportImportValidationException(
                    $"{row.SourceRowNumber}. satırdaki '{stockName}' ürünü için stok kodu çözümlenemedi.");
            }

            row.StockCode = stockCode;
        }
    }

    private static void ValidateNaturalKeys(IReadOnlyList<ParsedReportRow> rows)
    {
        EnsureUnique(rows, ReportRowType.CategorySummary, row => RequireKey(row.CategoryCode, "Kategori Kodu", row.SourceRowNumber));
        EnsureUnique(rows, ReportRowType.StoreSummary, row => RequireStore(row));
        EnsureUnique(rows, ReportRowType.StoreCategory, row => $"{RequireStore(row)}|{RequireKey(row.CategoryCode, "Kategori Kodu", row.SourceRowNumber)}");
        EnsureUnique(rows, ReportRowType.ProductSummary, row => RequireKey(row.StockCode, "Stok Kodu", row.SourceRowNumber));
        EnsureUnique(rows, ReportRowType.StoreProduct, row => $"{RequireStore(row)}|{RequireKey(row.CategoryCode, "Kategori Kodu", row.SourceRowNumber)}|{RequireKey(row.StockCode, "Stok Kodu", row.SourceRowNumber)}");
    }

    private static void EnsureUnique(
        IReadOnlyList<ParsedReportRow> rows,
        ReportRowType rowType,
        Func<ParsedReportRow, string> keySelector)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.Where(row => row.RowType == rowType))
        {
            var key = keySelector(row);
            if (!keys.Add(key))
            {
                throw new ReportImportValidationException(
                    $"{row.SourceRowNumber}. satırda '{rowType}' için tekrarlanan anahtar bulundu: {key}.");
            }
        }
    }

    private static (DateOnly StartDate, DateOnly EndDate) ResolvePeriod(IReadOnlyList<ParsedReportRow> rows)
    {
        var generalRow = rows.Single(row => row.RowType == ReportRowType.General);
        var match = PeriodRegex().Match(generalRow.SourceReportType);
        if (!match.Success ||
            !DateOnly.TryParseExact(match.Groups[1].Value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate) ||
            !DateOnly.TryParseExact(match.Groups[2].Value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate) ||
            endDate < startDate)
        {
            throw new ReportImportValidationException(
                $"Dönem bilgisi çözümlenemedi: '{generalRow.SourceReportType}'.");
        }

        return (startDate, endDate);
    }

    private static ReportScope ResolveScope(IReadOnlyList<ParsedReportRow> rows)
    {
        var categoryCodes = rows
            .Where(row => row.RowType == ReportRowType.CategorySummary)
            .Select(row => RequireKey(row.CategoryCode, "Kategori Kodu", row.SourceRowNumber))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (categoryCodes.All(code => code.StartsWith("12.", StringComparison.Ordinal)))
        {
            return ReportScope.Delicatessen;
        }

        if (categoryCodes.All(code => code.StartsWith("15.", StringComparison.Ordinal)))
        {
            return ReportScope.NutsAndDriedFruit;
        }

        throw new ReportImportValidationException(
            $"Rapor kapsamı kategori kodlarından belirlenemedi: {string.Join(", ", categoryCodes)}.");
    }

    private static bool IsEmptyRow(IExcelDataReader reader)
    {
        for (var index = 0; index < reader.FieldCount; index++)
        {
            if (reader.GetValue(index) is not null)
            {
                return false;
            }
        }

        return true;
    }

    private static int GetRequiredInt(
        IExcelDataReader reader,
        IReadOnlyDictionary<string, int> headers,
        string header,
        int sourceRowNumber)
    {
        return GetNullableInt(reader, headers, header, sourceRowNumber)
            ?? throw new ReportImportValidationException($"{sourceRowNumber}. satırda '{header}' boş.");
    }

    private static int? GetNullableInt(
        IExcelDataReader reader,
        IReadOnlyDictionary<string, int> headers,
        string header,
        int sourceRowNumber)
    {
        var value = reader.GetValue(headers[header]);
        if (value is null || string.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.InvariantCulture)))
        {
            return null;
        }

        if (value is string text)
        {
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out var localizedValue) ||
                decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out localizedValue))
            {
                if (localizedValue != decimal.Truncate(localizedValue))
                {
                    throw new ReportImportValidationException(
                        $"{sourceRowNumber}. satırdaki '{header}' tam sayı değil: '{text}'.");
                }

                return decimal.ToInt32(localizedValue);
            }

            throw new ReportImportValidationException(
                $"{sourceRowNumber}. satırdaki '{header}' tam sayı değil: '{text}'.");
        }

        try
        {
            var decimalValue = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            if (decimalValue != decimal.Truncate(decimalValue))
            {
                throw new FormatException();
            }

            return decimal.ToInt32(decimalValue);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            throw new ReportImportValidationException(
                $"{sourceRowNumber}. satırdaki '{header}' tam sayı değil: '{value}'.");
        }
    }

    private static decimal? GetDecimal(
        IExcelDataReader reader,
        IReadOnlyDictionary<string, int> headers,
        string header,
        int sourceRowNumber)
    {
        var value = reader.GetValue(headers[header]);
        if (value is null || string.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.InvariantCulture)))
        {
            return null;
        }

        if (value is string text)
        {
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out var localizedValue) ||
                decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out localizedValue))
            {
                return localizedValue;
            }

            throw new ReportImportValidationException(
                $"{sourceRowNumber}. satırdaki '{header}' sayısal değil: '{text}'.");
        }

        try
        {
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            throw new ReportImportValidationException(
                $"{sourceRowNumber}. satırdaki '{header}' sayısal değil: '{NormalizeText(value)}'.");
        }
    }

    private static string GetRequiredText(
        IExcelDataReader reader,
        IReadOnlyDictionary<string, int> headers,
        string header,
        int sourceRowNumber)
    {
        return GetText(reader, headers, header)
            ?? throw new ReportImportValidationException($"{sourceRowNumber}. satırda '{header}' boş.");
    }

    private static string? GetText(
        IExcelDataReader reader,
        IReadOnlyDictionary<string, int> headers,
        string header)
    {
        var text = NormalizeText(reader.GetValue(headers[header]));
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string NormalizeText(object? value)
    {
        return (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
            .Trim()
            .Normalize(NormalizationForm.FormC);
    }

    private static string? NullIfRollup(string? value)
    {
        return string.Equals(value, "Tümü", StringComparison.OrdinalIgnoreCase) ? null : value;
    }

    private static int? NormalizeStoreNumber(int? value) => value == 0 ? null : value;

    private static string RequireKey(string? value, string columnName, int sourceRowNumber)
    {
        return value ?? throw new ReportImportValidationException(
            $"{sourceRowNumber}. satırda '{columnName}' boş.");
    }

    private static string RequireStore(ParsedReportRow row)
    {
        return row.StoreNumber?.ToString(CultureInfo.InvariantCulture)
            ?? throw new ReportImportValidationException($"{row.SourceRowNumber}. satırda 'Depo No' boş.");
    }

    [GeneratedRegex(@"^Genel Durum\s+(\d{2}\.\d{2}\.\d{4})-(\d{2}\.\d{2}\.\d{4})$", RegexOptions.CultureInvariant)]
    private static partial Regex PeriodRegex();
}
