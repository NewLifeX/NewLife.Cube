using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Web;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>审计日志控制器</summary>
    [DisplayName("审计日志")]
    [Description("系统内重要操作均记录日志，便于审计。任何人都不能删除、修改或伪造操作日志。")]
    [Area("Admin")]
    public class LogController : ReadOnlyEntityController<XLog>
    {
        static LogController()
        {
            MenuOrder = 70;

            // 日志列表需要显示详细信息，不需要显示用户编号
            ListFields.AddField("Action", "Remark");
            ListFields.RemoveField("CreateUserID");
            FormFields.RemoveField("Remark");
        }

        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<XLog> Search(Pager p) => XLog.Search(p["Q"], p["userid"].ToInt(-1), p["category"], p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p);
    }
}