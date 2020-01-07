using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Web;
using XCode;
using XCode.Membership;
using XCode.Statistics;
#if __CORE__
using NewLife.Cube.Charts;
using static XCode.Membership.VisitStat;
#endif

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>访问统计控制器</summary>
    [DisplayName("访问统计")]
    [Description("每个页面每天的访问统计信息")]
    [Area("Admin")]
    public class VisitStatController : ReadOnlyEntityController<VisitStat>
    {
        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<VisitStat> Search(Pager p)
        {
            var model = new VisitStatModel();
            model.Fill(p.Params, StatLevels.Day);
            model.Page = p["p"];

            p.RetrieveState = true;

            var list = VisitStat.Search(model, p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p);

#if __CORE__
            if (list.Count > 0)
            {
                var chart = new ECharts
                {
                    //Title = new ChartTitle { Text = "每个页面每天的访问统计信息" },
                    Legend = new[] { "次数", "用户", "IP", "错误" },
                    //XAxis = list.Select(e => e.Page).ToArray(),
                    XAxis = list.Select(e => e.Time.ToString("yy-MM-dd")).ToArray(),
                    //YAxis = new[] { _.Times.DisplayName, _.Users.DisplayName, _.IPs.DisplayName, _.Error.DisplayName },
                    YAxis = new { type = "value" },

                    Tooltip = new
                    {
                        trigger = "axis",
                        axisPointer = new
                        {
                            type = "cross",
                            label = new
                            {
                                backgroundColor = "#6a7985"
                            }
                        },
                    },
                };

                chart.Add(new Series
                {
                    Name = "次数",
                    Type = "line",
                    Data = list.Select(e => e.Times).ToArray(),
                    Smooth = true,
                });

                chart.Add(new Series
                {
                    Name = "用户",
                    Type = "line",
                    Data = list.Select(e => e.Users).ToArray(),
                });

                chart.Add(new Series
                {
                    Name = "IP",
                    Type = "line",
                    Data = list.Select(e => e.IPs).ToArray(),
                });

                chart.Add(new Series
                {
                    Name = "错误",
                    Type = "line",
                    Data = list.Select(e => e.Error).ToArray(),
                });

                ViewBag.Charts = new[] { chart };
            }
#endif

            return list;
        }

        /// <summary>菜单不可见</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            if (menu.Visible)
            {
                menu.Visible = false;
                (menu as IEntity).Update();
            }

            return base.ScanActionMenu(menu);
        }
    }
}