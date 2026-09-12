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

public partial class NotificationRecord : Entity<NotificationRecord>
{
    #region 对象操作
    static NotificationRecord()
    {
        // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
        //var df = Meta.Factory.AdditionalFields;
        //df.Add(nameof(TenantId));

        // 过滤器 UserInterceptor、TimeInterceptor、IPInterceptor
        Meta.Interceptors.Add<TimeInterceptor>();
        Meta.Interceptors.Add(new IPInterceptor { AllowEmpty = false });
        Meta.Interceptors.Add<TraceInterceptor>();
        Meta.Interceptors.Add<TenantInterceptor>();
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

    //    if (XTrace.Debug) XTrace.WriteLine("开始初始化NotificationRecord[通知记录]数据……");

    //    var entity = new NotificationRecord();
    //    entity.Id = 0;
    //    entity.TenantId = 0;
    //    entity.Action = "abc";
    //    entity.Channel = "abc";
    //    entity.ConfigId = 0;
    //    entity.ConfigName = "abc";
    //    entity.Provider = "abc";
    //    entity.UserId = 0;
    //    entity.Target = "abc";
    //    entity.Title = "abc";
    //    entity.Content = "abc";
    //    entity.Success = true;
    //    entity.Result = "abc";
    //    entity.Read = true;
    //    entity.ReadTime = DateTime.Now;
    //    entity.Insert();

    //    if (XTrace.Debug) XTrace.WriteLine("完成初始化NotificationRecord[通知记录]数据！");
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

    // Select Count(Id) as Id,Channel From NotificationRecord Where CreateTime>'2020-01-24 00:00:00' Group By Channel Order By Id Desc limit 20
    static readonly FieldCache<NotificationRecord> _ChannelCache = new(nameof(Channel))
    {
        //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
    };

    /// <summary>获取渠道列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
    /// <returns></returns>
    public static IDictionary<String, String> GetChannelList() => _ChannelCache.FindAllName();
    #endregion

    #region 业务操作
    /// <summary>转换为通知记录模型</summary>
    /// <returns>通知记录模型</returns>
    public NotificationRecordModel ToModel()
    {
        var model = new NotificationRecordModel();
        model.Copy(this);

        return model;
    }

    #endregion

    #region 站内信
    /// <summary>构建当前用户可见的站内信查询条件。广播消息(UserId=0)仅系统管理员可见，个人消息(UserId&gt;0)仅本人可见</summary>
    /// <param name="userId">当前用户编号</param>
    /// <param name="admin">是否系统管理员（IsSystem 角色）</param>
    /// <returns>查询条件</returns>
    public static Expression GetInAppExp(Int32 userId, Boolean admin)
    {
        var exp = _.Channel == "InApp";
        if (admin)
            exp &= _.UserId == 0 | _.UserId == userId;
        else
            exp &= _.UserId == userId;

        return exp;
    }

    /// <summary>统计当前用户未读站内信数。广播消息(UserId=0)仅系统管理员计入</summary>
    /// <param name="userId">当前用户编号</param>
    /// <param name="admin">是否系统管理员（IsSystem 角色）</param>
    /// <returns>未读数</returns>
    public static Int32 CountUnread(Int32 userId, Boolean admin)
    {
        if (userId <= 0 && !admin) return 0;

        var exp = GetInAppExp(userId, admin) & _.Read == false;

        return (Int32)FindCount(exp);
    }

    /// <summary>获取当前用户最近的未读站内信，用于导航栏铃铛下拉</summary>
    /// <param name="userId">当前用户编号</param>
    /// <param name="admin">是否系统管理员（IsSystem 角色）</param>
    /// <param name="size">条数。默认10</param>
    /// <returns>未读站内信列表</returns>
    public static IList<NotificationRecord> GetRecentUnread(Int32 userId, Boolean admin, Int32 size = 10)
    {
        if (userId <= 0 && !admin) return [];

        var exp = GetInAppExp(userId, admin) & _.Read == false;

        return FindAll(exp, _.Id.Desc(), null, 0, size);
    }

    /// <summary>标记站内信已读。广播消息(UserId=0)任意系统管理员可标记，个人消息仅本人可标记</summary>
    /// <param name="id">通知编号</param>
    /// <param name="userId">当前用户编号</param>
    /// <param name="userName">当前用户名称，写入已读人便于审计</param>
    /// <param name="admin">是否系统管理员（IsSystem 角色）</param>
    /// <returns>已读后的记录；记录不存在、非站内信或无权时返回null</returns>
    public static NotificationRecord MarkRead(Int64 id, Int32 userId, String userName, Boolean admin)
    {
        var entity = FindById(id);
        if (entity == null || !entity.Channel.EqualIgnoreCase("InApp")) return null;

        // 广播消息仅系统管理员可读，个人消息仅本人可读
        if (entity.UserId == 0 && !admin) return null;
        if (entity.UserId != 0 && entity.UserId != userId) return null;

        if (!entity.Read)
        {
            entity.Read = true;
            entity.ReadTime = DateTime.Now;
            if (!userName.IsNullOrEmpty()) entity.Result = $"已读：{userName}";
            entity.Update();
        }

        return entity;
    }

    /// <summary>批量标记当前用户可见的全部未读站内信为已读</summary>
    /// <param name="userId">当前用户编号</param>
    /// <param name="userName">当前用户名称，写入已读人便于审计</param>
    /// <param name="admin">是否系统管理员（IsSystem 角色）</param>
    /// <returns>已读数量</returns>
    public static Int32 MarkAllRead(Int32 userId, String userName, Boolean admin)
    {
        if (userId <= 0 && !admin) return 0;

        var exp = GetInAppExp(userId, admin) & _.Read == false;
        var list = FindAll(exp, null, null, 0, 0);
        if (list.Count == 0) return 0;

        foreach (var entity in list)
        {
            entity.Read = true;
            entity.ReadTime = DateTime.Now;
            if (!userName.IsNullOrEmpty()) entity.Result = $"已读：{userName}";
        }

        return list.Save();
    }
    #endregion
}
