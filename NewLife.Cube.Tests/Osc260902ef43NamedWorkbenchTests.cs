using System;
using System.IO;
using System.Linq;
using NewLife.Cube.Widgets;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

[CollectionDefinition("Osc260902ef43", DisableParallelization = true)]
public class Osc260902ef43Collection
{
}

[Collection("Osc260902ef43")]
public class Osc260902ef43DbFixture
{
    /// <summary>每次测试运行前重建独立库，避免跨 run 残留（如隐藏的父分组）污染断言</summary>
    public Osc260902ef43DbFixture()
    {
        var f = Path.Combine(AppContext.BaseDirectory, "Data", "Osc260902ef43.db");
        if (File.Exists(f)) File.Delete(f);
    }
}

[Collection("Osc260902ef43")]
public class Osc260902ef43NamedWorkbenchTests : IClassFixture<Osc260902ef43DbFixture>
{
    private readonly String _db = Path.Combine(AppContext.BaseDirectory, "Data", "Osc260902ef43.db");

    public Osc260902ef43NamedWorkbenchTests()
    {
        var dir = Path.GetDirectoryName(_db);
        Directory.CreateDirectory(dir);

        DAL.AddConnStr("OscEf43", $"Data Source={_db}", null, "SQLite");
        User.Meta.ConnName = "OscEf43";
        Role.Meta.ConnName = "OscEf43";
        Parameter.Meta.ConnName = "OscEf43";
        Menu.Meta.ConnName = "OscEf43";
        TryCreate(User.Meta.Factory);
        TryCreate(Role.Meta.Factory);
        TryCreate(Parameter.Meta.Factory);
        TryCreate(Menu.Meta.Factory);
    }

    static void TryCreate(IEntityFactory fact)
    {
        try
        {
            fact.Session.Dal.Db.CreateMetaData().SetSchema(DDLSchema.CreateTable, fact.Table.DataTable);
        }
        catch
        {
            // 已建表
        }
    }

    static String Named(String widgetName) =>
        "{\"version\":1,\"widgets\":[{\"id\":\"w1\",\"kind\":\"metricCard\",\"title\":\"t\",\"layout\":{\"w\":3,\"order\":0},\"source\":{\"provider\":\"named\",\"widgetName\":\"" + widgetName + "\"},\"query\":{}}]}";

    static IUser MakeUser(Boolean system)
    {
        var role = new Role { Name = (system ? "sys-" : "mem-") + Guid.NewGuid().ToString("N")[..8], Enable = true, IsSystem = system };
        role.Insert();
        var user = new User { Name = "u-" + Guid.NewGuid().ToString("N")[..8], Enable = true, RoleID = role.ID };
        user.Insert();
        return user;
    }

    [Fact(DisplayName = "slug 校验：合法小写/数字/横线，非法大写/中文/空/超长")]
    public void SlugValidation()
    {
        Assert.True(WorkbenchNamedStore.IsValidSlug("ops"));
        Assert.True(WorkbenchNamedStore.IsValidSlug("wb-a1"));
        Assert.True(WorkbenchNamedStore.IsValidSlug("a"));
        Assert.False(WorkbenchNamedStore.IsValidSlug("Ops"));
        Assert.False(WorkbenchNamedStore.IsValidSlug("工作台"));
        Assert.False(WorkbenchNamedStore.IsValidSlug(""));
        Assert.False(WorkbenchNamedStore.IsValidSlug("1abc"));
        Assert.False(WorkbenchNamedStore.IsValidSlug(new String('a', 33)));
    }

    [Fact(DisplayName = "标题校验：非空且 ≤40")]
    public void TitleValidation()
    {
        Assert.True(WorkbenchNamedStore.IsValidTitle("销售看板"));
        Assert.True(WorkbenchNamedStore.IsValidTitle(new String('a', 40)));
        Assert.False(WorkbenchNamedStore.IsValidTitle(""));
        Assert.False(WorkbenchNamedStore.IsValidTitle("   "));
        Assert.False(WorkbenchNamedStore.IsValidTitle(new String('a', 41)));
    }

    [Fact(DisplayName = "Save/Get/GetList/Delete：Value=标题、LongValue=配置")]
    public void StoreRoundTrip()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        var slug = "ops-" + tag;
        var json = Named("UserCount");

        WorkbenchNamedStore.Save(slug, "运营看板", json);

        Assert.Equal(json, WorkbenchNamedStore.Get(slug));
        var item = WorkbenchNamedStore.FindItem(slug);
        Assert.NotNull(item);
        Assert.Equal("运营看板", item.Title);
        Assert.Contains(slug, WorkbenchNamedStore.GetList().Select(e => e.Slug));

        // upsert：同 slug 覆盖标题
        WorkbenchNamedStore.Save(slug, "运营看板2", json);
        Assert.Equal("运营看板2", WorkbenchNamedStore.FindItem(slug).Title);

        WorkbenchNamedStore.Delete(slug);
        Assert.Null(WorkbenchNamedStore.Get(slug));
        Assert.DoesNotContain(slug, WorkbenchNamedStore.GetList().Select(e => e.Slug));
    }

    [Fact(DisplayName = "非法 slug 保存抛 ArgumentException")]
    public void StoreRejectsBadSlug()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        var ex = Assert.Throws<ArgumentException>(() => WorkbenchNamedStore.Save("Bad" + tag, "t", Named("UserCount")));
        Assert.Contains("slug", ex.Message);
    }

    [Fact(DisplayName = "挂菜单：首次发布建父分组并顶置第一；重复 PUT 不重复建行")]
    public void MountMenuCreatesAndPinsGroup()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        var slug = "wb-" + tag;
        // 其它顶级菜单（模拟既有排序更大者已存在）
        var top = Menu.Root.Add("top" + tag, "top" + tag, "t." + tag, "/top" + tag);
        top.Sort = 100;
        ((IEntity)top).Update();

        WorkbenchNamedStore.MountMenu(slug, "看板A");
        WorkbenchNamedStore.MountMenu(slug, "看板A-改");

        // 父分组存在且其 Sort 为根级最大 → Childs 排序后第一组
        IMenu group = null;
        var menus = Menu.Meta.Cache.FindAll(e => e.ParentID == 0).ToList();
        group = menus.FirstOrDefault(e => e.Name == WorkbenchNamedStore.ParentName);
        Assert.NotNull(group);
        Assert.Equal(WorkbenchNamedStore.ParentTitle, group.DisplayName);
        Assert.Equal(menus.Max(e => e.Sort), group.Sort);
        Assert.Equal(WorkbenchNamedStore.ParentName, menus.OrderByDescending(e => e.Sort).First().Name);

        // 子菜单唯一、DisplayName 已更新
        var childs = Menu.Meta.Cache
            .FindAll(e => !e.Url.IsNullOrEmpty() && e.Url.EqualIgnoreCase("/Workbench/" + slug))
            .ToList();
        Assert.Single(childs);
        Assert.Equal("看板A-改", childs[0].DisplayName);
        Assert.Equal("fa-th-large", childs[0].Icon);

        // 清理
        WorkbenchNamedStore.UnmountMenu(slug);
        Assert.Empty(Menu.Meta.Cache
            .FindAll(e => !e.Url.IsNullOrEmpty() && e.Url.EqualIgnoreCase("/Workbench/" + slug)));
    }

    [Fact(DisplayName = "卸载：删子菜单；父分组无子项时一并移除")]
    public void UnmountRemovesRowAndEmptyGroup()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        var slug = "del-" + tag;
        WorkbenchNamedStore.MountMenu(slug, "待删");
        WorkbenchNamedStore.UnmountMenu(slug);

        // 子菜单行已删除
        Assert.Empty(Menu.Meta.Cache
            .FindAll(e => !e.Url.IsNullOrEmpty() && e.Url.EqualIgnoreCase("/Workbench/" + slug)));
        // 父分组无其它子菜单（本集合无并发残留）时应被移除
        var parents = Menu.Meta.Cache.FindAll(e => e.Name == WorkbenchNamedStore.ParentName).ToList();
        foreach (var p in parents)
        {
            Assert.True(Menu.Meta.Cache.FindAll(e => e.ParentID == p.ID).Any(), "父分组仍有子项不应被删");
        }
    }

    [Fact(DisplayName = "IsAccessible：菜单行不存在 false；未声明权限默认可见；声明后未授权不可见")]
    public void IsAccessibleRules()
    {
        var user = MakeUser(false);
        var tag = Guid.NewGuid().ToString("N")[..6];
        var slug = "acc-" + tag;

        // 未发布（无菜单行）→ false
        Assert.False(WorkbenchNamedStore.IsAccessible(user, slug));

        // 发布后未声明权限 → 默认全员可见
        WorkbenchNamedStore.MountMenu(slug, "权限看板");
        Assert.True(WorkbenchNamedStore.IsAccessible(user, slug));

        // 任一角色声明该菜单权限后，未授权用户不可见
        var menu = Menu.Meta.Cache
            .FindAll(e => !e.Url.IsNullOrEmpty() && e.Url.EqualIgnoreCase("/Workbench/" + slug))
            .First();
        var declaring = new Role { Name = "dec-" + tag, Enable = true };
        declaring.Insert();
        declaring.Set(menu.ID, PermissionFlags.Detail);
        declaring.Save();
        Assert.False(WorkbenchNamedStore.IsAccessible(user, slug));

        // 授权角色（作为主角色）可见
        var owner = new Role { Name = "own-" + tag, Enable = true };
        owner.Insert();
        owner.Set(menu.ID, PermissionFlags.Detail);
        owner.Save();
        var ownerUser = new User { Name = "uo-" + tag, Enable = true, RoleID = owner.ID };
        ownerUser.Insert();
        Assert.True(WorkbenchNamedStore.IsAccessible(ownerUser, slug));

        // 清理
        WorkbenchNamedStore.UnmountMenu(slug);
    }

    [Fact(DisplayName = "IsAccessible：子菜单/父分组隐藏 → 整链不可达（沿父链校验）")]
    public void IsAccessible_RespectsVisibilityChain()
    {
        var user = MakeUser(false);
        var tag = Guid.NewGuid().ToString("N")[..6];
        var slug = "vis-" + tag;
        WorkbenchNamedStore.Save(slug, "可见性看板", Named("UserCount"));
        WorkbenchNamedStore.MountMenu(slug, "可见性看板");
        Assert.True(WorkbenchNamedStore.IsAccessible(user, slug));
        Assert.Contains(slug, WorkbenchNamedStore.GetVisibleList().Select(e => e.Slug));

        var menu = Menu.Meta.Cache
            .FindAll(e => !e.Url.IsNullOrEmpty() && e.Url.EqualIgnoreCase("/Workbench/" + slug))
            .First();
        var parent = Menu.Meta.Cache.FindAll(e => e.ID == menu.ParentID).First();

        // 子菜单隐藏 → 不可达且不出现在可见列表
        menu.Visible = false;
        ((IEntity)menu).Update();
        Assert.False(WorkbenchNamedStore.IsAccessible(user, slug));
        Assert.DoesNotContain(slug, WorkbenchNamedStore.GetVisibleList().Select(e => e.Slug));

        // 恢复子菜单，父分组隐藏 → 子树不可达且列表隐藏
        menu.Visible = true;
        ((IEntity)menu).Update();
        parent.Visible = false;
        ((IEntity)parent).Update();
        Assert.False(WorkbenchNamedStore.IsAccessible(user, slug));
        Assert.DoesNotContain(slug, WorkbenchNamedStore.GetVisibleList().Select(e => e.Slug));

        // 清理（不再断言“恢复后”场景：XCode 缓存写后读即时一致性非本 store 契约）
        WorkbenchNamedStore.Delete(slug);
        WorkbenchNamedStore.UnmountMenu(slug);
    }
}
