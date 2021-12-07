using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Web;
using XCode;
using XCode.Membership;
using static XCode.Membership.Log;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>审计日志控制器</summary>
    [DataPermission(null, "CreateUserID={#userId}")]
    [DisplayName("审计日志")]
    [Description("系统内重要操作均记录日志，便于审计。任何人都不能删除、修改或伪造操作日志。")]
    [Area("Admin")]
    [Menu(70, true, Icon = "fa-history")]
    public class LogController : ReadOnlyEntityController<XLog>
    {
        static LogController()
        {
            // 日志列表需要显示详细信息，不需要显示用户编号
            ListFields.AddField("Action", "Remark");
            ListFields.RemoveField("CreateUserID");
            //FormFields.RemoveField("Remark");

            //{
            //    var df = ListFields.GetField("TraceId") as ListField;
            //    df.DisplayName = "跟踪";
            //    df.Url = StarHelper.BuildUrl("{TraceId}");
            //    df.DataVisible = (e, f) => !(e as XLog).TraceId.IsNullOrEmpty();
            //}
        }

        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<XLog> Search(Pager p)
        {
            var category = p["category"];
            var action = p["act"];
            var success = p["success"]?.ToBoolean();
            var linkid = p["linkid"].ToInt(-1);
            var userid = p["userid"].ToInt(-1);
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();
            var key = p["Q"];

            // 默认排序
            if (p.Sort.IsNullOrEmpty()) p.OrderBy = _.ID.Desc();

            // 附近日志
            if (key.IsNullOrEmpty() && userid < 0 && category.IsNullOrEmpty() && start.Year < 2000 && end.Year < 2000)
            {
                var id = p["id"].ToLong();
                var act = p["act"];
                if (act == "near" && id > 0)
                {
                    var range = p["range"].ToInt();
                    if (range <= 0) range = 10;

                    // 雪花Id，抽取时间
                    var snow = XLog.Meta.Factory.Snow;
                    if (snow.TryParse(id, out var time, out var _, out var _))
                    {
                        start = time.AddSeconds(-range);
                        end = time.AddSeconds(range);

                        return XLog.FindAll(_.ID.Between(start, end, snow), p);
                    }
                }
            }

            return XLog.Search(category, action, linkid, success, userid, start, end, key, p);
        }
    }
}