using System;
using System.Linq;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Data;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>OSC-260813397e：租户 OnInsert→TenantUser、UserTenantSearch 隔离</summary>
public class Osc260813397eTenantTests
{
    public Osc260813397eTenantTests()
    {
        DAL.AddConnStr("Membership", "Data Source=Osc397eMembership;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    [Fact(DisplayName = "EnsureManagerTenantUser：插入后存在 Enable 的 TenantUser")]
    public void EnsureManager_Creates_TenantUser()
    {
        var mgr = new User
        {
            Name = "mgr397e_" + Guid.NewGuid().ToString("N")[..8],
            Enable = true,
        };
        mgr.Insert();

        var tenant = new Tenant
        {
            Name = "租户A",
            Code = "ta_" + Guid.NewGuid().ToString("N")[..6],
            ManagerId = mgr.ID,
            Enable = true,
            RoleIds = ",1,",
        };
        tenant.Insert();

        var tu = TenantManagerHelper.EnsureManagerTenantUser(tenant, true);
        Assert.True(tu.Id > 0);
        Assert.True(tu.Enable);
        Assert.Equal(tenant.Id, tu.TenantId);
        Assert.Equal(mgr.ID, tu.UserId);

        var found = TenantUser.FindByTenantIdAndUserId(tenant.Id, mgr.ID);
        Assert.NotNull(found);
        Assert.True(found!.Enable);
    }

    [Fact(DisplayName = "UserTenantSearch：仅返回本租户绑定用户")]
    public void UserTenantSearch_Filters_OtherTenant()
    {
        var u1 = new User { Name = "u1_" + Guid.NewGuid().ToString("N")[..8], Enable = true };
        var u2 = new User { Name = "u2_" + Guid.NewGuid().ToString("N")[..8], Enable = true };
        u1.Insert();
        u2.Insert();

        var t1 = new Tenant { Name = "T1", Code = "t1_" + Guid.NewGuid().ToString("N")[..6], ManagerId = u1.ID, Enable = true };
        var t2 = new Tenant { Name = "T2", Code = "t2_" + Guid.NewGuid().ToString("N")[..6], ManagerId = u2.ID, Enable = true };
        t1.Insert();
        t2.Insert();

        TenantManagerHelper.EnsureManagerTenantUser(t1, true);
        TenantManagerHelper.EnsureManagerTenantUser(t2, true);

        var page = new PageParameter { PageIndex = 1, PageSize = 50 };
        var list1 = UserTenantSearch.Search(t1.Id, Array.Empty<Int32>(), Array.Empty<Int32>(), Array.Empty<Int32>(), true, DateTime.MinValue, DateTime.MinValue, "", page);
        Assert.NotNull(list1);
        Assert.Contains(list1, x => x.ID == u1.ID);
        Assert.DoesNotContain(list1, x => x.ID == u2.ID);
    }
}
