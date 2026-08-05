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

public partial class ViewProfile : Entity<ViewProfile>
{
    #region 对象操作
    // 控制最大缓存数量，Find/FindAll查询方法在表行数小于该值时走实体缓存
    private static Int32 MaxCacheCount = 1000;

    static ViewProfile()
    {
        // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
        //var df = Meta.Factory.AdditionalFields;
        //df.Add(nameof(UserId));

        // 拦截器 UserInterceptor、TimeInterceptor、IPInterceptor
        Meta.Interceptors.Add(new UserInterceptor { AllowEmpty = false });
        Meta.Interceptors.Add<TimeInterceptor>();
        Meta.Interceptors.Add(new IPInterceptor { AllowEmpty = false });

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
        // CheckExist(method == DataMethod.Insert, nameof(UserId), nameof(TypePath));

        return true;
    }

    ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
    //[EditorBrowsable(EditorBrowsableState.Never)]
    //protected override void InitData()
    //{
    //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
    //    if (Meta.Session.Count > 0) return;

    //    if (XTrace.Debug) XTrace.WriteLine("开始初始化ViewProfile[实体视图配置]数据……");

    //    var entity = new ViewProfile();
    //    entity.UserId = 0;
    //    entity.TypePath = "abc";
    //    entity.View = "abc";
    //    entity.ColumnsJson = "abc";
    //    entity.GanttJson = "abc";
    //    entity.CardJson = "abc";
    //    entity.FiltersJson = "abc";
    //    entity.Version = 0;
    //    entity.Insert();

    //    if (XTrace.Debug) XTrace.WriteLine("完成初始化ViewProfile[实体视图配置]数据！");
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

    // Select Count(Id) as Id,Category From ViewProfile Where CreateTime>'2020-01-24 00:00:00' Group By Category Order By Id Desc limit 20
    //static readonly FieldCache<ViewProfile> _CategoryCache = new(nameof(Category))
    //{
    //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
    //};

    ///// <summary>获取类别列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
    ///// <returns></returns>
    //public static IDictionary<String, String> GetCategoryList() => _CategoryCache.FindAllName();
    #endregion

    #region 业务操作
    /// <summary>合法页面条数选项，与前端 PAGE_SIZE_OPTIONS 保持一致</summary>
    private static readonly Int32[] _pageSizeOptions = [20, 50, 100, 200, 500, 1000];

    /// <summary>归一化页面条数。仅接受选项内正整数，非法/负数/非选项值一律归一为 0（未配置）</summary>
    /// <param name="size">原始页面条数</param>
    private static Int32 NormalizePageSize(Int32 size)
    {
        if (size <= 0) return 0;
        return Array.IndexOf(_pageSizeOptions, size) >= 0 ? size : 0;
    }

    /// <summary>转为模型</summary>
    public ViewProfileModel ToModel()
    {
        var model = new ViewProfileModel();
        model.Copy(this);

        return model;
    }

    /// <summary>为指定用户与实体路径 upsert 视图配置</summary>
    public static ViewProfile UpsertForUser(Int32 userId, String typePath, ViewProfileModel model)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        if (typePath.IsNullOrEmpty()) throw new ArgumentNullException(nameof(typePath));

        var entity = FindByUserIdAndTypePath(userId, typePath)
            ?? new ViewProfile { UserId = userId, TypePath = typePath };
        if (model != null)
        {
            if (model.View != null) entity.View = model.View;
            if (model.ColumnsJson != null) entity.ColumnsJson = model.ColumnsJson;
            if (model.ViewsJson != null) entity.ViewsJson = model.ViewsJson;
            if (model.ActiveViewId != null) entity.ActiveViewId = model.ActiveViewId;
            if (model.GanttJson != null) entity.GanttJson = model.GanttJson;
            if (model.CardJson != null) entity.CardJson = model.CardJson;
            if (model.FiltersJson != null) entity.FiltersJson = model.FiltersJson;
            // PageSize：仅接受 PAGE_SIZE_OPTIONS 合法值，非法归一 0（未配置）；0/缺省不覆盖已有配置
            if (model.PageSize > 0) entity.PageSize = NormalizePageSize(model.PageSize);
            if (model.FormJson != null) entity.FormJson = model.FormJson;
            if (model.Version > 0) entity.Version = model.Version;
            else if (entity.Version <= 0) entity.Version = 1;
            if (model.Remark != null) entity.Remark = model.Remark;
            if (!model.TypePath.IsNullOrEmpty()) entity.TypePath = model.TypePath;
        }
        else if (entity.Version <= 0)
            entity.Version = 1;

        entity.UserId = userId;
        if (entity.TypePath.IsNullOrEmpty()) entity.TypePath = typePath;
        entity.Save();
        return entity;
    }

    /// <summary>删除当前用户指定实体路径的视图配置（恢复默认）</summary>
    public static Boolean DeleteForUser(Int32 userId, String typePath)
    {
        if (userId <= 0 || typePath.IsNullOrEmpty()) return false;
        var entity = FindByUserIdAndTypePath(userId, typePath);
        if (entity == null) return false;
        entity.Delete();
        return true;
    }

    /// <summary>全局视图配置的用户标识。0 表示系统级（管理员定义的表单布局等）</summary>
    public const Int32 GlobalUserId = 0;

    /// <summary>查找全局视图配置（系统级，UserId=0）</summary>
    /// <param name="typePath">实体路径。如 Admin/User</param>
    public static ViewProfile FindGlobal(String typePath)
    {
        if (typePath.IsNullOrEmpty()) return null;
        return FindByUserIdAndTypePath(GlobalUserId, typePath);
    }

    /// <summary>
    /// 保存全局表单布局（仅管理员调用）。表单布局为系统全局唯一配置，作用于所有用户。
    /// formJson 为空壳（无 add/edit/detail 模式）时删除全局布局（恢复默认）。
    /// </summary>
    /// <param name="typePath">实体路径</param>
    /// <param name="formJson">表单布局 JSON</param>
    public static ViewProfile SaveGlobalFormJson(String typePath, String formJson)
    {
        if (typePath.IsNullOrEmpty()) throw new ArgumentNullException(nameof(typePath));

        // 空壳（无任何模式布局）等价于未配置：删除全局布局，恢复默认
        if (IsEmptyFormJson(formJson))
        {
            DeleteGlobalFormJson(typePath);
            return FindGlobal(typePath);
        }

        var entity = FindGlobal(typePath)
            ?? new ViewProfile { UserId = GlobalUserId, TypePath = typePath };
        entity.FormJson = formJson;
        if (entity.Version <= 0) entity.Version = 1;
        entity.Save();
        return entity;
    }

    /// <summary>删除全局表单布局（恢复默认）</summary>
    /// <param name="typePath">实体路径</param>
    public static Boolean DeleteGlobalFormJson(String typePath)
    {
        if (typePath.IsNullOrEmpty()) return false;
        var entity = FindGlobal(typePath);
        if (entity == null) return false;
        // 全局记录仅承载系统级配置（当前仅表单布局），无其他有效字段时整条删除
        var hasOther = !entity.View.IsNullOrEmpty()
            || !entity.ColumnsJson.IsNullOrEmpty()
            || !entity.ViewsJson.IsNullOrEmpty()
            || !entity.ActiveViewId.IsNullOrEmpty()
            || !entity.GanttJson.IsNullOrEmpty()
            || !entity.CardJson.IsNullOrEmpty()
            || !entity.FiltersJson.IsNullOrEmpty()
            || entity.PageSize > 0;
        if (hasOther)
            entity.FormJson = null;
        else
            entity.Delete();
        return true;
    }

    /// <summary>
    /// 保存全局模板（视图/筛选域，仅管理员调用）。模板为每个 typePath 一份的 UserId=0 全局记录，
    /// 普通用户基于模板可创建个人配置域（个人 > 模板 > 系统默认）。
    /// null 表示不覆盖该域；空串/空壳（无实际配置内容）表示清除该域（恢复默认）。
    /// </summary>
    /// <param name="typePath">实体路径</param>
    /// <param name="viewsJson">视图域模板 JSON；null 不覆盖，空壳清除</param>
    /// <param name="filtersJson">筛选域模板 JSON；null 不覆盖，空壳清除</param>
    public static ViewProfile SaveGlobalTemplate(String typePath, String viewsJson, String filtersJson)
    {
        if (typePath.IsNullOrEmpty()) throw new ArgumentNullException(nameof(typePath));
        if (viewsJson == null && filtersJson == null) return FindGlobal(typePath);

        var entity = FindGlobal(typePath)
            ?? new ViewProfile { UserId = GlobalUserId, TypePath = typePath };
        if (viewsJson != null) entity.ViewsJson = IsEmptyTemplateJson(viewsJson, true) ? null : viewsJson;
        if (filtersJson != null) entity.FiltersJson = IsEmptyTemplateJson(filtersJson, false) ? null : filtersJson;
        if (entity.Version <= 0) entity.Version = 1;
        entity.Save();
        return entity;
    }

    /// <summary>删除全局模板（视图/筛选域，回落系统默认）。全局记录仍承载其他域（如表单布局）时保留记录。</summary>
    /// <param name="typePath">实体路径</param>
    public static Boolean DeleteGlobalTemplate(String typePath)
    {
        if (typePath.IsNullOrEmpty()) return false;
        var entity = FindGlobal(typePath);
        if (entity == null) return false;

        entity.ViewsJson = null;
        entity.FiltersJson = null;
        var hasContent = !entity.View.IsNullOrEmpty()
            || !entity.ColumnsJson.IsNullOrEmpty()
            || !entity.ActiveViewId.IsNullOrEmpty()
            || !entity.GanttJson.IsNullOrEmpty()
            || !entity.CardJson.IsNullOrEmpty()
            || !entity.FormJson.IsNullOrEmpty()
            || entity.PageSize > 0;
        if (hasContent)
            entity.Save();
        else
            entity.Delete();
        return true;
    }

    /// <summary>判断模板 JSON 是否为空壳（无实际配置内容）。视图域空数组/空对象、筛选域空 views map 均视为未配置。</summary>
    /// <param name="json">模板 JSON</param>
    /// <param name="isViews">是否视图域（ViewsJson）</param>
    private static Boolean IsEmptyTemplateJson(String json, Boolean isViews)
    {
        if (json.IsNullOrWhiteSpace()) return true;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (isViews)
            {
                // ViewsJson：命名视图数组，空数组视为未配置
                if (root.ValueKind == System.Text.Json.JsonValueKind.Array) return root.GetArrayLength() == 0;
                if (root.ValueKind == System.Text.Json.JsonValueKind.Object) return !root.EnumerateObject().Any();
                return false;
            }
            // FiltersJson：views map 为空视为未配置
            if (root.ValueKind == System.Text.Json.JsonValueKind.Object
                && root.TryGetProperty("views", out var views)
                && views.ValueKind == System.Text.Json.JsonValueKind.Object)
                return !views.EnumerateObject().Any();
            if (root.ValueKind == System.Text.Json.JsonValueKind.Object) return !root.EnumerateObject().Any();
            return false;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>判断 FormJson 是否为空壳（无 add/edit/detail 任何模式布局）</summary>
    /// <param name="json">表单布局 JSON</param>
    private static Boolean IsEmptyFormJson(String json)
    {
        if (json.IsNullOrEmpty()) return true;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            // 注意：XCode 实体有 _ 成员，不能用 out _ 弃元，改用具名变量
            if (root.TryGetProperty("add", out var addEl)) return false;
            if (root.TryGetProperty("edit", out var editEl)) return false;
            if (root.TryGetProperty("detail", out var detailEl)) return false;
            return true;
        }
        catch
        {
            return true;
        }
    }

    #endregion
}
