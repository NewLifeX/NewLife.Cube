using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Charts;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;
using static NewLife.Cube.Entity.UserStat;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>访问统计控制器</summary>
    [Area("Admin")]
    [Menu(0, false)]
    public class UserStatController : ReadOnlyEntityController<UserStat>
    {
        static UserStatController()
        {
            ListFields.RemoveField("ID", "CreateTime", "UpdateTime", "Remark");
        }

        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<UserStat> Search(Pager p)
        {
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();

            p.RetrieveState = true;

            var list = UserStat.SearchByDate(start, end, p["Q"], p);

            if (list.Count > 0)
            {
                var list2 = list.OrderBy(e => e.Date).ToList();
                var chart = new ECharts
                {
                    Height = 400,
                };
                chart.SetX(list2, _.Date);
                //chart.SetY("数值");
                chart.YAxis = new[] {
                    new { name = "数值", type = "value" },
                    new { name = "总数", type = "value" }
                };

                var line = chart.AddLine(list2, _.Total, null, true);
                line["yAxisIndex"] = 1;

                chart.Add(list2, _.Logins);
                chart.Add(list2, _.OAuths);
                chart.Add(list2, _.MaxOnline);
                chart.Add(list2, _.Actives);
                chart.Add(list2, _.ActivesT7);
                chart.Add(list2, _.ActivesT30);
                chart.Add(list2, _.News);
                chart.Add(list2, _.NewsT7);
                chart.Add(list2, _.NewsT30);
                //chart.Add(list2, _.OnlineTime);
                chart.SetTooltip();

                //var chart2 = new ECharts();
                //chart2.AddPie(list, _.Total, e => new NameValue(e.Date.ToString("yyyy-MM-dd"), e.Total));
                //chart2.AddPie(list, _.MaxOnline, e => new NameValue(e.Date.ToString("yyyy-MM-dd"), e.Total));

                ViewBag.Charts = new[] { chart };
                //ViewBag.Charts2 = new[] { chart2 };
            }

            return list;
        }
    }
}