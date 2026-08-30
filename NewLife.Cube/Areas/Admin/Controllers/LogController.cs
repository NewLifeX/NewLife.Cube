using System.ComponentModel;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using XCode;
using XCode.Membership;
using static XCode.Membership.Log;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>审计日志控制器</summary>
[DataPermission(null, "CreateUserID={#userId}")]
[DisplayName("审计日志")]
[Description("系统内重要操作均记录日志，便于审计。任何人都不能删除、修改或伪造操作日志。")]
[AdminArea]
[Menu(70, true, Icon = "Timer")]
public class LogController : ReadOnlyEntityController<XLog>
{
    static LogController()
    {
        // 日志列表需要显示详细信息，不需要显示用户编号
        ListFields.AddDataField("Remark", null, "Action");
        ListFields.RemoveField("Id");
        //FormFields.RemoveField("Remark");

        // 精简列表：去掉扩展字段、性能追踪与冗余的用户相关字段，只保留审计核心信息
        ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6", "CreateUserID", "CreateUser", "CreateUserName", "Success");

        // 搜索字段显式配置，受后台控制（默认只显示有索引的列）
        SearchFields.RemoveField("LinkID");
        SearchFields.AddField("CreateTime");
        // 用户编号用虚拟文本字段（标量参数 userid），真实 CreateUserID 字段会渲染成范围输入，不适合单值查询
        SearchFields.AddDataField("UserID", null, "Success").DisplayName = "用户编号";
        SearchFields.AddDataField("Q", "Category", null).DisplayName = "关键字";

        //{
        //    var df = ListFields.GetField("TraceId") as ListField;
        //    df.DisplayName = "跟踪";
        //    df.Url = StarHelper.BuildUrl("{TraceId}");
        //    df.DataVisible = (e, f) => !(e as XLog).TraceId.IsNullOrEmpty();
        //}
        {
            // 今天的时间不显示日期
            var df = ListFields.GetField("CreateTime") as ListField;
            df.GetValue = e => (e as XLog).CreateTime.ToFullString("").TrimPrefix(DateTime.Today.ToString("yyyy-MM-dd "));
        }
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<XLog> Search(Pager p)
    {
        var category = p["category"];
        // React 搜索区按字段名提交 action，MVC 老界面提交 act，两者都兼容
        var action = p["action"] ?? p["act"];
        var success = p["success"]?.ToBoolean();
        var linkid = p["linkid"].ToInt(-1);
        var userid = p["userid"].ToInt(-1);
        if (userid < 0) userid = p["createuserid"].ToInt(-1);
        // 时间范围：React 按字段名提交 CreateTime[0]/[1]，MVC 老界面提交 dtStart/dtEnd，两者都兼容
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();
        if (start.Year < 2000) start = p["CreateTime[0]"].ToDateTime();
        if (end.Year < 2000) end = p["CreateTime[1]"].ToDateTime();
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