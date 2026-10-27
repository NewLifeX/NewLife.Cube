using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Threading;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>定时任务</summary>
[CubeArea]
[Menu(35, true, Icon = "Clock")]
public class CronJobController : EntityController<CronJob, CronJobModel>
{
    static CronJobController()
    {
        LogOnChange = true;

        ListFields.RemoveField("Method", "Remark");
        ListFields.RemoveCreateField();

        {
            var df = ListFields.AddListField("Log", null, "Enable");
            //df.Header = "日志";
            df.DisplayName = "Log";
            df.Url = "/Admin/Log?category=CronJob&linkId={Id}";
        }
        {
            var df = ListFields.AddListField("JobLog", null, "Enable");
            //df.Header = "作业日志";
            df.DisplayName = "Job log";
            df.Url = "/Admin/Log?category=JobService&linkId={Id}";
        }
        {
            var df = ListFields.AddListField("Execute", null, "NextTime");
            //df.Header = "马上执行";
            df.DisplayName = "Run now";
            df.Url = "/Cube/CronJob/ExecuteNow?id={Id}";
            df.DataAction = "action";
        }
    }

    /// <summary>修改数据时，唤醒作业服务跟进</summary>
    /// <param name="entity">待校验的作业实体</param>
    /// <param name="type">数据操作类型（添加/修改/删除）</param>
    /// <param name="post">是否为提交后校验</param>
    /// <returns>校验通过返回 true，否则抛出异常或返回 false</returns>
    protected override Boolean Valid(CronJob entity, DataObjectMethodType type, Boolean post)
    {
        if (post)
        {
            var next = GetNextRunTime(entity.Cron, DateTime.Now);

            // 重算下一次的时间
            if (entity is IEntity e && !e.Dirtys[nameof(entity.Name)]) entity.NextTime = next;

            JobService.Wake();
        }

        return base.Valid(entity, type, post);
    }

    /// <summary>解析分号分隔的 Cron 表达式，返回最近一次执行时间</summary>
    /// <param name="cron">一个或多个以分号分隔的 Cron 表达式（6 字段：秒 分 时 日 月 周）</param>
    /// <param name="now">基准时间</param>
    /// <returns>最近一次执行时间；无有效表达式时返回 <see cref="DateTime.MinValue"/></returns>
    /// <exception cref="ArgumentException">存在无法解析的表达式</exception>
    internal static DateTime GetNextRunTime(String cron, DateTime now)
    {
        var next = DateTime.MinValue;
        foreach (var item in cron.Split(";"))
        {
            var c = new Cron();
            if (!c.Parse(item)) throw new ArgumentException(L.T("Invalid cron expression."), nameof(cron));

            var dt = c.GetNext(now);
            if (next == DateTime.MinValue || dt < next) next = dt;
        }

        return next;
    }

    /// <summary>马上执行</summary>
    /// <param name="id">作业编号</param>
    /// <returns>执行结果 JSON</returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [HttpPost]
    public ActionResult ExecuteNow(String id)
    {
        var entity = CronJob.FindById(id.ToInt());
        if (entity != null && entity.Enable)
        {
            entity.NextTime = DateTime.Now;
            entity.Update();

            JobService.Wake(entity.Id, -1);
        }

        return Json(0, L.T("Execution scheduled."));
    }
}