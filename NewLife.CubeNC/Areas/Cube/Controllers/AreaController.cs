using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Charts;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using XCode;
using XCode.Membership;
using static XCode.Membership.Area;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>地区</summary>
[DisplayName("地区")]
[CubeArea]
[Menu(50, true, Icon = "fa-area-chart")]
public class AreaController : EntityController<Area, AreaModel>
{
    static AreaController()
    {
        LogOnChange = true;

        ListFields.RemoveCreateField();
        ListFields.RemoveRemarkField();

        {
            var df = ListFields.GetField("ParentID") as ListField;
            df.DisplayName = "{ParentPath}";
            df.Url = "/Cube/Area?Id={ParentID}";
        }
        {
            var df = ListFields.AddDataField("sub", "Level") as ListField;
            df.DisplayName = "下级";
            df.Url = "/Cube/Area?parentId={ID}";
        }

        //AddFormFields.AddField("ID");
    }

    private static Int32 _inited;
    /// <summary>初始化地区数据</summary>
    public static void InitAreaData()
    {
        if (_inited == 0 && Interlocked.CompareExchange(ref _inited, 1, 0) == 0)
        {
            // 异步初始化数据
            //if (Area.Meta.Count == 0) ThreadPoolX.QueueUserWorkItem(() => Area.FetchAndSave());
            // 必须同步初始化，否则无法取得当前登录用户信息
            //if (Area.Meta.Count == 0) Area.FetchAndSave();
            if (Area.Meta.Count == 0)
            {
                Task.Factory.StartNew(() =>
                {
                    // 先加载民政部数据，然后导入旧版数据
                    FetchAndSave(null);
                    Import("http://x.newlifex.com/Area.csv.gz", true, 4, true);
                }, TaskCreationOptions.LongRunning);
            }
        }
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<Area> Search(Pager p)
    {
        InitAreaData();

        var id = p["id"].ToInt(-1);
        if (id < 0) id = p["q"].ToInt(-1);
        if (id > 0)
        {
            var ss = new List<Area>();
            var entity = FindByID(id);
            if (entity != null) ss.Add(entity);
            return ss;
        }

        Boolean? enable = null;
        if (!p["enable"].IsNullOrEmpty()) enable = p["enable"].ToBoolean();

        var idstart = p["idStart"].ToInt(-1);
        var idend = p["idEnd"].ToInt(-1);

        var parentid = p["parentid"].ToInt(-1);
        if (parentid < 0)
        {
            var areaId = p["AreaID"];
            parentid = ("-1/" + areaId).SplitAsInt("/").LastOrDefault();
        }

        var level = p["Level"].ToInt(-1);
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        // 地区默认升序
        if (p.Sort.IsNullOrEmpty()) p.OrderBy = _.ID.Asc();

        var list = Area.Search(parentid, level, idstart, idend, enable, p["q"], start, end, p);

        if (list.Count > 0)
        {
            var exp = new WhereExpression();
            exp &= _.ID <= 999999;
            var list2 = Area.FindAll(exp.GroupBy(_.Kind), null, _.ID.Count() & _.Kind, 0, 0);
            list2 = list2.OrderByDescending(e => e.ID).ToList();
            if (list2.Count >= 0)
            {
                AddChart(list2, _.Kind, "个数", [_.ID], SeriesTypes.Bar);

                //var chart = new ECharts
                //{
                //    Height = 400,
                //};
                //chart.SetX(list2, _.Kind, e => e.Kind ?? "未知");
                //chart.SetY("个数", "value");
                //chart.SetTooltip();

                //var bar = chart.AddBar(list2, _.Kind, e => e.ID);

                //ViewBag.Charts = new[] { chart };
            }
            if (list2.Count >= 0)
            {
                var chart = AddChart(list2, _.Kind, null, [_.ID], SeriesTypes.Pie, "bottom");
                chart.XAxis = null;
                var pie = chart.Series[0];

                //var chart = new ECharts
                //{
                //    Height = 400,
                //};
                ////chart.SetX(list2, _.Kind);
                ////chart.SetY(null, "value");
                //chart.Legend = new { show = "false", top = "5%", left = "center" };

                //var pie = chart.AddPie(list2, _.Kind, e => new NameValue(e.Kind ?? "未知", e.ID));
                pie["radius"] = new[] { "40%", "70%" };
                pie["avoidLabelOverlap"] = false;
                pie["itemStyle"] = new { borderRadius = 10, borderColor = "#fff", borderWidth = 2 };
                pie["label"] = new { show = false, position = "center" };
                pie["emphasis"] = new { label = new { show = true, fontSize = 40, fontWeight = "bold" } };
                pie["labelLine"] = new { show = false };

                //chart.SetTooltip("item", null, null);
                //ViewBag.Charts2 = new[] { chart };
            }
        }

        return list;
    }

    /// <summary>
    /// 中国地图
    /// </summary>
    /// <returns></returns>
    public ActionResult Map()
    {
        PageSetting.EnableNavbar = false;

        return View("Map");
    }
}