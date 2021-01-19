using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>应用日志</summary>
    [DisplayName("应用日志")]
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class AppLogController : ReadOnlyEntityController<AppLog>
    {
        static AppLogController()
        {
            ListFields.RemoveField("ID");
        }

        /// <summary>搜索</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<AppLog> Search(Pager p)
        {
            if (p.Sort.IsNullOrEmpty()) p.Sort = AppLog._.ID.Desc();

            return base.Search(p);
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