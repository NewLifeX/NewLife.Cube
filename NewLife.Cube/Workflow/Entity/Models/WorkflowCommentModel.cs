using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Workflow.Entity;

/// <summary>流程意见。审批动作与意见内容，附件走 Attachment</summary>
public partial class WorkflowCommentModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>流程实例</summary>
    public Int64 InstanceId { get; set; }

    /// <summary>任务。0=发起意见</summary>
    public Int64 TaskId { get; set; }

    /// <summary>动作。start/approve/reject/addSign/cc/rollback/withdraw/transfer/timeout</summary>
    public String Action { get; set; }

    /// <summary>意见内容</summary>
    public String Content { get; set; }

    /// <summary>创建者</summary>
    public String CreateUser { get; set; }

    /// <summary>创建用户</summary>
    public Int32 CreateUserID { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(WorkflowCommentModel model)
    {
        Id = model.Id;
        InstanceId = model.InstanceId;
        TaskId = model.TaskId;
        Action = model.Action;
        Content = model.Content;
        CreateUser = model.CreateUser;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
    }
    #endregion
}
