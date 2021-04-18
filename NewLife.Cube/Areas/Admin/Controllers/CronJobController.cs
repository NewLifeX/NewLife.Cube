using System;
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
    }
}