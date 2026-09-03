using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Workflow.Entity;

/// <summary>流程主体。实例关联的业务记录，一批最多 100 条</summary>
public partial class WorkflowSubjectModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>流程实例</summary>
    public Int64 InstanceId { get; set; }

    /// <summary>实体路径。必须等于实例 TypePath</summary>
    public String TypePath { get; set; }

    /// <summary>业务主键。归一化后的单主键字符串</summary>
    public String EntityKey { get; set; }

    /// <summary>标题。发起时快照显示名</summary>
    public String Title { get; set; }

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
    public void Copy(WorkflowSubjectModel model)
    {
        Id = model.Id;
        InstanceId = model.InstanceId;
        TypePath = model.TypePath;
        EntityKey = model.EntityKey;
        Title = model.Title;
        CreateUser = model.CreateUser;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
    }
    #endregion
}
