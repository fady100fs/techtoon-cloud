using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

public class RoleSeedDebugTests
{
    [Fact]
    public void EnsureSchema_Removes_Extra_Permissions_From_Cashier()
    {
        if (!TestDb.Available) return;
        var before = TestDb.Scalar(
            "SELECT COUNT(*) FROM role_permissions rp JOIN roles r ON r.id=rp.role_id WHERE r.name='CASHIER' AND rp.permission_key='subscriptions.edit' AND rp.is_granted=TRUE");
        RoleService.EnsureSchema();
        var after = TestDb.Scalar(
            "SELECT COUNT(*) FROM role_permissions rp JOIN roles r ON r.id=rp.role_id WHERE r.name='CASHIER' AND rp.permission_key='subscriptions.edit' AND rp.is_granted=TRUE");
        // The seed should remove subscriptions.edit from the cashier (not in catalog)
        Assert.True(Convert.ToInt32(after) == 0,
            "seed did not remove subscriptions.edit from cashier (before=" + before + " after=" + after + ")");
    }
}
