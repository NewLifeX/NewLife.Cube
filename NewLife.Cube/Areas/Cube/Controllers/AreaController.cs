using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using XCode;
using XCode.Membership;
using static XCode.Membership.Area;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>地区</summary>
[DisplayName("地区")]
[CubeArea]
[Menu(50, true, Icon = "DataLine")]
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

                    var url = NewLife.Setting.Current.PluginServer.TrimSuffix("/");
                    Import(url + "/Area.csv.gz", true, 4, true);
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

        return Area.Search(parentid, level, idstart, idend, enable, p["q"], start, end, p);
    }

    ///// <summary>
    ///// 中国地图
    ///// </summary>
    ///// <returns></returns>
    //public ActionResult Map()
    //{
    //    PageSetting.EnableNavbar = false;

    //    return View("Map");
    //}

    /// <summary>中国地图数据：省级 + 有经纬度城市散点，供 React 地图模式渲染（对齐 MVC Map.cshtml）</summary>
    /// <returns>省份与城市经纬度列表</returns>
    [HttpGet("/api/[area]/[controller]/Map")]
    [EntityAuthorize(PermissionFlags.Detail)]
    public ActionResult Map()
    {
        InitAreaData();

        // 缓存一次全量加载，避免逐省查询子级（对齐 MVC Root.Childs 语义）
        var all = Area.FindAllWithCache();

        // 省级（父级为根 0）且有经纬度
        var provinces = all.Where(e => e.ParentID == 0 && (e.Longitude != 0 || e.Latitude != 0)).ToList();
        var provIds = provinces.Select(e => e.ID).ToHashSet();

        // 城市（省直下）且有经纬度
        var cities = all.Where(e => provIds.Contains(e.ParentID) && e.Longitude > 0 && e.Latitude > 0).ToList();

        return Json(0, null, new
        {
            provinces = provinces.Select(e => new { e.Name, e.Longitude, e.Latitude, e.Kind }),
            cities = cities.Select(e => new { e.Name, e.Longitude, e.Latitude }),
        });
    }
}