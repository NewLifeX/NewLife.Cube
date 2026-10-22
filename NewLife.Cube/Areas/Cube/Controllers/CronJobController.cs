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
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected override Boolean Valid(CronJob entity, DataObjectMethodType type, Boolean post)
    {
        if (post)
        {
            var next = DateTime.MinValue;
            foreach (var item in entity.Cron.Split(";"))
            {
                var cron = new Cron();
                if (!cron.Parse(item)) throw new ArgumentException(L.T("Invalid cron expression."), nameof(entity.Cron));

                var dt = cron.GetNext(DateTime.Now);
                if (next == DateTime.MinValue || dt < next) next = dt;
            }

            // 重算下一次的时间
            if (entity is IEntity e && !e.Dirtys[nameof(entity.Name)]) entity.NextTime = next;

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

        return Json(0, L.T("Execution scheduled."));
    }
}