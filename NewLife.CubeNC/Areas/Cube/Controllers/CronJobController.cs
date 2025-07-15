using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using NewLife.Threading;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>定时任务</summary>
[CubeArea]
[Menu(35, true, Icon = "fa-clock-o")]
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
            df.DisplayName = "日志";
            df.Url = "/Admin/Log?category=定时作业&linkId={Id}";
            df.Target = "_blank";
        }
        {
            var df = ListFields.AddListField("JobLog", null, "Enable");
            //df.Header = "作业日志";
            df.DisplayName = "作业日志";
            df.Url = "/Admin/Log?category=JobService&linkId={Id}";
            df.Target = "_blank";
        }
        {
            var df = ListFields.AddListField("Execute", null, "NextTime");
            //df.Header = "马上执行";
            df.DisplayName = "马上执行";
            df.Url = "/Cube/CronJob/ExecuteNow?id={Id}";
            df.DataAction = "action";
        }

        // 扩展Argument字段
        {
            var ef = EditFormFields.GetField("Argument") as FormField;
            //ef.GetExpand = e => (e as CronJob)?.GetArgument();
            //ef.RetainExpand = false;
            ef.Expand = new ExpandField
            {
                Decode = e => (e as CronJob)?.GetArgument(),
                Retain = false,
            };
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
                if (!cron.Parse(item)) throw new ArgumentException("Cron表达式有误！", nameof(entity.Cron));

                var dt = cron.GetNext(DateTime.Now);
                if (next == DateTime.MinValue || dt < next) next = dt;
            }

            // 重算下一次的时间
            if (entity is IEntity e && !e.Dirtys[nameof(entity.Name)]) entity.NextTime = next;

            JobService.Wake();
        }

        return base.Valid(entity, type, post);
    }

    ///// <summary>获取字段信息。加上参数扩展字段</summary>
    ///// <param name="kind"></param>
    ///// <param name="model"></param>
    ///// <returns></returns>
    //protected override FieldCollection OnGetFields(ViewKinds kind, Object model)
    //{
    //    var fields = base.OnGetFields(kind, model);

    //    // 表单嵌入配置字段
    //    if (kind == ViewKinds.EditForm && model is CronJob entity)
    //    {
    //        // 获取参数对象，展开参数，作为表单字段
    //        var p = entity.GetArgument();
    //        if (p != null && p is not String)
    //            fields.Expand(entity, p);
    //    }

    //    return fields;
    //}

    ///// <summary>更新实体前，从表单读取扩展字段数据</summary>
    ///// <param name="entity"></param>
    ///// <returns></returns>
    //protected override Int32 OnUpdate(CronJob entity)
    //{
    //    // 获取参数对象，展开参数，从表单字段接收参数
    //    var p = entity.GetArgument();
    //    if (p != null && p is not String && !(entity as IEntity).Dirtys[nameof(entity.Argument)])
    //    {
    //        var form = Request.Form;
    //        var flag = FieldCollection.ReadForm(p, form);

    //        // 保存参数对象
    //        if (flag) entity.Argument = p.ToJson(true);
    //    }

    //    return base.OnUpdate(entity);
    //}

    /// <summary>马上执行</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    public ActionResult ExecuteNow(String id)
    {
        var entity = CronJob.FindById(id.ToInt());
        if (entity != null && entity.Enable)
        {
            entity.NextTime = DateTime.Now;
            entity.Update();

            JobService.Wake(entity.Id, -1);
        }

        return JsonRefresh($"已安排执行！");
    }
}