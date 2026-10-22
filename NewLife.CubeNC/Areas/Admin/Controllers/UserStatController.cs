using NewLife.Cube.Charts;
using NewLife.Cube.Charts.Models;
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

        var list = SearchByDate(start, end, p["Q"], p);

        if (list.Count > 0)
        {
            var list2 = list.OrderBy(e => e.Date).ToList();
            var chart = AddChart(list2, _.Date, null, [_.Logins, _.OAuths, _.MaxOnline, _.Actives, _.ActivesT7, _.ActivesT30, _.News, _.NewsT7, _.NewsT30], SeriesTypes.Line);
            chart.SetY([L.T("Users"), L.T("Total"), L.T("Duration")], "value", [null, null, L.T("{value}s")]);

            var line = chart.AddLine(list2, _.Total, null, true);
            line.YAxisIndex = 1;

            var line3 = chart.AddLine(list2, _.OnlineTime, null, true);
            line3.YAxisIndex = 2;

            // 绘制平均线和最大最小值
            line3.SetMarkLine(true);
            line3.SetMarkPoint(true, true);

            // 设置工具栏
            chart.SetTooltip("axis", """
                function tooltipFormatter(params) {
                    let res = `${params[0].axisValueLabel.substr(0, 10)}<br>`;
                    params.forEach(p => {
                        if (p.value[1] !== 0) {
                            res += `${p.marker} ${p.seriesName} <span style="float:right;margin-left:20px;"><b>${p.value[1]}</b></span><br>`;
                        }
                    });
                    return res;
                }
                """);

            chart.SetToolbox();

            var chart2 = new ECharts();
            chart2.SetX(list2, "Date", e => e.Date.ToString("MM-dd"));
            chart2.AddBoxplot(L.T("User box plot (demo only)"),
                list2.Select(e => new BoxplotItem(e.News, e.Actives, e.NewsT7, e.ActivesT7, e.NewsT30)),
                [L.T("New users"), L.T("Active today"), L.T("Registered (7d)"), L.T("Active (7d)"), L.T("Registered (30d)")]);

            var chart3 = new ECharts();
            chart3.SetX(list2, "Date", e => e.Date.ToString("MM-dd"));
            chart3.AddCandlestick(L.T("User candlestick (demo only)"),
                list2.Select(e => new CandlestickItem(e.Actives, e.NewsT7, e.News, e.ActivesT7)));

            ViewBag.Charts2 = new[] { chart2, chart3 };
        }

        return list;
    }
}