using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewLife.Cube;
using NewLife.Cube.Automation;
using NewLife.Cube.Entity;
using NewLife.Cube.Widgets;
using NewLife.Remoting;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

[CollectionDefinition("Osc260903", DisableParallelization = true)]
public class Osc260903Collection
{
}

[DisplayName("OSC260903 Widget 查询实体")]
[BindTable("Osc260903Item", ConnName = "Osc260903Item", DbType = DatabaseType.None)]
public class Osc260903Item : Entity<Osc260903Item>
{
    private Int32 _Id;
    [DisplayName("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private String _Name = "";
    [DisplayName("名称")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Name", "名称", "")]
    public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

    private Int32 _Amount;
    [DisplayName("金额")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Amount", "金额", "")]
    public Int32 Amount { get => _Amount; set { if (OnPropertyChanging("Amount", value)) { _Amount = value; OnPropertyChanged("Amount"); } } }

    private Int32 _RoleId;
    [DisplayName("角色")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("RoleId", "角色", "")]
    public Int32 RoleId { get => _RoleId; set { if (OnPropertyChanging("RoleId", value)) { _RoleId = value; OnPropertyChanged("RoleId"); } } }

    private DateTime _CreateTime;
    [DisplayName("创建时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }
}

public class Osc260903ItemController : EntityController<Osc260903Item>
{
}

[Collection("Osc260903")]
public class Osc260903WidgetQueryTests
{
    public Osc260903WidgetQueryTests()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "Osc260903Item.db*"))
        {
            try { File.Delete(f); } catch { /* 连接占用时沿用已有库 */ }
        }
        DAL.AddConnStr("Osc260903Item", $"Data Source={Path.Combine(dir, "Osc260903Item.db")}", null, "SQLite");
        DAL.AddConnStr("Osc260903Cube", $"Data Source={Path.Combine(dir, "Osc260903Cube.db")}", null, "SQLite");
        DAL.AddConnStr("Osc260903Mem", $"Data Source={Path.Combine(dir, "Osc260903Mem.db")}", null, "SQLite");
        Osc260903Item.Meta.ConnName = "Osc260903Item";
        ViewProfile.Meta.ConnName = "Osc260903Cube";
        User.Meta.ConnName = "Osc260903Mem";
        Role.Meta.ConnName = "Osc260903Mem";
        EntityPageRegistry.Register(typeof(Osc260903Item), "/Admin/Osc260903", "Id");
        TryCreate(Osc260903Item.Meta.Factory);
    }

    static void TryCreate(IEntityFactory fact)
    {
        try
        {
            fact.Session.Dal.Db.CreateMetaData().SetSchema(DDLSchema.CreateTable, fact.Table.DataTable);
        }
        catch
        {
            // 文件库可能已建表
        }
    }

    static IUser SystemUser()
    {
        TryCreate(Role.Meta.Factory);
        TryCreate(User.Meta.Factory);
        var role = new Role { Name = "sys-" + Guid.NewGuid().ToString("N")[..8], Enable = true, IsSystem = true };
        role.Insert();
        var user = new User { Name = "u-" + Guid.NewGuid().ToString("N")[..8], Enable = true, RoleID = role.ID };
        user.Insert();
        return user;
    }

    static void SeedItem(String name, Int32 amount, Int32 roleId, DateTime? createTime = null)
    {
        var e = new Osc260903Item();
        e.SetItem(nameof(Osc260903Item.Name), name);
        e.SetItem(nameof(Osc260903Item.Amount), amount);
        e.SetItem(nameof(Osc260903Item.RoleId), roleId);
        e.SetItem(nameof(Osc260903Item.CreateTime), createTime ?? DateTime.Today);
        Assert.True(e.Insert() > 0);
    }

    static String WidgetJson(String extraFilter)
    {
        var w = """{"version":1,"widgets":[{"id":"w1","kind":"metricCard","title":"t","layout":{"w":3,"order":0},"source":{"provider":"entity.aggregate","typePath":"Admin/Osc260903"},"query":{"measure":{"fn":"count"},"extraFilter":__EF__}}]}""";
        return w.Replace("__EF__", extraFilter);
    }

    static String HostJson() => """{"version":1,"widgets":[{"id":"w1","kind":"metricCard","title":"t","layout":{"w":3,"order":0},"source":{"provider":"entity.aggregate","typePath":"Admin/Osc260903"},"query":{"measure":{"fn":"count"}}}]}""";

    #region DashboardJson 保存端校验

    [Fact(DisplayName = "DashboardJson：合法字面 extraFilter 保存通过且保留")]
    public void DashboardJson_ValidLiteralFilter()
    {
        var json = WidgetJson("""{"logic":"all","conditions":[{"field":"Name","op":"eq","value":"keep"}]}""");
        Assert.True(DashboardJson.TryNormalize(json, null, false, DashboardJson.SurfaceInsight, "Admin/Osc260903", out var n, out var err), err);
        Assert.Contains("\"extraFilter\"", n);
        Assert.Contains("\"keep\"", n);
    }

    [Fact(DisplayName = "DashboardJson：字段越权（非 search∪list）→ 400")]
    public void DashboardJson_UnknownFieldRejected()
    {
        var json = WidgetJson("""{"logic":"all","conditions":[{"field":"Nope","op":"eq","value":1}]}""");
        Assert.False(DashboardJson.TryNormalize(json, null, false, DashboardJson.SurfaceInsight, "Admin/Osc260903", out _, out var err));
        Assert.Contains("未授权字段", err);
    }

    [Fact(DisplayName = "DashboardJson：操作符非法 → 400")]
    public void DashboardJson_IllegalOpRejected()
    {
        var json = WidgetJson("""{"logic":"all","conditions":[{"field":"Name","op":"like","value":"x"}]}""");
        Assert.False(DashboardJson.TryNormalize(json, null, false, DashboardJson.SurfaceInsight, "Admin/Osc260903", out _, out var err));
        Assert.Contains("操作符", err);
    }

    [Fact(DisplayName = "DashboardJson：复杂度上限（any>5 / 总数>10）→ 400")]
    public void DashboardJson_TooManyRejected()
    {
        var any5 = "{\"logic\":\"any\",\"conditions\":[" + string.Join(",", Enumerable.Range(0, 6).Select(i => $"{{\"field\":\"Name\",\"op\":\"eq\",\"value\":\"v{i}\"}}")) + "]}";
        Assert.False(DashboardJson.TryNormalize(WidgetJson(any5), null, false, DashboardJson.SurfaceInsight, "Admin/Osc260903", out _, out var err1));
        Assert.Contains("过多", err1);

        var all10 = "{\"logic\":\"all\",\"conditions\":[" + string.Join(",", Enumerable.Range(0, 11).Select(i => $"{{\"field\":\"Name\",\"op\":\"eq\",\"value\":\"v{i}\"}}")) + "]}";
        Assert.False(DashboardJson.TryNormalize(WidgetJson(all10), null, false, DashboardJson.SurfaceInsight, "Admin/Osc260903", out _, out var err2));
        Assert.Contains("过多", err2);
    }

    [Fact(DisplayName = "DashboardJson：insight $host 合法通过；宿主字段不存在 / 非法对象 → 400")]
    public void DashboardJson_HostRefValidation()
    {
        // 合法：$host 引用宿主字段 RoleId（宿主实体存在该字段）
        var ok = WidgetJson("""{"logic":"all","conditions":[{"field":"RoleId","op":"eq","value":{"$host":"RoleId"}}]}""");
        Assert.True(DashboardJson.TryNormalize(ok, null, false, DashboardJson.SurfaceInsight, "Admin/Osc260903", out var n, out var err), err);
        Assert.Contains("$host", n);

        // 宿主字段不存在
        var bad = WidgetJson("""{"logic":"all","conditions":[{"field":"RoleId","op":"eq","value":{"$host":"Nope"}}]}""");
        Assert.False(DashboardJson.TryNormalize(bad, null, false, DashboardJson.SurfaceInsight, "Admin/Osc260903", out _, out var err2));
        Assert.Contains("未知宿主字段", err2);

        // 对象值非 $host 单键
        var obj = WidgetJson("""{"logic":"all","conditions":[{"field":"RoleId","op":"eq","value":{"x":1}}]}""");
        Assert.False(DashboardJson.TryNormalize(obj, null, false, DashboardJson.SurfaceInsight, "Admin/Osc260903", out _, out var err3));
        Assert.Contains("值无效", err3);
    }

    [Fact(DisplayName = "DashboardJson：工作台域含 $host → 400（无宿主）")]
    public void DashboardJson_WorkbenchRejectsHostRef()
    {
        var json = WidgetJson("""{"logic":"all","conditions":[{"field":"RoleId","op":"eq","value":{"$host":"RoleId"}}]}""");
        Assert.False(DashboardJson.TryNormalize(json, null, false, DashboardJson.SurfaceWorkbench, null, out _, out var err));
        Assert.Contains("不支持宿主引用", err);

        // 工作台静态条件仍允许
        var staticJson = WidgetJson("""{"logic":"all","conditions":[{"field":"Name","op":"eq","value":"a"}]}""");
        Assert.True(DashboardJson.TryNormalize(staticJson, null, false, DashboardJson.SurfaceWorkbench, null, out _, out var err2), err2);
    }

    [Fact(DisplayName = "DashboardJson：旧 overload（无宿主）不含 extraFilter 兼容")]
    public void DashboardJson_OldOverloadCompat()
    {
        Assert.True(DashboardJson.TryNormalize(HostJson(), null, false, out var n, out _));
        Assert.DoesNotContain("extraFilter", n);
    }

    [Fact(DisplayName = "DashboardJson：extraFilter 畸形形状（非对象 / 条件元素非对象）→ 400")]
    public void DashboardJson_MalformedShapeRejected()
    {
        // 非对象
        foreach (var bad in new[] { "\"abc\"", "[\"x\"]", "123" })
        {
            Assert.False(DashboardJson.TryNormalize(WidgetJson(bad), null, false, DashboardJson.SurfaceInsight, "Admin/Osc260903", out _, out var err), bad);
            Assert.Contains("值无效", err);
        }

        // conditions 元素非对象
        var elem = WidgetJson("""{"logic":"all","conditions":["x"]}""");
        Assert.False(DashboardJson.TryNormalize(elem, null, false, DashboardJson.SurfaceInsight, "Admin/Osc260903", out _, out var err2));
        Assert.Contains("值无效", err2);

        // 纯静态条件不要求宿主实体可解析（hostTypePath 归一后仍可保存）
        var stat = WidgetJson("""{"logic":"all","conditions":[{"field":"Name","op":"eq","value":"a"}]}""");
        Assert.True(DashboardJson.TryNormalize(stat, null, false, DashboardJson.SurfaceInsight, "/Admin/Osc260903/", out _, out var err3), err3);
    }

    #endregion

    #region WidgetQueryService $host 解析

    static WidgetQueryRequest Query(String mode = "aggregate", String hostTypePath = null)
    {
        return new WidgetQueryRequest
        {
            Mode = mode,
            TypePath = "Admin/Osc260903",
            HostTypePath = hostTypePath,
            Measure = new WidgetMeasure { Fn = "count" },
        };
    }

    static Object HostRef(String field = "RoleId") => JsonDocument.Parse(($$"""{"$host":"{{field}}"}""")).RootElement.Clone();

    static ViewFilterDto Filter(params ViewFilterConditionDto[] conds) => new() { Logic = "all", Conditions = conds.ToList() };

    [Fact(DisplayName = "静态 extraFilter：count/group/list 生效")]
    public void Execute_StaticExtraFilter()
    {
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = false;
        try
        {
            Osc260903Item.Meta.ConnName = "Osc260903Item";
            Osc260903Item.Meta.Session.Truncate();
            SeedItem("a", 10, 1);
            SeedItem("b", 20, 1);
            SeedItem("c", 30, 2);
            var user = SystemUser();

            var count = Query();
            count.ExtraFilter = Filter(new ViewFilterConditionDto { Field = "RoleId", Op = "eq", Value = 1 });
            var r = WidgetQueryService.Execute(user, count);
            Assert.Equal(2L, Convert.ToInt64(r.Value));
            Assert.False(r.HostFilterApplied);

            var list = Query("list");
            list.ExtraFilter = Filter(new ViewFilterConditionDto { Field = "Amount", Op = "gt", Value = 15 });
            var rows = WidgetQueryService.Execute(user, list);
            Assert.NotNull(rows.Rows);
            Assert.Equal(2, rows.Rows.Count);

            // 分组模式：条件同样生效（仅 Name=c）
            var group = Query();
            group.GroupBy = "Name";
            group.ExtraFilter = Filter(new ViewFilterConditionDto { Field = "Amount", Op = "gt", Value = 25 });
            var g = WidgetQueryService.Execute(user, group);
            Assert.NotNull(g.Items);
            var one = Assert.Single(g.Items);
            Assert.Equal("c", one.Key);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = oldTenant;
        }
    }

    [Fact(DisplayName = "$host 解析：宿主筛选等值命中 → 过滤且 HostFilterApplied=true")]
    public void Execute_HostRefResolved()
    {
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = false;
        try
        {
            Osc260903Item.Meta.ConnName = "Osc260903Item";
            Osc260903Item.Meta.Session.Truncate();
            SeedItem("a", 10, 1);
            SeedItem("b", 20, 1);
            SeedItem("c", 30, 2);
            var user = SystemUser();

            var req = Query(hostTypePath: "Admin/User");
            req.ExtraFilter = Filter(new ViewFilterConditionDto { Field = "RoleId", Op = "eq", Value = HostRef() });
            req.HostFilter = Filter(new ViewFilterConditionDto { Field = "RoleId", Op = "eq", Value = 1 });

            var r = WidgetQueryService.Execute(user, req);
            Assert.Equal(2L, Convert.ToInt64(r.Value));
            Assert.True(r.HostFilterApplied);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = oldTenant;
        }
    }

    [Fact(DisplayName = "$host 缺宿主上下文：条件跳过、HostFilterApplied=false、返回全量")]
    public void Execute_HostRefMissing()
    {
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = false;
        try
        {
            Osc260903Item.Meta.ConnName = "Osc260903Item";
            Osc260903Item.Meta.Session.Truncate();
            SeedItem("a", 10, 1);
            SeedItem("b", 20, 2);
            var user = SystemUser();

            var req = Query(hostTypePath: "Admin/User");
            req.ExtraFilter = Filter(new ViewFilterConditionDto { Field = "RoleId", Op = "eq", Value = HostRef() });

            var r = WidgetQueryService.Execute(user, req);
            Assert.Equal(2L, Convert.ToInt64(r.Value));
            Assert.False(r.HostFilterApplied);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = oldTenant;
        }
    }

    [Fact(DisplayName = "$host 与静态混合：宿主缺失时静态仍生效")]
    public void Execute_HostRefMixedWithStatic()
    {
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = false;
        try
        {
            Osc260903Item.Meta.ConnName = "Osc260903Item";
            Osc260903Item.Meta.Session.Truncate();
            SeedItem("a", 5, 1);
            SeedItem("b", 50, 1);
            SeedItem("c", 60, 2);
            var user = SystemUser();

            // 无宿主上下文：$host 跳过，静态 Amount>=10 生效 → b/c = 2
            var req = Query(hostTypePath: "Admin/User");
            req.ExtraFilter = Filter(
                new ViewFilterConditionDto { Field = "RoleId", Op = "eq", Value = HostRef() },
                new ViewFilterConditionDto { Field = "Amount", Op = "gte", Value = 10 });
            var r = WidgetQueryService.Execute(user, req);
            Assert.Equal(2L, Convert.ToInt64(r.Value));
            Assert.False(r.HostFilterApplied);

            // 有宿主上下文 RoleId=1：RoleId=1 AND Amount>=10 → b = 1
            req.HostFilter = Filter(new ViewFilterConditionDto { Field = "RoleId", Op = "eq", Value = 1 });
            var r2 = WidgetQueryService.Execute(user, req);
            Assert.Equal(1L, Convert.ToInt64(r2.Value));
            Assert.True(r2.HostFilterApplied);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = oldTenant;
        }
    }

    [Fact(DisplayName = "$host 非法对象值（非单键 $host）→ 400")]
    public void Execute_HostRefIllegalObjectRejected()
    {
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = false;
        try
        {
            Osc260903Item.Meta.ConnName = "Osc260903Item";
            Osc260903Item.Meta.Session.Truncate();
            SeedItem("a", 10, 1);
            var user = SystemUser();

            var req = Query(hostTypePath: "Admin/User");
            req.ExtraFilter = Filter(new ViewFilterConditionDto
            {
                Field = "RoleId",
                Op = "eq",
                Value = JsonDocument.Parse("""{"x":1}""").RootElement.Clone(),
            });
            var ex = Assert.Throws<ApiException>(() => WidgetQueryService.Execute(user, req));
            Assert.Equal(400, ex.Code);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = oldTenant;
        }
    }

    [Fact(DisplayName = "list 模式 $host 解析 → 行集过滤")]
    public void Execute_List_HostRef()
    {
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = false;
        try
        {
            Osc260903Item.Meta.ConnName = "Osc260903Item";
            Osc260903Item.Meta.Session.Truncate();
            SeedItem("a", 10, 1);
            SeedItem("b", 20, 2);
            var user = SystemUser();

            var req = Query("list", hostTypePath: "Admin/User");
            req.ExtraFilter = Filter(new ViewFilterConditionDto { Field = "RoleId", Op = "eq", Value = HostRef() });
            req.HostFilter = Filter(new ViewFilterConditionDto { Field = "RoleId", Op = "eq", Value = 2 });
            var r = WidgetQueryService.Execute(user, req);
            Assert.NotNull(r.Rows);
            var row = Assert.Single(r.Rows);
            Assert.Equal("b", row["Name"] + "");
            Assert.True(r.HostFilterApplied);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = oldTenant;
        }
    }

    [Fact(DisplayName = "group 模式 $host 解析 → 分组项收敛且 HostFilterApplied=true")]
    public void Execute_Group_HostRef()
    {
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = false;
        try
        {
            Osc260903Item.Meta.ConnName = "Osc260903Item";
            Osc260903Item.Meta.Session.Truncate();
            SeedItem("a", 10, 1);
            SeedItem("b", 20, 1);
            SeedItem("c", 30, 2);
            var user = SystemUser();

            var req = Query();
            req.GroupBy = "Name";
            req.ExtraFilter = Filter(new ViewFilterConditionDto { Field = "RoleId", Op = "eq", Value = HostRef() });
            req.HostFilter = Filter(new ViewFilterConditionDto { Field = "RoleId", Op = "eq", Value = 1 });

            var r = WidgetQueryService.Execute(user, req);
            Assert.NotNull(r.Items);
            Assert.Equal(2, r.Items.Count);
            Assert.All(r.Items, it => Assert.Contains(it.Key, new[] { "a", "b" }));
            Assert.True(r.HostFilterApplied);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = oldTenant;
        }
    }

    [Fact(DisplayName = "$host 引用时间字段：宿主等值条件解析生效；非等值（after）不解析 → 条件跳过")]
    public void Execute_HostRefOnTimeField()
    {
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = false;
        try
        {
            Osc260903Item.Meta.ConnName = "Osc260903Item";
            Osc260903Item.Meta.Session.Truncate();
            SeedItem("old", 10, 1, DateTime.Today.AddDays(-3));
            SeedItem("new", 20, 1, DateTime.Today);
            var user = SystemUser();

            // 宿主筛选等值：CreateTime == 今天 → $host 解析为该值，条件收敛
            var req = Query(hostTypePath: "Admin/User");
            req.ExtraFilter = Filter(new ViewFilterConditionDto { Field = "CreateTime", Op = "eq", Value = HostRef("CreateTime") });
            req.HostFilter = Filter(new ViewFilterConditionDto { Field = "CreateTime", Op = "eq", Value = DateTime.Today });

            var r = WidgetQueryService.Execute(user, req);
            Assert.Equal(1L, Convert.ToInt64(r.Value));
            Assert.True(r.HostFilterApplied);

            // 宿主筛选非等值（after）：不满足「等值条件」定义 → $host 条件跳过，无过滤
            var req2 = Query(hostTypePath: "Admin/User");
            req2.ExtraFilter = Filter(new ViewFilterConditionDto { Field = "CreateTime", Op = "after", Value = HostRef("CreateTime") });
            req2.HostFilter = Filter(new ViewFilterConditionDto { Field = "CreateTime", Op = "after", Value = DateTime.Today.AddDays(-1) });

            var r2 = WidgetQueryService.Execute(user, req2);
            Assert.Equal(2L, Convert.ToInt64(r2.Value));
            Assert.False(r2.HostFilterApplied);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = oldTenant;
        }
    }

    #endregion
}
