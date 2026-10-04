using System.ComponentModel.DataAnnotations;
using Platform.Domain.Enums;

namespace Platform.Application.DTOs;

/// <summary>
/// اعلان. <see cref="IsRead"/> عمداً قابل‌تغییر است چون UI آن را بدون رفرش
/// (هنگام کلیک روی زنگ اعلان) عوض می‌کند.
/// </summary>
public record NotificationDto(
    int Id,
    string Title,
    string? Body,
    NotificationType Type,
    string? LinkUrl,
    DateTime CreatedAt)
{
    public bool IsRead { get; set; }
}

public record NotificationFeedDto(
    int UnreadCount,
    IReadOnlyList<NotificationDto> Items);

public class BroadcastDto
{
    [Required(ErrorMessage = "عنوان الزامی است.")]
    [StringLength(200, ErrorMessage = "عنوان نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "متن نمی‌تواند بیشتر از ۱٬۰۰۰ کاراکتر باشد.")]
    public string? Body { get; set; }

    /// <summary>null = همهٔ کاربران؛ در غیر این صورت نام نقش.</summary>
    public string? RoleName { get; set; }

    public NotificationType Type { get; set; } = NotificationType.System;

    [StringLength(300)]
    public string? LinkUrl { get; set; }
}