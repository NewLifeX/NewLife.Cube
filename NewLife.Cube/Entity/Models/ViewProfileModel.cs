using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>实体视图配置。按用户与实体路径的视图类型与列布局</summary>
public partial class ViewProfileModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>用户</summary>
    public Int32 UserId { get; set; }

    /// <summary>实体路径。如 Admin/User</summary>
    public String TypePath { get; set; }

    /// <summary>视图。table/tree/card/gantt</summary>
    public String View { get; set; }

    /// <summary>列布局。JSON 数组（与活跃命名视图同步）</summary>
    public String ColumnsJson { get; set; }

    /// <summary>命名视图集合。JSON 数组</summary>
    public String ViewsJson { get; set; }

    /// <summary>当前激活的命名视图 Id</summary>
    public String ActiveViewId { get; set; }

    /// <summary>甘特映射。JSON</summary>
    public String GanttJson { get; set; }

    /// <summary>卡片映射。JSON</summary>
    public String CardJson { get; set; }

    /// <summary>筛选记忆。JSON</summary>
    public String FiltersJson { get; set; }

    /// <summary>预定义查询。JSON</summary>
    public String QueriesJson { get; set; }

    /// <summary>页面条数。每页显示记录数，0 表示未配置</summary>
    public Int32 PageSize { get; set; }

    /// <summary>表单布局。JSON：add/edit/detail 的字段顺序/显隐/分组折叠</summary>
    public String FormJson { get; set; }

    /// <summary>页面仪表盘。JSON：version+widgets（实体级，不跟命名视图走）</summary>
    public String DashboardJson { get; set; }

    /// <summary>版本。配置契约版本</summary>
    public Int32 Version { get; set; }

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

    /// <summary>备注</summary>
    public String Remark { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(ViewProfileModel model)
    {
        Id = model.Id;
        UserId = model.UserId;
        TypePath = model.TypePath;
        View = model.View;
        ColumnsJson = model.ColumnsJson;
        ViewsJson = model.ViewsJson;
        ActiveViewId = model.ActiveViewId;
        GanttJson = model.GanttJson;
        CardJson = model.CardJson;
        FiltersJson = model.FiltersJson;
        QueriesJson = model.QueriesJson;
        PageSize = model.PageSize;
        FormJson = model.FormJson;
        DashboardJson = model.DashboardJson;
        Version = model.Version;
        CreateUserId = model.CreateUserId;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserId = model.UpdateUserId;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
        Remark = model.Remark;
    }
    #endregion
}
