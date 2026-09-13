using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife;
using NewLife.Caching;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using XCode;
using XCode.DataAccessLayer;
using Xunit;

namespace XUnitTest;

/// <summary>Map 外键候选在列表分区（ListField）的补齐规则：编号列补候选，展示列与地区列保持原样。</summary>
/// <remarks>
/// GetPage 的 list/allList 分区此前不调用 <see cref="MapCandidateFiller"/>，列表中的外键编号列只能显示原始数字。
/// 现按字段类型分流：ListField 且非 String 的编号列补内联候选/远程值集；String 展示列（RoleName/ParentName 等）
/// 本身已是可读名称，补字典只会多出徽章且无法翻译多值串；地区列沿用既有级联渲染，不改写 ItemType。
/// 用例使用独立测试实体 + 固定 MapProvider + 预置行数缓存，既绕开真实库表，也不与其它测试类共享缓存键。
/// </remarks>
public class MapCandidateFillerListTests
{
    #region 测试实体

    /// <summary>列表候选目标实体</summary>
    [DisplayName("列表候选目标")]
    [BindTable("OscListMapTarget", ConnName = "Cube", DbType = DatabaseType.None)]
    [BindIndex("IU_OscListMapTarget_Name", true, "Name")]
    public partial class ListMapTarget : Entity<ListMapTarget>
    {
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get; set; }

        /// <summary>名称</summary>
        [DisplayName("名称")]
        [BindColumn("Name", "名称", "", Master = true)]
        public String? Name { get; set; }
    }

    /// <summary>带 Map 外键的源实体</summary>
    [DisplayName("列表候选源")]
    [BindTable("OscListMapSource", ConnName = "Cube", DbType = DatabaseType.None)]
    [BindIndex("IX_OscListMapSource_TargetId", false, "TargetId")]
    public partial class ListMapSource : Entity<ListMapSource>
    {
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get; set; }

        /// <summary>目标编号</summary>
        [DisplayName("目标")]
        [Map(nameof(TargetId), typeof(ListMapTarget), "Id")]
        public Int32 TargetId { get; set; }

        /// <summary>目标名称（查找列）</summary>
        [DisplayName("目标名称")]
        [Map(nameof(TargetId))]
        public String? TargetName { get; set; }

        /// <summary>地区</summary>
        [DisplayName("地区")]
        public Int32 AreaId { get; set; }

        /// <summary>名称</summary>
        [DisplayName("名称")]
        [BindColumn("Name", "名称", "", Master = true)]
        public String? Name { get; set; }
    }

    #endregion

    #region 辅助

    private static IEntityFactory SourceFactory() => EntityFactory.CreateFactory(typeof(ListMapSource));

    /// <summary>固定候选来源：绕开 MapProvider.GetDataSource 的库表依赖</summary>
    private sealed class FixedMapProvider : MapProvider
    {
        private readonly IDictionary<Object, String> _dic;

        public FixedMapProvider(IDictionary<Object, String> dic)
        {
            EntityType = typeof(ListMapTarget);
            Key = "Id";
            _dic = dic;
        }

        public override IDictionary<Object, String> GetDataSource() => _dic;
    }

    /// <summary>构造固定候选来源并预置目标表行数缓存（60s），避免填充器走 Session.Count 触库</summary>
    /// <param name="count">目标表行数，用于驱动小表/大表分支</param>
    /// <returns>候选提供者</returns>
    private static MapProvider NewProvider(Int32 count = 1)
    {
        MemoryCache.Instance.Set("LovMapCount:" + typeof(ListMapTarget).FullName, count, 60);

        return new FixedMapProvider(new Dictionary<Object, String> { [1] = "目标A", [2] = "目标B" });
    }

    #endregion

    [Fact(DisplayName = "列表 Map 候选：Int32 外键编号列补内联候选")]
    public void List_Int32ForeignKey_FillsCandidates()
    {
        var lf = new ListField
        {
            Name = "TargetId",
            MapField = "TargetId",
            Type = typeof(Int32),
            MapProvider = NewProvider(),
        };

        MapCandidateFiller.Apply(new List<DataField> { lf }, SourceFactory());

        Assert.True(lf.DataSourceMap != null && lf.DataSourceMap.Count > 0, "列表外键编号列应补内联候选");
        Assert.True(lf.LovCode.IsNullOrEmpty(), "小表目标不应注册 Entity. 值集");
    }

    [Fact(DisplayName = "列表 Map 候选：String 展示列不补候选、不改写物理列")]
    public void List_StringDisplayColumn_Untouched()
    {
        var lf = new ListField
        {
            Name = "TargetName",
            MapField = "TargetId",
            Type = typeof(String),
            ReadOnly = true,
            MapProvider = NewProvider(),
        };

        MapCandidateFiller.Apply(new List<DataField> { lf }, SourceFactory());

        Assert.Null(lf.DataSourceMap);
        Assert.True(lf.LovCode.IsNullOrEmpty());
        Assert.Equal("TargetName", lf.Name);
        Assert.True(lf.ReadOnly);
    }

    [Fact(DisplayName = "列表 Map 候选：地区列不改写 ItemType（保持既有级联渲染）")]
    public void List_AreaColumn_KeepsItemType()
    {
        var lf = new ListField { Name = "AreaId", Type = typeof(Int32), MapProvider = NewProvider() };

        MapCandidateFiller.Apply(new List<DataField> { lf }, SourceFactory());

        Assert.True(lf.ItemType.IsNullOrEmpty());
        Assert.True(lf.DataSourceMap == null || lf.DataSourceMap.Count == 0);
    }

    [Fact(DisplayName = "列表 Map 候选：大表编号列走 Entity. 值集（>MaxDropDownList）")]
    public void List_BigTableForeignKey_UsesLovCode()
    {
        var lf = new ListField { Name = "TargetId", Type = typeof(Int32), MapProvider = NewProvider(100) };

        MapCandidateFiller.Apply(new List<DataField> { lf }, SourceFactory());

        Assert.Equal("Entity." + typeof(ListMapTarget).FullName, lf.LovCode);
        Assert.True(lf.DataSourceMap == null || lf.DataSourceMap.Count == 0);

        MemoryCache.Instance.Remove("LovMapCount:" + typeof(ListMapTarget).FullName);
    }

    [Fact(DisplayName = "表单/搜索字段行为不变：仍还原物理列、取消只读并补候选")]
    public void Form_BehaviorUnchanged()
    {
        var fact = SourceFactory();
        var ff = new FormField
        {
            Name = "TargetName",
            MapField = "TargetId",
            Type = typeof(String),
            ReadOnly = true,
            MapProvider = NewProvider(),
        };

        MapCandidateFiller.Apply(new List<DataField> { ff }, fact);

        Assert.Equal("TargetId", ff.Name);
        Assert.Equal(typeof(Int32), ff.Type);
        Assert.False(ff.ReadOnly);
        Assert.True(ff.DataSourceMap != null && ff.DataSourceMap.Count > 0);
    }

    [Fact(DisplayName = "表单/搜索字段行为不变：地区字段仍标 area4")]
    public void Form_AreaField_SetsArea4()
    {
        var area = new FormField { Name = "AreaId", Type = typeof(Int32) };

        MapCandidateFiller.Apply(new List<DataField> { area }, SourceFactory());

        Assert.Equal("area4", area.ItemType);
        Assert.True(area.DataSourceMap == null || area.DataSourceMap.Count == 0);
    }

    [Fact(DisplayName = "契约闭环：填充器内联候选经 ToDictionary 输出 dataSource（外键字段可下拉）")]
    public void Filler_ToDictionary_Contract()
    {
        var ff = new FormField
        {
            Name = "TargetId",
            MapField = "TargetId",
            Type = typeof(Int32),
            MapProvider = NewProvider(),
        };

        MapCandidateFiller.Apply(new List<DataField> { ff }, SourceFactory());

        var dic = ff.ToDictionary();
        var ds = dic["dataSource"] as IDictionary<String, String>;

        Assert.NotNull(ds);
        Assert.Equal("目标A", ds!["1"]);
        Assert.False(dic.ContainsKey("multiple"), "物化字典来源不按名称推断多选");
    }
}
