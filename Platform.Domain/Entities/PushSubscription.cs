namespace Platform.Domain.Entities;

/// <summary>
/// اشتراک وب‌پوش یک دستگاه کاربر (Web Push API). Endpoint یکتاست؛ با لغو اشتراک
/// در مرورگر، رکورد با اولین ارسال ناموفق (410 Gone) پاک می‌شود.
/// </summary>
public class PushSubscription
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    /// <summary>کلید p256dh (base64url).</summary>
    public string P256dh { get; set; } = string.Empty;

    /// <summary>کلید auth (base64url).</summary>
    public string Auth { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
