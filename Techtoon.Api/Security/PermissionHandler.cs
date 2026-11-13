using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Techtoon.Core.Permissions;

namespace Techtoon.Api.Security;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        // دور ADMIN يملك كل الصلاحيات
        if (context.User.IsInRole("ADMIN"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (Has(context, requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // مطابقة بديلة: المفتاح القديم يقبل الحديث والعكس.
        // بدونها تُرفض كل نقطة محمية بمفتاح قديم لمستخدم غير ADMIN.
        if (PermissionCatalog.LegacyAliases.TryGetValue(requirement.Permission, out var modern))
            foreach (var m in modern)
                if (Has(context, m)) { context.Succeed(requirement); return Task.CompletedTask; }

        if (PermissionCatalog.ReverseAliases.TryGetValue(requirement.Permission, out var legacy))
            foreach (var l in legacy)
                if (Has(context, l)) { context.Succeed(requirement); return Task.CompletedTask; }

        return Task.CompletedTask;
    }

    private static bool Has(AuthorizationHandlerContext context, string key)
        => !string.IsNullOrWhiteSpace(key) && context.User.HasClaim("permission", key);
}
