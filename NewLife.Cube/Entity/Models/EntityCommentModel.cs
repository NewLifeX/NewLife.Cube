using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>实体评论。挂在业务记录上的用户评论，支持同表回复</summary>
public partial class EntityCommentModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>分类。实体类型或业务类别</summary>
    public String Category { get; set; }

    /// <summary>关联。业务记录主键</summary>
    public Int64 LinkId { get; set; }

    /// <summary>父评论。0 表示顶层评论；回复时指向被回复的评论 Id</summary>
    public Int32 ParentId { get; set; }

    /// <summary>根评论。线程根节点 Id；顶层评论插入后等于自身 Id</summary>
    public Int32 RootId { get; set; }

    /// <summary>回复对象。被回复评论的作者用户 Id，顶层为 0</summary>
    public Int32 ReplyUserId { get; set; }

    /// <summary>回复对象名。被回复作者显示名</summary>
    public String ReplyUser { get; set; }

    /// <summary>内容</summary>
    public String Content { get; set; }

    /// <summary>创建人</summary>
    public String CreateUser { get; set; }

    /// <summary>创建者</summary>
    public Int32 CreateUserId { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>更新者</summary>
    public Int32 UpdateUserId { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>更新地址</summary>
    public String UpdateIP { get; set; }
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
}
