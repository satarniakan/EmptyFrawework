using Platform.Application.Validators;
using Platform.Domain.Exceptions;

namespace Platform.Tests;

/// <summary>
/// گاردهای مشترک اعتبارسنجی: پیام فارسیِ امن می‌دهند و روی ورودی نامعتبر خطا می‌دهند،
/// روی ورودی معتبر ساکت‌اند.
/// </summary>
public class CommonValidationsTests
{
    [Fact]
    public void ValidateItemsNotEmpty_EmptyList_ThrowsBusinessRule()
    {
        var ex = Assert.Throws<BusinessRuleException>(() =>
            CommonValidations.ValidateItemsNotEmpty(new List<int>(), "حداقل یک مدعو انتخاب کنید."));
        Assert.Equal("حداقل یک مدعو انتخاب کنید.", ex.Message);
    }

    [Fact]
    public void ValidateItemsNotEmpty_NonEmptyList_DoesNotThrow()
    {
        CommonValidations.ValidateItemsNotEmpty(new List<int> { 1 });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ValidateQuantityPositive_NonPositive_Throws(decimal quantity)
    {
        Assert.Throws<BusinessRuleException>(() => CommonValidations.ValidateQuantityPositive(quantity));
    }

    [Fact]
    public void ValidateAmountPositive_Positive_DoesNotThrow()
    {
        CommonValidations.ValidateAmountPositive(1000);
    }

    [Fact]
    public void ValidatePriceNonNegative_Negative_Throws()
    {
        Assert.Throws<BusinessRuleException>(() => CommonValidations.ValidatePriceNonNegative(-1));
    }

    [Fact]
    public void ValidatePriceNonNegative_Zero_DoesNotThrow()
    {
        CommonValidations.ValidatePriceNonNegative(0);
    }
}
