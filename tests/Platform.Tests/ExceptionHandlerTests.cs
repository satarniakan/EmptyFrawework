using Platform.Domain.Exceptions;
using Platform.Web.Components.Shared;
using Platform.Web.Hosting.Middleware;
using Xunit;

namespace Platform.Tests;

/// <summary>
/// نگاشت سراسری خطاها: هر استثنا یک کد وضعیت و یک پیام فارسیِ امن می‌گیرد و
/// جزئیات فنی هرگز به کاربر نمی‌رسد.
/// </summary>
public class ExceptionHandlerTests
{
    [Fact]
    public void BusinessRuleException_MapsTo400_WithItsOwnPersianMessage()
    {
        var result = PlatformExceptionHandler.Translate(
            new BusinessRuleException("موجودی کافی نیست."));

        Assert.Equal(400, result.StatusCode);
        Assert.Equal("موجودی کافی نیست.", result.Message);
        Assert.True(result.IsExpected);
    }

    [Fact]
    public void NotFoundException_MapsTo404()
    {
        var result = PlatformExceptionHandler.Translate(new NotFoundException("جلسه", 7));

        Assert.Equal(404, result.StatusCode);
        Assert.Contains("جلسه", result.Message);
        Assert.True(result.IsExpected);
    }

    [Fact]
    public void DataIntegrityException_MapsTo409_NotTo500()
    {
        var result = PlatformExceptionHandler.Translate(
            new DataIntegrityException("این مقدار تکراری است."));

        Assert.Equal(409, result.StatusCode);
        Assert.Equal("این مقدار تکراری است.", result.Message);
        Assert.True(result.IsExpected);
    }

    [Fact]
    public void Cancellation_MapsTo499_AndIsNotTreatedAsFailure()
    {
        var result = PlatformExceptionHandler.Translate(new OperationCanceledException());

        Assert.Equal(499, result.StatusCode);
        Assert.True(result.IsExpected);
    }

    [Fact]
    public void UnknownException_LeaksNoDetails()
    {
        var result = PlatformExceptionHandler.Translate(
            new InvalidOperationException("CSQL Server: رمز عبور sa در پیام خطا"));

        Assert.Equal(500, result.StatusCode);
        Assert.Equal(ErrorMessages.Unexpected, result.Message);
        Assert.False(result.IsExpected);

        // نباید هیچ بخشی از پیام فنی در خروجی باشد
        Assert.DoesNotContain("sa", result.Message);
        Assert.DoesNotContain("SQL", result.Message);
    }

    [Fact]
    public void NullException_StillProducesSafeMessage()
    {
        var result = PlatformExceptionHandler.Translate(null);

        Assert.Equal(500, result.StatusCode);
        Assert.Equal(ErrorMessages.Unexpected, result.Message);
    }

    [Fact]
    public void ErrorMessageHelper_UsesTheSameTranslation()
    {
        // صفحات Blazor از هِلپر پیام می‌گیرند؛ نباید از مدیریت سراسری فاصله بگیرد
        Assert.Equal(
            PlatformExceptionHandler.Translate(new BusinessRuleException("خطا شد.")).Message,
            ErrorMessageHelper.ToUserMessage(new BusinessRuleException("خطا شد.")));

        Assert.Equal(
            ErrorMessages.Unexpected,
            ErrorMessageHelper.ToUserMessage(new Exception("جزئیات فنی محرمانه")));
    }

    [Fact]
    public void DataIntegrityMessage_IsShownToUser_NotSwallowed()
    {
        var message = ErrorMessageHelper.ToUserMessage(
            new DataIntegrityException("این شماره قبلاً ثبت شده است."));

        Assert.Equal("این شماره قبلاً ثبت شده است.", message);
    }
}
