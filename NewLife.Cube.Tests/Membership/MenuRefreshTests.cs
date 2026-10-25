using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife;
using NewLife.Cube.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>覆盖菜单刷新动作的落库往返</summary>
/// <remarks>复用多租户夹具的 SQLite 数据库，避免另起 Membership 连接串与其他用例互相覆盖</remarks>
[Collection("TenantAuth")]
public class MenuRefreshTests : IDisposable
{
    private readonly TenantAuthFixture _fx;
    private readonly List<Int32> _ids = [];

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户夹具，保证数据库与会话已就绪</param>
    public MenuRefreshTests(TenantAuthFixture fixture) => _fx = fixture;

    /// <summary>清理本用例插入的菜单，避免污染共享数据库</summary>
    public void Dispose()
    {
        foreach (var id in _ids)
        {
            XCode.Membership.Menu.FindByID(id)?.Delete();
        }
        _ids.Clear();
        Menu.Meta.Session.ClearCache("MenuRefreshTests", true);
    }

    /// <summary>确认夹具已初始化（菜单表由夹具统一创建）</summary>
    [Fact(DisplayName = "菜单刷新 - 数据库已就绪")]
    public void FixtureReady() => Assert.NotNull(_fx.Provider);

    [Fact(DisplayName = "菜单刷新 - 用代码源文覆盖落库值并持久化")]
    public void RefreshPersistsSourceName()
    {
        var menu = new Menu { Name = "RefreshUser", DisplayName = "旧名字", FullName = typeof(RefreshUserController).FullName, Url = "/Test/RefreshUser" };
        menu.Insert();
        _ids.Add(menu.ID);

        var db = Reload(menu.ID);
        Assert.True(MenuHelper.RefreshDisplayName(db));
        db.Update();

        var db2 = Reload(menu.ID);
        Assert.Equal("用户", db2.DisplayName);
        Assert.Equal(typeof(RefreshUserController).FullName, db2.FullName);
    }

    [Fact(DisplayName = "菜单刷新 - 类型解析不到时保持原值")]
    public void RefreshSkipsUnresolved()
    {
        var menu = new Menu { Name = "RefreshUnknown", DisplayName = "保留名字", FullName = "No.Such.Controller", Url = "/Test/RefreshUnknown" };
        menu.Insert();
        _ids.Add(menu.ID);

        var db = Reload(menu.ID);
        Assert.False(MenuHelper.RefreshDisplayName(db));
        Assert.Equal("保留名字", db.DisplayName);
    }

    private static Menu Reload(Int32 id)
    {
        Menu.Meta.Session.ClearCache("MenuRefreshTests", true);

        var entity = Menu.FindByKey(id);
        Assert.NotNull(entity);

        return entity!;
    }

    [DisplayName("用户")]
    private class RefreshUserController { }
}
