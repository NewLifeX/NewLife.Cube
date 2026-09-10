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
using NewLife.Cube.Workflow;

namespace NewLife.Cube.Workflow.Entity;

public partial class WorkflowTask : Entity<WorkflowTask>
{
    #region 对象操作
    static WorkflowTask()
    {
        // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
        //var df = Meta.Factory.AdditionalFields;
        //df.Add(nameof(InstanceId));

        // 拦截器 UserInterceptor、TimeInterceptor、IPInterceptor
        Meta.Interceptors.Add(new UserInterceptor { AllowEmpty = true });
        Meta.Interceptors.Add<TimeInterceptor>();
        Meta.Interceptors.Add(new IPInterceptor { AllowEmpty = true });

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

        // 处理当前已登录用户信息，可以由UserInterceptor拦截器代劳
        /*var user = ManageProvider.User;
        if (user != null)
        {
            if (method == DataMethod.Insert && !Dirtys[nameof(CreateUserID)]) CreateUserID = user.ID;
            if (!Dirtys[nameof(UpdateUserID)]) UpdateUserID = user.ID;
        }*/
        //if (method == DataMethod.Insert && !Dirtys[nameof(CreateTime)]) CreateTime = DateTime.Now;
        //if (!Dirtys[nameof(UpdateTime)]) UpdateTime = DateTime.Now;
        //if (method == DataMethod.Insert && !Dirtys[nameof(CreateIP)]) CreateIP = ManageProvider.UserHost;
        //if (!Dirtys[nameof(UpdateIP)]) UpdateIP = ManageProvider.UserHost;

        return true;
    }

    ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
    //[EditorBrowsable(EditorBrowsableState.Never)]
    //protected override void InitData()
    //{
    //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
    //    if (Meta.Session.Count > 0) return;

    //    if (XTrace.Debug) XTrace.WriteLine("开始初始化WorkflowTask[流程任务]数据……");

    //    var entity = new WorkflowTask();
    //    entity.Id = 0;
    //    entity.InstanceId = 0;
    //    entity.NodeId = "abc";
    //    entity.Mode = "abc";
    //    entity.AssigneeId = 0;
    //    entity.CandidateJson = "abc";
    //    entity.SequenceIndex = 0;
    //    entity.Visible = true;
    //    entity.Status = "abc";
    //    entity.DueTime = DateTime.Now;
    //    entity.TimeoutAction = "abc";
    //    entity.TimeoutTransferTo = "abc";
    //    entity.ClaimTime = DateTime.Now;
    //    entity.FinishTime = DateTime.Now;
    //    entity.Insert();

    //    if (XTrace.Debug) XTrace.WriteLine("完成初始化WorkflowTask[流程任务]数据！");
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

    // Select Count(Id) as Id,Category From WorkflowTask Where CreateTime>'2020-01-24 00:00:00' Group By Category Order By Id Desc limit 20
    //static readonly FieldCache<WorkflowTask> _CategoryCache = new(nameof(Category))
    //{
    //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
    //};

    ///// <summary>获取类别列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
    ///// <returns></returns>
    //public static IDictionary<String, String> GetCategoryList() => _CategoryCache.FindAllName();
    #endregion

    #region 业务操作
    /// <summary>转为模型对象</summary>
    /// <returns>模型</returns>
    public WorkflowTaskModel ToModel()
    {
        var model = new WorkflowTaskModel();
        model.Copy(this);

        return model;
    }

    /// <summary>按用户查找待办任务（可见且 Active/Pending：已认领本人，或或签未认领且候选含本人）</summary>
    /// <param name="userId">用户编号</param>
    /// <returns>待办列表</returns>
    /// <remarks>
    /// 或签多人共用一条任务（AssigneeId=0，候选人在 CandidateJson）；仅按 AssigneeId 过滤会漏掉未认领候选人（G-04）。
    /// 已被他人认领（AssigneeId&gt;0 且非本人）的任务不再出现在待办。
    /// </remarks>
    public static IList<WorkflowTask> FindTodoByUser(Int32 userId)
    {
        if (userId <= 0) return [];

        var open = _.Visible == true & _.Status.In(new[] { WorkflowStatuses.Active, WorkflowStatuses.Pending });
        var claimed = FindAll(_.AssigneeId == userId & open);
        var unclaimed = FindAll(_.AssigneeId == 0 & open)
            .Where(t => CandidateContains(t, userId))
            .ToList();
        if (unclaimed.Count == 0) return claimed;
        if (claimed.Count == 0) return unclaimed;
        return claimed.Concat(unclaimed).OrderByDescending(e => e.Id).ToList();
    }

    /// <summary>统计用户待办数量，供角标使用（口径与 <see cref="FindTodoByUser"/> 一致）</summary>
    /// <param name="userId">用户编号</param>
    /// <returns>待办条数</returns>
    public static Int64 CountTodoByUser(Int32 userId)
    {
        if (userId <= 0) return 0;
        return FindTodoByUser(userId).Count;
    }

    /// <summary>按用户查找已办任务（本人办理且状态为 Done/Transferred/Rejected）</summary>
    /// <param name="userId">用户编号</param>
    /// <returns>已办列表</returns>
    public static IList<WorkflowTask> FindDoneByUser(Int32 userId)
    {
        if (userId <= 0) return [];
        return FindAll(_.AssigneeId == userId)
            .Where(e => e.Status is WorkflowStatuses.Done or WorkflowStatuses.Transferred or WorkflowStatuses.Rejected)
            .ToList();
    }

    /// <summary>CandidateJson 是否包含用户 Id</summary>
    /// <param name="task">任务</param>
    /// <param name="userId">用户</param>
    /// <returns>是否候选人</returns>
    public static Boolean CandidateContains(WorkflowTask task, Int32 userId)
    {
        if (task == null || userId <= 0) return false;
        try
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(task.CandidateJson.IsNullOrEmpty() ? "[]" : task.CandidateJson);
            return WorkflowHelper.ReadIntArray(node as System.Text.Json.Nodes.JsonArray).Contains(userId);
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
