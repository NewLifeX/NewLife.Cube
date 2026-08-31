using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Caching;
using NewLife.Common;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Controllers;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// 租户 UI 能力测试：多租户开关（EnableTenant）对菜单/字段的显隐控制，
/// 以及租户切换接口（Auth/SwitchTenant）与用户信息（Auth/Info）的租户数据。
/// 复用 <see cref="TenantAuthFixture"/>（SQLite 真实数据 + 真实服务管线），串行执行。
/// </summary>
[Collection("TenantAuth")]
public class TenantSwitchUiTests
{
    private readonly TenantAuthFixture _fx;

    public TenantSwitchUiTests(TenantAuthFixture fixture) => _fx = fixture;

    /// <summary>开启多租户并清空租户上下文</summary>
    private static void EnableTenant()
    {
        CubeSetting.Current.EnableTenant = true;
        CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Shadow;
        TenantContext.Current = null!;
    }

    /// <summary>恢复进程级状态</summary>
    private static void Restore()
    {
        CubeSetting.Current.EnableTenant = false;
        CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Shadow;
        TenantContext.Current = null!;
    }

    /// <summary>构造认证控制器。SwitchTenant/Info 不依赖短信/邮件/MFA 等能力，依赖以 null 占位</summary>
    private static AuthController CreateController(TenantAuthFixture fx, HttpContext ctx)
    {
        var controller = new AuthController(fx.UserService, null!, null!, new TestCacheProvider(), null!)
        {
            ControllerContext = new ControllerContext { HttpContext = ctx },
        };
        return controller;
    }

    #region 菜单门控（MenuHelper.IsTenantMenu）

    [Fact(DisplayName = "IsTenantMenu：租户管理菜单为 true，通用 Admin|Tenant 菜单为 false")]
    public void IsTenantMenu_Detects_TenantControllers()
    {
        // 租户管理/租户成员：菜单名含 Tenant 或租户专属
        Assert.True(NewLife.Cube.Membership.MenuHelper.IsTenantMenu(typeof(TenantController).FullName!));
        Assert.True(NewLife.Cube.Membership.MenuHelper.IsTenantMenu(typeof(TenantUserController).FullName!));

        // 通用菜单（用户管理虽声明 Admin|Tenant，但非租户专属）：多租户关闭时仍显示
        Assert.False(NewLife.Cube.Membership.MenuHelper.IsTenantMenu(typeof(UserController).FullName!));
        Assert.False(NewLife.Cube.Membership.MenuHelper.IsTenantMenu(typeof(MailConfigController).FullName!));

        Assert.False(NewLife.Cube.Membership.MenuHelper.IsTenantMenu(""));
        Assert.False(NewLife.Cube.Membership.MenuHelper.IsTenantMenu((String?)null));
        Assert.False(NewLife.Cube.Membership.MenuHelper.IsTenantMenu("No.Such.Controller"));
    }

    #endregion

    #region 字段门控（OnGetFields 隐藏租户字段）

    /// <summary>测试控制器子类，暴露受保护的 OnGetFields（基类已有公开 GetFields，改名避免遮蔽警告）</summary>
    private class TestTenantUserController : ReadOnlyEntityController<TenantUser>
    {
        public FieldCollection GetFieldsForTest(ViewKinds kind) => OnGetFields(kind, null);
    }

    [Fact(DisplayName = "OnGetFields：关闭多租户时列表字段不含 TenantId/TenantName，开启时包含")]
    public void OnGetFields_Hides_TenantId_When_Disabled()
    {
        var ctrl = new TestTenantUserController();
        try
        {
            // 开启多租户：列表字段应包含租户字段（TenantId 映射为 TenantName 展示）
            CubeSetting.Current.EnableTenant = true;
            var withTenant = ctrl.GetFieldsForTest(ViewKinds.List);
            Assert.Contains(withTenant, f => f.Name == "TenantName");

            // 关闭多租户：列表字段应隐藏租户字段
            CubeSetting.Current.EnableTenant = false;
            var withoutTenant = ctrl.GetFieldsForTest(ViewKinds.List);
            Assert.DoesNotContain(withoutTenant, f => f.Name == "TenantId");
            Assert.DoesNotContain(withoutTenant, f => f.Name == "TenantName");

            // 表单/搜索字段同样隐藏
            Assert.DoesNotContain(ctrl.GetFieldsForTest(ViewKinds.AddForm), f => f.Name == "TenantId");
            Assert.DoesNotContain(ctrl.GetFieldsForTest(ViewKinds.EditForm), f => f.Name == "TenantId");
            Assert.DoesNotContain(ctrl.GetFieldsForTest(ViewKinds.Search), f => f.Name == "TenantId");
            Assert.DoesNotContain(ctrl.GetFieldsForTest(ViewKinds.Detail), f => f.Name == "TenantId");
        }
        finally
        {
            Restore();
        }
    }

    #endregion

    #region 列表字段值注入（OnFillListValues）

    /// <summary>测试控制器子类，暴露 OnFillListValues</summary>
    private class TestFillValuesController : ReadOnlyEntityController<TenantUser>
    {
        public void FillForTest(IEnumerable<TenantUser> list, IList<DataField>? fields = null) => OnFillListValues(list, fields);
    }

    [Fact(DisplayName = "OnFillListValues：虚拟字段 GetValue 按行计算值写入 Items，DataVisible=false 跳过")]
    public void OnFillListValues_Injects_VirtualFieldValues()
    {
        var ctrl = new TestFillValuesController();

        var avatar = new ListField { Name = "AvatarImage", ItemType = "image" };
        avatar.GetValue = e => "url://" + (e as TenantUser)?.UserId;
        var hidden = new ListField { Name = "Hidden", GetValue = e => "x", DataVisible = e => false };
        var fields = new FieldCollection(ViewKinds.List) { avatar, hidden };

        var u1 = new TenantUser { UserId = 11 };
        var u2 = new TenantUser { UserId = 22 };

        ctrl.FillForTest([u1, u2], fields);

        // 虚拟字段按行计算值并写入实体扩展，随行 JSON 内联输出
        Assert.Equal("url://11", ((IExtend)u1).Items["AvatarImage"]);
        Assert.Equal("url://22", ((IExtend)u2).Items["AvatarImage"]);
        // DataVisible=false 的字段不输出
        Assert.False(((IExtend)u1).Items.ContainsKey("Hidden"));
        // 普通实体字段不在 Items 中（不重复注入）
        Assert.False(((IExtend)u1).Items.ContainsKey("UserId"));
    }

    #endregion

    #region 登录配置暴露多租户开关

    [Fact(DisplayName = "LoginConfig：EnableTenant 跟随 CubeSetting 开关")]
    public void LoginConfig_Exposes_EnableTenant()
    {
        try
        {
            CubeSetting.Current.EnableTenant = true;
            Assert.True(new LoginConfigModel().EnableTenant);

            CubeSetting.Current.EnableTenant = false;
            Assert.False(new LoginConfigModel().EnableTenant);
        }
        finally
        {
            Restore();
        }
    }

    #endregion

    #region 租户切换接口（Auth/SwitchTenant）

    [Fact(DisplayName = "SwitchTenant：未开启多租户时拒绝切换")]
    public void SwitchTenant_Rejects_When_Tenant_Disabled()
    {
        Restore();
        var ctx = _fx.CreateContext();
        _fx.Provider.Login("tenant01", TenantAuthFixture.Password, false);
        var controller = CreateController(_fx, ctx);

        Assert.Throws<InvalidOperationException>(() => controller.SwitchTenant(_fx.Tenant1.Id));
    }

    [Fact(DisplayName = "SwitchTenant：系统管理员切换管理后台（0）成功并写 Cookie")]
    public void SwitchTenant_Admin_To_Backend_Succeeds()
    {
        EnableTenant();
        try
        {
            var ctx = _fx.CreateContext();
            _fx.Provider.Login("admin01", TenantAuthFixture.Password, false);
            var controller = CreateController(_fx, ctx);

            var res = controller.SwitchTenant(0);
            Assert.True(res.Data);

            // 租户上下文已切到管理后台，Cookie 已写入（HttpOnly，服务端读取）
            Assert.Equal(0, TenantContext.CurrentId);
            var cookie = ctx.Response.Headers["Set-Cookie"].ToString();
            Assert.Contains("TenantId-", cookie);
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "SwitchTenant：普通用户切换管理后台（0）被拒绝")]
    public void SwitchTenant_NormalUser_To_Backend_Rejected()
    {
        EnableTenant();
        try
        {
            var ctx = _fx.CreateContext();
            _fx.Provider.Login("tenant01", TenantAuthFixture.Password, false);
            var controller = CreateController(_fx, ctx);

            Assert.Throws<InvalidOperationException>(() => controller.SwitchTenant(0));
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "SwitchTenant：成员切换自己所属租户成功")]
    public void SwitchTenant_Member_To_Own_Tenant_Succeeds()
    {
        EnableTenant();
        try
        {
            var ctx = _fx.CreateContext();
            _fx.Provider.Login("tenant01", TenantAuthFixture.Password, false);
            var controller = CreateController(_fx, ctx);

            var res = controller.SwitchTenant(_fx.Tenant1.Id);
            Assert.True(res.Data);
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "SwitchTenant：非成员切换别家租户被拒绝")]
    public void SwitchTenant_NonMember_To_Other_Tenant_Rejected()
    {
        EnableTenant();
        try
        {
            var ctx = _fx.CreateContext();
            _fx.Provider.Login("tenant01", TenantAuthFixture.Password, false);
            var controller = CreateController(_fx, ctx);

            // tenant01 只属于 Tenant1，Tenant2 非其成员
            Assert.Throws<InvalidOperationException>(() => controller.SwitchTenant(_fx.Tenant2.Id));
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "SwitchTenant：切换到已停用租户被拒绝")]
    public void SwitchTenant_To_Disabled_Tenant_Rejected()
    {
        EnableTenant();
        try
        {
            var disabled = new Tenant { Code = "t3-disabled", Name = "停用租户", Enable = false };
            disabled.Insert();

            var ctx = _fx.CreateContext();
            _fx.Provider.Login("admin01", TenantAuthFixture.Password, false);
            var controller = CreateController(_fx, ctx);

            // 管理员虽豁免成员校验，但租户本身已停用仍拒绝
            Assert.Throws<ArgumentException>(() => controller.SwitchTenant(disabled.Id));
        }
        finally
        {
            Restore();
        }
    }

    #endregion

    #region 用户信息携带租户数据（Auth/Info）

    [Fact(DisplayName = "Info：租户模式返回当前租户、所属租户列表与成员标记")]
    public void Info_Returns_TenantInfo_In_TenantMode()
    {
        EnableTenant();
        try
        {
            TenantContext.Current = new TenantContext { TenantId = _fx.Tenant1.Id };
            var ctx = _fx.CreateContext();
            _fx.Provider.Login("tenant01", TenantAuthFixture.Password, false);
            var controller = CreateController(_fx, ctx);

            var result = controller.Info() as ContentResult;
            Assert.NotNull(result);

            using var doc = JsonDocument.Parse(result!.Content!);
            var data = doc.RootElement.GetProperty("data");
            Assert.True(data.GetProperty("enableTenant").GetBoolean());
            Assert.Equal(_fx.Tenant1.Id, data.GetProperty("tenantId").GetInt32());
            Assert.Equal("t1", data.GetProperty("tenantCode").GetString());
            Assert.Equal("测试租户", data.GetProperty("tenantName").GetString());
            Assert.Equal(2, data.GetProperty("tenantMode").GetInt32()); // Tenant
            Assert.False(data.GetProperty("isSystemAdmin").GetBoolean());

            var tenants = data.GetProperty("tenants");
            Assert.Equal(1, tenants.GetArrayLength());
            Assert.Equal(_fx.Tenant1.Id, tenants[0].GetProperty("id").GetInt32());
            Assert.Equal("t1", tenants[0].GetProperty("code").GetString());
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "Info：管理后台模式返回 isSystemAdmin=true 且 tenantMode=1")]
    public void Info_Returns_AdminFlag_In_Backend_Mode()
    {
        EnableTenant();
        try
        {
            TenantContext.Current = new TenantContext { TenantId = 0 };
            var ctx = _fx.CreateContext();
            _fx.Provider.Login("admin01", TenantAuthFixture.Password, false);
            var controller = CreateController(_fx, ctx);

            var result = controller.Info() as ContentResult;
            Assert.NotNull(result);

            using var doc = JsonDocument.Parse(result!.Content!);
            var data = doc.RootElement.GetProperty("data");
            Assert.True(data.GetProperty("enableTenant").GetBoolean());
            Assert.Equal(0, data.GetProperty("tenantId").GetInt32());
            Assert.Equal(1, data.GetProperty("tenantMode").GetInt32()); // AdminBackend
            Assert.True(data.GetProperty("isSystemAdmin").GetBoolean());
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "Info：未开启多租户时租户字段为空（enableTenant=false）")]
    public void Info_Returns_No_Tenant_When_Disabled()
    {
        Restore();
        var ctx = _fx.CreateContext();
        _fx.Provider.Login("tenant01", TenantAuthFixture.Password, false);
        var controller = CreateController(_fx, ctx);

        var result = controller.Info() as ContentResult;
        Assert.NotNull(result);

        using var doc = JsonDocument.Parse(result!.Content!);
        var data = doc.RootElement.GetProperty("data");
        Assert.False(data.GetProperty("enableTenant").GetBoolean());
        Assert.Equal(0, data.GetProperty("tenantId").GetInt32());
        Assert.Equal(0, data.GetProperty("tenantMode").GetInt32());
        // 未开启多租户时租户列表为 null（前端以空列表/缺失处理）
        var tenants = data.GetProperty("tenants");
        Assert.True(tenants.ValueKind == JsonValueKind.Null || tenants.GetArrayLength() == 0);
    }

    #endregion
}
