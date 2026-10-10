namespace Platform.Domain.Enums;

/// <summary>
/// نوع اعلان درون‌برنامه‌ای. generic نگه داشته شده تا هر ماژول
/// بدون تغییر پایه، نوع خودش را با همین مقادیر پوشش دهد.
/// </summary>
public enum NotificationType
{
    General = 1,
    Meeting = 2,
    System = 3
}
