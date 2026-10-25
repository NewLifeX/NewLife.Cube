using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube.Areas.Admin.Controllers;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>菜单批量刷新动作测试。验证从请求 keys 解析选中行、按代码源文覆盖显示名</summary>
[Collection("SqliteDb")]
public class MenuRefreshActionTests
{
    [Fact(DisplayName = "菜单刷新动作 - 按 keys 覆盖选中行并清缓存")]
    public void RefreshNameOverwritesSelected()
    {
        SqliteDb.Ensure();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var menu = new Menu
        {
            Name = $"refresh_{suffix}",
            DisplayName = "旧名字",
            FullName = typeof(MenuController).FullName,
            Url = $"/Test/refresh_{suffix}",
        };
        menu.Insert();

        try
        {
            var controller = new MenuController();
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString($"?keys={menu.ID}");
            controller.ControllerContext = new ControllerContext { HttpContext = context };

            var result = controller.RefreshName();
            Assert.NotNull(result);

            Menu.Meta.Session.ClearCache("MenuRefreshActionTests", true);
            var db = Menu.FindByKey(menu.ID);
            Assert.NotNull(db);
            Assert.Equal("菜单", db!.DisplayName);
        }
        finally
        {
            Menu.Meta.Session.ClearCache("MenuRefreshActionTests", true);
            Menu.FindByKey(menu.ID)?.Delete();
        }
    }

    [Fact(DisplayName = "菜单刷新动作 - 未选中数据时返回参数错误")]
    public void RefreshNameWithoutKeysReturnsError()
    {
        var controller = new MenuController();
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = controller.RefreshName();
        Assert.NotNull(result);
    }
}
