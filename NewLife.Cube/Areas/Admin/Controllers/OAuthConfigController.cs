using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>OAuth配置</summary>
    [Area("Admin")]
    public class OAuthConfigController : EntityController<OAuthConfig>
    {
        static OAuthConfigController()
        {
            MenuOrder = 18;

            ListFields.RemoveField("Secret", "Logo", "Remark");
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