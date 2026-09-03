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

public partial class WorkflowDefinition : Entity<WorkflowDefinition>
{
    #region 对象操作
    static WorkflowDefinition()
    {
        // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
        //var df = Meta.Factory.AdditionalFields;
        //df.Add(nameof(TenantId));

        // 拦截器 UserInterceptor、TimeInterceptor、IPInterceptor
        Meta.Interceptors.Add(new UserInterceptor { AllowEmpty = true });
        Meta.Interceptors.Add<TimeInterceptor>();
        Meta.Interceptors.Add(new IPInterceptor { AllowEmpty = true });
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

        // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
        if (TypePath.IsNullOrEmpty()) throw new ArgumentNullException(nameof(TypePath), "实体路径不能为空！");
        TypePath = TypePath.Trim();
        if (TypePath.IsNullOrEmpty()) throw new ArgumentNullException(nameof(TypePath), "实体路径不能为空！");

        if (Name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Name), "名称不能为空！");
        Name = Name.Trim();
        if (Name.IsNullOrEmpty() || Name.Length > 50) throw new ArgumentOutOfRangeException(nameof(Name), "名称长度须为 1–50！");

        if (LockPolicy.IsNullOrEmpty()) LockPolicy = WorkflowStatuses.LockFull;
        if (Version <= 0) Version = 1;
        if (StartFilter.IsNullOrEmpty()) StartFilter = "{}";

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

    //    if (XTrace.Debug) XTrace.WriteLine("开始初始化WorkflowDefinition[流程定义]数据……");

    //    var entity = new WorkflowDefinition();
    //    entity.Id = 0;
    //    entity.TenantId = 0;
    //    entity.TypePath = "abc";
    //    entity.Name = "abc";
    //    entity.Enable = true;
    //    entity.Published = true;
    //    entity.Version = 0;
    //    entity.LockPolicy = "abc";
    //    entity.StartFilter = "abc";
    //    entity.GraphJson = "abc";
    //    entity.PublishedGraphJson = "abc";
    //    entity.Insert();

    //    if (XTrace.Debug) XTrace.WriteLine("完成初始化WorkflowDefinition[流程定义]数据！");
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
    #endregion

    #region 业务操作
    /// <summary>转为模型对象</summary>
    /// <returns>模型</returns>
    public WorkflowDefinitionModel ToModel()
    {
        var model = new WorkflowDefinitionModel();
        model.Copy(this);

        return model;
    }

    /// <summary>查找指定路径下已启用且已发布的定义</summary>
    /// <param name="tenantId">租户。0=平台</param>
    /// <param name="typePath">实体路径</param>
    /// <returns>定义列表</returns>
    public static IList<WorkflowDefinition> FindPublished(Int32 tenantId, String typePath)
    {
        if (typePath.IsNullOrEmpty()) return [];

        return FindAll(_.TenantId == tenantId & _.TypePath == typePath & _.Enable == true & _.Published == true);
    }

    /// <summary>发布校验入口。校验图非空后钉扎 PublishedGraphJson 并递增版本</summary>
    /// <remarks>完整图结构校验在引擎侧补齐；此处保证发布快照与版本钉扎。</remarks>
    /// <returns>是否发布成功</returns>
    public Boolean Publish()
    {
        if (TypePath.IsNullOrEmpty()) throw new InvalidOperationException("实体路径不能为空");
        if (Name.IsNullOrEmpty()) throw new InvalidOperationException("名称不能为空");
        if (GraphJson.IsNullOrEmpty()) throw new InvalidOperationException("设计图不能为空，无法发布");

        PublishedGraphJson = GraphJson;
        Version = Version <= 0 ? 1 : Version + 1;
        Published = true;
        if (LockPolicy.IsNullOrEmpty()) LockPolicy = WorkflowStatuses.LockFull;

        return Update() > 0;
    }

    #endregion
}
