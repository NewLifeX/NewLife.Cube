using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NewLife.Web;
using XCode;
using XCode.Membership;
using XCode.Model;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>审计日志控制器</summary>
    [DataPermission(null, "CreateUserID={#userId}")]
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
        protected override IEnumerable<XLog> Search(Pager p)
        {
            var key = p["Q"];
            var userid = p["userid"].ToInt(-1);
            var category = p["category"];
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();

            // 附近日志
            if (key.IsNullOrEmpty() && userid < 0 && category.IsNullOrEmpty() && start.Year < 2000 && end.Year < 2000)
            {
                var id = p["id"].ToInt();
                var act = p["act"];
                if (act == "near" && id > 0)
                {
                    var range = p["range"].ToInt();
                    if (range <= 0) range = 10;

                    var exp = XLog._.ID >= id - range & XLog._.ID < id + range;
                    return XLog.FindAll(exp, p);
                }
            }

            return XLog.Search(key, userid, category, start, end, p);
        }
    }
}