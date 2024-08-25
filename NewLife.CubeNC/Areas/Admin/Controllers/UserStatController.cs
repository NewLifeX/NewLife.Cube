using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Charts;
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
            chart.SetY(["用户数", "总数", "时长"], "value", [null, null, "{value}秒"]);

            var line = chart.AddLine(list2, _.Total, null, true);
            line.YAxisIndex = 1;

            var line3 = chart.AddLine(list2, _.OnlineTime, null, true);
            line3.YAxisIndex = 2;

            //var chart = new ECharts
            //{
            //    Height = 400,
            //};
            //chart.SetX(list2, _.Date);
            ////chart.SetY("数值");
            //chart.YAxis = new[] {
            //    new { name = "数值", type = "value" },
            //    new { name = "总数", type = "value" }
            //};
            //chart.AddDataZoom();

            //var line = chart.AddLine(list2, _.Total, null, true);
            //line["yAxisIndex"] = 1;

            //chart.Add(list2, _.Logins);
            //chart.Add(list2, _.OAuths);
            //chart.Add(list2, _.MaxOnline);
            //chart.Add(list2, _.Actives);
            //chart.Add(list2, _.ActivesT7);
            //chart.Add(list2, _.ActivesT30);
            //chart.Add(list2, _.News);
            //chart.Add(list2, _.NewsT7);
            //chart.Add(list2, _.NewsT30);
            ////chart.Add(list2, _.OnlineTime);
            //chart.SetTooltip();

            //ViewBag.Charts = new[] { chart };
        }

        return list;
    }
}