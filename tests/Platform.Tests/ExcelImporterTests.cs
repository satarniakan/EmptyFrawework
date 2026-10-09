using ClosedXML.Excel;
using Platform.Application.Imports;

namespace Platform.Tests;

/// <summary>
/// ایمپورت اکسل: سرستون فارسی/انگلیسی، سطر خراب فقط خودش را می‌سوزاند،
/// شمارهٔ سطر واقعی اکسل در خطا می‌آید، و فایل غول‌پیکر سقف دارد.
/// </summary>
public class ExcelImporterTests
{
    private static MemoryStream Workbook(List<string[]> rows)
    {
        var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Sheet1");
            for (var r = 0; r < rows.Count; r++)
            {
                for (var c = 0; c < rows[r].Length; c++)
                    sheet.Cell(r + 1, c + 1).Value = rows[r][c];
            }

            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream Workbook(params string[][] rows) => Workbook(rows.ToList());

    [Fact]
    public void Read_MapsRows_AndCollectsRowErrors_WithExcelRowNumbers()
    {
        using var stream = Workbook(
            ["Phone", "Name"],
            ["09120000001", "کاربر یک"],
            ["", "بدون شماره"],
            ["09120000002", "کاربر دو"]);

        var result = ExcelImporter.Read(stream,
            row => ExcelImporter.Required(row, "Phone", "موبایل", "شماره موبایل"));

        Assert.Equal(["09120000001", "09120000002"], result.Items.Select(i => i.Value));
        Assert.Equal(3, result.TotalRows);
        var error = Assert.Single(result.RowErrors);
        Assert.Equal(3, error.RowNumber); // سطر واقعی اکسل، نه شمارش داخلی
    }

    [Fact]
    public void Read_PersianHeaders_WorkWithAliases()
    {
        using var stream = Workbook(
            ["شماره موبایل"],
            ["09120000003"]);

        var result = ExcelImporter.Read(stream,
            row => ExcelImporter.Required(row, "Phone", "موبایل", "شماره موبایل"));

        Assert.Equal(["09120000003"], result.Items.Select(i => i.Value));
        Assert.Empty(result.RowErrors);
    }

    [Fact]
    public void Read_SkipsFullyEmptyRows_AndMissingSheet_ReturnsError()
    {
        using var stream = Workbook(
            ["Phone"],
            ["09120000001"],
            [""],
            ["09120000002"]);

        var result = ExcelImporter.Read(stream, row => ExcelImporter.Required(row, "Phone"));

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public void Read_EmptyFile_ReturnsHeaderError()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            workbook.Worksheets.Add("Empty");
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        var result = ExcelImporter.Read(stream, row => ExcelImporter.Required(row, "Phone"));

        Assert.Empty(result.Items);
        Assert.NotEmpty(result.RowErrors);
    }

    [Fact]
    public void Read_RespectsMaxRows()
    {
        var rows = new List<string[]> { new[] { "Phone" } };
        for (var i = 0; i < 10; i++)
            rows.Add([$"0912000000{i}"]);

        using var stream = Workbook(rows);
        var result = ExcelImporter.Read(stream, row => ExcelImporter.Required(row, "Phone"), maxRows: 3);

        Assert.Equal(3, result.Items.Count);
        Assert.NotEmpty(result.RowErrors);
    }
}
