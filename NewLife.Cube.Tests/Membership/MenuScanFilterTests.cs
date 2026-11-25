using System;
using System.Collections.Generic;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Controllers;
using NewLife.Cube.Membership;
using NewLife.Cube.Tests.LegacyMenuScan.Controllers;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>覆盖区域外控制器菜单扫描规则：仅登录不建菜单，显式权限位建不可见权限菜单</summary>
[Collection("TenantAuth")]
public class MenuScanFilterTests : IDisposable
{
    private const String TestNamespace = "NewLife.Cube.Tests.LegacyMenuScan.Controllers";

    private readonly List<Int32> _ids = [];

    /// <summary>实例化</summary>
    /// <param name="_">多租户夹具，复用其 SQLite 菜单表</param>
    public MenuScanFilterTests(TenantAuthFixture _) { }

    /// <summary>清理本用例插入的菜单，避免污染共享数据库</summary>
    public void Dispose()
    {
        Menu.Meta.Session.ClearCache("MenuScanFilterTests", true);

        foreach (var menu in Menu.FindAll())
        {
            if (menu.FullName.IsNullOrEmpty()) continue;
            if (!menu.FullName.EqualIgnoreCase(TestNamespace) &&
                !menu.FullName.StartsWith(TestNamespace + ".", StringComparison.OrdinalIgnoreCase)) continue;

            menu.Delete();
        }

        foreach (var id in _ids)
        {
            Menu.FindByKey(id)?.Delete();
        }
        _ids.Clear();

        Menu.Meta.Session.ClearCache("MenuScanFilterTests", true);
    }

    [Fact(DisplayName = "AreaBase直接作特性 - 不注册为区域")]
    public void AreaBase_AsAttribute_DoesNotRegisterArea()
    {
        _ = new AreaBase("TestDirectArea");

        Assert.DoesNotContain(typeof(AreaBase), AreaBase.GetAreas());
    }

    [Fact(DisplayName = "区域外控制器权限菜单判定 - 无特性或仅登录不需要")]
    public void NeedPermissionMenu_OnlyWhenPermissions()
    {
        Assert.False(MenuHelper.NeedPermissionMenu(typeof(PlainScanController)));
        Assert.False(MenuHelper.NeedPermissionMenu(typeof(LoginOnlyScanController)));
        Assert.True(MenuHelper.NeedPermissionMenu(typeof(PermissionScanController)));

        // 框架内置区域外控制器：仅 AI 声明了 Detail 权限，其余只需要登录
        Assert.True(MenuHelper.NeedPermissionMenu(typeof(AiController)));
        Assert.False(MenuHelper.NeedPermissionMenu(typeof(AuthController)));
        Assert.False(MenuHelper.NeedPermissionMenu(typeof(CubeController)));
        Assert.False(MenuHelper.NeedPermissionMenu(typeof(MfaController)));
        Assert.False(MenuHelper.NeedPermissionMenu(typeof(NotifyController)));
        Assert.False(MenuHelper.NeedPermissionMenu(typeof(SsoController)));
    }

    [Fact(DisplayName = "区域外控制器权限菜单 - 只建单个不可见功能项")]
    public void EnsurePermissionMenu_CreatesSingleHiddenMenu()
    {
        var type = typeof(PermissionScanController);

        var menu = MenuHelper.EnsurePermissionMenu(type) as Menu;
        Assert.NotNull(menu);
        _ids.Add(menu!.Id);

        var db = Menu.FindByKey(menu.Id);
        Assert.NotNull(db);
        Assert.False(db!.Visible);
        Assert.Equal(MenuTypes.功能, db.Type);
        Assert.Equal(0, db.ParentId);
        Assert.True(db.Permissions.ContainsKey((Int32)PermissionFlags.Detail));

        // 创建后必须能按类型全名立即查到，供 EntityAuthorizeAttribute 解析菜单
        Assert.NotNull(Menu.FindByFullName(type.FullName));

        // 同命名空间的其它控制器不应被扫描成菜单
        Assert.Null(Menu.FindByFullName(typeof(PlainScanController).FullName));
        Assert.Null(Menu.FindByFullName(typeof(LoginOnlyScanController).FullName));
    }

    [Fact(DisplayName = "区域外控制器历史菜单清理 - 保留有权限项并隐藏")]
    public void CleanupNonAreaMenus_KeepsPermissionMenuAndDeletesOthers()
    {
        MenuHelper.CleanupNonAreaMenus(TestNamespace);

        var root = AddMenu("LegacyScanRoot", TestNamespace, 0);
        var permission = AddMenu("PermissionScan", typeof(PermissionScanController).FullName, root.Id);
        var plain = AddMenu("PlainScan", typeof(PlainScanController).FullName, root.Id);
        var action = AddMenu("Index", typeof(PermissionScanController).FullName + ".Index", root.Id);

        const String areaNamespace = "NewLife.Cube.Tests.LegacyMenuScan.Areas.Legit.Controllers";
        var areaRoot = AddMenu("LegitArea", areaNamespace, 0);
        var areaChild = AddMenu("User", typeof(User).FullName, areaRoot.Id);

        var count = MenuHelper.CleanupNonAreaMenus(TestNamespace);
        Assert.True(count > 0);

        var kept = Menu.FindByKey(permission.Id);
        Assert.NotNull(kept);
        Assert.False(kept!.Visible);
        Assert.Equal(MenuTypes.功能, kept.Type);
        Assert.Equal(0, kept.ParentId);

        Assert.Null(Menu.FindByKey(plain.Id));
        Assert.Null(Menu.FindByKey(action.Id));
        Assert.Null(Menu.FindByKey(root.Id));

        Assert.NotNull(Menu.FindByKey(areaRoot.Id));
        Assert.NotNull(Menu.FindByKey(areaChild.Id));
    }

    private Menu AddMenu(String name, String fullName, Int32 parentId, Boolean visible = true)
    {
        var menu = new Menu
        {
            Name = name,
            DisplayName = name,
            FullName = fullName,
            ParentId = parentId,
            Url = "/" + name,
            Visible = visible,
        };
        menu.Insert();
        _ids.Add(menu.Id);

        return menu;
    }
}
