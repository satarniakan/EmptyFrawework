using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Platform.Application.DTOs;
using Platform.Application.Imports;
using Platform.Domain.Identity;
using Platform.Domain.Queries;

namespace Platform.Application.Services;

/// <summary>نتیجهٔ ایمپورت کاربران: تعداد ساخته‌شده + خطاهای سطری (شماره سطر واقعی اکسل).</summary>
public record UserImportResult(int ImportedCount, List<ImportRowError> RowErrors, int TotalRows);

/// <summary>
/// ایمپورت گروهی کاربران از اکسل (فهرست پرسنل و…). ستون‌ها:
/// شماره موبایل (Phone/PhoneNumber/موبایل/شماره موبایل/شماره — الزامی)،
/// نام (FullName/نام/نام کامل — الزامی)، ایمیل (Email/ایمیل — اختیاری)،
/// رمز (Password/رمز — اختیاری؛ خالی یعنی ورود فقط با پیامک).
/// <para>
/// هر سطر خراب فقط خودش را می‌سوزاند. شماره‌ها نرمال (ارقام انگلیسی) و تکراری‌های
/// داخل فایل در همان‌جا رد می‌شوند. نقش همهٔ واردشده‌ها «کاربر» است.
/// </para>
/// </summary>
public interface IUserImportService
{
    Task<UserImportResult> ImportAsync(Stream excelStream);
}

public partial class UserImportService : IUserImportService
{
    private readonly IUserAdminService _users;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContext;

    public UserImportService(IUserAdminService users, IAuditService audit, IHttpContextAccessor httpContext)
    {
        _users = users;
        _audit = audit;
        _httpContext = httpContext;
    }

    public async Task<UserImportResult> ImportAsync(Stream excelStream)
    {
        var parsed = ExcelImporter.Read(excelStream, row => new ImportedUserRow(
            PersianSearch.NormalizePhone(ExcelImporter.Required(row,
                "Phone", "PhoneNumber", "موبایل", "شماره موبایل", "شماره")),
            ExcelImporter.Required(row, "FullName", "نام", "نام کامل").Trim(),
            ExcelImporter.Get(row, "Email", "ایمیل")?.Trim(),
            ExcelImporter.Get(row, "Password", "رمز")?.Trim()));

        var errors = new List<ImportRowError>(parsed.RowErrors);
        var seenPhones = new HashSet<string>(StringComparer.Ordinal);
        var imported = 0;

        foreach (var item in parsed.Items)
        {
            var (rowNumber, row) = (item.RowNumber, item.Value);

            if (!PhonePattern().IsMatch(row.Phone))
            {
                errors.Add(new ImportRowError(rowNumber, "Phone", $"شمارهٔ «{row.Phone}» معتبر نیست."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.FullName))
            {
                errors.Add(new ImportRowError(rowNumber, "FullName", "نام الزامی است."));
                continue;
            }

            if (!seenPhones.Add(row.Phone))
            {
                errors.Add(new ImportRowError(rowNumber, "Phone", $"شمارهٔ «{row.Phone}» در فایل تکراری است."));
                continue;
            }

            var result = await _users.CreateUserAsync(new CreateUserDto
            {
                FullName = row.FullName,
                PhoneNumber = row.Phone,
                Email = string.IsNullOrWhiteSpace(row.Email) ? null : row.Email,
                Password = string.IsNullOrWhiteSpace(row.Password) ? null : row.Password,
                RoleNames = [Roles.User]
            });

            if (result.Succeeded)
            {
                imported++;
            }
            else
            {
                errors.Add(new ImportRowError(rowNumber, "Phone",
                    string.Join("، ", result.Errors.Select(e => e.Description))));
            }
        }

        errors.Sort((a, b) => a.RowNumber.CompareTo(b.RowNumber));

        if (imported > 0)
        {
            var actor = _httpContext.HttpContext?.User.Identity?.Name;
            await _audit.LogEventAsync("user.imported", actor,
                $"{imported} کاربر از اکسل وارد شدند ({errors.Count} خطا).");
        }

        return new UserImportResult(imported, errors, parsed.TotalRows);
    }

    private sealed record ImportedUserRow(string Phone, string FullName, string? Email, string? Password);

    [GeneratedRegex("^09\\d{9}$")]
    private static partial Regex PhonePattern();
}
