using ClosedXML.Excel;

namespace Platform.Application.Imports;

/// <summary>خطای یک سلول در ایمپورت: mapper برای مقدار نامعتبر همین را می‌اندازد.</summary>
public sealed class RowImportException(string column, string message) : Exception(message)
{
    public string Column { get; } = column;
}

/// <summary>خطای یک سطر: شمارهٔ سطرِ اکسل + ستون + پیام فارسی.</summary>
public record ImportRowError(int RowNumber, string Column, string Message);

/// <summary>یک سطر موفق به‌همراه شمارهٔ واقعی‌اش در اکسل (برای خطاهای مرحلهٔ بعد، مثل تطبیق).</summary>
public record ImportedRow<T>(int RowNumber, T Value);

/// <summary>نتیجهٔ ایمپورت: ردیف‌های موفق + خطاهای سطری. سطرهای خراب بقیه را نمی‌سوزانند.</summary>
public record ExcelImportResult<T>(
    List<ImportedRow<T>> Items,
    List<ImportRowError> RowErrors,
    int TotalRows)
{
    public bool HasErrors => RowErrors.Count > 0;
}

/// <summary>
/// خواندن اکسلِ کاربرساخته (ایمپورت). سطر اول سرستون است؛ سطرهای کاملاً خالی نادیده
/// گرفته می‌شوند؛ هر سطر خراب فقط یک خطا می‌سازد و بقیه پردازش می‌شوند.
/// <para>
/// قرارداد: mapper برای سلول نامعتبر <see cref="RowImportException"/> می‌اندازد
/// (کمک‌کننده‌ها: <see cref="Required"/>). استثنای دیگر یعنی باگ برنامه و بالا می‌آید.
/// </para>
/// </summary>
public static class ExcelImporter
{
    /// <summary>سقف سطر برای جلوگیری از خوردن حافظه با فایل غول‌پیکر.</summary>
    public const int MaxRows = 5000;

    public static ExcelImportResult<T> Read<T>(
        Stream stream,
        Func<IReadOnlyDictionary<string, string?>, T> mapRow,
        int maxRows = MaxRows)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(mapRow);

        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet is null)
            return new([], [new(0, "", "فایل اکسل هیچ کاربرگی ندارد.")], 0);

        var headers = sheet.Row(1).CellsUsed()
            .Select(c => c.GetValue<string>().Trim())
            .Where(h => h.Length > 0)
            .ToList();

        if (headers.Count == 0)
            return new([], [new(1, "", "سطر اول (سرستون‌ها) خالی است.")], 0);

        var items = new List<ImportedRow<T>>();
        var errors = new List<ImportRowError>();
        var totalRows = 0;

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            if (row.RowNumber() == 1)
                continue;

            var cells = row.Cells(1, headers.Count)
                .Select(c => c.GetValue<string>().Trim())
                .ToList();

            if (cells.All(string.IsNullOrEmpty))
                continue; // سطر کاملاً خالی

            totalRows++;
            if (totalRows > maxRows)
            {
                errors.Add(new(row.RowNumber(), "", $"فایل بیش از {maxRows} سطر دارد؛ بقیه نادیده گرفته شد."));
                break;
            }

            var record = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count && i < cells.Count; i++)
                record[headers[i]] = string.IsNullOrEmpty(cells[i]) ? null : cells[i];

            try
            {
                items.Add(new ImportedRow<T>(row.RowNumber(), mapRow(record)));
            }
            catch (RowImportException ex)
            {
                errors.Add(new(row.RowNumber(), ex.Column, ex.Message));
            }
        }

        return new(items, errors, totalRows);
    }

    /// <summary>خواندن ستون الزامی (با پشتیبانی از چند نام جایگزین برای سرستون).</summary>
    /// <exception cref="RowImportException">اگر ستون نباشد یا مقدارش خالی باشد.</exception>
    public static string Required(IReadOnlyDictionary<string, string?> row, params string[] columns)
    {
        foreach (var column in columns)
        {
            if (row.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        throw new RowImportException(columns[0], $"ستون «{columns[0]}» الزامی است.");
    }

    /// <summary>خواندن ستون اختیاری؛ اگر نباشد null.</summary>
    public static string? Get(IReadOnlyDictionary<string, string?> row, params string[] columns)
    {
        foreach (var column in columns)
        {
            if (row.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
}
