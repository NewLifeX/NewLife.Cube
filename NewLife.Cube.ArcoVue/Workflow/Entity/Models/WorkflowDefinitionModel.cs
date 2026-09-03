using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Workflow.Entity;

/// <summary>流程定义。按实体路径配置的审批图草稿与发布快照</summary>
public partial class WorkflowDefinitionModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>租户。0=平台</summary>
    public Int32 TenantId { get; set; }

    /// <summary>实体路径。与 GetPage 一致，禁止空=全部实体</summary>
    public String TypePath { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>已发布。仅已发布定义可发起</summary>
    public Boolean Published { get; set; }

    /// <summary>版本。每次发布加一</summary>
    public Int32 Version { get; set; }

    /// <summary>锁策略。full 全锁 / nodeFields 节点字段</summary>
    public String LockPolicy { get; set; }

    /// <summary>发起过滤。ViewFilter 同构 JSON，空表示全记录可发起</summary>
    public String StartFilter { get; set; }

    /// <summary>设计草稿。FlowGram 图 JSON</summary>
    public String GraphJson { get; set; }

    /// <summary>发布快照。发起时再拷到实例</summary>
    public String PublishedGraphJson { get; set; }

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

    /// <summary>备注</summary>
    public String Remark { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(WorkflowDefinitionModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        TypePath = model.TypePath;
        Name = model.Name;
        Enable = model.Enable;
        Published = model.Published;
        Version = model.Version;
        LockPolicy = model.LockPolicy;
        StartFilter = model.StartFilter;
        GraphJson = model.GraphJson;
        PublishedGraphJson = model.PublishedGraphJson;
        CreateUser = model.CreateUser;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateUser = model.UpdateUser;
        UpdateUserID = model.UpdateUserID;
        UpdateIP = model.UpdateIP;
        UpdateTime = model.UpdateTime;
        Remark = model.Remark;
    }
    #endregion
}
