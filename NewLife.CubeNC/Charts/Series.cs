using System.Web.Script.Serialization;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Cube.Charts;

/// <summary>系列。一组数值以及他们映射成的图</summary>
public class Series : IExtend
{
    #region 属性
    /// <summary>图表类型</summary>
    public String Type { get; set; }
    //public SeriesTypes Type { get; set; }

    /// <summary>组件 ID</summary>
    /// <remark>默认不指定。指定则可用于在 option 或者 API 中引用组件。</remark>
    public String Id { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.2.0 开始支持
    /// 从调色盘 option.color 中取色的策略，可取值为：
    /// 'series'：按照系列分配调色盘中的颜色，同一系列中的所有数据都是用相同的颜色；
    /// 'data'：按照数据项分配调色盘中的颜色，每个数据项都使用不同的颜色。
    /// </remark>
    public String ColorBy { get; set; }

    /// <summary>数据</summary>
    public virtual Object[] Data { get; set; }

    ///// <summary>折线光滑</summary>
    //public virtual Boolean? Smooth { get; set; }

    /// <summary>标记的图形</summary>
    public virtual String Symbol { get; set; }

    ///// <summary>标记点。例如最大最小值</summary>
    //public Object MarkPoint { get; set; }

    ///// <summary>标记线。例如平均线</summary>
    //public Object MarkLine { get; set; }

    /// <summary>使用的 x 轴的 index，在单个图表实例中存在多个 x 轴的时候有用。</summary>
    public Double? XAxisIndex { get; set; }

    /// <summary>使用的 y 轴的 index，在单个图表实例中存在多个 y轴的时候有用。</summary>
    public Double? YAxisIndex { get; set; }

    /// <summary>图表标注</summary>
    public Object MarkPoint { get; set; }

    /// <summary>图表标线</summary>
    public Object MarkLine { get; set; }

    /// <summary>扩展字典</summary>
    [ScriptIgnore]
    public IDictionary<String, Object> Items { get; set; } = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>扩展数据</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Object this[String key] { get => Items[key]; set => Items[key] = value; }
    #endregion

    #region 方法
    /// <summary>标记最大最小值</summary>
    /// <param name="max"></param>
    /// <param name="min"></param>
    public void SetMarkPoint(Boolean max, Boolean min)
    {
        var typeNames = new Dictionary<String, String>();

        if (max) typeNames["max"] = "Max";
        if (min) typeNames["min"] = "Min";

        //MarkPoint(typeNames);
        MarkPoint = new { data = typeNames.Select(e => new { type = e.Key, name = e.Value }).ToArray() };
    }

    ///// <summary>标记点。例如最大最小值</summary>
    ///// <param name="typeNames"></param>
    //public void MarkPoint(IDictionary<String, String> typeNames)
    //{
    //    Items["markPoint"] = new { data = typeNames.Select(e => new { type = e.Key, name = e.Value }).ToArray() };
    //}

    /// <summary>标记平均线</summary>
    /// <param name="avg"></param>
    public void SetMarkLine(Boolean avg)
    {
        var typeNames = new Dictionary<String, String>();

        if (avg) typeNames["average"] = "Avg";

        //MarkLine(typeNames);
        MarkLine = new { data = typeNames.Select(e => new { type = e.Key, name = e.Value }).ToArray() };
    }

    ///// <summary>标记线</summary>
    ///// <param name="typeNames"></param>
    //public void MarkLine(IDictionary<String, String> typeNames)
    //{
    //    Items["markLine"] = new { data = typeNames.Select(e => new { type = e.Key, name = e.Value }).ToArray() };
    //}
    #endregion
}