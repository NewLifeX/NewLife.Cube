using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>模型列</summary>
    [Area("Admin")]
    public class ModelColumnController : EntityController<ModelColumn>
    {
        static ModelColumnController()
        {
            MenuOrder = 56;

            ListFields.RemoveField("TableId");
        }


        protected override IEnumerable<ModelColumn> Search(Pager p)
        {
            var tableId = p["tableId"].ToInt(-1);
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();
            var key = p["Q"];

            if (p.Sort.IsNullOrEmpty()) p.Sort = ModelColumn._.Sort.Asc();

            return ModelColumn.Search(tableId, null, start, end, key, p);
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