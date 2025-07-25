using System.Collections;
using System.Web.Script.Serialization;
using NewLife.Collections;
using NewLife.Cube.Charts.Models;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;
using XCode.Configuration;

namespace NewLife.Cube.Charts;

/// <summary>ECharts实例</summary>
public class ECharts : IExtend
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; } = Rand.NextString(8);

    /// <summary>宽度。单位px，负数表示百分比，默认-100</summary>
    public Int32 Width { get; set; } = -100;

    /// <summary>高度。单位px，负数表示百分比，默认300px</summary>
    public Int32 Height { get; set; } = 300;

    /// <summary>网格</summary>
    public ChartGrid Grid { get; set; } = new();

    /// <summary>CSS样式</summary>
    public String Style { get; set; }

    /// <summary>CSS类</summary>
    public String Class { get; set; }

    /// <summary>标题。字符串或匿名对象</summary>
    public ChartTitle Title { get; set; }

    /// <summary>提示</summary>
    public Tooltip Tooltip { get; set; }

    /// <summary>图例组件。</summary>
    /// <remarks>图例组件展现了不同系列的标记(symbol)，颜色和名字。可以通过点击图例控制哪些系列不显示。</remarks>
    public Object Legend { get; set; }

    /// <summary>X轴</summary>
    public IList<XAxis> XAxis { get; set; } = [];

    /// <summary>Y轴</summary>
    public IList<YAxis> YAxis { get; set; } = [];

    /// <summary>工具箱</summary>
    public Toolbox Toolbox { get; set; }

    /// <summary>数据缩放</summary>
    public DataZoom[] DataZoom { get; set; }

    /// <summary>系列数据</summary>
    public IList<Series> Series { get; set; } = [];

    /// <summary>标记的图形。设置后添加的图形都使用该值</summary>
    [ScriptIgnore]
    public String Symbol { get; set; }

    /// <summary>扩展字典</summary>
    [ScriptIgnore]
    public IDictionary<String, Object> Items { get; set; } = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>扩展数据</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Object this[String key] { get => Items[key]; set => Items[key] = value; }

    /// <summary>附加JavaScript脚本</summary>
    /// <remarks>在某些组件的formatter中，可以设置回调函数，例如{#lineFormatter}，然后把js函数lineFormatter写在这里</remarks>
    [ScriptIgnore]
    public IList<String> Scripts { get; set; } = [];

    Object _xField;
    String _timeField;
    Func<IModel, String> _timeSelector;
    #endregion

    #region 两轴设置
    /// <summary>设置X轴。直接使用数据</summary>
    /// <param name="data">数据列表，从中选择数据构建X轴</param>
    public XAxis SetX(IEnumerable<Object> data)
    {
        var axis = new XAxis { Data = data.ToArray() };
        XAxis.Add(axis);
        return axis;
    }

    /// <summary>设置X轴</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">数据列表，从中选择数据构建X轴</param>
    /// <param name="name">作为X轴的字段，支持time时间轴</param>
    /// <param name="selector">构建X轴的委托</param>
    public XAxis SetX<T>(IList<T> list, String name, Func<T, String> selector = null) where T : class, IModel
    {
        var axis = new XAxis
        {
            Data = list.Select(e => selector == null ? e[name] + "" : selector(e)).ToArray()
        };
        XAxis.Add(axis);

        _xField = name;

        return axis;
    }

    /// <summary>设置X轴</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">数据列表，从中选择数据构建X轴</param>
    /// <param name="name">作为X轴的字段，支持time时间轴</param>
    /// <param name="selector">构建X轴的委托</param>
    public XAxis SetX4Time<T>(IList<T> list, String name, Func<T, String> selector = null) where T : class, IModel
    {
        var axis = new XAxis
        {
            Type = "time",
        };
        //if (name.EndsWithIgnoreCase("Date")) axis.AxisLabel = new { formatter = "{yyyy}-{MM}-{dd}" };
        XAxis.Add(axis);
        _timeField = name;
        _xField = name;

        if (selector != null)
            _timeSelector = e => selector(e as T) + "";

        if (Symbol.IsNullOrEmpty() && list.Count > 100) Symbol = "none";

        return axis;
    }

    /// <summary>设置X轴</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">数据列表，从中选择数据构建X轴</param>
    /// <param name="field">作为X轴的字段，支持time时间轴</param>
    /// <param name="selector">构建X轴的委托</param>
    public XAxis SetX<T>(IList<T> list, DataField field, Func<T, String> selector = null) where T : class, IModel
    {
        _xField = field;

        if (field != null && field.Type == typeof(DateTime))
            return SetX4Time(list, field.Name, selector);
        else
            return SetX(list, field.Name, selector);
    }

    /// <summary>设置X轴</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">数据列表，从中选择数据构建X轴</param>
    /// <param name="field">作为X轴的字段，支持time时间轴</param>
    /// <param name="selector">构建X轴的委托</param>
    public XAxis SetX<T>(IList<T> list, FieldItem field, Func<T, String> selector = null) where T : class, IModel
    {
        _xField = field;

        if (field != null && field.Type == typeof(DateTime))
            return SetX4Time(list, field.Name, selector);
        else
            return SetX(list, field.Name, selector);
    }

    Object GetTimeValue(IModel entity) => _timeSelector != null ? _timeSelector(entity) : entity[_timeField];

    /// <summary>设置Y轴</summary>
    /// <param name="name"></param>
    /// <param name="type">
    /// 坐标轴类型。
    /// value 数值轴，适用于连续数据。
    /// category 类目轴，适用于离散的类目数据，为该类型时必须通过 data 设置类目数据。
    /// time 时间轴，适用于连续的时序数据，与数值轴相比时间轴带有时间的格式化，在刻度计算上也有所不同，例如会根据跨度的范围来决定使用月，星期，日还是小时范围的刻度。
    /// log 对数轴。适用于对数数据。
    /// </param>
    public YAxis SetY(String name, String type = "value")
    {
        var axis = new YAxis { Name = name, Type = type };
        YAxis.Add(axis);
        return axis;
    }

    /// <summary>设置Y轴</summary>
    /// <param name="name"></param>
    /// <param name="type">
    /// 坐标轴类型。
    /// value 数值轴，适用于连续数据。
    /// category 类目轴，适用于离散的类目数据，为该类型时必须通过 data 设置类目数据。
    /// time 时间轴，适用于连续的时序数据，与数值轴相比时间轴带有时间的格式化，在刻度计算上也有所不同，例如会根据跨度的范围来决定使用月，星期，日还是小时范围的刻度。
    /// log 对数轴。适用于对数数据。
    /// </param>
    /// <param name="formatter"></param>
    public YAxis SetY(String name, String type, String formatter)
    {
        var axis = new YAxis { Name = name, Type = type, AxisLabel = new { formatter } };
        YAxis.Add(axis);
        return axis;
    }

    /// <summary>设置多个Y轴</summary>
    /// <param name="names"></param>
    /// <param name="type">
    /// 坐标轴类型。
    /// value 数值轴，适用于连续数据。
    /// category 类目轴，适用于离散的类目数据，为该类型时必须通过 data 设置类目数据。
    /// time 时间轴，适用于连续的时序数据，与数值轴相比时间轴带有时间的格式化，在刻度计算上也有所不同，例如会根据跨度的范围来决定使用月，星期，日还是小时范围的刻度。
    /// log 对数轴。适用于对数数据。
    /// </param>
    /// <param name="formatters">Y轴格式化字符串。如：{value} °C</param>
    public YAxis[] SetY(String[] names, String type = "value", String[] formatters = null)
    {
        //YAxis = names.Select(e => new { name = e, type }).ToArray();

        var list = new List<YAxis>();
        for (var i = 0; i < names.Length; i++)
        {
            var formatter = formatters != null && formatters.Length > i ? formatters[i] : null;
            if (i == 0)
                list.Add(new YAxis { Name = names[i], Type = type, AxisLabel = new { formatter } });
            else if (i == 1)
                list.Add(new YAxis { Name = names[i], Type = type, Position = "right", AxisLabel = new { formatter } });
            else
                list.Add(new YAxis { Name = names[i], Type = type, Position = "right", Offset = 40 * (i - 1), AxisLine = new { show = true }, AxisLabel = new { formatter } });
        }

        // 多Y轴时，右边偏移加大
        var n = names.Length - 1;
        if (n > 0) Grid.Right *= n;

        YAxis = list;

        return list.ToArray();
    }

    /// <summary>设置工具栏</summary>
    /// <param name="trigger">
    /// 触发类型。
    /// item, 数据项图形触发，主要在散点图，饼图等无类目轴的图表中使用。
    /// axis, 坐标轴触发，主要在柱状图，折线图等会使用类目轴的图表中使用。
    /// none, 什么都不触发。
    /// </param>
    /// <param name="axisPointerType">坐标轴指示器配置项。cross，坐标系会自动选择显示哪个轴的 axisPointer</param>
    /// <param name="backgroundColor"></param>
    public Tooltip SetTooltip(String trigger = "axis", String axisPointerType = "cross", String backgroundColor = "#6a7985")
    {
        return Tooltip = new Tooltip
        {
            Trigger = trigger,
            AxisPointer = new
            {
                type = axisPointerType,
                label = new
                {
                    backgroundColor
                }
            },
        };
    }

    /// <summary>设置工具栏</summary>
    /// <param name="trigger">
    /// 触发类型。
    /// item, 数据项图形触发，主要在散点图，饼图等无类目轴的图表中使用。
    /// axis, 坐标轴触发，主要在柱状图，折线图等会使用类目轴的图表中使用。
    /// none, 什么都不触发。
    /// </param>
    /// <param name="formatterScript">
    /// 格式化脚本。
    /// function lineFormatter(params) {
    ///     let res = `${params[0].name}&lt;br&gt;`;
    ///     params.forEach(p =&gt; {
    ///         if (p.value !== 0) {
    ///             res += `${p.marker} ${p.seriesName}: ${Math.abs(p.value)}吨&lt;br&gt;`;
    ///         }
    ///     });
    ///     return res;
    /// }
    /// </param>
    public Tooltip SetTooltip(String trigger, String formatterScript)
    {
        if (trigger.IsNullOrEmpty()) trigger = "axis";
        if (formatterScript.IsNullOrEmpty()) throw new ArgumentNullException(nameof(formatterScript));

        var p = formatterScript.IndexOf("function") + "function".Length;
        if (p < 0) throw new ArgumentOutOfRangeException(nameof(formatterScript), "无效js函数");

        var p2 = formatterScript.IndexOf('(', p);
        if (p2 < 0) throw new ArgumentOutOfRangeException(nameof(formatterScript), "无效js函数");

        var name = formatterScript.Substring(p, p2 - p).Trim();

        var tooltip = new Tooltip
        {
            Trigger = trigger,
            Formatter = $"{{#{name}}}"
        };

        Scripts.Add(formatterScript);

        return Tooltip = tooltip;
    }

    /// <summary>设置提示</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="field"></param>
    /// <param name="selector"></param>
    public void SetLegend<T>(IList<T> list, FieldItem field, Func<T, String> selector = null) where T : IModel
        => Legend = list.Select(e => selector == null ? e[field.Name] + "" : selector(e)).ToArray();

    /// <summary>设置工具栏</summary>
    /// <param name="orient">布局朝向。纵向vertical，默认横向horizontal</param>
    /// <param name="left">左边。默认靠右right</param>
    /// <param name="top">顶部。默认top</param>
    /// <param name="magicTypes">动态类型切换。默认line+bar+stack</param>
    /// <param name="dataView">显示数据视图。默认true</param>
    /// <returns></returns>
    public Toolbox SetToolbox(String orient = "horizontal", String left = "right", String top = "auto", String[] magicTypes = null, Boolean dataView = true)
    {
        if (magicTypes == null || magicTypes.Length == 0) magicTypes = ["line", "bar", "stack"];

        var toolbox = new Toolbox
        {
            Show = true,
            Orient = orient,
            Left = left,
            Top = top,
            Feature = new
            {
                mark = new { show = true },
                dataView = new { show = dataView },
                magicType = new
                {
                    show = true,
                    type = magicTypes
                },
                restore = new { show = true },
                saveAsImage = new { show = true },
            }
        };

        return Toolbox = toolbox;
    }

    /// <summary>添加缩放。默认X0轴，其它设置可直接修改返回对象</summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public DataZoom AddDataZoom(Int32 start = 0, Int32 end = 100)
    {
        var dz = new DataZoom
        {
            XAxiaIndex = [0],
            Start = start,
            End = end,
        };

        var list = DataZoom?.ToList() ?? [];
        list.Add(dz);

        DataZoom = list.ToArray();

        return dz;
    }
    #endregion

    #region 方法
    /// <summary>添加系列数据</summary>
    /// <param name="series"></param>
    public void Add(Series series)
    {
        Series.Add(series);
    }

    private Series Create(String name, String type, Object[] data = null)
    {
        if (type.IsNullOrEmpty()) type = "line";
        var sr = type switch
        {
            "line" => new SeriesLine { Type = type },
            "line3D" => new SeriesLine3D { Type = type },
            "lines" => new SeriesLines { Type = type },
            "lines3D" => new SeriesLines3D { Type = type },
            "bar" => new SeriesBar { Type = type },
            "bar3D" => new SeriesBar3D { Type = type },
            "pie" => new SeriesPie { Type = type },
            "graph" => new SeriesGraph { Type = type },
            "effectScatter" => new SeriesEffectScatter { Type = type },
            "boxplot" => new SeriesBoxplot { Type = type },
            "radar" => new SeriesRadar { Type = type },
            "funnel" => new SeriesFunnel { Type = type },
            "gauge" => new SeriesGauge { Type = type },
            "heatmap" => new SeriesHeatmap { Type = type },
            "sunburst" => new SeriesSunburst { Type = type },
            "tree" => new SeriesTree { Type = type },
            "treemap" => new SeriesTreemap { Type = type },
            "sankey" => new SeriesSankey { Type = type },
            _ => new Series { Type = type },
        };
        sr.Name = name;
        sr.Data = data;

        return sr;
    }

    /// <summary>添加系列数据</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="name">要使用数据的字段</param>
    /// <param name="type">图表类型，默认折线图line</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <returns></returns>
    public Series Add<T>(IList<T> list, String name, String type = "line", Func<T, Object> selector = null) where T : IModel
    {
        if (type.IsNullOrEmpty()) type = "line";

        var data = _timeField != null ?
            list.Select(e => new Object[] { GetTimeValue(e), selector == null ? e[name] : selector(e) }).ToArray() :
            list.Select(e => selector == null ? e[name] : selector(e)).ToArray();

        var sr = Create(name, type, data);

        if (!Symbol.IsNullOrEmpty()) sr.Symbol = Symbol;

        Add(sr);

        return sr;
    }

    /// <summary>添加系列数据</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="field">要使用数据的字段</param>
    /// <param name="type">图表类型，默认折线图line</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <returns></returns>
    public Series Add<T>(IList<T> list, FieldItem field, String type = "line", Func<T, Object> selector = null) where T : IModel
    {
        if (type.IsNullOrEmpty()) type = "line";

        var data = _timeField != null ?
            list.Select(e => new Object[] { GetTimeValue(e), selector == null ? e[field.Name] : selector(e) }).ToArray() :
            list.Select(e => selector == null ? e[field.Name] : selector(e)).ToArray();

        var sr = Create(field?.DisplayName ?? field.Name, type, data);

        if (!Symbol.IsNullOrEmpty()) sr.Symbol = Symbol;

        Add(sr);
        return sr;
    }

    /// <summary>添加系列数据</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="field">要使用数据的字段</param>
    /// <param name="type">图表类型，默认折线图line</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <returns></returns>
    public Series Add<T>(IList<T> list, FieldItem field, SeriesTypes type, Func<T, Object> selector = null) where T : IModel
    {
        var data = _timeField != null ?
            list.Select(e => new Object[] { GetTimeValue(e), selector == null ? e[field.Name] : selector(e) }).ToArray() :
            list.Select(e => selector == null ? e[field.Name] : selector(e)).ToArray();

        var sr = Create(field?.DisplayName ?? field.Name, type.ToString().ToLower(), data);

        if (!Symbol.IsNullOrEmpty()) sr.Symbol = Symbol;

        Add(sr);
        return sr;
    }

    /// <summary>批量添加系列数据</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="fields">要使用数据的字段</param>
    /// <param name="type">图表类型，默认折线图line</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <returns></returns>
    public IList<Series> Add<T>(IList<T> list, DataField[] fields, SeriesTypes type = SeriesTypes.Line, Func<T, Object> selector = null) where T : IModel
    {
        var rs = new List<Series>();
        foreach (var field in fields)
        {
            Series series = null;
            if (type == SeriesTypes.Line)
                series = AddLine(list, field, selector);
            else if (type == SeriesTypes.Bar)
                series = AddBar(list, field, selector);
            else if (type == SeriesTypes.Pie)
                series = AddPie(list, field);
            else
            {
                var data = _timeField != null ?
                list.Select(e => new Object[] { GetTimeValue(e), selector == null ? e[field.Name] : selector(e) }).ToArray() :
                list.Select(e => selector == null ? e[field.Name] : selector(e)).ToArray();

                series = Create(field?.DisplayName ?? field.Name, type.ToString().ToLower(), data);

                if (!Symbol.IsNullOrEmpty()) series.Symbol = Symbol;

                Add(series);
            }
            rs.Add(series);
        }

        return rs;
    }

    /// <summary>添加曲线系列数据</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="name">要使用数据的字段</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <param name="smooth">折线光滑</param>
    /// <returns></returns>
    public Series AddLine<T>(IList<T> list, String name, Func<T, Object> selector = null, Boolean smooth = false) where T : IModel => AddLine(list, new DataField { Name = name }, selector, smooth);

    /// <summary>添加曲线系列数据</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="field">要使用数据的字段</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <param name="smooth">折线光滑</param>
    /// <returns></returns>
    public Series AddLine<T>(IList<T> list, DataField field, Func<T, Object> selector = null, Boolean? smooth = null) where T : IModel
    {
        var data = _timeField != null ?
            list.Select(e => new Object[] { GetTimeValue(e), selector == null ? e[field.Name] : selector(e) }).ToArray() :
            list.Select(e => selector == null ? e[field.Name] : selector(e)).ToArray();

        var sr = Create(field?.DisplayName ?? field.Name, "line", data) as SeriesLine;
        sr.Smooth = smooth;

        if (!Symbol.IsNullOrEmpty()) sr.Symbol = Symbol;

        Add(sr);
        return sr;
    }

    /// <summary>添加曲线系列数据</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="field">要使用数据的字段</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <param name="smooth">折线光滑</param>
    /// <returns></returns>
    public Series AddLine<T>(IList<T> list, FieldItem field, Func<T, Object> selector = null, Boolean smooth = false) where T : IModel => AddLine(list, new DataField(field), selector, smooth);

    /// <summary>添加柱状图</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="name">要使用数据的字段</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <returns></returns>
    public Series AddBar<T>(IList<T> list, String name, Func<T, Object> selector = null) where T : IModel => AddBar(list, new DataField { Name = name }, selector);

    /// <summary>添加柱状图</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="field">要使用数据的字段</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <returns></returns>
    public Series AddBar<T>(IList<T> list, DataField field, Func<T, Object> selector = null) where T : IModel
    {
        var data = _timeField != null ?
            list.Select(e => new Object[] { GetTimeValue(e), selector == null ? e[field.Name] : selector(e) }).ToArray() :
            list.Select(e => selector == null ? e[field.Name] : selector(e)).ToArray();

        var sr = Create(field?.DisplayName ?? field.Name, "bar", data);

        Add(sr);
        return sr;
    }

    /// <summary>添加柱状图</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="field">要使用数据的字段</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <returns></returns>
    public Series AddBar<T>(IList<T> list, FieldItem field, Func<T, Object> selector = null) where T : IModel => AddBar(list, new DataField(field), selector);

    /// <summary>添加饼图</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="keyName">要使用分类的字段</param>
    /// <param name="valueName">要使用数据的字段</param>
    /// <param name="displayName">显示名</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <returns></returns>
    public Series AddPie<T>(IList<T> list, String keyName, String valueName, String displayName, Func<T, NameValue> selector = null) where T : IModel
    {
        var sr = Create(displayName ?? valueName, "pie");
        sr.Data = list.Select(e => selector == null ? new NameValue(e[keyName] + "", e[valueName]) : selector(e)).ToArray();

        Add(sr);
        return sr;
    }

    /// <summary>添加饼图</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="field">数据字段</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <returns></returns>
    public Series AddPie<T>(IList<T> list, DataField field, Func<T, NameValue> selector = null) where T : IModel
    {
        var keyName = field.Category;
        keyName ??= _xField as String;
        keyName ??= (_xField as DataField)?.Name;
        keyName ??= (_xField as FieldItem)?.Name;
        var sr = Create(field?.DisplayName ?? field.Name, "pie");
        sr.Data = list.Select(e => selector == null ? new NameValue(e[keyName] + "", e[field.Name]) : selector(e)).ToArray();

        Add(sr);
        return sr;
    }

    /// <summary>添加饼图</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">实体列表</param>
    /// <param name="field">要使用数据的字段</param>
    /// <param name="selector">数据选择器，默认null时直接使用字段数据</param>
    /// <returns></returns>
    public Series AddPie<T>(IList<T> list, FieldItem field, Func<T, NameValue> selector = null) where T : IModel
    {
        var keyName = _xField as String;
        keyName ??= (_xField as DataField)?.Name;
        keyName ??= (_xField as FieldItem)?.Name;
        keyName ??= field.Table.Master?.Name ?? field.Table.PrimaryKeys.FirstOrDefault()?.Name;
        var sr = Create(field?.DisplayName ?? field.Name, "pie");
        sr.Data = list.Select(e => selector == null ? new NameValue(e[keyName] + "", e[field.Name]) : selector(e)).ToArray();

        Add(sr);
        return sr;
    }

    /// <summary>添加图形。有向图/引力图</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public Series AddGraph(GraphViewModel model)
    {
        var graph = Create(model.Title, "graph");
        graph["Layout"] = model.Layout;
        if (model.Layout == "force")
        {
            graph["Force"] = new
            {
                initLayout = "circular",
                Repulsion = 300,
                gravity = 0.1,
                EdgeLength = new[] { 50, 300 },
                layoutAnimation = true,
                friction = 0.2,
            };
        }

        graph["edgeSymbol"] = new[] { "circle", "arrow" };
        graph["edgeSymbolSize"] = new[] { 4, 10 };
        graph["roam"] = true;
        graph["label"] = new { show = true, position = "right" };
        graph["labelLayout"] = new { hideOverlap = true, moveOverlap = true };
        graph["lineStyle"] = new { color = "target", curveness = 0.3, opacity = 0.8, width = 3 };

        graph.Data = model.Nodes;
        graph["links"] = model.Links;
        graph["categories"] = model.Categories;

        Legend = new { Data = model.Categories.Select(e => e.Name).ToArray() };

        return graph;
    }
    #endregion

    #region 构建生成
    /// <summary>构建选项Json</summary>
    /// <returns></returns>
    public String Build()
    {
        var dic = new Dictionary<String, Object>();
        var series = Series;

        // 标题
        var title = Title;
        if (title != null) dic[nameof(title)] = title;

        // 提示
        var tooltip = Tooltip;
        if (tooltip != null) dic[nameof(tooltip)] = GetJsonObject(tooltip);

        // 提示
        var legend = Legend;
        if (legend == null && series != null && series.Count > 0)
        {
            if (series.Any(e => e.Type == "line" || e.Type == "bar"))
                legend = series.Select(e => e.Name).ToArray();
            else if (series.Any(e => e.Type == "pie"))
                legend = new { show = true, top = "5%", bottom = "5%", left = "center" };
        }
        if (legend != null)
        {
            if (legend is String str)
                legend = new { data = new[] { str } };
            else if (legend is String[] ss)
                legend = new { data = ss };

            dic[nameof(legend)] = legend;
        }

        // 网格
        var grid = Grid;
        if (grid != null)
        {
            dic[nameof(grid)] = grid;
        }

        // 工具箱
        var toolbox = Toolbox;
        if (toolbox != null) dic[nameof(toolbox)] = GetJsonObject(toolbox);

        // X轴
        var xAxis = XAxis;
        if (xAxis != null)
        {
            //if (xAxis is String str)
            //    xAxis = new { data = new[] { str } };
            //else if (xAxis is String[] ss)
            //    xAxis = new { data = ss };

            dic[nameof(xAxis)] = GetJsonObject(xAxis);
        }

        // Y轴
        var yAxis = YAxis;
        if (yAxis != null) dic[nameof(yAxis)] = GetJsonObject(yAxis);

        var dataZoom = DataZoom;
        if (dataZoom != null) dic[nameof(dataZoom)] = dataZoom;

        // 系列数据
        if (series != null) dic[nameof(series)] = GetJsonObject(series);

        // 合并Items
        foreach (var item in Items)
        {
            dic[item.Key] = item.Value;
        }

#if DEBUG
        return dic.ToJson(true, true, true);
#else
        return dic.ToJson(false, true, true);
#endif
    }

    /// <summary>获取Json对象，去掉空成员</summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected virtual Object GetJsonObject(Object obj)
    {
        if (obj == null) return obj;

        var type = obj.GetType();
        if (type.GetTypeCode() != TypeCode.Object) return obj;

        if (obj is IList list)
        {
            var rs = new List<Object>();
            foreach (var item in list)
            {
                var val = GetJsonObject(item);
                if (val != null) rs.Add(val);
            }
            return rs;
        }

        var dic = new Dictionary<String, Object>();
        foreach (var pi in type.GetProperties(true))
        {
            if (pi.GetIndexParameters().Length > 0) continue;

            var val = pi.GetValue(obj, null);
            if (val is ICollection collection && collection.Count == 0) continue;

            if (val != null) dic[pi.Name] = val;
        }
        if (obj is IExtend ext)
        {
            foreach (var item in ext.Items)
            {
                if (item.Value != null) dic[item.Key] = item.Value;
            }
        }

        return dic;
    }
    #endregion
}