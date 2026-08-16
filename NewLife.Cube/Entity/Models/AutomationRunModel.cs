using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>自动化运行。一次触发的执行队列与状态，ConnName=Log</summary>
public partial class AutomationRunModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>租户。复制自规则</summary>
    public Int32 TenantId { get; set; }

    /// <summary>规则编号</summary>
    public Int64 AutomationId { get; set; }

    /// <summary>实体路径</summary>
    public String TypePath { get; set; }

    /// <summary>记录主键。定时/Webhook 可空</summary>
    public String RecordKey { get; set; }

    /// <summary>触发种类</summary>
    public String TriggerKind { get; set; }

    /// <summary>状态。queued/running/waiting/succeeded/failed/cancelled</summary>
    public String Status { get; set; }

    /// <summary>调用深度。runAutomation +1</summary>
    public Int32 Depth { get; set; }

    /// <summary>延时续跑时间。非 waiting 为 MinValue</summary>
    public DateTime ResumeAt { get; set; }

    /// <summary>节点轨迹。JSON</summary>
    public String NodeTrace { get; set; }

    /// <summary>错误摘要</summary>
    public String Error { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(AutomationRunModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        AutomationId = model.AutomationId;
        TypePath = model.TypePath;
        RecordKey = model.RecordKey;
        TriggerKind = model.TriggerKind;
        Status = model.Status;
        Depth = model.Depth;
        ResumeAt = model.ResumeAt;
        NodeTrace = model.NodeTrace;
        Error = model.Error;
        CreateTime = model.CreateTime;
        UpdateTime = model.UpdateTime;
    }
    #endregion
}
