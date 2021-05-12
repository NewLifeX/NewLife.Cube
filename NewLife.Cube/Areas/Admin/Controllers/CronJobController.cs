using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>定时任务</summary>
    [Area("Admin")]
    public class CronJobController : EntityController<CronJob>
    {
        static CronJobController()
        {
            MenuOrder = 35;

            ListFields.RemoveCreateField();

            {
                var df = ListFields.AddDataField("Log", null, "Enable");
                df.Header = "日志";
                df.DisplayName = "日志";
                df.Url = "Log?category=CronJob&linkId={Id}";
            }
            {
                var df = ListFields.AddDataField("JobLog", null, "Enable");
                df.Header = "作业日志";
                df.DisplayName = "作业日志";
                df.Url = "Log?category=JobService&linkId={Id}";
            }
        }

        /// <summary>修改数据时，唤醒作业服务跟进</summary>
        /// <param name="entity"></param>
        /// <param name="type"></param>
        /// <param name="post"></param>
        /// <returns></returns>
        protected override Boolean Valid(CronJob entity, DataObjectMethodType type, Boolean post)
        {
            if (post) JobService.Wake();

            return base.Valid(entity, type, post);
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