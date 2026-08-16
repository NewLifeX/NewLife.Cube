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
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Threading;
using NewLife.Web;
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
        // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
        //var df = Meta.Factory.AdditionalFields;
        //df.Add(nameof(TenantId));

        // 拦截器 UserInterceptor、TimeInterceptor、IPInterceptor
        Meta.Interceptors.Add<TimeInterceptor>();
        Meta.Interceptors.Add<TenantInterceptor>();

        // 实体缓存
        // var ec = Meta.Cache;
        // ec.Expire = 60;
    }

    /// <summary>验证并修补数据，返回验证结果，或者通过抛出异常的方式提示验证失败。</summary>
    /// <param name="method">添删改方法</param>
    public override Boolean Valid(DataMethod method)
    {
        //if (method == DataMethod.Delete) return true;
        // 如果没有脏数据，则不需要进行任何处理
        if (!HasDirty) return true;

        // 建议先调用基类方法，基类方法会做一些统一处理
        if (!base.Valid(method)) return false;

        // 在新插入数据或者修改了指定字段时进行修正
        //if (method == DataMethod.Insert && !Dirtys[nameof(CreateTime)]) CreateTime = DateTime.Now;
        //if (!Dirtys[nameof(UpdateTime)]) UpdateTime = DateTime.Now;

        return true;
    }

    ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
    //[EditorBrowsable(EditorBrowsableState.Never)]
    //protected override void InitData()
    //{
    //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
    //    if (Meta.Session.Count > 0) return;

    //    if (XTrace.Debug) XTrace.WriteLine("开始初始化AutomationRun[自动化运行]数据……");

    //    var entity = new AutomationRun();
    //    entity.Id = 0;
    //    entity.TenantId = 0;
    //    entity.AutomationId = 0;
    //    entity.TypePath = "abc";
    //    entity.RecordKey = "abc";
    //    entity.TriggerKind = "abc";
    //    entity.Status = "abc";
    //    entity.Depth = 0;
    //    entity.ResumeAt = DateTime.Now;
    //    entity.NodeTrace = "abc";
    //    entity.Error = "abc";
    //    entity.Insert();

    //    if (XTrace.Debug) XTrace.WriteLine("完成初始化AutomationRun[自动化运行]数据！");
    //}

    ///// <summary>已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert</summary>
    ///// <returns></returns>
    //public override Int32 Insert()
    //{
    //    return base.Insert();
    //}

    ///// <summary>已重载。在事务保护范围内处理业务，位于Valid之后</summary>
    ///// <returns></returns>
    //protected override Int32 OnDelete()
    //{
    //    return base.OnDelete();
    //}
    #endregion

    #region 扩展属性
    #endregion

    #region 高级查询

    // Select Count(Id) as Id,TypePath From AutomationRun Where ResumeAt>'2020-01-24 00:00:00' Group By TypePath Order By Id Desc limit 20
    static readonly FieldCache<AutomationRun> _TypePathCache = new(nameof(TypePath))
    {
        //Where = _.ResumeAt > DateTime.Today.AddDays(-30) & Expression.Empty
    };

    /// <summary>获取实体路径列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
    /// <returns></returns>
    public static IDictionary<String, String> GetTypePathList() => _TypePathCache.FindAllName();

    // Select Count(Id) as Id,Status From AutomationRun Where ResumeAt>'2020-01-24 00:00:00' Group By Status Order By Id Desc limit 20
    static readonly FieldCache<AutomationRun> _StatusCache = new(nameof(Status))
    {
        //Where = _.ResumeAt > DateTime.Today.AddDays(-30) & Expression.Empty
    };

    /// <summary>获取状态列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
    /// <returns></returns>
    public static IDictionary<String, String> GetStatusList() => _StatusCache.FindAllName();
    #endregion

    #region 业务操作
    /// <summary>转为模型</summary>
    public AutomationRunModel ToModel()
    {
        var model = new AutomationRunModel();
        model.Copy(this);
        return model;
    }

    /// <summary>入队一条运行（落 Log 库）。默认 Status=queued。</summary>
    public static AutomationRun Enqueue(EntityAutomation rule, String recordKey, String triggerKind, Int32 depth = 0, DateTime resumeAt = default, String status = "queued")
    {
        var now = DateTime.Now;
        var run = new AutomationRun
        {
            TenantId = rule?.TenantId ?? 0,
            AutomationId = rule?.Id ?? 0,
            TypePath = NewLife.Cube.Automation.AutomationPaths.NormalizeTypePath(rule?.TypePath),
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

    /// <summary>到期 waiting（ResumeAt≤now），按续跑时间升序</summary>
    public static IList<AutomationRun> FindDueWaiting(DateTime now, Int32 max = 50)
    {
        if (max <= 0) max = 50;
        return FindAll(_.Status == "waiting" & _.ResumeAt <= now, _.ResumeAt.Asc() & _.Id.Asc(), null, 0, max);
    }

    /// <summary>待消费的 queued（进程重启后由 Worker 捞回）</summary>
    public static IList<AutomationRun> FindQueued(Int32 max = 20)
    {
        if (max <= 0) max = 20;
        return FindAll(_.Status == "queued", _.Id.Asc(), null, 0, max);
    }

    /// <summary>同规则同记录在窗口内是否已有 queued/running（防抖）</summary>
    public static Boolean HasRecentActive(Int64 automationId, String recordKey, Int32 withinMs)
    {
        if (automationId <= 0 || withinMs <= 0) return false;
        var since = DateTime.Now.AddMilliseconds(-withinMs);
        var exp = _.AutomationId == automationId & _.RecordKey == (recordKey ?? "")
            & (_.Status == "queued" | _.Status == "running")
            & _.CreateTime >= since;
        return FindCount(exp) > 0;
    }

    /// <summary>列表查询（Runs API）</summary>
    public static IList<AutomationRun> SearchRuns(String typePath, Int64 automationId, String recordKey, PageParameter page)
    {
        typePath = NewLife.Cube.Automation.AutomationPaths.NormalizeTypePath(typePath);
        var exp = new WhereExpression();
        if (!typePath.IsNullOrEmpty()) exp &= _.TypePath == typePath;
        if (automationId > 0) exp &= _.AutomationId == automationId;
        if (!recordKey.IsNullOrEmpty()) exp &= _.RecordKey == recordKey;
        if (page != null && page.Sort.IsNullOrEmpty())
        {
            page.Sort = nameof(Id);
            page.Desc = true;
        }
        return FindAll(exp, page);
    }

    /// <summary>测试清空（仅单测）</summary>
    public static void ResetForTests()
    {
        var list = FindAll(null, null, null, 0, 10000);
        foreach (var e in list) e.Delete();
    }

    #endregion
}
