using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Caching;
using NewLife.Cube;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTest;

/// <summary>OSC-0016 通用查询与预定义查询：ViewProfile QueriesJson / Map 搜索候选 / entity: 内部值集 / GetPage MasterTime</summary>
public class Osc0016Tests
{
    // -------------------------------------------------------------------------
    // 测试实体（独立命名，避免与既有实体/测试缓存冲突）
    // -------------------------------------------------------------------------

    /// <summary>小表目标：Map 外键候选内联数据源</summary>
    [DisplayName("OSC0016 小表目标")]
    [BindTable("OscSmallTarget", ConnName = "Cube", DbType = DatabaseType.None)]
    [BindIndex("IU_OscSmallTarget_Name", true, "Name")]
    public partial class OscSmallTarget : Entity<OscSmallTarget>
    {
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get; set; }

        /// <summary>名称</summary>
        [DisplayName("名称")]
        public String Name { get; set; }
    }

    /// <summary>大表目标：行数超阈值触发 Entity. 值集注册</summary>
    [DisplayName("OSC0016 大表目标")]
    [BindTable("OscBigTarget", ConnName = "Cube", DbType = DatabaseType.None)]
    [BindIndex("IU_OscBigTarget_Name", true, "Name")]
    public partial class OscBigTarget : Entity<OscBigTarget>
    {
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get; set; }

        /// <summary>名称</summary>
        [DisplayName("名称")]
        public String Name { get; set; }
    }

    /// <summary>带 Map 外键的源实体（小表目标）</summary>
    [DisplayName("OSC0016 小表源")]
    [BindTable("OscSmallSource", ConnName = "Cube", DbType = DatabaseType.None)]
    [BindIndex("IX_OscSmallSource_TargetId", false, "TargetId")]
    public partial class OscSmallSource : Entity<OscSmallSource>
    {
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get; set; }

        /// <summary>目标</summary>
        [Map(nameof(TargetId), typeof(OscSmallTarget), "Id")]
        [DisplayName("目标")]
        public Int32 TargetId { get; set; }

        /// <summary>名称</summary>
        [DisplayName("名称")]
        public String Name { get; set; }
    }

    /// <summary>带 Map 外键的源实体（大表目标）</summary>
    [DisplayName("OSC0016 大表源")]
    [BindTable("OscBigSource", ConnName = "Cube", DbType = DatabaseType.None)]
    [BindIndex("IX_OscBigSource_TargetId", false, "TargetId")]
    public partial class OscBigSource : Entity<OscBigSource>
    {
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get; set; }

        /// <summary>目标</summary>
        [Map(nameof(TargetId), typeof(OscBigTarget), "Id")]
        [DisplayName("目标")]
        public Int32 TargetId { get; set; }

        /// <summary>名称</summary>
        [DisplayName("名称")]
        public String Name { get; set; }
    }

    /// <summary>含主时间字段的实体（T4 有 MasterTime 分支）</summary>
    [DisplayName("OSC0016 含主时间源")]
    [BindTable("OscTimeSource", ConnName = "Cube", DbType = DatabaseType.None)]
    [BindIndex("IX_OscTimeSource_Name", false, "Name")]
    public partial class OscTimeSource : Entity<OscTimeSource>
    {
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get; set; }

        /// <summary>名称</summary>
        [DisplayName("名称")]
        public String Name { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime UpdateTime { get; set; }
    }

    /// <summary>无主时间字段的实体（T4 无 MasterTime 分支）</summary>
    [DisplayName("OSC0016 无时间源")]
    [BindTable("OscNoTimeSource", ConnName = "Cube", DbType = DatabaseType.None)]
    [BindIndex("IX_OscNoTimeSource_Name", false, "Name")]
    public partial class OscNoTimeSource : Entity<OscNoTimeSource>
    {
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get; set; }

        /// <summary>名称</summary>
        [DisplayName("名称")]
        public String Name { get; set; }
    }

    /// <summary>T4 用控制器（有主时间）</summary>
    public class OscTimeController : ReadOnlyEntityController<OscTimeSource> { }

    /// <summary>T4 用控制器（无主时间）</summary>
    public class OscNoTimeController : ReadOnlyEntityController<OscNoTimeSource> { }

    // -------------------------------------------------------------------------

    private readonly ITestOutputHelper _output;

    public Osc0016Tests(ITestOutputHelper output)
    {
        _output = output;
        // 隔离测试库，避免污染开发库
        DAL.AddConnStr("Cube", "Data Source=Osc0016Tests;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    #region T1 ViewProfile QueriesJson

    [Fact]
    [DisplayName("T1 ViewProfile QueriesJson upsert 读写一致；null 不覆盖；空串可清空")]
    public void ViewProfile_QueriesJson_Upsert()
    {
        var uid = Environment.TickCount & 0x0FFF_FFFF | 0x1000_0000;
        var path = "Admin/Osc0016" + (Environment.TickCount & 0xFFFF);
        var queries = """{"version":1,"queries":[{"id":"q_1","name":"昨日新增客户","params":{"Q":"客户"}}]}""";

        // 写入后读回一致
        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel { TypePath = path, QueriesJson = queries, Version = 1 });
        var saved = ViewProfile.FindByUserIdAndTypePath(uid, path);
        Assert.NotNull(saved);
        Assert.Equal(queries, saved.QueriesJson);

        // null 不覆盖已有值
        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel { TypePath = path, Version = 1 });
        Assert.Equal(queries, ViewProfile.FindByUserIdAndTypePath(uid, path)?.QueriesJson);

        // 显式空串/空壳覆盖（清空域）
        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel { TypePath = path, QueriesJson = "{}", Version = 1 });
        Assert.Equal("{}", ViewProfile.FindByUserIdAndTypePath(uid, path)?.QueriesJson);
    }

    #endregion

    #region T2 Map 字段搜索候选

    [Fact]
    [DisplayName("T2 Map 候选：小表目标（≤MaxDropDownList）内联 DataSourceMap")]
    public void FieldCollection_Search_MapCandidates_SmallTable()
    {
        new OscSmallTarget { Name = "目标A" }.Insert();
        new OscSmallTarget { Name = "目标B" }.Insert();

        var fact = EntityFactory.CreateFactory(typeof(OscSmallSource));
        var field = fact.Table.FindByName("TargetId");
        var sf = new SearchField { Name = "TargetId" };
        var mi = typeof(FieldCollection).GetMethod("FillMapCandidates", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(mi);
        mi.Invoke(null, [sf, field]);

        Assert.True(sf.DataSourceMap != null && sf.DataSourceMap.Count > 0, "小表目标应内联 DataSourceMap");
        Assert.True(sf.LovCode.IsNullOrEmpty(), "小表目标不应注册 Entity. 值集");
    }

    [Fact]
    [DisplayName("T2 Map 候选：大表目标（行数>MaxDropDownList）注册 Entity. LovCode")]
    public void FieldCollection_Search_MapCandidates_BigTable()
    {
        // 预置目标表行数缓存（内存库自增插入仅落 1 行，直接以 100 行驱动大表分支判定）
        MemoryCache.Instance.Set("LovMapCount:" + typeof(OscBigTarget).FullName, 100, 60);

        var fact = EntityFactory.CreateFactory(typeof(OscBigSource));
        var field = fact.Table.FindByName("TargetId");
        var sf = new SearchField { Name = "TargetId" };
        var mi = typeof(FieldCollection).GetMethod("FillMapCandidates", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(mi);
        mi.Invoke(null, [sf, field]);

        Assert.Equal("Entity." + typeof(OscBigTarget).FullName, sf.LovCode);
        Assert.True(sf.DataSourceMap == null || sf.DataSourceMap.Count == 0, "大表目标不应内联 DataSourceMap");
    }

    [Fact]
    [DisplayName("T2 Map 候选：手工已设 LovCode 不被覆盖")]
    public void FieldCollection_Search_MapCandidates_ManualLovCodeWins()
    {
        var fact = EntityFactory.CreateFactory(typeof(OscSmallSource));
        var field = fact.Table.FindByName("TargetId");
        var sf = new SearchField { Name = "TargetId", LovCode = "Manual.Code" };

        var mi = typeof(FieldCollection).GetMethod("FillMapCandidates", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(mi);
        mi.Invoke(null, [sf, field]);

        Assert.Equal("Manual.Code", sf.LovCode);
        Assert.Null(sf.DataSourceMap);
    }

    #endregion

    #region T3 entity: 内部实体值集自动注册

    [Fact]
    [DisplayName("T3 LovAutoRegisterService：行数超阈值 Map 目标自动注册 Entity. 值集与 entity: ListConfig")]
    public void LovAutoRegister_ScanMapLovs_RegistersBigTable()
    {
        // 插入 1 行目标数据并触发源实体注册（ScanAndRegisterMapLovs 只遍历已注册工厂）
        new OscBigTarget { Name = "大目标" }.Insert();
        EntityFactory.CreateFactory(typeof(OscBigSource));

        // 临时调低阈值：内存库自增插入仅落 1 行，行数 1 > 0 即触发大表注册分支
        var old = CubeSetting.Current.MaxDropDownList;
        CubeSetting.Current.MaxDropDownList = 0;
        try
        {
            var svc = new LovAutoRegisterService();
            var reg = svc.ScanAndRegisterMapLovs();
            Assert.True(reg >= 1, $"应注册至少 1 个值集，实际 {reg}");

            var bigDef = LovDefinition.Find(LovDefinition._.LovCode == "Entity." + typeof(OscBigTarget).FullName);
            Assert.NotNull(bigDef);
            Assert.Equal("LIST", bigDef.Type);
            Assert.Equal("AUTO", bigDef.Source);
            var cfg = LovListConfig.FindByLovDefId(bigDef.Id);
            Assert.NotNull(cfg);
            Assert.StartsWith("entity:", cfg.RequestUrl);
            Assert.True(cfg.Pageable);
        }
        finally
        {
            CubeSetting.Current.MaxDropDownList = old;
        }
    }

    #endregion

    #region T4 GetPage setting MasterTime

    [Fact]
    [DisplayName("T4 GetPage setting：含 MasterTime 实体输出 masterTimeName/masterTimeDisplayName")]
    public void GetPage_Setting_WithMasterTime()
    {
        // 显式指定主时间字段（生产实体在静态初始化时设置）
        var fact = EntityFactory.CreateFactory(typeof(OscTimeSource));
        fact.MasterTime = fact.Table.FindByName("UpdateTime");

        var controller = new OscTimeController();
        var rs = (JsonResult)controller.GetPage();
        var wrapper = rs.Value!;
        var data = wrapper.GetType().GetProperty("data")!.GetValue(wrapper)!;
        var setting = data.GetType().GetProperty("setting")!.GetValue(data)!;
        var name = setting.GetType().GetProperty("masterTimeName")?.GetValue(setting) as String;
        var display = setting.GetType().GetProperty("masterTimeDisplayName")?.GetValue(setting) as String;
        Assert.Equal("UpdateTime", name);
        Assert.Equal("更新时间", display);
    }

    [Fact]
    [DisplayName("T4 GetPage setting：无 MasterTime 实体两键为 null")]
    public void GetPage_Setting_WithoutMasterTime()
    {
        var controller = new OscNoTimeController();
        var rs = (JsonResult)controller.GetPage();
        var wrapper = rs.Value!;
        var data = wrapper.GetType().GetProperty("data")!.GetValue(wrapper)!;
        var setting = data.GetType().GetProperty("setting")!.GetValue(data)!;
        var name = setting.GetType().GetProperty("masterTimeName")?.GetValue(setting);
        var display = setting.GetType().GetProperty("masterTimeDisplayName")?.GetValue(setting);
        Assert.Null(name);
        Assert.Null(display);
    }

    #endregion
}
