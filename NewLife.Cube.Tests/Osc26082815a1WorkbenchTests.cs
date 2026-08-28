using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Cube.Controllers;
using NewLife.Cube.Entity;
using NewLife.Cube.Widgets;
using NewLife.Cube.Widgets.Workbench;
using NewLife.Serialization;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

[CollectionDefinition("Osc26082815a1", DisableParallelization = true)]
public class Osc26082815a1Collection
{
}

[Collection("Osc26082815a1")]
public class Osc26082815a1WorkbenchTests
{
    public Osc26082815a1WorkbenchTests()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        DAL.AddConnStr("Osc15a1Cube", $"Data Source={Path.Combine(dir, "Osc15a1Cube.db")}", null, "SQLite");
        DAL.AddConnStr("Osc15a1Mem", $"Data Source={Path.Combine(dir, "Osc15a1Mem.db")}", null, "SQLite");
        UserProfile.Meta.ConnName = "Osc15a1Cube";
        User.Meta.ConnName = "Osc15a1Mem";
        Role.Meta.ConnName = "Osc15a1Mem";
        Parameter.Meta.ConnName = "Osc15a1Mem";
        TryCreate(UserProfile.Meta.Factory);
        TryCreate(User.Meta.Factory);
        TryCreate(Role.Meta.Factory);
        TryCreate(Parameter.Meta.Factory);
        CubeWidgetManager.Reset();
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

    static IUser MakeUser(Boolean system)
    {
        var role = new Role { Name = (system ? "sys-" : "mem-") + Guid.NewGuid().ToString("N")[..8], Enable = true, IsSystem = system };
        role.Insert();
        var user = new User { Name = "u-" + Guid.NewGuid().ToString("N")[..8], Enable = true, RoleID = role.ID };
        user.Insert();
        return user;
    }

    static String Named(String widgetName, Int32 w = 3, String kind = "metricCard") =>
        "{\"version\":1,\"widgets\":[{\"id\":\"w1\",\"kind\":\"" + kind + "\",\"title\":\"t\",\"layout\":{\"w\":" + w +
        ",\"order\":0},\"source\":{\"provider\":\"named\",\"widgetName\":\"" + widgetName + "\"},\"query\":{}}]}";

    static String Kanban() =>
        """{"version":1,"widgets":[{"id":"k1","kind":"miniKanban","title":"k","layout":{"w":6,"order":0},"source":{"provider":"entity.list","typePath":"Admin/User"},"query":{"mapping":{"groupField":"RoleID","titleField":"Name"}}}]}""";

    [Fact(DisplayName = "Resolver：合法 HomeJson（含空数组）→ source=user")]
    public void Resolver_UserDomain()
    {
        var user = MakeUser(true);
        UserProfile.UpsertForUser(user.ID, new UserProfileModel { HomeJson = """{"version":1,"widgets":[]}""" });
        var r = WorkbenchResolver.Resolve(user);
        Assert.Equal("user", r.Source);
        Assert.Contains("\"widgets\":[]", r.ConfigJson.Replace(" ", ""));
    }

    [Fact(DisplayName = "Resolver：空串 HomeJson 不视为 user，回落 system")]
    public void Resolver_EmptyHomeJson_Inherits()
    {
        var user = MakeUser(true);
        UserProfile.UpsertForUser(user.ID, new UserProfileModel { HomeJson = "" });
        var r = WorkbenchResolver.Resolve(user);
        Assert.Equal("system", r.Source);
        Assert.Contains("seed-Monitor", r.ConfigJson);
    }

    [Fact(DisplayName = "Resolver：用户合法 HomeJson 压过角色模板（用户>角色）")]
    public void Resolver_UserBeatsRole()
    {
        var user = MakeUser(false);
        WorkbenchRoleStore.Save(user.RoleID, Named("MyLogins"));
        UserProfile.UpsertForUser(user.ID, new UserProfileModel { HomeJson = Named("MyDays") });
        var r = WorkbenchResolver.Resolve(user);
        Assert.Equal("user", r.Source);
        Assert.Contains("MyDays", r.ConfigJson);
        Assert.DoesNotContain("MyLogins", r.ConfigJson);
    }

    [Fact(DisplayName = "Resolver：空串 HomeJson + 有角色模板 → source=role（用户>角色>系统）")]
    public void Resolver_EmptyHomeJson_FallsToRole()
    {
        var user = MakeUser(false);
        WorkbenchRoleStore.Save(user.RoleID, Named("MyLogins"));
        UserProfile.UpsertForUser(user.ID, new UserProfileModel { HomeJson = "" });
        var r = WorkbenchResolver.Resolve(user);
        Assert.Equal("role", r.Source);
        Assert.Contains("MyLogins", r.ConfigJson);
    }

    [Fact(DisplayName = "Resolver：角色 Parameter 命中且 UserID=0")]
    public void Resolver_RoleParameter()
    {
        var user = MakeUser(false);
        var json = """{"version":1,"widgets":[{"id":"seed-MyLogins","kind":"metricCard","title":"我的登录","layout":{"w":3,"order":0},"source":{"provider":"named","widgetName":"MyLogins"},"query":{}}]}""";
        WorkbenchRoleStore.Save(user.RoleID, json);
        var p = Parameter.FindByUserIDAndCategoryAndName(0, WorkbenchRoleStore.Category, user.RoleID + "");
        Assert.NotNull(p);
        Assert.Equal(0, p.UserID);
        Assert.True(p.Value.IsNullOrEmpty());
        Assert.False(p.LongValue.IsNullOrEmpty());
        var r = WorkbenchResolver.Resolve(user);
        Assert.Equal("role", r.Source);
        Assert.Contains("seed-MyLogins", r.ConfigJson);
    }

    [Fact(DisplayName = "Resolver：无配置系统角色 → admin 种子")]
    public void Resolver_SystemSeed()
    {
        var user = MakeUser(true);
        var r = WorkbenchResolver.Resolve(user);
        Assert.Equal("system", r.Source);
        Assert.Contains("seed-Monitor", r.ConfigJson);
        Assert.Contains("seed-UserCount", r.ConfigJson);
        Assert.DoesNotContain("seed-MyLogins", r.ConfigJson);
    }

    [Fact(DisplayName = "Resolver：无配置普通用户 → member 种子")]
    public void Resolver_MemberSeed()
    {
        var user = MakeUser(false);
        var r = WorkbenchResolver.Resolve(user);
        Assert.Equal("system", r.Source);
        Assert.Contains("seed-MyLogins", r.ConfigJson);
        Assert.DoesNotContain("seed-Monitor", r.ConfigJson);
        Assert.DoesNotContain("seed-UserCount", r.ConfigJson);
    }

    [Fact(DisplayName = "TryNormalize：insight 拒 miniKanban 与 w=2；workbench 接受")]
    public void TryNormalize_SurfaceBranch()
    {
        Assert.False(DashboardJson.TryNormalize(Kanban(), null, false, out _, out var e1));
        Assert.Contains("数据看板", e1);

        Assert.True(DashboardJson.TryNormalize(Kanban(), null, false, DashboardJson.SurfaceWorkbench, out var kn, out var e2), e2);
        Assert.Contains("miniKanban", kn);
        Assert.Contains("\"w\":6", kn);

        var w2 = Named("MyLogins", 2);
        Assert.True(DashboardJson.TryNormalize(w2, null, false, out var insight, out _), "insight 应把非法 w 归一为 3");
        Assert.Contains("\"w\":3", insight);

        Assert.True(DashboardJson.TryNormalize(w2, null, false, DashboardJson.SurfaceWorkbench, out var wb, out var e3), e3);
        Assert.Contains("\"w\":2", wb);

        var w8 = Named("Monitor", 8, "monitorChart");
        Assert.True(DashboardJson.TryNormalize(w8, null, false, DashboardJson.SurfaceWorkbench, out var m, out _), "w=8");
        Assert.Contains("\"w\":8", m);
    }

    [Fact(DisplayName = "Catalog workbench：普通用户无 AdminOnly named")]
    public void Catalog_MemberHidesAdminOnly()
    {
        CubeWidgetManager.Reset();
        var member = MakeUser(false);
        var names = CubeWidgetManager.CatalogFor(member, "workbench").Select(e => e.Name).ToArray();
        Assert.Contains("MyLogins", names);
        Assert.Contains("MyDays", names);
        Assert.Contains("QuickLink", names);
        Assert.Contains("Profile", names);
        Assert.Contains("Inbox", names);
        Assert.DoesNotContain("UserCount", names);
        Assert.DoesNotContain("Monitor", names);
        Assert.DoesNotContain("SysInfo", names);
        Assert.DoesNotContain("LoginLog", names);
        Assert.False(CubeWidgetManager.Visible(CubeWidgetManager.Find("UserCount"), member));
    }

    [Fact(DisplayName = "Catalog insight 不含 workbench-only named；系统角色 workbench 含 14 件")]
    public void Catalog_SurfaceIsolation()
    {
        CubeWidgetManager.Reset();
        var admin = MakeUser(true);
        var insight = CubeWidgetManager.CatalogFor(admin, "insight").Select(e => e.Name).ToArray();
        Assert.DoesNotContain("UserCount", insight);
        Assert.DoesNotContain("MyLogins", insight);
        var wb = CubeWidgetManager.CatalogFor(admin, "workbench").Select(e => e.Name).ToArray();
        Assert.Equal(14, wb.Length);
        Assert.Contains("Inbox", wb);
        Assert.Contains("UserCount", wb);
        Assert.Contains("Monitor", wb);
    }

    [Fact(DisplayName = "UserCount GetData 与 User.Meta.Count 一致")]
    public void UserCount_MatchesMeta()
    {
        var user = MakeUser(true);
        var n = User.Meta.Count;
        var data = new UserCountWidget().GetData(new WidgetContext { User = user });
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        Assert.Contains(n.ToString("n0"), json);
    }

    [Fact(DisplayName = "Decode 经 FastJson 写出后仍含 widgets（禁止 JsonElement）")]
    public void Decode_FastJsonKeepsWidgets()
    {
        var raw = WorkbenchSeeds.Admin;
        var decoded = WorkbenchController.Decode(raw);
        Assert.NotNull(decoded);
        var writer = new JsonWriter();
        writer.Options.CamelCase = true;
        writer.Write(new { code = 0, data = new { source = "system", config = decoded } });
        var wire = writer.GetString();
        Assert.DoesNotContain("valueKind", wire, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"widgets\"", wire);
        Assert.Contains("seed-UserCount", wire);
        var root = JsonParser.Decode(wire) as IDictionary<String, Object>;
        Assert.NotNull(root);
        var data = root["data"] as IDictionary<String, Object>;
        Assert.NotNull(data);
        var config = data["config"] as IDictionary<String, Object>;
        Assert.NotNull(config);
        Assert.True(config["widgets"] is IList<Object> list && list.Count > 0);
    }
}
