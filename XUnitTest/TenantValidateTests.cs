using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using NewLife.Cube;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>多租户认证层校验（ValidateTenant）语义测试：普通用户必须携带有效租户，管理员豁免</summary>
[Collection("SqliteDb")]
public class TenantValidateTests : IDisposable
{
    private readonly Boolean _origin;

    public TenantValidateTests()
    {
        SqliteDb.Ensure();

        // Tenant 表不在共享初始化列表内，单独补齐结构
        var dal = DAL.Create("Membership");
        dal.SetTables((IDataTable)Tenant.Meta.Table.DataTable.Clone());
        var _ = Tenant.Meta.Count;

        _origin = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = true;
    }

    public void Dispose() => CubeSetting.Current.EnableTenant = _origin;

    [Fact(DisplayName = "多租户校验：普通用户缺租户标识被拒，管理员豁免")]
    public void MissingIdentifier()
    {
        var ctx = new DefaultHttpContext();
        var user = CreateUser(false);

        Assert.False(ctx.ValidateTenant(user));
        Assert.Equal("MissingIdentifier", ctx.Items[ManagerProviderHelper.TenantCheckFailedReasonKey] as String);

        var admin = CreateUser(true);
        var ctx2 = new DefaultHttpContext();
        Assert.True(ctx2.ValidateTenant(admin));
    }

    [Fact(DisplayName = "多租户校验：显式租户不存在被拒，非成员被拒，成员放行")]
    public void ExplicitTenant()
    {
        var tenant = CreateTenant();
        var user = CreateUser(false);

        var ctx1 = new DefaultHttpContext();
        ctx1.Request.Headers["X-Tenant"] = "not-exist-" + Guid.NewGuid().ToString("N")[..6];
        Assert.False(ctx1.ValidateTenant(user));
        Assert.Equal("InvalidIdentifier", ctx1.Items[ManagerProviderHelper.TenantCheckFailedReasonKey] as String);

        var ctx2 = new DefaultHttpContext();
        ctx2.Request.Headers["X-Tenant"] = tenant.Code;
        Assert.False(ctx2.ValidateTenant(user));
        Assert.Equal("NotMember", ctx2.Items[ManagerProviderHelper.TenantCheckFailedReasonKey] as String);

        new TenantUser { TenantId = tenant.Id, UserId = user.Id, Enable = true }.Insert();

        var ctx3 = new DefaultHttpContext();
        ctx3.Request.Headers["X-Tenant"] = tenant.Code;
        Assert.True(ctx3.ValidateTenant(user));
    }

    private static User CreateUser(Boolean system)
    {
        var role = new Role { Name = $"租户角色_{Guid.NewGuid():N}", IsSystem = system };
        role.Insert();

        var user = new User { Name = $"租户用户_{Guid.NewGuid():N}", RoleId = role.Id, Enable = true };
        user.Insert();
        return user;
    }

    private static Tenant CreateTenant()
    {
        var tenant = new Tenant { Name = $"租户_{Guid.NewGuid():N}", Code = "t" + Guid.NewGuid().ToString("N")[..8], Enable = true };
        tenant.Insert();
        return tenant;
    }
}
