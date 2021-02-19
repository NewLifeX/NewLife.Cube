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
    /// <summary>模型表</summary>
    [Area("Admin")]
    public class ModelTableController : EntityController<ModelTable>
    {
        static ModelTableController()
        {
            MenuOrder = 55;

            ListFields.RemoveField("Controller", "TableName", "ConnName");

            {
                var df = ListFields.AddDataField("Columns", "Enable");
                df.Header = "列集合";
                df.DisplayName = "列集合";
                df.Url = "ModelColumn?tableId={Id}";
            }
        }

        protected override IEnumerable<ModelTable> Search(Pager p)
        {
            var category = p["category"];
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();
            var key = p["Q"];

            return ModelTable.Search(category, null, start, end, key, p);
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