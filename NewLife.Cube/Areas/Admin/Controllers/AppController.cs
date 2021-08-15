﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>应用系统</summary>
    [DisplayName("应用系统")]
    [Area("Admin")]
    public class AppController : EntityController<App>
    {
        static AppController()
        {
            MenuOrder = 38;
            LogOnChange = true;

            ListFields.RemoveField("Secret", "Logo", "White", "Black", "Urls", "Remark");

            {
                var df = ListFields.AddListField("AppLog", "Enable");
                df.Header = "日志";
                df.DisplayName = "日志";
                df.Url = "AppLog?appId={ID}";
            }

            {
                var df = AddFormFields.AddDataField("RoleIds");
                df.DataSource = (entity, field) => Role.FindAllWithCache().ToDictionary(e => e.ID, e => e.Name);
            }

            {
                var df = EditFormFields.AddDataField("RoleIds");
                df.DataSource = (entity, field) => Role.FindAllWithCache().ToDictionary(e => e.ID, e => e.Name);
            }

            {
                var df = ListFields.AddListField("Log", "UpdateUserId");
                df.DisplayName = "修改日志";
                df.Header = "修改日志";
                df.Url = "/Admin/Log?category=应用系统&linkId={ID}";
            }
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