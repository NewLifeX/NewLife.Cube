using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Entity;

/// <summary>实体评论。挂在业务记录上的用户评论，支持同表回复</summary>
[Serializable]
[DataObject]
[Description("实体评论。挂在业务记录上的用户评论，支持同表回复")]
[BindIndex("IX_EntityComment_Category_LinkId", false, "Category,LinkId")]
[BindIndex("IX_EntityComment_ParentId", false, "ParentId")]
[BindIndex("IX_EntityComment_RootId", false, "RootId")]
[BindIndex("IX_EntityComment_CreateUserId", false, "CreateUserId")]
[BindTable("EntityComment", Description = "实体评论。挂在业务记录上的用户评论，支持同表回复", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class EntityComment : IEntity<EntityCommentModel>
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private String _Category;
    /// <summary>分类。实体类型或业务类别</summary>
    [DisplayName("分类")]
    [Description("分类。实体类型或业务类别")]
    [DataObjectField(false, false, false, 100)]
    [BindColumn("Category", "分类。实体类型或业务类别", "")]
    public String Category { get => _Category; set { if (OnPropertyChanging("Category", value)) { _Category = value; OnPropertyChanged("Category"); } } }

    private Int64 _LinkId;
    /// <summary>关联。业务记录主键</summary>
    [DisplayName("关联")]
    [Description("关联。业务记录主键")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("LinkId", "关联。业务记录主键", "")]
    public Int64 LinkId { get => _LinkId; set { if (OnPropertyChanging("LinkId", value)) { _LinkId = value; OnPropertyChanged("LinkId"); } } }

    private Int32 _ParentId;
    /// <summary>父评论。0 表示顶层评论；回复时指向被回复的评论 Id</summary>
    [DisplayName("父评论")]
    [Description("父评论。0 表示顶层评论；回复时指向被回复的评论 Id")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ParentId", "父评论。0 表示顶层评论；回复时指向被回复的评论 Id", "")]
    public Int32 ParentId { get => _ParentId; set { if (OnPropertyChanging("ParentId", value)) { _ParentId = value; OnPropertyChanged("ParentId"); } } }

    private Int32 _RootId;
    /// <summary>根评论。线程根节点 Id；顶层评论插入后等于自身 Id</summary>
    [DisplayName("根评论")]
    [Description("根评论。线程根节点 Id；顶层评论插入后等于自身 Id")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("RootId", "根评论。线程根节点 Id；顶层评论插入后等于自身 Id", "")]
    public Int32 RootId { get => _RootId; set { if (OnPropertyChanging("RootId", value)) { _RootId = value; OnPropertyChanged("RootId"); } } }

    private Int32 _ReplyUserId;
    /// <summary>回复对象。被回复评论的作者用户 Id，顶层为 0</summary>
    [DisplayName("回复对象")]
    [Description("回复对象。被回复评论的作者用户 Id，顶层为 0")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ReplyUserId", "回复对象。被回复评论的作者用户 Id，顶层为 0", "")]
    public Int32 ReplyUserId { get => _ReplyUserId; set { if (OnPropertyChanging("ReplyUserId", value)) { _ReplyUserId = value; OnPropertyChanged("ReplyUserId"); } } }

    private String _ReplyUser;
    /// <summary>回复对象名。被回复作者显示名</summary>
    [DisplayName("回复对象名")]
    [Description("回复对象名。被回复作者显示名")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("ReplyUser", "回复对象名。被回复作者显示名", "")]
    public String ReplyUser { get => _ReplyUser; set { if (OnPropertyChanging("ReplyUser", value)) { _ReplyUser = value; OnPropertyChanged("ReplyUser"); } } }

    private String _Content;
    /// <summary>内容</summary>
    [DisplayName("内容")]
    [Description("内容")]
    [DataObjectField(false, false, false, -1)]
    [BindColumn("Content", "内容", "")]
    public String Content { get => _Content; set { if (OnPropertyChanging("Content", value)) { _Content = value; OnPropertyChanged("Content"); } } }

    private String _CreateUser;
    /// <summary>创建人</summary>
    [Category("扩展")]
    [DisplayName("创建人")]
    [Description("创建人")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateUser", "创建人", "")]
    public String CreateUser { get => _CreateUser; set { if (OnPropertyChanging("CreateUser", value)) { _CreateUser = value; OnPropertyChanged("CreateUser"); } } }

    private Int32 _CreateUserId;
    /// <summary>创建者</summary>
    [Category("扩展")]
    [DisplayName("创建者")]
    [Description("创建者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CreateUserId", "创建者", "")]
    public Int32 CreateUserId { get => _CreateUserId; set { if (OnPropertyChanging("CreateUserId", value)) { _CreateUserId = value; OnPropertyChanged("CreateUserId"); } } }

    private DateTime _CreateTime;
    /// <summary>创建时间</summary>
    [Category("扩展")]
    [DisplayName("创建时间")]
    [Description("创建时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

    private String _CreateIP;
    /// <summary>创建地址</summary>
    [Category("扩展")]
    [DisplayName("创建地址")]
    [Description("创建地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateIP", "创建地址", "")]
    public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

    private Int32 _UpdateUserId;
    /// <summary>更新者</summary>
    [Category("扩展")]
    [DisplayName("更新者")]
    [Description("更新者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UpdateUserId", "更新者", "")]
    public Int32 UpdateUserId { get => _UpdateUserId; set { if (OnPropertyChanging("UpdateUserId", value)) { _UpdateUserId = value; OnPropertyChanged("UpdateUserId"); } } }

    private DateTime _UpdateTime;
    /// <summary>更新时间</summary>
    [Category("扩展")]
    [DisplayName("更新时间")]
    [Description("更新时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("UpdateTime", "更新时间", "")]
    public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

    private String _UpdateIP;
    /// <summary>更新地址</summary>
    [Category("扩展")]
    [DisplayName("更新地址")]
    [Description("更新地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UpdateIP", "更新地址", "")]
    public String UpdateIP { get => _UpdateIP; set { if (OnPropertyChanging("UpdateIP", value)) { _UpdateIP = value; OnPropertyChanged("UpdateIP"); } } }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(EntityCommentModel model)
    {
        Id = model.Id;
        Category = model.Category;
        LinkId = model.LinkId;
        ParentId = model.ParentId;
        RootId = model.RootId;
        ReplyUserId = model.ReplyUserId;
        ReplyUser = model.ReplyUser;
        Content = model.Content;
        CreateUser = model.CreateUser;
        CreateUserId = model.CreateUserId;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserId = model.UpdateUserId;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
    }
    #endregion

    #region 获取/设置 字段值
    /// <summary>获取/设置 字段值</summary>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public override Object this[String name]
    {
        get => name switch
        {
            "Id" => _Id,
            "Category" => _Category,
            "LinkId" => _LinkId,
            "ParentId" => _ParentId,
            "RootId" => _RootId,
            "ReplyUserId" => _ReplyUserId,
            "ReplyUser" => _ReplyUser,
            "Content" => _Content,
            "CreateUser" => _CreateUser,
            "CreateUserId" => _CreateUserId,
            "CreateTime" => _CreateTime,
            "CreateIP" => _CreateIP,
            "UpdateUserId" => _UpdateUserId,
            "UpdateTime" => _UpdateTime,
            "UpdateIP" => _UpdateIP,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToInt(); break;
                case "Category": _Category = Convert.ToString(value); break;
                case "LinkId": _LinkId = value.ToLong(); break;
                case "ParentId": _ParentId = value.ToInt(); break;
                case "RootId": _RootId = value.ToInt(); break;
                case "ReplyUserId": _ReplyUserId = value.ToInt(); break;
                case "ReplyUser": _ReplyUser = Convert.ToString(value); break;
                case "Content": _Content = Convert.ToString(value); break;
                case "CreateUser": _CreateUser = Convert.ToString(value); break;
                case "CreateUserId": _CreateUserId = value.ToInt(); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "UpdateUserId": _UpdateUserId = value.ToInt(); break;
                case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion

    #region 关联映射
    #endregion

    #region 扩展查询
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static EntityComment FindById(Int32 id)
    {
        if (id < 0) return null;

        // 实体缓存
        if (Meta.Session.Count < MaxCacheCount) return Meta.Cache.Find(e => e.Id == id);

        // 单对象缓存
        return Meta.SingleCache[id];

        //return Find(_.Id == id);
    }

    /// <summary>根据分类、关联查找</summary>
    /// <param name="category">分类</param>
    /// <param name="linkId">关联</param>
    /// <returns>实体列表</returns>
    public static IList<EntityComment> FindAllByCategoryAndLinkId(String category, Int64 linkId)
    {
        if (category.IsNullOrEmpty()) return [];
        if (linkId < 0) return [];

        // 实体缓存
        if (Meta.Session.Count < MaxCacheCount) return Meta.Cache.FindAll(e => e.Category.EqualIgnoreCase(category) && e.LinkId == linkId);

        return FindAll(_.Category == category & _.LinkId == linkId);
    }

    /// <summary>根据父评论查找</summary>
    /// <param name="parentId">父评论</param>
    /// <returns>实体列表</returns>
    public static IList<EntityComment> FindAllByParentId(Int32 parentId)
    {
        if (parentId < 0) return [];

        // 实体缓存
        if (Meta.Session.Count < MaxCacheCount) return Meta.Cache.FindAll(e => e.ParentId == parentId);

        return FindAll(_.ParentId == parentId);
    }

    /// <summary>根据根评论查找</summary>
    /// <param name="rootId">根评论</param>
    /// <returns>实体列表</returns>
    public static IList<EntityComment> FindAllByRootId(Int32 rootId)
    {
        if (rootId < 0) return [];

        // 实体缓存
        if (Meta.Session.Count < MaxCacheCount) return Meta.Cache.FindAll(e => e.RootId == rootId);

        return FindAll(_.RootId == rootId);
    }

    /// <summary>根据创建者查找</summary>
    /// <param name="createUserId">创建者</param>
    /// <returns>实体列表</returns>
    public static IList<EntityComment> FindAllByCreateUserId(Int32 createUserId)
    {
        if (createUserId < 0) return [];

        // 实体缓存
        if (Meta.Session.Count < MaxCacheCount) return Meta.Cache.FindAll(e => e.CreateUserId == createUserId);

        return FindAll(_.CreateUserId == createUserId);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="category">分类。实体类型或业务类别</param>
    /// <param name="linkId">关联。业务记录主键</param>
    /// <param name="parentId">父评论。0 表示顶层评论；回复时指向被回复的评论 Id</param>
    /// <param name="rootId">根评论。线程根节点 Id；顶层评论插入后等于自身 Id</param>
    /// <param name="createUserId">创建者</param>
    /// <param name="start">更新时间开始</param>
    /// <param name="end">更新时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<EntityComment> Search(String category, Int64 linkId, Int32 parentId, Int32 rootId, Int32 createUserId, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (!category.IsNullOrEmpty()) exp &= _.Category == category;
        if (linkId >= 0) exp &= _.LinkId == linkId;
        if (parentId >= 0) exp &= _.ParentId == parentId;
        if (rootId >= 0) exp &= _.RootId == rootId;
        if (createUserId >= 0) exp &= _.CreateUserId == createUserId;
        exp &= _.UpdateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 字段名
    /// <summary>取得实体评论字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>分类。实体类型或业务类别</summary>
        public static readonly Field Category = FindByName("Category");

        /// <summary>关联。业务记录主键</summary>
        public static readonly Field LinkId = FindByName("LinkId");

        /// <summary>父评论。0 表示顶层评论；回复时指向被回复的评论 Id</summary>
        public static readonly Field ParentId = FindByName("ParentId");

        /// <summary>根评论。线程根节点 Id；顶层评论插入后等于自身 Id</summary>
        public static readonly Field RootId = FindByName("RootId");

        /// <summary>回复对象。被回复评论的作者用户 Id，顶层为 0</summary>
        public static readonly Field ReplyUserId = FindByName("ReplyUserId");

        /// <summary>回复对象名。被回复作者显示名</summary>
        public static readonly Field ReplyUser = FindByName("ReplyUser");

        /// <summary>内容</summary>
        public static readonly Field Content = FindByName("Content");

        /// <summary>创建人</summary>
        public static readonly Field CreateUser = FindByName("CreateUser");

        /// <summary>创建者</summary>
        public static readonly Field CreateUserId = FindByName("CreateUserId");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>更新者</summary>
        public static readonly Field UpdateUserId = FindByName("UpdateUserId");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得实体评论字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>分类。实体类型或业务类别</summary>
        public const String Category = "Category";

        /// <summary>关联。业务记录主键</summary>
        public const String LinkId = "LinkId";

        /// <summary>父评论。0 表示顶层评论；回复时指向被回复的评论 Id</summary>
        public const String ParentId = "ParentId";

        /// <summary>根评论。线程根节点 Id；顶层评论插入后等于自身 Id</summary>
        public const String RootId = "RootId";

        /// <summary>回复对象。被回复评论的作者用户 Id，顶层为 0</summary>
        public const String ReplyUserId = "ReplyUserId";

        /// <summary>回复对象名。被回复作者显示名</summary>
        public const String ReplyUser = "ReplyUser";

        /// <summary>内容</summary>
        public const String Content = "Content";

        /// <summary>创建人</summary>
        public const String CreateUser = "CreateUser";

        /// <summary>创建者</summary>
        public const String CreateUserId = "CreateUserId";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>更新者</summary>
        public const String UpdateUserId = "UpdateUserId";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";
    }
    #endregion
}
