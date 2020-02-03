using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>地区</summary>
    [DisplayName("地区")]
    [Area("Admin")]
    public class AreaController : EntityController<Area>
    {
        static AreaController()
        {
            MenuOrder = 50;

            ListFields.RemoveCreateField();
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
                if (Area.Meta.Count == 0) Area.FetchAndSave();
            }

            var id = p["id"].ToInt(-1);
            if (id < 0) id = p["q"].ToInt(-1);
            if (id > 0)
            {
                var list = new List<Area>();
                var entity = Area.FindByID(id);
                if (entity != null) list.Add(entity);
                return list;
            }

            Boolean? enable = null;
            if (!p["enable"].IsNullOrEmpty()) enable = p["enable"].ToBoolean();

            var parentid = p["parentid"].ToInt(-1);
            var idstart = p["idStart"].ToInt(-1);
            var idend = p["idEnd"].ToInt(-1);

            var prov = p["ProvinceID"].ToInt(-1);
            var city = p["CityID"].ToInt(-1);
            var dist = p["DistrictID"].ToInt(-1);

            var level = p["Level"].ToInt(-1);
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();

            return Area.Search(parentid, level, idstart, idend, enable, p["q"], start, end, p);
        }

        /// <summary>菜单不可见</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            if (menu.Visible && !menu.Necessary)
            {
                menu.Visible = false;
                (menu as IEntity).Update();
            }

            return base.ScanActionMenu(menu);
        }
    }
}