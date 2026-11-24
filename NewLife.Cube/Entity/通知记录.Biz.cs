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
using NewLife.Serialization;
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
        Reflect.Copy(model, this);

        return model;
    }

    #endregion

    #region 投递
    /// <summary>把超时的发送中记录重置为待发送。节点崩溃或重启后，Sending 超过指定时间视为遗留</summary>
    /// <param name="now">当前时间</param>
    /// <param name="timeout">超时时间。默认5分钟</param>
    /// <returns>重置行数</returns>
    public static Int32 ResetStaleSending(DateTime now, TimeSpan? timeout = null)
    {
        // 0=待发送，1=发送中
        var pending = 0;
        var sending = 1;
        var db = Meta.Session.Dal.Db;
        var stale = db.FormatDateTime(now.AddMinutes(-(timeout?.TotalMinutes ?? 5)));

        return Update($"Status={pending}", $"Status={sending} AND UpdateTime < {stale}");
    }

    /// <summary>查找待投递的外部渠道记录</summary>
    /// <param name="now">当前时间</param>
    /// <param name="maxAttempts">最大尝试次数。默认3</param>
    /// <param name="size">批量大小。默认50</param>
    /// <returns>记录列表</returns>
    public static IList<NotificationRecord> FindDispatchable(DateTime now, Int32 maxAttempts = 3, Int32 size = 50)
    {
        if (size <= 0) size = 50;

        // 0=待发送，3=失败
        var pending = 0;
        var failed = 3;

        return FindAll(
            _.Channel != NotificationChannels.InApp
            & _.Deleted == false
            & _.Attempts < maxAttempts
            & (_.Status == pending | _.Status == failed)
            & (_.NextRetryTime.IsNull() | _.NextRetryTime <= now),
            _.Id.Asc(), null, 0, size);
    }

    /// <summary>抢占一条待投递记录，避免多节点重复投递</summary>
    /// <param name="id">记录编号</param>
    /// <param name="now">当前时间</param>
    /// <returns>影响行数</returns>
    public static Int32 TryClaimForSending(Int64 id, DateTime now)
    {
        // 0=待发送，1=发送中，3=失败
        var pending = 0;
        var sending = 1;
        var failed = 3;
        var db = Meta.Session.Dal.Db;
        var nowText = db.FormatDateTime(now);

        return Update($"Status={sending}, UpdateTime={nowText}", $"Id={id} AND (Status={pending} OR Status={failed})");
    }

    /// <summary>判断同一业务消息是否已被任一收件人已读或处理</summary>
    /// <param name="tenantId">租户编号</param>
    /// <param name="bizId">业务消息编号</param>
    /// <returns>是否已读或已处理</returns>
    public static Boolean ExistsReadOrHandled(Int32 tenantId, String bizId)
    {
        if (bizId.IsNullOrEmpty()) return false;

        return FindCount(_.TenantId == tenantId & _.BizId == bizId & (_.Read == true | _.Handled == true)) > 0;
    }

    /// <summary>判断指定业务消息在时间窗口内是否已存在</summary>
    /// <param name="tenantId">租户编号</param>
    /// <param name="bizId">业务消息编号</param>
    /// <param name="since">起始时间</param>
    /// <param name="prefix">是否按前缀匹配（每人一个 BizId 的场景）</param>
    /// <returns>是否存在</returns>
    public static Boolean ExistsBizId(Int32 tenantId, String bizId, DateTime since, Boolean prefix)
    {
        if (bizId.IsNullOrEmpty()) return false;

        var exp = _.TenantId == tenantId & _.CreateTime >= since
            & (prefix
                ? _.BizId == bizId | _.BizId.StartsWith(bizId + ":")
                : _.BizId == bizId);

        return FindCount(exp) > 0;
    }

    /// <summary>判断指定动作在时间之后是否已有记录</summary>
    /// <param name="action">动作</param>
    /// <param name="since">起始时间</param>
    /// <returns>是否存在</returns>
    public static Boolean ExistsActionSince(String action, DateTime since)
    {
        if (action.IsNullOrEmpty()) return false;

        return FindCount(_.Action == action & _.CreateTime >= since) > 0;
    }
    #endregion

    #region 站内信
    /// <summary>构建当前用户可见的站内信查询条件。广播消息(UserId=0)仅系统管理员可见，个人消息(UserId&gt;0)仅本人可见</summary>
    /// <param name="userId">当前用户编号</param>
    /// <param name="admin">是否系统管理员（IsSystem 角色）</param>
    /// <returns>查询条件</returns>
    public static Expression GetInAppExp(Int32 userId, Boolean admin)
    {
        var exp = _.Channel == NotificationChannels.InApp & (_.Deleted == false | _.Deleted.IsNull());
        exp &= _.ExpireTime.IsNull() | _.ExpireTime <= DateTime.MinValue | _.ExpireTime > DateTime.Now;
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

    /// <summary>获取指定编号之后的站内信增量，用于 SSE 与轮询兜底</summary>
    /// <param name="userId">当前用户编号</param>
    /// <param name="admin">是否系统管理员（IsSystem 角色）</param>
    /// <param name="sinceId">起始编号，不含</param>
    /// <param name="size">条数。默认20</param>
    /// <returns>增量列表，按编号升序</returns>
    public static IList<NotificationRecord> GetIncremental(Int32 userId, Boolean admin, Int64 sinceId, Int32 size = 20)
    {
        if (userId <= 0 && !admin) return [];
        if (size <= 0) size = 20;

        var exp = GetInAppExp(userId, admin);
        if (sinceId > 0) exp &= _.Id > sinceId;

        return FindAll(exp, _.Id.Asc(), null, 0, size);
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
        if (entity == null || !entity.Channel.EqualIgnoreCase(NotificationChannels.InApp)) return null;

        // 广播消息仅系统管理员可读，个人消息仅本人可读
        if (entity.UserId == 0 && !admin) return null;
        if (entity.UserId != 0 && entity.UserId != userId) return null;

        if (!entity.Read || entity.ReadBy != userId)
        {
            entity.Read = true;
            entity.ReadTime = DateTime.Now;
            entity.ReadBy = userId;
            if (entity.ReadChannel.IsNullOrEmpty()) entity.ReadChannel = NotificationChannels.InApp;
            if (!userName.IsNullOrEmpty()) entity.Result = $"已读：{userName}";
            entity.Update();
        }

        return entity;
    }

    /// <summary>按 BizId 标记已读。团队共享消息会同时更新同 BizId 的所有站内信行</summary>
    /// <param name="bizId">业务消息编号</param>
    /// <param name="userId">当前用户编号</param>
    /// <param name="userName">当前用户名称，写入已读人便于审计</param>
    /// <param name="tenantId">租户编号</param>
    /// <param name="channel">触发已读的渠道。InApp/AppPush/Sms/Mail</param>
    /// <returns>当前用户的站内信记录；记录不存在或无权时返回null</returns>
    public static NotificationRecord MarkReadByBizId(String bizId, Int32 userId, String userName, Int32 tenantId, String channel)
    {
        if (bizId.IsNullOrEmpty() || userId <= 0) return null;

        var exp = _.TenantId == tenantId & _.BizId == bizId & (_.Deleted == false | _.Deleted.IsNull());
        var list = FindAll(exp, null, null, 0, 0);
        if (list.Count == 0) return null;

        var own = list.FirstOrDefault(e => e.UserId == userId && e.Channel.EqualIgnoreCase(NotificationChannels.InApp))
            ?? list.FirstOrDefault(e => e.UserId == userId);
        if (own == null) return null;

        var now = DateTime.Now;
        var channel2 = channel.IsNullOrEmpty() ? NotificationChannels.InApp : channel;
        var reason = $"Cancelled because read on {channel2}.";
        var targets = new List<NotificationRecord>();
        foreach (var entity in list)
        {
            // 团队共享时更新全组；普通消息只更新本人
            if (!own.Shared && entity.UserId != userId) continue;

            if (!entity.Read)
            {
                entity.Read = true;
                entity.ReadTime = now;
                entity.ReadBy = userId;
                entity.ReadChannel = channel2;
            }

            // 任一渠道已读后，取消尚未发送的后续渠道
            if (own.CancelOnRead && entity.Status == 0 && entity.SentTime == DateTime.MinValue)
            {
                entity.Status = 4;
                entity.Success = true;
                entity.Result = reason;
            }

            targets.Add(entity);
        }

        var count = targets.Save();

        WriteLog(own.Shared ? "TeamRead" : "Read", bizId, own.Id, userId, userName, tenantId, channel2, null, count);

        return own;
    }

    /// <summary>按 BizId 处理通知。团队共享消息任一成员处理即闭环</summary>
    /// <param name="bizId">业务消息编号</param>
    /// <param name="userId">当前用户编号</param>
    /// <param name="userName">当前用户名称，写入处理人便于审计</param>
    /// <param name="tenantId">租户编号</param>
    /// <param name="result">处理结果摘要</param>
    /// <returns>当前用户的站内信记录；记录不存在或无权时返回null</returns>
    public static NotificationRecord HandleByBizId(String bizId, Int32 userId, String userName, Int32 tenantId, String result)
    {
        if (bizId.IsNullOrEmpty() || userId <= 0) return null;

        var exp = _.TenantId == tenantId & _.BizId == bizId & (_.Deleted == false | _.Deleted.IsNull());
        var list = FindAll(exp, null, null, 0, 0);
        if (list.Count == 0) return null;

        var own = list.FirstOrDefault(e => e.UserId == userId && e.Channel.EqualIgnoreCase(NotificationChannels.InApp))
            ?? list.FirstOrDefault(e => e.UserId == userId);
        if (own == null) return null;

        var now = DateTime.Now;
        var reason = "Cancelled because handled.";
        var targets = new List<NotificationRecord>();
        foreach (var entity in list)
        {
            if (!own.Shared && entity.UserId != userId) continue;

            if (!entity.Handled)
            {
                entity.Handled = true;
                entity.HandledBy = userId;
                entity.HandledTime = now;
                entity.HandleResult = result;
            }
            if (!entity.Read)
            {
                entity.Read = true;
                entity.ReadTime = now;
                entity.ReadBy = userId;
                entity.ReadChannel = NotificationChannels.InApp;
            }
            if (own.CancelOnRead && entity.Status == 0 && entity.SentTime == DateTime.MinValue)
            {
                entity.Status = 4;
                entity.Success = true;
                entity.Result = reason;
            }

            targets.Add(entity);
        }

        var count = targets.Save();

        WriteLog("Handle", bizId, own.Id, userId, userName, tenantId, NotificationChannels.InApp, result, count);

        return own;
    }

    /// <summary>批量标记当前用户可见的全部未读站内信为已读。只处理个人消息，不关闭团队共享消息</summary>
    /// <param name="userId">当前用户编号</param>
    /// <param name="userName">当前用户名称，写入已读人便于审计</param>
    /// <param name="admin">是否系统管理员（IsSystem 角色）</param>
    /// <returns>已读数量</returns>
    public static Int32 MarkAllRead(Int32 userId, String userName, Boolean admin)
    {
        if (userId <= 0 && !admin) return 0;

        var exp = GetInAppExp(userId, admin) & _.Read == false & (_.Shared == false | _.Shared.IsNull());
        var list = FindAll(exp, null, null, 0, 0);
        if (list.Count == 0) return 0;

        var now = DateTime.Now;
        foreach (var entity in list)
        {
            entity.Read = true;
            entity.ReadTime = now;
            entity.ReadBy = userId;
            entity.ReadChannel = "Batch";
            if (!userName.IsNullOrEmpty()) entity.Result = $"已读：{userName}";
        }

        var count = list.Save();
        if (count > 0)
        {
            var bizIds = list.Where(e => e.CancelOnRead).Select(e => e.BizId);
            var cancelled = CancelPending(bizIds, userId, "Cancelled by batch read.");
            WriteLog("BatchRead", "", 0, userId, userName, 0, "Batch", $"cancelled={cancelled}", count);
        }

        return count;
    }

    /// <summary>取消指定业务消息下尚未发送的个人渠道记录</summary>
    /// <param name="bizIds">业务消息编号集合</param>
    /// <param name="userId">用户编号</param>
    /// <param name="reason">取消原因</param>
    /// <returns>取消行数</returns>
    private static Int32 CancelPending(IEnumerable<String> bizIds, Int32 userId, String reason)
    {
        var ids = bizIds.Where(e => !e.IsNullOrEmpty()).Distinct().ToArray();
        if (ids.Length == 0) return 0;

        var exp = _.UserId == userId
            & _.BizId.In(ids)
            & _.Status == 0
            & (_.SentTime.IsNull() | _.SentTime == DateTime.MinValue)
            & (_.Deleted == false | _.Deleted.IsNull());
        var list = FindAll(exp, null, null, 0, 0);
        if (list.Count == 0) return 0;

        foreach (var entity in list)
        {
            entity.Status = 4;
            entity.Success = true;
            entity.Result = reason;
        }

        return list.Save();
    }

    /// <summary>软删除站内信。个人只能删除自己的非共享消息，管理员可清理共享消息</summary>
    /// <param name="id">通知编号</param>
    /// <param name="userId">当前用户编号</param>
    /// <param name="admin">是否系统管理员（IsSystem 角色）</param>
    /// <returns>影响行数</returns>
    public static Int32 SoftDelete(Int64 id, Int32 userId, Boolean admin)
    {
        var entity = FindById(id);
        if (entity == null || !entity.Channel.EqualIgnoreCase(NotificationChannels.InApp)) return 0;

        if (!admin && (entity.Shared || entity.UserId != userId)) return 0;

        entity.Deleted = true;

        return entity.Update();
    }

    /// <summary>写入通知已读/处理审计日志。失败只记日志，不影响主流程</summary>
    /// <param name="action">动作。Read/TeamRead/BatchRead/Handle</param>
    /// <param name="bizId">业务消息编号</param>
    /// <param name="recordId">通知记录编号</param>
    /// <param name="actorId">操作人编号</param>
    /// <param name="actorName">操作人名称</param>
    /// <param name="tenantId">租户编号</param>
    /// <param name="channel">渠道</param>
    /// <param name="result">处理结果</param>
    /// <param name="affected">影响行数</param>
    private static void WriteLog(String action, String bizId, Int64 recordId, Int32 actorId, String actorName, Int32 tenantId, String channel, String result, Int32 affected)
    {
        try
        {
            var log = new XCode.Membership.Log
            {
                Category = "Notification",
                Action = action,
                LinkId = recordId,
                CreateUserId = actorId,
                CreateUser = actorName,
                CreateTime = DateTime.Now,
                Success = true,
                Ex1 = tenantId,
                Ex4 = bizId,
                Ex5 = channel,
                Remark = new { recordId, bizId, actorId, channel, result, affected }.ToJson(),
            };
            log.Insert();
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }
    }
    #endregion

    /// <summary>分页获取当前用户可见的站内信</summary>
    /// <param name="userId">当前用户编号</param>
    /// <param name="admin">是否系统管理员</param>
    /// <param name="category">分类</param>
    /// <param name="read">已读状态</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页</param>
    /// <returns>站内信列表</returns>
    public static IList<NotificationRecord> GetInAppPage(Int32 userId, Boolean admin, String category, Boolean? read, DateTime start, DateTime end, String key, PageParameter page)
    {
        if (userId <= 0 && !admin) return [];

        var exp = GetInAppExp(userId, admin);
        if (!category.IsNullOrEmpty()) exp &= _.Category == category;
        if (read != null) exp &= _.Read == read;
        exp &= _.Id.Between(start, end, Meta.Factory.Snow);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        page ??= new PageParameter();
        if (page.Sort.IsNullOrEmpty()) page.Sort = _.Id.Desc();

        return FindAll(exp, page);
    }

    #region 高级查询
    /// <summary>按业务字段高级查询，供管理端列表使用</summary>
    /// <param name="tenantId">租户</param>
    /// <param name="bizId">业务消息编号</param>
    /// <param name="channel">渠道</param>
    /// <param name="userId">用户</param>
    /// <param name="category">分类</param>
    /// <param name="level">级别</param>
    /// <param name="read">已读</param>
    /// <param name="handled">已处理</param>
    /// <param name="success">成功</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页</param>
    /// <returns>通知记录列表</returns>
    public static IList<NotificationRecord> SearchBy(Int32 tenantId, String bizId, String channel, Int32 userId, String category, String level, Boolean? read, Boolean? handled, Boolean? success, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (tenantId >= 0) exp &= _.TenantId == tenantId;
        if (!bizId.IsNullOrEmpty()) exp &= _.BizId == bizId;
        if (!channel.IsNullOrEmpty()) exp &= _.Channel == channel;
        if (userId >= 0) exp &= _.UserId == userId;
        if (!category.IsNullOrEmpty()) exp &= _.Category == category;
        if (!level.IsNullOrEmpty()) exp &= _.Level == level;
        if (read != null) exp &= _.Read == read;
        if (handled != null) exp &= _.Handled == handled;
        if (success != null) exp &= _.Success == success;
        exp &= _.Id.Between(start, end, Meta.Factory.Snow);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);
        if (page?.State is Expression state) exp &= state;

        page ??= new PageParameter();
        if (page.Sort.IsNullOrEmpty()) page.Sort = _.Id.Desc();

        return FindAll(exp, page);
    }
    #endregion
}
