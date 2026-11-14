using System;
using System.Linq;
using Techtoon.UI.Services;
using Techtoon.UI.ViewModels;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// شاشة إدارة البيانات — العرض الديناميكي للمجموعات.
/// <para>لا تحتاج قاعدة بيانات: تتحقق من أن ViewModel يعرض كل المجموعات
/// المعرَّفة في الخدمة، وأن كل مجموعة تحمل عنوانًا ووصفًا وأمر حذف.</para>
/// </summary>
public class DataManagementViewModelTests
{
    [Fact]
    public void The_View_Model_Exposes_Every_Group_From_The_Service()
    {
        var vm = new DataManagementViewModel();

        Assert.Equal(DataManagementService.Groups.Count, vm.Groups.Count);
        Assert.Equal(
            DataManagementService.Groups.Select(g => g.Id).OrderBy(x => x),
            vm.Groups.Select(g => g.Id).OrderBy(x => x));

        // كل مجموعة داخل تصنيف واحد على الأقل
        var inCategories = vm.Categories.SelectMany(c => c.Groups).Select(g => g.Id).ToList();
        Assert.Equal(vm.Groups.Count, inCategories.Count);
        foreach (var g in vm.Groups)
            Assert.Contains(g.Id, inCategories);
    }

    [Fact]
    public void Every_Group_Has_A_Title_A_Description_And_Deletable_Steps()
    {
        foreach (var g in DataManagementService.Groups)
        {
            Assert.False(string.IsNullOrWhiteSpace(g.Title), "مجموعة بلا عنوان: " + g.Id);
            Assert.False(string.IsNullOrWhiteSpace(g.Description), "مجموعة بلا وصف: " + g.Id);
            Assert.False(string.IsNullOrWhiteSpace(g.Category), "مجموعة بلا تصنيف: " + g.Id);
            Assert.NotEmpty(g.Steps);

            foreach (var step in g.Steps)
            {
                Assert.False(string.IsNullOrWhiteSpace(step.Table), "خطوة بلا جدول في " + g.Id);
                Assert.False(string.IsNullOrWhiteSpace(step.Label), "خطوة بلا وصف في " + g.Id);
            }
        }
    }

    [Fact]
    public void The_Delete_Commands_Are_Wired()
    {
        var vm = new DataManagementViewModel();
        Assert.NotNull(vm.DeleteGroupCommand);
        Assert.NotNull(vm.DeletePartyDataCommand);
        Assert.NotNull(vm.DeleteBeforeDateCommand);
        Assert.NotNull(vm.FactoryResetCommand);
        Assert.NotNull(vm.RefreshPreviewCommand);
        Assert.True(vm.DeleteGroupCommand.CanExecute(null));
    }

    /// <summary>النطاقات الجزئية يجب أن تُنتج خطوات بأسماء جداول صحيحة.</summary>
    [Fact]
    public void The_Scoped_Deletions_Cover_The_Main_Transaction_Tables()
    {
        var partySteps = DataManagementService.PartyScopeSteps(1, includePartyRecord: true);
        var partyTables = partySteps.Select(s => s.Table).ToList();
        Assert.Contains("invoices", partyTables);
        Assert.Contains("payments", partyTables);
        Assert.Contains("subscriptions", partyTables);
        Assert.Contains("parties", partyTables);

        var scope = new DeleteScope { Kind = "before_date", Before = new DateTime(2025, 1, 1) };
        var preview = DataManagementService.PreviewScope(scope);
        Assert.NotEmpty(preview.Items);
        Assert.Contains(preview.Items, i => i.TableName == "invoices");
        Assert.Contains(preview.Items, i => i.TableName == "payments");
    }
}
