using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Platform.Domain.Interfaces;

namespace Platform.Infrastructure.Services;

/// <summary>
/// کاربر جاری از روی کوکی درخواست HTTP. وقتی درخواستی نباشد (کرون، design-time)
/// null برمی‌گردد تا ثبتِ «چه کسی» هرگز دیتای غلط نسازد.
/// </summary>
public class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContext;

    public HttpContextCurrentUser(IHttpContextAccessor httpContext) => _httpContext = httpContext;

    public string? UserId =>
        _httpContext.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
