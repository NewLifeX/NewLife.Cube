using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using NewLife.Cube;
using NewLife.Cube.Entity;
using NewLife.Cube.Widgets;
using NewLife.Remoting;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

[CollectionDefinition("Osc260828", DisableParallelization = true)]
public class Osc260828Collection
{
}

[DisplayName("OSC260828 Widget 实体")]
[BindTable("Osc260828Item", ConnName = "Osc260828Item", DbType = DatabaseType.None)]
public class Osc260828Item : Entity<Osc260828Item>
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

    private DateTime _CreateTime;
    [DisplayName("创建时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }
}

public class Osc260828ItemController : EntityController<Osc260828Item>
{
}

[Collection("Osc260828")]
public class Osc260828WidgetTests
{
    public Osc260828WidgetTests()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "Osc260828Item.db*"))
        {
            try { File.Delete(f); } catch { /* 连接占用时沿用已有库 */ }
        }
        DAL.AddConnStr("Osc260828Item", $"Data Source={Path.Combine(dir, "Osc260828Item.db")}", null, "SQLite");
        DAL.AddConnStr("Osc260828Cube", $"Data Source={Path.Combine(dir, "Osc260828Cube.db")}", null, "SQLite");
        DAL.AddConnStr("Osc260828Mem", $"Data Source={Path.Combine(dir, "Osc260828Mem.db")}", null, "SQLite");
        Osc260828Item.Meta.ConnName = "Osc260828Item";
        ViewProfile.Meta.ConnName = "Osc260828Cube";
        User.Meta.ConnName = "Osc260828Mem";
        Role.Meta.ConnName = "Osc260828Mem";
        EntityPageRegistry.Register(typeof(Osc260828Item), "/Admin/Osc260828", "Id");
        TryCreate(Osc260828Item.Meta.Factory);
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

    static void SeedItem(String name, Int32 amount, DateTime? createTime = null)
    {
        var e = new Osc260828Item();
        e.SetItem(nameof(Osc260828Item.Name), name);
        e.SetItem(nameof(Osc260828Item.Amount), amount);
        e.SetItem(nameof(Osc260828Item.CreateTime), createTime ?? DateTime.Today);
        Assert.True(e.Insert() > 0);
    }

    [Fact(DisplayName = "DashboardJson：重复 id / 超过 12 / 超 64KB / version 非法 → 失败")]
    public void DashboardJson_RejectsInvalid()
    {
        Assert.False(DashboardJson.TryNormalize("""{"version":2,"widgets":[]}""", null, false, out _, out _));

        var dup = """{"version":1,"widgets":[{"id":"a","kind":"metricCard","title":"t","layout":{"w":3,"order":0},"source":{"provider":"entity.aggregate","typePath":"Admin/Osc260828"},"query":{"measure":{"fn":"count"}}},{"id":"a","kind":"metricCard","title":"t2","layout":{"w":3,"order":1},"source":{"provider":"entity.aggregate","typePath":"Admin/Osc260828"},"query":{"measure":{"fn":"count"}}}]}""";
        Assert.False(DashboardJson.TryNormalize(dup, null, false, out _, out var e1));
        Assert.Contains("id", e1, StringComparison.OrdinalIgnoreCase);

        var tooMany = "{\"version\":1,\"widgets\":[" +
            string.Join(",", Enumerable.Range(0, 13).Select(i =>
                "{\"id\":\"w" + i + "\",\"kind\":\"metricCard\",\"title\":\"t\",\"layout\":{\"w\":3,\"order\":" + i +
                "},\"source\":{\"provider\":\"entity.aggregate\",\"typePath\":\"Admin/Osc260828\"},\"query\":{\"measure\":{\"fn\":\"count\"}}}")) +
            "]}";
        Assert.False(DashboardJson.TryNormalize(tooMany, null, false, out _, out _));

        var huge = "{\"version\":1,\"widgets\":[],\"pad\":\"" + new String('x', 70 * 1024) + "\"}";
        Assert.False(DashboardJson.TryNormalize(huge, null, false, out _, out _));

        Assert.False(DashboardJson.TryNormalize("""{"version":1,"widgets":[{"id":"l","kind":"legacyChart","title":"t","layout":{"w":3,"order":0},"source":{"provider":"entity.aggregate","typePath":"Admin/Osc260828"}}]}""", null, false, out _, out var e2));
        Assert.Contains("legacyChart", e2, StringComparison.OrdinalIgnoreCase);

        Assert.False(DashboardJson.TryNormalize("""{"version":1,"widgets":[{"id":"k","kind":"miniKanban","title":"t","layout":{"w":6,"order":0},"source":{"provider":"entity.list","typePath":"Admin/Osc260828"}}]}""", null, false, out _, out var e3));
        Assert.Contains("数据看板", e3);

        Assert.False(DashboardJson.TryNormalize("""{"version":1,"widgets":[{"id":"d","kind":"dataList","title":"t","layout":{"w":6,"order":0},"source":{"provider":"entity.list","typePath":"Admin/Osc260828"}}]}""", null, false, out _, out var e4));
        Assert.Contains("数据列表", e4);

        Assert.True(DashboardJson.TryNormalize("""{"version":1,"widgets":[{"id":"d","kind":"dataList","title":"t","layout":{"w":6,"order":0},"source":{"provider":"entity.list","typePath":"Admin/Osc260828"}}]}""", null, false, DashboardJson.SurfaceWorkbench, out var listJson, out var e5), e5);
        Assert.Contains("dataList", listJson);

        Assert.False(DashboardJson.TryNormalize("""{"version":1,"widgets":[{"id":"c","kind":"dataCard","title":"t","layout":{"w":6,"order":0},"source":{"provider":"entity.list","typePath":"Admin/Osc260828"}}]}""", null, false, out _, out var e6));
        Assert.Contains("数据卡片", e6);

        Assert.True(DashboardJson.TryNormalize("""{"version":1,"widgets":[{"id":"c","kind":"dataCard","title":"t","layout":{"w":6,"order":0},"source":{"provider":"entity.list","typePath":"Admin/Osc260828"}}]}""", null, false, DashboardJson.SurfaceWorkbench, out var cardJson, out var e7), e7);
        Assert.Contains("dataCard", cardJson);
    }

    [Fact(DisplayName = "DashboardJson：合法配置归一化 order 且保留未知键")]
    public void DashboardJson_Normalizes()
    {
        var json = """{"version":1,"widgets":[{"id":"b","kind":"metricCard","title":"t","layout":{"w":9,"order":5},"source":{"provider":"entity.aggregate","typePath":"/Admin/Osc260828"},"query":{"measure":{"fn":"count"}},"data":1}],"extraFlag":true}""";
        Assert.True(DashboardJson.TryNormalize(json, null, false, out var n, out var err), err);
        Assert.Contains("\"w\":3", n);
        Assert.Contains("\"order\":0", n);
        Assert.DoesNotContain("\"data\"", n);
        Assert.Contains("extraFlag", n);
        Assert.Contains("Admin/Osc260828", n);
    }

    [Fact(DisplayName = "ViewProfile DashboardJson 空串清除后个人域不视为有效")]
    public void DashboardDomain_EmptyString()
    {
        Assert.False(ViewProfile.HasDashboardDomain(null));
        Assert.False(ViewProfile.HasDashboardDomain(""));
        Assert.True(ViewProfile.HasDashboardDomain("""{"version":1,"widgets":[]}"""));
        Assert.True(ViewProfile.IsEmptyDashboardJson("""{"version":1,"widgets":[]}"""));
    }

    [Fact(DisplayName = "Query count 与 FindCount 一致；非法 field 400；sql 键 400")]
    public void Query_Count_And_Rejects()
    {
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = false;
        try
        {
        Osc260828Item.Meta.ConnName = "Osc260828Item";
        Osc260828Item.Meta.Session.Truncate();
        SeedItem("a", 10);
        SeedItem("b", 20);
        var user = SystemUser();
        Assert.Equal(2, Osc260828Item.FindCount());

        var r = WidgetQueryService.Execute(user, new WidgetQueryRequest
        {
            Mode = "aggregate",
            TypePath = "Admin/Osc260828",
            Measure = new WidgetMeasure { Fn = "count" },
        });
        Assert.Equal(2L, Convert.ToInt64(r.Value));

        var ex = Assert.Throws<ApiException>(() => WidgetQueryService.Execute(user, new WidgetQueryRequest
        {
            TypePath = "Admin/Osc260828",
            Measure = new WidgetMeasure { Fn = "sum", Field = "Name" },
        }));
        Assert.Equal(400, ex.Code);

        var sqlEx = Assert.Throws<ApiException>(() => WidgetQueryService.Execute(user, new WidgetQueryRequest
        {
            TypePath = "Admin/Osc260828",
            Extra = new System.Collections.Generic.Dictionary<String, System.Text.Json.JsonElement>
            {
                ["sql"] = System.Text.Json.JsonDocument.Parse("\"x\"").RootElement.Clone(),
            },
        }));
        Assert.Equal(400, sqlEx.Code);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = oldTenant;
        }
    }

    [Fact(DisplayName = "无系统角色且无菜单时 Query 403")]
    public void Query_Forbidden_WithoutDetail()
    {
        TryCreate(Role.Meta.Factory);
        TryCreate(User.Meta.Factory);
        var role = new Role { Name = "ns-" + Guid.NewGuid().ToString("N")[..8], Enable = true, IsSystem = false };
        role.Insert();
        var user = new User { Name = "u-" + Guid.NewGuid().ToString("N")[..8], Enable = true, RoleID = role.ID };
        user.Insert();
        var ex = Assert.Throws<ApiException>(() => WidgetQueryService.Execute(user, new WidgetQueryRequest
        {
            TypePath = "Admin/Osc260828",
            Measure = new WidgetMeasure { Fn = "count" },
        }));
        Assert.Equal(403, ex.Code);
    }

    [Fact(DisplayName = "GroupBy 聚合返回 items")]
    public void Query_GroupBy_ReturnsItems()
    {
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = false;
        try
        {
            Osc260828Item.Meta.ConnName = "Osc260828Item";
            Osc260828Item.Meta.Session.Truncate();
            SeedItem("a", 10);
            SeedItem("a", 11);
            SeedItem("b", 20);
            var user = SystemUser();
            var r = WidgetQueryService.Execute(user, new WidgetQueryRequest
            {
                Mode = "aggregate",
                TypePath = "Admin/Osc260828",
                Measure = new WidgetMeasure { Fn = "count" },
                GroupBy = "Name",
            });
            Assert.Null(r.Value);
            Assert.NotNull(r.Items);
            Assert.Equal(2, r.Items.Count);
            var a = r.Items.FirstOrDefault(e => e.Key == "a");
            Assert.NotNull(a);
            Assert.Equal(2L, Convert.ToInt64(a.Value));
            Assert.Equal("a", a.Label);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = oldTenant;
        }
    }

    [Fact(DisplayName = "分组标签：枚举/布尔友好名")]
    public void FormatGroupLabel_EnumAndBool()
    {
        var sexFi = User.Meta.Factory.Table.FindByName("Sex");
        Assert.NotNull(sexFi);
        Assert.Equal("男", WidgetQueryService.FormatGroupLabel(sexFi, "1"));
        Assert.Equal("女", WidgetQueryService.FormatGroupLabel(sexFi, "2"));
        Assert.Equal("未知", WidgetQueryService.FormatGroupLabel(sexFi, "0"));

        var enableFi = User.Meta.Factory.Table.FindByName("Enable");
        Assert.NotNull(enableFi);
        Assert.Equal("是", WidgetQueryService.FormatGroupLabel(enableFi, "true"));
        Assert.Equal("否", WidgetQueryService.FormatGroupLabel(enableFi, "0"));
    }

    [Fact(DisplayName = "DateBucketSql：常见方言有表达式，其它为 null")]
    public void DateBucketSql_SupportedDialects()
    {
        Assert.Contains("strftime", WidgetQueryService.DateBucketSql(DatabaseType.SQLite, "CreateTime"));
        Assert.Contains("DATE_FORMAT", WidgetQueryService.DateBucketSql(DatabaseType.MySql, "CreateTime"));
        Assert.Contains("CONVERT", WidgetQueryService.DateBucketSql(DatabaseType.SqlServer, "CreateTime"));
        Assert.Contains("to_char", WidgetQueryService.DateBucketSql(DatabaseType.PostgreSQL, "CreateTime"));
        Assert.Null(WidgetQueryService.DateBucketSql(DatabaseType.Oracle, "CreateTime"));
    }

    [Fact(DisplayName = "timeField SQLite 按日分桶")]
    public void Query_TimeField_SqliteBuckets()
    {
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = false;
        try
        {
            Osc260828Item.Meta.ConnName = "Osc260828Item";
            Osc260828Item.Meta.Session.Truncate();
            SeedItem("a", 1, DateTime.Today);
            SeedItem("b", 1, DateTime.Today);
            SeedItem("c", 1, DateTime.Today.AddDays(-1));
            var user = SystemUser();
            var r = WidgetQueryService.Execute(user, new WidgetQueryRequest
            {
                Mode = "aggregate",
                TypePath = "Admin/Osc260828",
                Measure = new WidgetMeasure { Fn = "count" },
                TimeField = "CreateTime",
                Buckets = 7,
            });
            Assert.NotNull(r.Items);
            Assert.True(r.Items.Count >= 1);
            Assert.Equal(3L, r.Items.Sum(e => Convert.ToInt64(e.Value)));
        }
        finally
        {
            CubeSetting.Current.EnableTenant = oldTenant;
        }
    }

    [Fact(DisplayName = "跨实体无 mapping 时 hostFilter 不进入 Where")]
    public void Query_CrossEntity_NoMapping()
    {
        var oldTenant = CubeSetting.Current.EnableTenant;
        CubeSetting.Current.EnableTenant = false;
        try
        {
        Osc260828Item.Meta.ConnName = "Osc260828Item";
        Osc260828Item.Meta.Session.Truncate();
        SeedItem("keep", 1);
        SeedItem("drop", 1);
        var user = SystemUser();
        Assert.Equal(2, Osc260828Item.FindCount());
        var r = WidgetQueryService.Execute(user, new WidgetQueryRequest
        {
            TypePath = "Admin/Osc260828",
            HostTypePath = "Admin/User",
            HostFilter = new NewLife.Cube.Automation.ViewFilterDto
            {
                Logic = "all",
                Conditions = [new() { Field = "Name", Op = "eq", Value = "keep" }],
            },
            Measure = new WidgetMeasure { Fn = "count" },
        });
        Assert.False(r.HostFilterApplied);
        Assert.Equal(2L, Convert.ToInt64(r.Value));
        }
        finally
        {
            CubeSetting.Current.EnableTenant = oldTenant;
        }
    }
}
