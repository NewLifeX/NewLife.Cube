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

public partial class WorkflowSubject : Entity<WorkflowSubject>
{
    #region 对象操作
    static WorkflowSubject()
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
        }*/
        //if (method == DataMethod.Insert && !Dirtys[nameof(CreateTime)]) CreateTime = DateTime.Now;
        //if (method == DataMethod.Insert && !Dirtys[nameof(CreateIP)]) CreateIP = ManageProvider.UserHost;

        return true;
    }

    ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
    //[EditorBrowsable(EditorBrowsableState.Never)]
    //protected override void InitData()
    //{
    //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
    //    if (Meta.Session.Count > 0) return;

    //    if (XTrace.Debug) XTrace.WriteLine("开始初始化WorkflowSubject[流程主体]数据……");

    //    var entity = new WorkflowSubject();
    //    entity.Id = 0;
    //    entity.InstanceId = 0;
    //    entity.TypePath = "abc";
    //    entity.EntityKey = "abc";
    //    entity.Title = "abc";
    //    entity.Insert();

    //    if (XTrace.Debug) XTrace.WriteLine("完成初始化WorkflowSubject[流程主体]数据！");
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

    // Select Count(Id) as Id,TypePath From WorkflowSubject Where CreateTime>'2020-01-24 00:00:00' Group By TypePath Order By Id Desc limit 20
    static readonly FieldCache<WorkflowSubject> _TypePathCache = new(nameof(TypePath))
    {
        //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
    };

    /// <summary>获取实体路径列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
    /// <returns></returns>
    public static IDictionary<String, String> GetTypePathList() => _TypePathCache.FindAllName();
    #endregion

    #region 业务操作
    /// <summary>转为模型对象</summary>
    /// <returns>模型</returns>
    public WorkflowSubjectModel ToModel()
    {
        var model = new WorkflowSubjectModel();
        model.Copy(this);

        return model;
    }

    /// <summary>查找指定业务记录上的在途主体（所属实例 Status=Running）</summary>
    /// <param name="typePath">实体路径</param>
    /// <param name="entityKey">归一化业务主键</param>
    /// <returns>在途主体，无则 null</returns>
    public static WorkflowSubject FindRunning(String typePath, String entityKey)
    {
        if (typePath.IsNullOrEmpty() || entityKey.IsNullOrEmpty()) return null;

        var list = FindAll(_.TypePath == typePath & _.EntityKey == entityKey);
        if (list.Count == 0) return null;

        foreach (var subject in list)
        {
            var inst = WorkflowInstance.FindById(subject.InstanceId);
            if (inst != null && inst.Status == WorkflowStatuses.Running) return subject;
        }

        return null;
    }

    /// <summary>按主键集合批量查找在途主体，避免列表叠加 N+1</summary>
    /// <param name="typePath">实体路径</param>
    /// <param name="entityKeys">业务主键集合</param>
    /// <returns>在途主体列表</returns>
    public static IList<WorkflowSubject> FindRunning(String typePath, IEnumerable<String> entityKeys)
    {
        if (typePath.IsNullOrEmpty() || entityKeys == null) return [];

        var keys = entityKeys.Where(e => !e.IsNullOrEmpty()).Distinct().ToArray();
        if (keys.Length == 0) return [];

        var subjects = FindAll(_.TypePath == typePath & _.EntityKey.In(keys));
        if (subjects.Count == 0) return [];

        var instIds = subjects.Select(e => e.InstanceId).Distinct().ToArray();
        var running = WorkflowInstance.FindAll(WorkflowInstance._.Id.In(instIds) & WorkflowInstance._.Status == WorkflowStatuses.Running)
            .Select(e => e.Id)
            .ToHashSet();

        return subjects.Where(e => running.Contains(e.InstanceId)).ToList();
    }

    #endregion
}
