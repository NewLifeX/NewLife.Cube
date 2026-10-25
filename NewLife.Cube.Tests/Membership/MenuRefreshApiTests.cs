using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Areas.Admin.Controllers;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>API 版菜单刷新动作测试。验证 keys 解析、落库覆盖与列表刷新标记</summary>
[Collection("TenantAuth")]
public class MenuRefreshApiTests : IDisposable
{
    private readonly TenantAuthFixture _fx;
    private readonly Int32 _menuId;

    /// <summary>实例化，并插入一条待刷新的菜单</summary>
    /// <param name="fixture">多租户夹具</param>
    public MenuRefreshApiTests(TenantAuthFixture fixture)
    {
        _fx = fixture;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var menu = new Menu
        {
            Name = $"refresh_{suffix}",
            DisplayName = "旧名字",
            FullName = typeof(MenuController).FullName,
            Url = $"/Test/refresh_{suffix}",
        };
        menu.Insert();
        _menuId = menu.ID;
    }

    /// <summary>删除本用例插入的菜单</summary>
    public void Dispose()
    {
        Menu.Meta.Session.ClearCache("MenuRefreshApiTests", true);
        Menu.FindByKey(_menuId)?.Delete();
    }

    [Fact(DisplayName = "菜单刷新接口 - 覆盖显示名并返回刷新标记")]
    public void RefreshNameOverwritesAndAsksRefresh()
    {
        Assert.NotNull(_fx.Provider);

        var controller = new MenuController();
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString($"?keys={_menuId}");
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        var res = controller.RefreshName();
        Assert.NotNull(res);
        Assert.Equal(0, res.Code);

        // data.url 为 [refresh]，供 SPA 工具栏提示后刷新列表
        var url = res.Data?.GetType().GetProperty("url")?.GetValue(res.Data) as String;
        Assert.Equal("[refresh]", url);

        Menu.Meta.Session.ClearCache("MenuRefreshApiTests", true);
        var db = Menu.FindByKey(_menuId);
        Assert.NotNull(db);
        Assert.Equal("菜单", db!.DisplayName);
    }
}
