using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Charts;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode;
using XCode.Membership;
using static NewLife.Cube.Entity.UserStat;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>访问统计控制器</summary>
    [Area("Admin")]
    public class UserStatController : ReadOnlyEntityController<UserStat>
    {
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
                var chart = new ECharts
                {
                    Height = 400,
                };
                chart.SetX(list, _.Date, e => e.Date.ToString("MMdd"));
                //chart.SetY("数值");
                chart.YAxis = new[] {
                    new { name = "数值", type = "value" },
                    new { name = "总数", type = "value" }
                };

                var line = chart.AddLine(list, _.Total, null, true);
                line["yAxisIndex"] = 1;

                chart.Add(list, _.MaxOnline);
                chart.Add(list, _.Actives);
                chart.Add(list, _.ActivesT7);
                chart.Add(list, _.ActivesT30);
                chart.Add(list, _.News);
                chart.Add(list, _.NewsT7);
                chart.Add(list, _.NewsT30);
                chart.Add(list, _.OnlineTime);
                chart.SetTooltip();

                var chart2 = new ECharts();
                chart2.AddPie(list, _.Total, e => new NameValue(e.Date.ToString("yyyy-MM-dd"), e.Total));
                chart2.AddPie(list, _.MaxOnline, e => new NameValue(e.Date.ToString("yyyy-MM-dd"), e.Total));

                ViewBag.Charts = new[] { chart };
                ViewBag.Charts2 = new[] { chart2 };
            }

            return list;
        }

        ///// <summary>菜单不可见</summary>
        ///// <param name="menu"></param>
        ///// <returns></returns>
        //protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        //{
        //    if (menu.Visible)
        //    {
        //        menu.Visible = false;
        //        (menu as IEntity).Update();
        //    }

        //    return base.ScanActionMenu(menu);
        //}
    }
}