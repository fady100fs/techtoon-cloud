using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// حارس ضد تكرار خلل «Microsoft.VisualBasic.Interaction.InputBox»:
/// هذه الدالة تتطلب System.Windows.Forms، وتطبيقنا WPF بلا WinForms،
/// فيرمي <see cref="PlatformNotSupportedException"/> عند أي استدعاء ويُسقط الزر.
/// </summary>
public class PlatformCompatibilityTests
{
    /// <summary>مجلد مصدر التطبيق.</summary>
    private static string UiSourceDirectory()
    {
        // Techtoon.Tests/bin/<cfg>/net10.0-windows  →  جذر المستودع
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Techtoon.UI");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("لم يُعثر على مجلد Techtoon.UI من: " + AppContext.BaseDirectory);
    }

    private static string[] SourceFiles()
    {
        var root = UiSourceDirectory();
        return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
            .ToArray();
    }

    [Fact]
    public void No_Interaction_InputBox_In_Wpf_Sources()
    {
        var offenders = new System.Collections.Generic.List<string>();
        foreach (var file in SourceFiles())
        {
            var text = File.ReadAllText(file);
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                // نتجاهل التعليقات التي تذكر الاسم فقط (توضيحية)
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("*")) continue;
                if (line.Contains("Interaction.InputBox") || line.Contains("Interaction.MsgBox"))
                    offenders.Add(Path.GetFileName(file) + ":" + (i + 1));
            }
        }
        Assert.True(offenders.Count == 0,
            "Interaction.InputBox غير مدعوم في WPF (يتطلب WinForms). استبدلها بـ InputDialog في: "
            + string.Join(", ", offenders));
    }

    [Fact]
    public void No_WindowsForms_Usages_In_Wpf_Sources()
    {
        var offenders = new System.Collections.Generic.List<string>();
        foreach (var file in SourceFiles())
        {
            var text = File.ReadAllText(file);
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("*")) continue;
                if (lines[i].Contains("using System.Windows.Forms;")
                    || lines[i].Contains("System.Windows.Forms.MessageBox")
                    || lines[i].Contains("System.Windows.Forms.SendKeys")
                    || lines[i].Contains("System.Windows.Forms.OpenFileDialog"))
                    offenders.Add(Path.GetFileName(file) + ":" + (i + 1));
            }
        }
        Assert.True(offenders.Count == 0,
            "تطبيق WPF لا يجب أن يستخدم System.Windows.Forms: " + string.Join(", ", offenders));
    }

    [Fact]
    public void InputDialog_Exists_And_Is_Usable()
    {
        // نافذة الإدخال بديلة صريحة — نتأكد أنها موجودة ومربوطة
        var file = Path.Combine(UiSourceDirectory(), "Views", "InputDialog.xaml.cs");
        Assert.True(File.Exists(file), "InputDialog.xaml.cs غير موجود");
        var text = File.ReadAllText(file);
        Assert.Contains("public static string? Show(", text);
    }

    [Fact]
    public void InputDialog_Xaml_Has_No_Invalid_SizeToContent()
    {
        // WPF لا يقبل SizeToContent="Auto" (يقبل Manual / WidthAndHeight / Height فقط)
        var file = Path.Combine(UiSourceDirectory(), "Views", "InputDialog.xaml");
        Assert.True(File.Exists(file), "InputDialog.xaml غير موجود");
        var text = File.ReadAllText(file);
        Assert.DoesNotContain("SizeToContent=\"Auto\"", text);
    }
}
