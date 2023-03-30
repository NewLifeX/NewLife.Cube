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
[Menu(50, true, Icon = "fa-area-chart")]
public class AreaController : EntityController<Area>
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

    private static Boolean _inited;

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<Area> Search(Pager p)
    {
        if (!_inited)
        {
            _inited = true;

            // 异步初始化数据
            //if (Area.Meta.Count == 0) ThreadPoolX.QueueUserWorkItem(() => Area.FetchAndSave());
            // 必须同步初始化，否则无法取得当前登录用户信息
            //if (Area.Meta.Count == 0) Area.FetchAndSave();
            if (Area.Meta.Count == 0) Import("http://x.newlifex.com/Area.csv.gz", true);
        }

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
}