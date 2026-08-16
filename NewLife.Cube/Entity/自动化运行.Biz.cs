using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Cube.Automation;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Threading;
using NewLife.Security;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Shards;

namespace NewLife.Cube.Entity;

public partial class AutomationRun : Entity<AutomationRun>
{
    #region 对象操作
    static AutomationRun()
    {
        // 拦截器 UserInterceptor、TimeInterceptor、IPInterceptor
        Meta.Interceptors.Add<TimeInterceptor>();

        // 实体缓存
        // var ec = Meta.Cache;
        // ec.Expire = 60;
    }

    /// <summary>验证并修补数据，返回验证结果，或者通过抛出异常的方式提示验证失败。</summary>
    /// <param name="method">添删改方法</param>
    public override Boolean Valid(DataMethod method)
    {
        // 如果没有脏数据，则不需要进行任何处理
        if (!HasDirty) return true;

        // 建议先调用基类方法，基类方法会做一些统一处理
        if (!base.Valid(method)) return false;

        return true;
    }
    #endregion

    #region 扩展属性
    #endregion

    #region 业务操作
    /// <summary>转为模型</summary>
    public AutomationRunModel ToModel()
    {
        var model = new AutomationRunModel();
        model.Copy(this);
        return model;
    }

    /// <summary>入队一次运行（落库；queued 状态由 Worker 消费）</summary>
    /// <param name="rule">自动化规则</param>
    /// <param name="recordKey">触发记录主键</param>
    /// <param name="triggerKind">触发种类</param>
    /// <param name="depth">深度</param>
    /// <param name="resumeAt">延时续跑时间</param>
    /// <param name="status">初始状态</param>
    /// <returns></returns>
    public static AutomationRun Enqueue(EntityAutomation rule, String recordKey, String triggerKind, Int32 depth = 0, DateTime resumeAt = default, String status = "queued")
    {
        var now = DateTime.Now;
        var run = new AutomationRun
        {
            TenantId = rule?.TenantId ?? 0,
            AutomationId = rule?.Id ?? 0,
            TypePath = AutomationPaths.NormalizeTypePath(rule?.TypePath),
            RecordKey = recordKey,
            TriggerKind = triggerKind,
            Status = status.IsNullOrEmpty() ? "queued" : status,
            Depth = depth,
            ResumeAt = resumeAt,
            CreateTime = now,
            UpdateTime = now,
        };
        run.Insert();
        return run;
    }

    /// <summary>按规则列出运行（新在前，供测试与调试）</summary>
    /// <param name="automationId">规则</param>
    /// <returns></returns>
    public static IList<AutomationRun> FindAllByAutomationId(Int64 automationId) =>
        automationId <= 0
            ? []
            : FindAll(_.AutomationId == automationId, _.Id.Desc(), null, 0, 500);

    /// <summary>到期 waiting（延时/延期续跑），按到期时间升序</summary>
    /// <param name="now">当前时间</param>
    /// <param name="max">最多返回</param>
    /// <returns></returns>
    public static IList<AutomationRun> FindDueWaiting(DateTime now, Int32 max = 50) =>
        FindAll(_.Status == "waiting" & _.ResumeAt <= now, _.ResumeAt.Asc() & _.Id.Asc(), null, 0, max <= 0 ? 50 : max);

    /// <summary>按实体路径+记录键列出运行（dateArrive once 查重）</summary>
    /// <param name="typePath">实体路径</param>
    /// <param name="recordKey">记录主键</param>
    /// <returns></returns>
    public static IList<AutomationRun> FindAllByTypePathAndRecordKey(String typePath, String recordKey)
    {
        typePath = AutomationPaths.NormalizeTypePath(typePath);
        if (typePath.IsNullOrEmpty() || recordKey.IsNullOrEmpty()) return [];
        return FindAll(_.TypePath == typePath & _.RecordKey == recordKey, _.Id.Desc(), null, 0, 200);
    }

    /// <summary>测试用：清理全部运行行（SQLite 共享缓存跨用例累积）</summary>
    public static void ResetForTests()
    {
        var list = FindAll(null, null, null, 0, 0);
        foreach (var e in list)
        {
            e.Delete();
        }
    }
    #endregion
}
