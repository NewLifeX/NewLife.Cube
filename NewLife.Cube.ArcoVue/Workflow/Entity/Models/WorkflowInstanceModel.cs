using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Workflow.Entity;

/// <summary>流程实例。一次审批运行，钉扎定义版本与图快照</summary>
public partial class WorkflowInstanceModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>租户。拷贝定义</summary>
    public Int32 TenantId { get; set; }

    /// <summary>流程定义</summary>
    public Int64 DefinitionId { get; set; }

    /// <summary>定义版本。发起时钉扎</summary>
    public Int32 DefinitionVersion { get; set; }

    /// <summary>实体路径</summary>
    public String TypePath { get; set; }

    /// <summary>图快照。发起时复制 PublishedGraphJson，之后只读</summary>
    public String GraphSnapshot { get; set; }

    /// <summary>状态。Running/Approved/Rejected/Withdrawn/Cancelled</summary>
    public String Status { get; set; }

    /// <summary>发起人</summary>
    public Int32 StarterId { get; set; }

    /// <summary>发起意见</summary>
    public String StartComment { get; set; }

    /// <summary>结束时间</summary>
    public DateTime FinishTime { get; set; }

    /// <summary>创建者</summary>
    public String CreateUser { get; set; }

    /// <summary>创建用户</summary>
    public Int32 CreateUserID { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>更新者</summary>
    public String UpdateUser { get; set; }

    /// <summary>更新用户</summary>
    public Int32 UpdateUserID { get; set; }

    /// <summary>更新地址</summary>
    public String UpdateIP { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(WorkflowInstanceModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        DefinitionId = model.DefinitionId;
        DefinitionVersion = model.DefinitionVersion;
        TypePath = model.TypePath;
        GraphSnapshot = model.GraphSnapshot;
        Status = model.Status;
        StarterId = model.StarterId;
        StartComment = model.StartComment;
        FinishTime = model.FinishTime;
        CreateUser = model.CreateUser;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateUser = model.UpdateUser;
        UpdateUserID = model.UpdateUserID;
        UpdateIP = model.UpdateIP;
        UpdateTime = model.UpdateTime;
    }
    #endregion
}
