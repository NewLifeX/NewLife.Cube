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
using NewLife.Security;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Shards;

namespace NewLife.Cube.Entity;

public partial class EntityAutomation : Entity<EntityAutomation>
{
    #region 对象操作
    static EntityAutomation()
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
        if (Name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Name), "名称不能为空！");
        if (TriggerKind == null) throw new ArgumentNullException(nameof(TriggerKind), "触发种类不能为空！");

        // 建议先调用基类方法，基类方法会做一些统一处理
        if (!base.Valid(method)) return false;

        // 在新插入数据或者修改了指定字段时进行修正

        // 处理当前已登录用户信息，可以由UserInterceptor拦截器代劳
        /*var user = ManageProvider.User;
        if (user != null)
        {
            if (method == DataMethod.Insert && !Dirtys[nameof(CreateUserId)]) CreateUserId = user.ID;
            if (!Dirtys[nameof(UpdateUserId)]) UpdateUserId = user.ID;
        }*/
        //if (method == DataMethod.Insert && !Dirtys[nameof(CreateTime)]) CreateTime = DateTime.Now;
        //if (!Dirtys[nameof(UpdateTime)]) UpdateTime = DateTime.Now;
        //if (method == DataMethod.Insert && !Dirtys[nameof(CreateIP)]) CreateIP = ManageProvider.UserHost;
        //if (!Dirtys[nameof(UpdateIP)]) UpdateIP = ManageProvider.UserHost;

        // 检查唯一索引
        // CheckExist(method == DataMethod.Insert, nameof(HookToken));

        return true;
    }

    ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
    //[EditorBrowsable(EditorBrowsableState.Never)]
    //protected override void InitData()
    //{
    //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
    //    if (Meta.Session.Count > 0) return;

    //    if (XTrace.Debug) XTrace.WriteLine("开始初始化EntityAutomation[实体自动化]数据……");

    //    var entity = new EntityAutomation();
    //    entity.Id = 0;
    //    entity.TenantId = 0;
    //    entity.TypePath = "abc";
    //    entity.Name = "abc";
    //    entity.Enable = true;
    //    entity.Priority = 0;
    //    entity.TriggerKind = "abc";
    //    entity.TriggerConfig = "abc";
    //    entity.GraphJson = "abc";
    //    entity.HookToken = "abc";
    //    entity.Version = 0;
    //    entity.Insert();

    //    if (XTrace.Debug) XTrace.WriteLine("完成初始化EntityAutomation[实体自动化]数据！");
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

    // Select Count(Id) as Id,Category From EntityAutomation Where CreateTime>'2020-01-24 00:00:00' Group By Category Order By Id Desc limit 20
    //static readonly FieldCache<EntityAutomation> _CategoryCache = new(nameof(Category))
    //{
    //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
    //};

    ///// <summary>获取类别列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
    ///// <returns></returns>
    //public static IDictionary<String, String> GetCategoryList() => _CategoryCache.FindAllName();
    #endregion

    #region 业务操作
    /// <summary>转为模型</summary>
    public EntityAutomationModel ToModel()
    {
        var model = new EntityAutomationModel();
        model.Copy(this);
        return model;
    }

    /// <summary>按实体路径列出启用规则（优先级升序；兼容历史带前导 / 的 TypePath）</summary>
    public static IList<EntityAutomation> FindEnabled(String typePath)
    {
        var path = NewLife.Cube.Automation.AutomationPaths.NormalizeTypePath(typePath);
        if (path.IsNullOrEmpty()) return [];
        // 历史数据可能存 /Admin/User；触发侧为 Admin/User
        return FindAll((_.TypePath == path | _.TypePath == "/" + path | _.TypePath == "~/" + path) & _.Enable == true,
            _.Priority.Asc() & _.Id.Asc(), null, 0, 200);
    }

    /// <summary>按实体路径列出全部规则（兼容历史带前导 / 的 TypePath）</summary>
    public static IList<EntityAutomation> FindAllByTypePath(String typePath)
    {
        var path = NewLife.Cube.Automation.AutomationPaths.NormalizeTypePath(typePath);
        if (path.IsNullOrEmpty()) return [];
        return FindAll(_.TypePath == path | _.TypePath == "/" + path | _.TypePath == "~/" + path,
            _.Priority.Asc() & _.Id.Asc(), null, 0, 500);
    }

    /// <summary>Webhook 令牌为空时生成 32 位小写 hex</summary>
    public void EnsureHookToken()
    {
        if (!TriggerKind.EqualIgnoreCase("webhook")) return;
        if (!HookToken.IsNullOrEmpty()) return;
        HookToken = BitConverter.ToString(Rand.NextBytes(16)).Replace("-", "").ToLowerInvariant();
    }

    #endregion
}
