using System;
using System.Collections.Generic;
using System.Linq;
using Techtoon.Core.Permissions;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// نظام الصلاحيات والأدوار:
/// - كل صلاحية في الكتالوج مرتبطة بصفحة أو فعل محدد.
/// - كل دور يملك صلاحياته الافتراضية.
/// - المستخدم يرث صلاحيات دوره، والاستثناءات تسبق الدور.
/// - كل مستخدم يرى صفحات مختلفة عن غيره.
/// </summary>
[Collection("DatabaseIntegration")]
public class PermissionsTests : IDisposable
{
    private readonly List<string> _createdUsers = new();

    public void Dispose()
    {
        foreach (var u in _createdUsers)
        {
            try { TestDb.Execute("DELETE FROM user_permissions WHERE user_id IN (SELECT id FROM users WHERE username=@u)", ("@u", u)); } catch { }
            try { TestDb.Execute("DELETE FROM audit_log WHERE username=@u", ("@u", u)); } catch { }
            try { TestDb.Execute("DELETE FROM users WHERE username=@u", ("@u", u)); } catch { }
        }
        AuthService.Logout();
    }

    // ==================== اختبارات الكتالوج (بلا قاعدة بيانات) ====================

    [Fact]
    public void Every_Role_Permission_Is_A_Real_Catalog_Key()
    {
        var known = new HashSet<string>(PermissionCatalog.AllKeys, StringComparer.OrdinalIgnoreCase);
        foreach (var role in RoleCatalog.All())
        {
            foreach (var key in role.Permissions)
                Assert.True(known.Contains(key),
                    $"الدور '{role.Name}' يشير إلى صلاحية غير موجودة: '{key}'");
        }
    }

    [Fact]
    public void Every_Page_Has_A_Permission_And_It_Exists()
    {
        var known = new HashSet<string>(PermissionCatalog.AllKeys, StringComparer.OrdinalIgnoreCase);
        foreach (var page in PermissionCatalog.AllPages)
        {
            var perm = PermissionCatalog.PagePermission(page);
            Assert.False(string.IsNullOrWhiteSpace(perm), $"الصفحة '{page}' بلا صلاحية");
            Assert.True(known.Contains(perm), $"الصفحة '{page}' تشير إلى صلاحية غير موجودة: '{perm}'");
        }
    }

    [Fact]
    public void Permission_Keys_Are_Unique_And_Well_Formed()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in PermissionCatalog.All)
        {
            Assert.True(seen.Add(p.Key), $"صلاحية مكررة: {p.Key}");
            Assert.Contains(".", p.Key);
            Assert.False(string.IsNullOrWhiteSpace(p.Label));
            Assert.False(string.IsNullOrWhiteSpace(p.ModuleLabel));
        }
    }

    [Fact]
    public void Roles_Are_Defined_For_The_Whole_Catalog()
    {
        var roles = RoleCatalog.All();
        Assert.Equal(7, roles.Count);
        Assert.Contains(roles, r => r.Name == "ADMIN");
        Assert.Contains(roles, r => r.Name == "VIEWER");

        // ADMIN يملك كل الصلاحيات
        var admin = roles.First(r => r.Name == "ADMIN");
        Assert.Equal(PermissionCatalog.AllKeys.Count, admin.Permissions.Count);

        // لاManaged دور يملك إدارة المستخدمين عدا ADMIN
        foreach (var r in roles.Where(r => r.Name != "ADMIN"))
            Assert.DoesNotContain(r.Permissions, k => k.StartsWith("users.roles"));

        // VIEWER لا يملك صلاحيات تعديل
        var viewer = roles.First(r => r.Name == "VIEWER");
        Assert.All(viewer.Permissions, k => Assert.EndsWith(".view", k));
    }

    [Fact]
    public void Different_Roles_Expose_Different_Pages()
    {
        var pagePerms = PermissionCatalog.AllPages
            .Select(p => (Page: p, Perm: PermissionCatalog.PagePermission(p)))
            .Where(x => !string.IsNullOrEmpty(x.Perm))
            .ToList();

        var salesman = RoleCatalog.Find("SALESPERSON")!.Permissions.ToHashSet();
        var keeper = RoleCatalog.Find("STOREKEEPER")!.Permissions.ToHashSet();
        var accountant = RoleCatalog.Find("ACCOUNTANT")!.Permissions.ToHashSet();

        // مندوب المبيعات يرى الفواتير ولا يرى المخزون المحاسبي
        Assert.Contains(pagePerms, x => x.Page == "Invoices" && salesman.Contains(x.Perm));
        Assert.DoesNotContain(pagePerms, x => x.Page == "StockCounts" && salesman.Contains(x.Perm));

        // أمين المخزن يرى الجرد ولا يرى الفواتير القابلة للتعديل
        Assert.Contains(pagePerms, x => x.Page == "StockCounts" && keeper.Contains(x.Perm));
        Assert.DoesNotContain(pagePerms, x => x.Page == "JournalEntries" && keeper.Contains(x.Perm));

        // المحاسب يرى القيود والتقارير
        Assert.Contains(pagePerms, x => x.Page == "JournalEntries" && accountant.Contains(x.Perm));
        Assert.Contains(pagePerms, x => x.Page == "Reports" && accountant.Contains(x.Perm));

        // ولا أحد يرى إدارة الأدوار عدا ADMIN
        var rolesPerm = PermissionCatalog.PagePermission("RolesPermissions");
        Assert.All(RoleCatalog.All().Where(r => r.Name != "ADMIN"), r => Assert.DoesNotContain(rolesPerm, r.Permissions));
    }

    [Fact]
    public void Legacy_Permission_Keys_Map_To_Modern_Ones()
    {
        // المفتاح القديم "sales" يقبل الصلاحية الحديثة invoices.view
        Assert.Contains("invoices.view", PermissionCatalog.LegacyAliases["sales"]);
        Assert.Contains("users.view", PermissionCatalog.ReverseAliases.Keys);
        Assert.Contains("manage_users", PermissionCatalog.ReverseAliases["users.view"]);
        Assert.All(PermissionCatalog.LegacyAliases, kv =>
            Assert.All(kv.Value, m => Assert.Contains(m, PermissionCatalog.AllKeys)));
    }

    // ==================== اختبارات قاعدة البيانات ====================

    private int EnsureSchema()
    {
        if (!TestDb.Available) return 0;
        RoleService.EnsureSchema();
        return 1;
    }

    private int CreateTestUser(string roleName)
    {
        var username = "permtest_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        _createdUsers.Add(username);
        var hash = BCrypt.Net.BCrypt.HashPassword("Test1234!", workFactor: 10);
        var role = RoleService.FindByName(roleName);
        var roleId = role?.Id ?? -1;
        var storedRole = role?.Name ?? roleName;
        TestDb.Execute(
            "INSERT INTO users (username, password_hash, full_name, role, role_id, is_active, created_at) " +
            "VALUES (@u, @h, @fn, @r, @rid, TRUE, NOW())",
            ("@u", username), ("@h", hash), ("@fn", username), ("@r", storedRole), ("@rid", roleId));
        var id = Convert.ToInt32(TestDb.Scalar("SELECT id FROM users WHERE username=@u", ("@u", username)));
        return id;
    }

    [Fact]
    public void Schema_Is_Created_And_System_Roles_Are_Seeded()
    {
        if (!TestDb.Available) return;
        EnsureSchema();

        // الجدول موجود
        Assert.NotNull(TestDb.Scalar("SELECT to_regclass('public.role_permissions')::text"));

        // الأدوار السبعة مزروعة
        var roles = RoleService.GetRoles();
        Assert.Equal(7, roles.Count);

        // ولكل دور صلاحياته الافتراضية
        foreach (var role in roles)
        {
            var perms = RoleService.GetRolePermissions(role.Id);
            var expected = RoleCatalog.Find(role.Name)!.Permissions;
            Assert.True(perms.Count >= expected.Count,
                $"الدور '{role.Name}' يملك {perms.Count} صلاحية بينما المتوقع {expected.Count}");
        }
        // دور ADMIN يملك كل الصلاحيات
        var admin = roles.First(r => r.Name == "ADMIN");
        Assert.Equal(PermissionCatalog.AllKeys.Count, RoleService.GetRolePermissions(admin.Id).Count);
    }

    [Fact]
    public void User_Inherits_The_Permissions_Of_Its_Role()
    {
        if (!TestDb.Available) return;
        EnsureSchema();

        var userId = CreateTestUser("SALESPERSON");
        var role = RoleService.FindByName("SALESPERSON")!;
        var rolePerms = RoleService.GetRolePermissions(role.Id);

        var effective = RoleService.ResolveEffectivePermissions(userId, role.Id, role.Name);

        // كل صلاحيات الدور موجودة
        foreach (var key in rolePerms)
            Assert.Contains(key, effective);

        // ولا صلاحية خارج الدور
        foreach (var key in effective)
            Assert.Contains(key, rolePerms);

        // الصفحات المتوقعة تظهر والغير المتوقعة لا
        Assert.Contains("invoices.view", effective);
        Assert.DoesNotContain("users.roles", effective);
        Assert.DoesNotContain("journal_entries", effective);
    }

    [Fact]
    public void User_Override_Grants_A_Permission_Above_The_Role()
    {
        if (!TestDb.Available) return;
        EnsureSchema();

        var userId = CreateTestUser("CASHIER");
        var role = RoleService.FindByName("CASHIER")!;

        // الكاشير لا يملك تقارير الأرباح
        Assert.DoesNotContain("reports.profit", RoleService.ResolveEffectivePermissions(userId, role.Id, role.Name));

        // استثناء يمنح له الصلاحية
        RoleService.SetUserOverrides(userId, new Dictionary<string, bool> { ["reports.profit"] = true });

        var after = RoleService.ResolveEffectivePermissions(userId, role.Id, role.Name);
        Assert.Contains("reports.profit", after);
        // وبقية صلاحيات الدور لم تتأثر
        Assert.Contains("payments.edit", after);
    }

    [Fact]
    public void User_Override_Denies_A_Permission_From_The_Role()
    {
        if (!TestDb.Available) return;
        EnsureSchema();

        var userId = CreateTestUser("ACCOUNTANT");
        var role = RoleService.FindByName("ACCOUNTANT")!;
        Assert.Contains("invoices.edit", RoleService.ResolveEffectivePermissions(userId, role.Id, role.Name));

        // استثناء يمنع الصلاحية رغم منحها من الدور
        RoleService.SetUserOverrides(userId, new Dictionary<string, bool> { ["invoices.edit"] = false });

        var after = RoleService.ResolveEffectivePermissions(userId, role.Id, role.Name);
        Assert.DoesNotContain("invoices.edit", after);
        // باقي صلاحيات الدور سليمة
        Assert.Contains("journals.post", after);
    }

    [Fact]
    public void Admin_Role_Resolves_To_Every_Permission()
    {
        if (!TestDb.Available) return;
        EnsureSchema();

        var userId = CreateTestUser("ADMIN");
        var effective = RoleService.ResolveEffectivePermissions(userId, null, "ADMIN");
        Assert.Equal(PermissionCatalog.AllKeys.Count, effective.Count);
        foreach (var key in PermissionCatalog.AllKeys)
            Assert.Contains(key, effective);
    }

    [Fact]
    public void Login_Resolves_Permissions_From_The_Role_And_Overrides()
    {
        if (!TestDb.Available) return;
        EnsureSchema();

        var username = "permtest_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        _createdUsers.Add(username);
        var hash = BCrypt.Net.BCrypt.HashPassword("Test1234!", workFactor: 10);
        TestDb.Execute(
            "INSERT INTO users (username, password_hash, full_name, role, role_id, is_active, created_at) " +
            "VALUES (@u, @h, @fn, 'STOREKEEPER', (SELECT id FROM roles WHERE name='STOREKEEPER'), TRUE, NOW())",
            ("@u", username), ("@h", hash), ("@fn", username));

        try
        {
            var result = AuthService.Login(username, "Test1234!");
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal("STOREKEEPER", AuthService.CurrentUser!.Role);

            // يملك صلاحية الجرد ولا يملك إدارة المستخدمين
            Assert.True(AuthService.HasPermission("inventory.count"));
            Assert.False(AuthService.HasPermission("users.view"));

            // استثناء يمنح صلاحية إضافية
            var userId = AuthService.CurrentUser.Id;
            RoleService.SetUserOverrides(userId, new Dictionary<string, bool> { ["users.view"] = true });
            AuthService.ReloadPermissions();
            Assert.True(AuthService.HasPermission("users.view"));

            // المفتاح القديم يُقبل عبر الصلاحية الحديثة (توافق مع واجهة API)
            Assert.True(AuthService.HasPermission("stock_adjustment"));   // inventory.adjust
            Assert.True(AuthService.HasPermission("manage_users"));        // users.*
        }
        finally
        {
            AuthService.Logout();
        }
    }

    [Fact]
    public void Setting_A_Role_For_A_User_Changes_The_Effective_Permissions()
    {
        if (!TestDb.Available) return;
        EnsureSchema();

        var userId = CreateTestUser("VIEWER");
        var viewer = RoleService.FindByName("VIEWER")!;
        var cashier = RoleService.FindByName("CASHIER")!;

        // كاشير يملك تعديل المدفوعات، والمطّلع لا يملكه
        Assert.Contains("payments.edit", RoleService.ResolveEffectivePermissions(userId, cashier.Id, cashier.Name));
        Assert.DoesNotContain("payments.edit", RoleService.ResolveEffectivePermissions(userId, viewer.Id, viewer.Name));

        // تغيير الدور فعليًا يغيّر الصلاحيات
        RoleService.SetUserRole(userId, cashier.Id);
        var roleName = Convert.ToString(TestDb.Scalar("SELECT role FROM users WHERE id=@id", ("@id", userId)));
        Assert.Equal("CASHIER", roleName);

        var after = RoleService.ResolveEffectivePermissions(userId, cashier.Id, roleName!);
        Assert.Contains("payments.edit", after);
    }

    [Fact]
    public void Custom_Role_Can_Be_Created_And_Deleted()
    {
        if (!TestDb.Available) return;
        EnsureSchema();

        var name = "TESTROLE" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        int roleId = RoleService.CreateRole(name, "دور اختباري", "أُنشئ في الاختبار");
        try
        {
            Assert.NotNull(RoleService.FindByName(name));

            RoleService.SetRolePermissions(roleId, new[] { "dashboard.view", "items.view" });
            var perms = RoleService.GetRolePermissions(roleId);
            Assert.Equal(2, perms.Count);
            Assert.Contains("items.view", perms);

            // مستخدم بهذا الدور يرى صفحتين فقط
            var userId = CreateTestUser(name);
            var effective = RoleService.ResolveEffectivePermissions(userId, roleId, name);
            Assert.Equal(2, effective.Count);
        }
        finally
        {
            RoleService.DeleteRole(roleId);
        }

        Assert.Null(RoleService.FindByName(name));
    }

    [Fact]
    public void Deleting_A_Role_Moves_Its_Users_To_Viewer()
    {
        if (!TestDb.Available) return;
        EnsureSchema();

        var name = "TMPROLE" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        int roleId = RoleService.CreateRole(name, "دور مؤقت", "");
        var userId = CreateTestUser(name);

        RoleService.DeleteRole(roleId);

        var roleName = Convert.ToString(TestDb.Scalar("SELECT role FROM users WHERE id=@id", ("@id", userId)));
        Assert.Equal("VIEWER", roleName);
    }

    [Fact]
    public void System_Roles_Cannot_Be_Deleted()
    {
        if (!TestDb.Available) return;
        EnsureSchema();

        var admin = RoleService.FindByName("ADMIN")!;
        Assert.Throws<InvalidOperationException>(() => RoleService.DeleteRole(admin.Id));
    }
}
