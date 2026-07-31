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

public partial class EntityComment : Entity<EntityComment>
{
    #region 对象操作
    // 控制最大缓存数量，Find/FindAll查询方法在表行数小于该值时走实体缓存
    private static Int32 MaxCacheCount = 1000;

    static EntityComment()
    {
        // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
        //var df = Meta.Factory.AdditionalFields;
        //df.Add(nameof(LinkId));

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
        if (Category.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Category), "分类不能为空！");
        if (Content == null) throw new ArgumentNullException(nameof(Content), "内容不能为空！");

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

        return true;
    }

    ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
    //[EditorBrowsable(EditorBrowsableState.Never)]
    //protected override void InitData()
    //{
    //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
    //    if (Meta.Session.Count > 0) return;

    //    if (XTrace.Debug) XTrace.WriteLine("开始初始化EntityComment[实体评论]数据……");

    //    var entity = new EntityComment();
    //    entity.Category = "abc";
    //    entity.LinkId = 0;
    //    entity.Content = "abc";
    //    entity.Insert();

    //    if (XTrace.Debug) XTrace.WriteLine("完成初始化EntityComment[实体评论]数据！");
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

    // Select Count(Id) as Id,Category From EntityComment Where CreateTime>'2020-01-24 00:00:00' Group By Category Order By Id Desc limit 20
    static readonly FieldCache<EntityComment> _CategoryCache = new(nameof(Category))
    {
        //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
    };

    /// <summary>获取分类列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
    /// <returns></returns>
    public static IDictionary<String, String> GetCategoryList() => _CategoryCache.FindAllName();
    #endregion

    #region 业务操作
    /// <summary>转为模型</summary>
    public EntityCommentModel ToModel()
    {
        var model = new EntityCommentModel();
        model.Copy(this);

        return model;
    }

    /// <summary>发表评论（作者绑定为当前用户；parentId&gt;0 时为同表回复）</summary>
    public static EntityComment AddComment(Int32 userId, String userName, String category, Int64 linkId, String content, Int32 parentId = 0)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        if (category.IsNullOrEmpty()) throw new ArgumentNullException(nameof(category));
        if (content.IsNullOrEmpty()) throw new ArgumentNullException(nameof(content));

        EntityComment parent = null;
        if (parentId > 0)
        {
            parent = FindById(parentId);
            if (parent == null) throw new ArgumentOutOfRangeException(nameof(parentId), "父评论不存在");
            if (!parent.Category.EqualIgnoreCase(category) || parent.LinkId != linkId)
                throw new ArgumentException("父评论与当前业务记录不匹配", nameof(parentId));
        }

        var entity = new EntityComment
        {
            Category = category,
            LinkId = linkId,
            ParentId = parent?.Id ?? 0,
            RootId = parent == null ? 0 : (parent.RootId > 0 ? parent.RootId : parent.Id),
            ReplyUserId = parent?.CreateUserId ?? 0,
            ReplyUser = parent?.CreateUser,
            Content = content,
            CreateUser = userName,
            CreateUserId = userId,
        };
        entity.Insert();

        // 顶层评论：RootId 回写为自身 Id，便于按线程查询
        if (entity.RootId == 0)
        {
            entity.RootId = entity.Id;
            entity.Update();
        }

        return entity;
    }

    /// <summary>按业务记录列出评论；parentId=-1 全部，0 仅顶层，&gt;0 指定父评论的直接回复</summary>
    public static IList<EntityComment> FindList(String category, Int64 linkId, Int32 parentId = -1)
    {
        var list = FindAllByCategoryAndLinkId(category, linkId);
        if (parentId < 0) return list;
        return list.Where(e => e.ParentId == parentId).ToList();
    }

    /// <summary>删除评论：仅本人或管理员（不级联；子回复仍保留，ParentId 指向已删节点时由前端处理）</summary>
    public static Boolean TryDelete(Int32 id, Int32 userId, Boolean isAdmin)
    {
        var entity = FindById(id);
        if (entity == null) return false;
        if (!isAdmin && entity.CreateUserId != userId) return false;
        entity.Delete();
        return true;
    }

    #endregion
}
