using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Workflow.Entity;

/// <summary>流程任务。节点上的待办/已办任务</summary>
public partial class WorkflowTaskModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>流程实例</summary>
    public Int64 InstanceId { get; set; }

    /// <summary>节点。图节点 id</summary>
    public String NodeId { get; set; }

    /// <summary>签核模式。or/and/sequence</summary>
    public String Mode { get; set; }

    /// <summary>办理人。0=未认领，候选人在 CandidateJson</summary>
    public Int32 AssigneeId { get; set; }

    /// <summary>候选人。用户 Id 数组</summary>
    public String CandidateJson { get; set; }

    /// <summary>依次签序号。从 0 起</summary>
    public Int32 SequenceIndex { get; set; }

    /// <summary>可见。依次签未轮到为 false</summary>
    public Boolean Visible { get; set; }

    /// <summary>状态。Pending/Active/Done/Rejected/Cancelled/Transferred</summary>
    public String Status { get; set; }

    /// <summary>截止时间</summary>
    public DateTime DueTime { get; set; }

    /// <summary>超时动作。pass/reject/transfer</summary>
    public String TimeoutAction { get; set; }

    /// <summary>超时转交。复用 to schema</summary>
    public String TimeoutTransferTo { get; set; }

    /// <summary>认领时间</summary>
    public DateTime ClaimTime { get; set; }

    /// <summary>完成时间</summary>
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
    public void Copy(WorkflowTaskModel model)
    {
        Id = model.Id;
        InstanceId = model.InstanceId;
        NodeId = model.NodeId;
        Mode = model.Mode;
        AssigneeId = model.AssigneeId;
        CandidateJson = model.CandidateJson;
        SequenceIndex = model.SequenceIndex;
        Visible = model.Visible;
        Status = model.Status;
        DueTime = model.DueTime;
        TimeoutAction = model.TimeoutAction;
        TimeoutTransferTo = model.TimeoutTransferTo;
        ClaimTime = model.ClaimTime;
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
