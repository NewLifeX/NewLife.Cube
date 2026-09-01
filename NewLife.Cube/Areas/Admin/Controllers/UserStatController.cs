using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;
using static NewLife.Cube.Entity.UserStat;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>访问统计控制器</summary>
[AdminArea]
[Menu(0, false)]
public class UserStatController : ReadOnlyEntityController<UserStat>
{
    static UserStatController()
    {
        ListFields.RemoveField("CreateTime", "UpdateTime", "Remark");
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<UserStat> Search(Pager p)
    {
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        p.RetrieveState = true;

        return SearchByDate(start, end, p["Q"], p);
    }

    /// <summary>构建图表数据。对齐 MVC 版工作台：主折线图（多系列三Y轴）+ 箱线图 + K线图</summary>
    /// <param name="data">搜索得到的数据列表</param>
    /// <returns>ECharts 配置数组，每个元素为 option 对象</returns>
    protected override Object[] OnGetChartData(IEnumerable<UserStat> data)
    {
        var list = data?.OrderBy(e => e.Date).ToList() ?? [];
        // 无日期参数时 Search 查空，兜底查询近 30 天保证图表默认有数据
        if (list.Count == 0)
            list = SearchByDate(DateTime.Today.AddDays(-30), DateTime.Today, null, new Pager { PageSize = 1000 }).OrderBy(e => e.Date).ToList();
        if (list.Count == 0) return [];

        // X 轴：日期
        var dates = list.Select(e => e.Date.ToString("MM-dd")).ToArray();

        // 主折线图：9 个统计系列 + 总数（右轴）+ 在线时间（右轴带平均线/最大最小）
        Object[] kpiSeries =
        [
            new { name = "登录数", type = "line", data = list.Select(e => e.Logins).ToArray() },
            new { name = "OAuth登录", type = "line", data = list.Select(e => e.OAuths).ToArray() },
            new { name = "最大在线", type = "line", data = list.Select(e => e.MaxOnline).ToArray() },
            new { name = "活跃", type = "line", data = list.Select(e => e.Actives).ToArray() },
            new { name = "7天活跃", type = "line", data = list.Select(e => e.ActivesT7).ToArray() },
            new { name = "30天活跃", type = "line", data = list.Select(e => e.ActivesT30).ToArray() },
            new { name = "新用户", type = "line", data = list.Select(e => e.News).ToArray() },
            new { name = "7天注册", type = "line", data = list.Select(e => e.NewsT7).ToArray() },
            new { name = "30天注册", type = "line", data = list.Select(e => e.NewsT30).ToArray() },
            new { name = "总数", type = "line", yAxisIndex = 1, smooth = true, data = list.Select(e => e.Total).ToArray() },
            new
            {
                name = "在线时间",
                type = "line",
                yAxisIndex = 2,
                smooth = true,
                data = list.Select(e => e.OnlineTime).ToArray(),
                markLine = new { data = new[] { new { type = "average", name = "Avg" } } },
                markPoint = new { data = new[] { new { type = "max", name = "Max" }, new { type = "min", name = "Min" } } },
            },
        ];

        var chart = new
        {
            title = new { text = "用户统计" },
            tooltip = new { trigger = "axis" },
            legend = new { data = new[] { "登录数", "OAuth登录", "最大在线", "活跃", "7天活跃", "30天活跃", "新用户", "7天注册", "30天注册", "总数", "在线时间" } },
            grid = new { left = "3%", right = "4%", bottom = "3%", containLabel = true },
            toolbox = new
            {
                show = true,
                feature = new
                {
                    mark = new { show = true },
                    dataView = new { show = true },
                    magicType = new { show = true, type = new[] { "line", "bar", "stack" } },
                    restore = new { show = true },
                    saveAsImage = new { show = true },
                }
            },
            xAxis = new { type = "category", data = dates },
            yAxis = new Object[]
            {
                new { type = "value", name = "用户数" },
                new { type = "value", name = "总数", position = "right" },
                new { type = "value", name = "时长", position = "right", offset = 40, axisLabel = new { formatter = "{value}秒" } },
            },
            series = kpiSeries,
        };

        // 箱线图：每日 5 个统计维度（对齐 MVC BoxplotItem）
        var box = new
        {
            title = new { text = "用户箱线（仅用于演示）" },
            tooltip = new { trigger = "item" },
            legend = new { data = new[] { "用户箱线（仅用于演示）" } },
            xAxis = new { type = "category", data = dates },
            yAxis = new { type = "value", name = "值" },
            series = new Object[]
            {
                new
                {
                    name = "用户箱线（仅用于演示）",
                    type = "boxplot",
                    data = list.Select(e => new Object[] { e.News, e.Actives, e.NewsT7, e.ActivesT7, e.NewsT30 }).ToArray(),
                }
            },
        };

        // K线图：开=活跃 收=7天注册 低=新用户 高=7天活跃（对齐 MVC CandlestickItem）
        var candle = new
        {
            title = new { text = "用户K线（仅用于演示）" },
            tooltip = new { trigger = "item" },
            legend = new { data = new[] { "用户K线（仅用于演示）" } },
            xAxis = new { type = "category", data = dates },
            yAxis = new { type = "value", name = "值" },
            series = new Object[]
            {
                new
                {
                    name = "用户K线（仅用于演示）",
                    type = "candlestick",
                    data = list.Select(e => new Object[] { e.Actives, e.NewsT7, e.News, e.ActivesT7 }).ToArray(),
                }
            },
        };

        return [chart, box, candle];
    }
}