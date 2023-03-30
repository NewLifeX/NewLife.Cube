using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Threading;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>定时任务</summary>
[Area("Cube")]
[Menu(35, true, Icon = "fa-clock-o")]
public class CronJobController : EntityController<CronJob>
{
    static CronJobController()
    {
        LogOnChange = true;

        ListFields.RemoveField("Method", "Remark");
        ListFields.RemoveCreateField();

        {
            var df = ListFields.AddListField("Log", null, "Enable");
            //df.Header = "日志";
            df.DisplayName = "日志";
            df.Url = "/Admin/Log?category=定时作业&linkId={Id}";
        }
        {
            var df = ListFields.AddListField("JobLog", null, "Enable");
            //df.Header = "作业日志";
            df.DisplayName = "作业日志";
            df.Url = "/Admin/Log?category=JobService&linkId={Id}";
        }
        {
            var df = ListFields.AddListField("Execute", null, "NextTime");
            //df.Header = "马上执行";
            df.DisplayName = "马上执行";
            df.Url = "/Cube/CronJob/ExecuteNow?id={Id}";
            df.DataAction = "action";
        }
    }

    /// <summary>修改数据时，唤醒作业服务跟进</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected override Boolean Valid(CronJob entity, DataObjectMethodType type, Boolean post)
    {
        if (post)
        {
            var cron = new Cron();
            if (!cron.Parse(entity.Cron)) throw new ArgumentException("Cron表达式有误！", nameof(entity.Cron));

            // 重算下一次的时间
            if (entity is IEntity e && !e.Dirtys[nameof(entity.Name)]) entity.NextTime = cron.GetNext(DateTime.Now);

            JobService.Wake();
        }

        return base.Valid(entity, type, post);
    }

    /// <summary>马上执行</summary>
    /// <param name="id"></param>
    /// <returns></returns>
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

        return Json(0, "已安排执行！");
    }
}