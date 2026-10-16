using System.Net;
using Platform.Domain.Identity;

namespace Platform.Application.Helpers;

/// <summary>
/// قالب HTML سادهٔ ایمیل‌های تراکنشی (بدون وابستگی به Razor/Motor). متنِ منبع
/// را با حفظ فاصلهٔ سطرها درون یک کارت راست‌به‌چپ می‌گذارد و لینک دکمه‌ای
/// اضافه می‌کند. برای ایمیل‌های پیچیده‌تر، پروژهٔ مصرف‌کننده قالب خودش را می‌سازد
/// و مستقیم <c>IEmailSender.SendHtmlAsync</c> را صدا می‌زند.
/// </summary>
public static class EmailTemplate
{
    /// <summary>کارت ایمیل با عنوان، متن (فاصلهٔ سطر حفظ می‌شود) و دکمهٔ اختیاری.</summary>
    public static string Card(string title, string body, string? buttonUrl = null, string? buttonText = null)
    {
        var bodyHtml = WebUtility.HtmlEncode(body).ReplaceLineEndings("<br/>");
        var button = buttonUrl is null
            ? string.Empty
            : $"""
               <p style="margin:24px 0">
                 <a href="{WebUtility.HtmlEncode(buttonUrl)}"
                    style="background:#2c3e50;color:#fff;text-decoration:none;padding:12px 24px;border-radius:6px;display:inline-block">
                   {WebUtility.HtmlEncode(buttonText ?? "مشاهده")}
                 </a>
               </p>
               """;

        var siteName = SettingDefaults.Get(FrameworkSettingKeys.SiteName);

        return $$"""
           <!DOCTYPE html>
           <html dir="rtl" lang="fa">
           <head><meta charset="utf-8"/></head>
           <body style="margin:0;background:#f5f7fa;font-family:Tahoma,Arial,sans-serif">
             <div style="max-width:560px;margin:24px auto;background:#fff;border-radius:8px;padding:24px;border:1px solid #dde1e6">
               <div style="color:#6c757d;font-size:13px;margin-bottom:16px">{{WebUtility.HtmlEncode(siteName)}}</div>
               <h2 style="margin:0 0 12px;color:#2c3e50;font-size:18px">{{WebUtility.HtmlEncode(title)}}</h2>
               <div style="color:#333;line-height:1.9;font-size:14px">{{bodyHtml}}</div>
               {{button}}
               <div style="color:#6c757d;font-size:12px;border-top:1px solid #dde1e6;padding-top:12px;margin-top:8px">
                 این ایمیل به‌صورت خودکار ارسال شده است؛ لطفاً به آن پاسخ ندهید.
               </div>
             </div>
           </body>
           </html>
           """;
    }
}