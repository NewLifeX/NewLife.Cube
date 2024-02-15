using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Data;
using XCode;
using XCode.Cache;
using XCode.Exceptions;
using XCode.Membership;

namespace NewLife.Cube.Entity;

/// <summary>OAuth日志。用于记录OAuth客户端请求，同时Id作为state，避免向OAuthServer泄漏本机Url</summary>
public partial class OAuthLog : Entity<OAuthLog>
{
    #region 对象操作
    static OAuthLog()
    {
        // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
        //var df = Meta.Factory.AdditionalFields;
        //df.Add(nameof(ConnectId));

        // 过滤器 UserModule、TimeModule、IPModule
        Meta.Modules.Add<TimeModule>();
        Meta.Modules.Add<IPModule>();
    }

    /// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>
    /// <param name="isNew">是否插入</param>
    public override void Valid(Boolean isNew)
    {
        // 如果没有脏数据，则不需要进行任何处理
        if (!HasDirty) return;

        // 建议先调用基类方法，基类方法会做一些统一处理
        base.Valid(isNew);

        // 在新插入数据或者修改了指定字段时进行修正
        //if (isNew && !Dirtys[nameof(CreateTime)]) CreateTime = DateTime.Now;
        //if (!Dirtys[nameof(UpdateTime)]) UpdateTime = DateTime.Now;
        //if (isNew && !Dirtys[nameof(CreateIP)]) CreateIP = ManageProvider.UserHost;
    }
    #endregion

    #region 扩展属性
    /// <summary>用户</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public User User => Extends.Get(nameof(User), k => User.FindByID(UserId));

    /// <summary>用户</summary>
    [Map(__.UserId)]
    public String UserName => User?.ToString();
    #endregion

    #region 扩展查询
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static OAuthLog FindById(Int64 id)
    {
        if (id <= 0) return null;

        // 单对象缓存
        return Meta.SingleCache[id];

        //return Find(_.Id == id);
    }

    /// <summary>根据提供商查找</summary>
    /// <param name="provider">提供商</param>
    /// <returns>实体列表</returns>
    public static IList<OAuthLog> FindAllByProvider(String provider) => FindAll(_.Provider == provider);

    /// <summary>根据链接查找</summary>
    /// <param name="connectId">链接</param>
    /// <returns>实体列表</returns>
    public static IList<OAuthLog> FindAllByConnectId(Int32 connectId) => FindAll(_.ConnectId == connectId);

    /// <summary>根据用户查找</summary>
    /// <param name="userId">用户</param>
    /// <returns>实体列表</returns>
    public static IList<OAuthLog> FindAllByUserId(Int32 userId) => FindAll(_.UserId == userId);
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="provider">提供商</param>
    /// <param name="connectId">链接</param>
    /// <param name="userId">用户</param>
    /// <param name="start">更新时间开始</param>
    /// <param name="end">更新时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<OAuthLog> Search(String provider, Int32 connectId, Int32 userId, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (!provider.IsNullOrEmpty()) exp &= _.Provider == provider;
        if (connectId >= 0) exp &= _.ConnectId == connectId;
        if (userId >= 0) exp &= _.UserId == userId;
        exp &= _.UpdateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= _.Action.Contains(key) | _.RedirectUri.Contains(key) | _.ResponseType.Contains(key) | _.Scope.Contains(key) | _.State.Contains(key) | _.AccessToken.Contains(key) | _.RefreshToken.Contains(key) | _.Remark.Contains(key) | _.CreateIP.Contains(key);

        return FindAll(exp, page);
    }

    // Select Count(Id) as Id,Provider From OAuthLog Where CreateTime>'2020-01-24 00:00:00' Group By Provider Order By Id Desc limit 20
    private static readonly FieldCache<OAuthLog> _ProviderCache = new FieldCache<OAuthLog>(nameof(Provider))
    {
        //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
    };

    /// <summary>获取提供商列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
    /// <returns></returns>
    public static IDictionary<String, String> GetProviderList() => _ProviderCache.FindAllName();
    #endregion

    #region 业务操作
    /// <summary>删除指定日期之前的数据</summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static Int32 DeleteBefore(DateTime date)
    {
        // SQLite下日志表较大时，删除可能报错，可以查询出来逐个删除
        var where = _.Id < Meta.Factory.Snow.GetId(date) & _.UserId == 0;
        try
        {
            return Delete(where);
        }
        catch (XSqlException)
        {
            var rs = 0;
            for (var i = 0; i < 100; i++)
            {
                var list = FindAll(where, null, null, 0, 10000);
                if (list.Count == 0) break;

                rs += list.Delete();
            }
            return rs;
        }
    }
    #endregion
}