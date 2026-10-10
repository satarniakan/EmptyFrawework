// Platform.Domain/Enums/OutboxEnums.cs
namespace Platform.Domain.Enums;

public enum OutboxChannel
{
    Sms = 1,
    Email = 2,

    /// <summary>
    /// اعلان وب‌پوش: Recipient شناسهٔ کاربر است (نه شماره/ایمیل) و Subject/Body
    /// همان عنوان و متن اعلان‌اند. بدون تغییر اسکیما اضافه شد (عدد ۳ تازه است).
    /// </summary>
    Push = 3
}

public enum OutboxStatus
{
    /// <summary>در انتظار ارسال</summary>
    Pending = 1,
    /// <summary>ارسال شده</summary>
    Sent = 2,
    /// <summary>نرم‌افزار /job/ پردازش آن را claim کرده و در حال ارسال است (قفل نرم)</summary>
    Processing = 4,
    /// <summary>ناموفق دائمی (پس از سقف تلاش)</summary>
    Failed = 3
}
