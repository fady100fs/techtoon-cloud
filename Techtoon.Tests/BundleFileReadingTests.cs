using System;
using System.IO;
using System.Linq;
using Techtoon.Core.Bundles;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// قراءة ملف الحزمة — الملف قد يكون مفتوحًا في Excel أو محجوبًا بمزامنة Google Drive.
/// </summary>
public class BundleFileReadingTests : IDisposable
{
    private readonly string _work;

    public BundleFileReadingTests()
    {
        _work = Path.Combine(Path.GetTempPath(), "techtoon_lock_" + Guid.NewGuid().ToString("N") + ".xlsx");
    }

    public void Dispose()
    {
        try { if (File.Exists(_work)) File.Delete(_work); } catch { }
    }

    /// <summary>ملف Excel صالح بأورقة واحدة.</summary>
    private static void WriteSampleWorkbook(string path)
    {
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.AddWorksheet("accounts");
        ws.Cell(1, 1).SetValue("code");
        ws.Cell(1, 2).SetValue("name");
        ws.Cell(2, 1).SetValue("1000");
        ws.Cell(2, 2).SetValue("Cash");
        wb.SaveAs(path);
    }

    [Fact]
    public void File_Shared_For_Reading_Can_Be_Imported()
    {
        WriteSampleWorkbook(_work);

        // كما لو كان الملف مفتوحًا للمشاركة (Excel في وضع القراءة / Google Drive)
        using (var share = new FileStream(_work, FileMode.Open, FileAccess.Read,
                   FileShare.ReadWrite | FileShare.Delete))
        {
            var bundle = MasterDataBundleService.ReadExcelBundle(_work);
            Assert.Single(bundle.Tables);
            Assert.NotEmpty(bundle.Tables["accounts"]);
        }
    }

    [Fact]
    public void Exclusively_Locked_File_Gives_A_Clear_Arabic_Message()
    {
        WriteSampleWorkbook(_work);

        using (var lockStream = new FileStream(_work, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => MasterDataBundleService.ReadExcelBundle(_work));
            Assert.Contains("Excel", ex.Message);
            Assert.Contains("أغلق الملف", ex.Message);
        }
    }

    [Fact]
    public void Missing_File_Reports_An_Error()
    {
        var missing = Path.Combine(Path.GetTempPath(), "no_such_" + Guid.NewGuid().ToString("N") + ".xlsx");
        Assert.ThrowsAny<Exception>(() => MasterDataBundleService.ReadExcelBundle(missing));
    }

    [Fact]
    public void Corrupt_File_Reports_An_Error()
    {
        File.WriteAllText(_work, "this is not a workbook");
        Assert.ThrowsAny<Exception>(() => MasterDataBundleService.ReadExcelBundle(_work));
    }
}
