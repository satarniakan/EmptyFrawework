using Platform.Domain.Queries;

namespace Platform.Tests;

/// <summary>
/// نرمال‌سازی فارسی: ي/ك عربی، ارقام فارسی/عربی، اعراب و کشیده یکدست می‌شوند؛
/// عبارت ContainsNormalized روی IQueryable حافظه درست کار می‌کند (ترجمهٔ SQL آن
/// REPLACE/CHARINDEX استاندارد EF است).
/// </summary>
public class PersianSearchTests
{
    [Theory]
    [InlineData("علي", "علی")]
    [InlineData("كتاب", "کتاب")]
    [InlineData("مدرسة", "مدرسه")]
    [InlineData("۰۹۱۲۳۴۵۶۷۸۹", "09123456789")]
    [InlineData("٠٩١٢٣٤٥٦٧٨٩", "09123456789")]
    [InlineData("مـتن", "متن")]
    [InlineData("سَلام", "سلام")]
    [InlineData("  فاصله   اضافه  ", "فاصله اضافه")]
    public void Normalize_UnifiesVariants(string input, string expected)
    {
        Assert.Equal(expected, PersianSearch.Normalize(input));
    }

    [Fact]
    public void Normalize_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, PersianSearch.Normalize(null));
        Assert.Equal(string.Empty, PersianSearch.Normalize("   "));
    }

    [Fact]
    public void ContainsNormalized_MatchesArabicYehAgainstPersianText()
    {
        var rows = new[]
        {
            new Row("علی رضایی"),
            new Row("مریم کریمی"),
            new Row(null)
        }.AsQueryable();

        var term = PersianSearch.Normalize("علي"); // با ي عربی جست‌وجو می‌شود
        var matched = rows.Where(PersianSearch.ContainsNormalized<Row>(term, r => r.Name)).ToList();

        var single = Assert.Single(matched);
        Assert.Equal("علی رضایی", single.Name);
    }

    [Fact]
    public void ContainsNormalized_SearchesAcrossMultipleProperties()
    {
        var rows = new[]
        {
            new Row("نام", "test@example.com"),
            new Row("نام", "other@example.com")
        }.AsQueryable();

        var matched = rows.Where(
            PersianSearch.ContainsNormalized<Row>("test", r => r.Name, r => r.Email)).ToList();

        Assert.Single(matched);
    }

    private sealed record Row(string? Name, string? Email = null);
}
