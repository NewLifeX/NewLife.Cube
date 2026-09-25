using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Workflow.Entity;

/// <summary>在途占用。同一业务键同时只有一条在途审批</summary>
public partial class WorkflowOccupancyModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>实体路径</summary>
    public String TypePath { get; set; }

    /// <summary>业务主键</summary>
    public String EntityKey { get; set; }

    /// <summary>流程实例</summary>
    public Int64 InstanceId { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(WorkflowOccupancyModel model)
    {
        Id = model.Id;
        TypePath = model.TypePath;
        EntityKey = model.EntityKey;
        InstanceId = model.InstanceId;
        CreateTime = model.CreateTime;
    }
    #endregion
}
