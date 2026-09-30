using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Areas.Admin.Models;
using XCode;
using XCode.Membership;
using XCode.Model;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>角色页批量改字段：JSON Valid 不得在落库前清缓存或按空 Permission 重建</summary>
[Collection("TenantAuth")]
public class RoleBatchUpdateFieldsTests
{
    private readonly TenantAuthFixture _fx;

    public RoleBatchUpdateFieldsTests(TenantAuthFixture fx) => _fx = fx;

    private sealed class TestRoleController : RoleController
    {
        protected override Boolean ValidPermission(Role entity, DataObjectMethodType type, Boolean post) => true;

        protected override WhereBuilder CreateWhere() => null;
    }

    [Fact(DisplayName = "BatchUpdateFields：DataScope 与 Ex1 落库且列表再读可见")]
    public void BatchUpdateFields_Writes_DataScope_And_Ex1()
    {
        var role = new Role
        {
            Name = "BU" + DateTime.Now.Ticks,
            Enable = true,
            Type = RoleTypes.普通,
            DataScope = DataScopes.仅本人,
            Ex1 = 0,
        };
        role.Insert();

        var ctx = _fx.CreateContext();
        ctx.Items["CurrentUser"] = _fx.AdminUser;
        ctx.Items["userId"] = _fx.AdminUser.ID;
        ctx.Request.ContentType = "application/json";

        var ad = new ControllerActionDescriptor();
        ad.RouteValues["action"] = "BatchUpdateFields";
        ad.RouteValues["controller"] = "Role";
        ad.RouteValues["area"] = "Admin";

        var oldTenant = TenantContext.Current;
        TenantContext.Current = null;
        try
        {
            var controller = new TestRoleController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = ctx,
                    ActionDescriptor = ad,
                },
            };

            var res = controller.BatchUpdateFields(new EntityController<Role, RoleModel>.BatchUpdateFieldsRequest
            {
                Keys = role.ID + "",
                Fields =
                [
                    new() { Field = "DataScope", Value = "1" },
                    new() { Field = "Ex1", Value = "10" },
                ],
            });

            Assert.Equal(0, res.Code);
            Assert.True(res.Data.Ok >= 1, $"Ok={res.Data.Ok} Fail={res.Data.Fail} {res.Data.Errors?.FirstOrDefault()?.Message}");
            Assert.Equal(0, res.Data.Fail);

            Role.Meta.Session.ClearCache("assert", true);
            var again = Role.FindByID(role.ID);
            Assert.NotNull(again);
            Assert.Equal(DataScopes.本部门及下级, again.DataScope);
            Assert.Equal(10, again.Ex1);
        }
        finally
        {
            TenantContext.Current = oldTenant!;
        }
    }
}
