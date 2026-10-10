using Microsoft.EntityFrameworkCore;
using Platform.Application.DTOs;

namespace Platform.Application.Queries;

/// <summary>
/// صفحه‌بندی در سمت دیتابیس برای هر کوئری <see cref="IQueryable{T}"/>.
/// <para>
/// چرا لازم است: الگوی رایج «همهٔ ردیف‌ها را بگیر، بعد در حافظه صفحه‌بندی کن» با بزرگ‌شدن
/// جدول هم حافظه را می‌خورد و هم کلید را از دست می‌دهد. اینجا <c>COUNT</c> و
/// <c>OFFSET/FETCH</c> روی خودِ دیتابیس می‌رود و فقط ردیف‌های همان صفحه برمی‌گردند.
/// </para>
/// <para>
/// ترتیب را پیش از فراخوانی خودتان تعیین کنید؛ این متد روی ترتیب کاری نمی‌کند چون
/// نمی‌داند کوئری شما چه معنایی دارد.
/// </para>
/// </summary>
public static class PagedQueryExtensions
{
    /// <summary>
    /// سقف صفحه. بدون آن، یک پارامتر URL مثل <c>?pageSize=10000000</c>
    /// می‌توانست کل جدول را به حافظه بکشد (حملهٔ ردیف‌بُر).
    /// </summary>
    public const int MaxPageSize = 500;

    public static async Task<PagedResult<T>> ToPagedAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        // شمارش باید پیش از بریدن انجام شود، وگرنه صفحه‌بندی و «n از m» اشتباه می‌شود
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
