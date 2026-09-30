using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;
using NewLife.Remoting;
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

    /// <summary>按 ID 批量取地区名称。空数组返回空字典；去重后超过 200 返回 400。</summary>
    /// <param name="model">ids 为地区主键字符串</param>
    /// <returns>键为 ID 十进制字符串、值为名称</returns>
    [HttpPost("/api/[area]/[controller]/Names")]
    [EntityAuthorize(PermissionFlags.Detail)]
    public ActionResult Names([FromBody] AreaNamesModel? model)
    {
        var ids = NormalizeNameIds(model?.Ids);
        return Json(0, null, ResolveNames(ids));
    }

    /// <summary>去空白、忽略大小写去重。超过 200 个抛出 400，不截断。不能解析为整数的元素留给 <see cref="ResolveNames"/> 忽略。</summary>
    /// <param name="ids">原始 ID 列表，null 当空</param>
    /// <returns>去重后的 ID 字符串</returns>
    public static IList<String> NormalizeNameIds(IList<String>? ids)
    {
        var list = new List<String>();
        var seen = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        if (ids != null)
        {
            foreach (var raw in ids)
            {
                var s = raw?.Trim();
                if (s.IsNullOrEmpty()) continue;
                if (!seen.Add(s)) continue;
                list.Add(s);
            }
        }
        if (list.Count > 200) throw new ApiException(400, "一次最多查询 200 个地区");
        return list;
    }

    /// <summary>先整表缓存，未命中再按主键回表。只收录非空 Name，未知 ID 不出现。</summary>
    /// <param name="ids">已规范化的 ID 字符串</param>
    /// <returns>ID 到名称</returns>
    public static IDictionary<String, String> ResolveNames(IList<String>? ids)
    {
        var result = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        if (ids == null || ids.Count == 0) return result;

        var pending = new HashSet<Int32>();
        foreach (var s in ids)
        {
            if (Int32.TryParse(s, out var id) && id > 0) pending.Add(id);
        }
        if (pending.Count == 0) return result;

        foreach (var area in Area.FindAllWithCache())
        {
            if (!pending.Contains(area.ID)) continue;
            if (!area.Name.IsNullOrEmpty()) result[area.ID.ToString()] = area.Name;
            pending.Remove(area.ID);
            if (pending.Count == 0) break;
        }

        if (pending.Count > 0)
        {
            var extra = Area.FindAll(_.ID.In(pending.ToArray()), null, "ID,Name", 0, 0);
            foreach (var area in extra)
            {
                if (!area.Name.IsNullOrEmpty()) result[area.ID.ToString()] = area.Name;
            }
        }
        return result;
    }
}

/// <summary>地区名称批量查询体</summary>
public class AreaNamesModel
{
    /// <summary>地区 ID 字符串</summary>
    public List<String>? Ids { get; set; }
}