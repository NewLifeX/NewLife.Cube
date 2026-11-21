using System;
using System.Linq;
using NewLife.Cube.Tests.Membership;
using NewLife.Data;
using XCode;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// PERM-4 部门管理测试。覆盖部门创建与编码查找、租户/父级隔离、条件检索与名称必填校验。
/// </summary>
[Collection("TenantAuth")]
public class DepartmentTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public DepartmentTests(TenantAuthFixture fixture) => _fx = fixture;

    private static String Unique(String prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    [Fact(DisplayName = "部门_创建后可按名称与编码查找")]
    public void Department_Add_ThenFind()
    {
        var name = Unique("研发");
        var code = Unique("D");

        var dept = Department.Add(name, code, 0);

        Assert.NotNull(dept);
        Assert.True(dept.Id > 0);
        Assert.Equal(name, dept.Name);
        Assert.Equal(code, dept.Code);
        Assert.Equal(0, dept.ParentId);

        Assert.Equal(dept.Id, Department.FindByCode(code)?.Id);
        Assert.Contains(Department.FindAllByName(name), e => e.Id == dept.Id);
    }

    [Fact(DisplayName = "部门_父子层级与租户隔离")]
    public void Department_ParentAndTenantIsolation()
    {
        var tenantId = 91;
        var parent = new Department { TenantId = tenantId, Name = Unique("总部"), Type = (DepartmentTypes)1, Enable = true, Visible = true };
        parent.Insert();
        var child = new Department { TenantId = tenantId, Name = Unique("分部"), ParentId = parent.Id, Type = (DepartmentTypes)2, Enable = true, Visible = true };
        child.Insert();

        // 父级隔离
        var children = Department.FindAllByTenantIdAndParentId(tenantId, parent.Id);
        Assert.Contains(children, e => e.Id == child.Id);
        Assert.DoesNotContain(children, e => e.Id == parent.Id);

        // 租户隔离：另一租户同名部门不串数据
        var otherTenant = new Department { TenantId = tenantId + 1, Name = child.Name, ParentId = parent.Id, Type = (DepartmentTypes)2, Enable = true, Visible = true };
        otherTenant.Insert();

        Assert.Equal(child.Id, Department.FindByTenantIdAndParentIdAndName(tenantId, parent.Id, child.Name)?.Id);
        Assert.Equal(otherTenant.Id, Department.FindByTenantIdAndParentIdAndName(tenantId + 1, parent.Id, child.Name)?.Id);
        Assert.Null(Department.FindByTenantIdAndParentIdAndName(tenantId + 2, parent.Id, child.Name));
    }

    [Fact(DisplayName = "部门_按名称关键字与父级条件检索")]
    public void Department_Search_Filters()
    {
        var tenantId = 92;
        var parent = new Department { TenantId = tenantId, Name = Unique("集团"), Type = (DepartmentTypes)1, Enable = true, Visible = true };
        parent.Insert();
        var hit = new Department { TenantId = tenantId, Name = "财务" + Unique("中心"), ParentId = parent.Id, Type = (DepartmentTypes)2, Enable = true, Visible = true };
        hit.Insert();
        var miss = new Department { TenantId = tenantId, Name = Unique("市场中心"), ParentId = parent.Id, Type = (DepartmentTypes)2, Enable = true, Visible = true };
        miss.Insert();

        var page = new PageParameter { PageIndex = 1, PageSize = 20 };
        var list = Department.Search(tenantId, (DepartmentTypes)(-1), -1, -1, null, null, "财务", page);

        Assert.Contains(list, e => e.Id == hit.Id);
        Assert.DoesNotContain(list, e => e.Id == miss.Id);

        var all = Department.FindAllByTenantIdAndParentId(tenantId, parent.Id);
        Assert.Contains(all, e => e.Id == hit.Id);
        Assert.Contains(all, e => e.Id == miss.Id);
    }

    [Fact(DisplayName = "部门_名称必填")]
    public void Department_NameRequired()
    {
        var dept = new Department { Name = null, TenantId = 0, Enable = true, Visible = true };

        Assert.Throws<ArgumentNullException>(() => dept.Insert());
    }
}


