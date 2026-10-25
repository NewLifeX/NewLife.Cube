using System;
using System.Collections.Generic;
using System.Globalization;
using NewLife;
using NewLife.Cube.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>覆盖菜单命名标记的落库往返与渲染期翻译</summary>
/// <remarks>复用多租户夹具的 SQLite 数据库，避免另起 Membership 连接串与其他用例互相覆盖</remarks>
[Collection("TenantAuth")]
public class MenuNameSyncIntegrationTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户夹具，保证数据库与会话已就绪</param>
    public MenuNameSyncIntegrationTests(TenantAuthFixture fixture) => _fx = fixture;

    /// <summary>确认夹具已初始化（菜单表由夹具统一创建）</summary>
    [Fact(DisplayName = "菜单命名 - 数据库已就绪")]
    public void FixtureReady() => Assert.NotNull(_fx.Provider);

    [Fact(DisplayName = "菜单命名 - 自动项标记落库后跟随代码改名")]
    public void AutoMarkerRoundTrips()
    {
        var menu = new Menu { Name = "AutoUser", DisplayName = "用户", Ex6 = "用户", Url = "/Test/AutoUser" };
        menu.Insert();

        var db = Reload(menu.ID);
        Assert.Equal("用户", db.Ex6);

        // 代码侧改名：标记与显示名一致，视为自动项并跟随
        Assert.True(MenuHelper.SyncDisplayName(db, "Users"));
        db.Update();

        var db2 = Reload(menu.ID);
        Assert.Equal("Users", db2.DisplayName);
        Assert.Equal("Users", db2.Ex6);
    }

    [Fact(DisplayName = "菜单命名 - 人工改名后落库为自定义且后续扫描不覆盖")]
    public void CustomNameSurvivesRescan()
    {
        var menu = new Menu { Name = "CustomUser", DisplayName = "用户", Ex6 = "用户", Url = "/Test/CustomUser" };
        menu.Insert();

        // 模拟在菜单管理里改名：只改 DisplayName，标记仍是旧源文
        var db = Reload(menu.ID);
        db.DisplayName = "我的用户";
        db.Update();

        // 扫描发现显示名与标记不一致，转自定义并保留用户输入
        var db2 = Reload(menu.ID);
        Assert.True(MenuHelper.SyncDisplayName(db2, "Users"));
        Assert.Equal("我的用户", db2.DisplayName);
        Assert.Equal(MenuHelper.CustomNameMarker, db2.Ex6);
        db2.Update();

        // 再次扫描不再改动
        var db3 = Reload(menu.ID);
        Assert.False(MenuHelper.SyncDisplayName(db3, "Users"));
        Assert.Equal("我的用户", db3.DisplayName);
        Assert.Equal(MenuHelper.CustomNameMarker, db3.Ex6);
    }

    [Fact(DisplayName = "菜单命名 - 落库源文按请求语言渲染")]
    public void SourceRendersInRequestLanguage()
    {
        var menu = new Menu { Name = "RenderUser", DisplayName = "用户", Ex6 = "用户", Url = "/Test/RenderUser" };
        menu.Insert();

        var db = Reload(menu.ID);

        var prev = Localization.Culture;
        var culture = CultureInfo.GetCultureInfo("sw");
        try
        {
            Localization.Register(culture, new Dictionary<String, String> { ["用户"] = "User" }, overwrite: true);
            Localization.Culture = culture;

            Assert.Equal("User", MenuHelper.GetDisplayName(db));
        }
        finally
        {
            Localization.Culture = prev;
            Localization.Unregister(culture);
        }
    }

    private static Menu Reload(Int32 id)
    {
        Menu.Meta.Session.ClearCache("MenuNameSyncIntegrationTests", true);

        var entity = Menu.FindByKey(id);
        Assert.NotNull(entity);

        return entity!;
    }
}
