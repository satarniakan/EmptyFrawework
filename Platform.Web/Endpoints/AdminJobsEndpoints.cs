using Platform.App.Services;
using Platform.Domain.Identity;

namespace Platform.App.Endpoints;

/// <summary>
/// نظارت ادمین بر کارهای تکرارشونده (<c>IRecurringJob</c>): فقط خواندن وضعیت
/// (نام، فاصله، آخرین اجرا، آخرین خطا، شمارنده‌ها). اجرا/توقف دستی عمداً ندارد —
/// تغییر رفتار jobها تصمیم deploy است نه دکمهٔ وب.
/// </summary>
public static class AdminJobsEndpoints
{
    public static IEndpointRouteBuilder MapAdminJobsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/jobs", (RecurringJobStatusTracker tracker) =>
        {
            var jobs = tracker.Snapshot().Select(j => new
            {
                j.Name,
                intervalMinutes = Math.Round(j.Interval.TotalMinutes, 1),
                lastRunUtc = j.LastRunUtc,
                lastSuccessUtc = j.LastSuccessUtc,
                j.LastError,
                j.RunCount,
                j.ErrorCount
            });

            return Results.Ok(jobs);
        }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute
        {
            Roles = Roles.Admin
        });

        return app;
    }
}