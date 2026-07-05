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

public partial class LovDefinition : Entity<LovDefinition>
{
    #region 对象操作
    static LovDefinition()
    {
        // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
        //var df = Meta.Factory.AdditionalFields;
        //df.Add(nameof(CreateUserID));

        // 过滤器 UserInterceptor、TimeInterceptor、IPInterceptor
        Meta.Interceptors.Add(new UserInterceptor { AllowEmpty = false });
        Meta.Interceptors.Add<TimeInterceptor>();
        Meta.Interceptors.Add(new IPInterceptor { AllowEmpty = false });

        // 实体缓存
        // var ec = Meta.Cache;
        // ec.Expire = 60;

        // 单对象缓存
        var sc = Meta.SingleCache;
        // sc.Expire = 60;
        sc.FindSlaveKeyMethod = k => Find(_.LovCode == k);
        sc.GetSlaveKeyMethod = e => e.LovCode;
    }

    /// <summary>验证并修补数据，返回验证结果，或者通过抛出异常的方式提示验证失败。</summary>
    /// <param name="method">添删改方法</param>
    public override Boolean Valid(DataMethod method)
    {
        //if (method == DataMethod.Delete) return true;
        // 如果没有脏数据，则不需要进行任何处理
        if (!HasDirty) return true;

        // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
        if (LovCode.IsNullOrEmpty()) throw new ArgumentNullException(nameof(LovCode), "值集编码不能为空！");

        // 建议先调用基类方法，基类方法会做一些统一处理
        if (!base.Valid(method)) return false;

        // 在新插入数据或者修改了指定字段时进行修正

        // 处理当前已登录用户信息，可以由UserModule过滤器代劳
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

        // 检查唯一索引
        // CheckExist(method == DataMethod.Insert, nameof(LovCode));

        return true;
    }

    ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
    //[EditorBrowsable(EditorBrowsableState.Never)]
    //protected override void InitData()
    //{
    //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
    //    if (Meta.Session.Count > 0) return;

    //    if (XTrace.Debug) XTrace.WriteLine("开始初始化LovDefinition[值集定义]数据……");

    //    var entity = new LovDefinition();
    //    entity.LovCode = "abc";
    //    entity.Name = "abc";
    //    entity.Type = "abc";
    //    entity.ValueField = "abc";
    //    entity.LabelField = "abc";
    //    entity.Source = "abc";
    //    entity.Enabled = true;
    //    entity.Insert();

    //    if (XTrace.Debug) XTrace.WriteLine("完成初始化LovDefinition[值集定义]数据！");
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
    /// <summary>高级查询</summary>
    /// <param name="lovCode">值集编码。带前缀的完整编码，如 Enum.ProcessCard.EnableStatus / List.User</param>
    /// <param name="type">值集类型。ENUM=枚举型/LIST=列表型，须与LovCode前缀一致</param>
    /// <param name="enabled">启用</param>
    /// <param name="start">更新时间开始</param>
    /// <param name="end">更新时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<LovDefinition> Search(String lovCode, String type, Boolean? enabled, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (!lovCode.IsNullOrEmpty()) exp &= _.LovCode == lovCode;
        if (!type.IsNullOrEmpty()) exp &= _.Type == type;
        if (enabled != null) exp &= _.Enabled == enabled;
        exp &= _.UpdateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }

    // Select Count(Id) as Id,Type From LovDefinition Where CreateTime>'2020-01-24 00:00:00' Group By Type Order By Id Desc limit 20
    static readonly FieldCache<LovDefinition> _TypeCache = new(nameof(Type))
    {
        //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
    };

    /// <summary>获取值集类型列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
    /// <returns></returns>
    public static IDictionary<String, String> GetTypeList() => _TypeCache.FindAllName();
    #endregion

    #region 业务操作
    public LovDefinitionModel ToModel()
    {
        var model = new LovDefinitionModel();
        model.Copy(this);

        return model;
    }

    #endregion
}
