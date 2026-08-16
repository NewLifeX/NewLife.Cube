using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>实体自动化流程。按实体配置的触发与动作规则，GraphJson 存线性图</summary>
public partial class EntityAutomationModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }

    /// <summary>实体路径。如 Admin/User</summary>
    public String TypePath { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>优先级。越小越先</summary>
    public Int32 Priority { get; set; }

    /// <summary>触发种类</summary>
    public String TriggerKind { get; set; }

    /// <summary>触发配置。JSON</summary>
    public String TriggerConfig { get; set; }

    /// <summary>流程图。nodes/edges JSON</summary>
    public String GraphJson { get; set; }

    /// <summary>入站令牌。webhook 用</summary>
    public String HookToken { get; set; }

    /// <summary>版本。乐观并发</summary>
    public Int32 Version { get; set; }

    /// <summary>创建人</summary>
    public String CreateUser { get; set; }

    /// <summary>创建者</summary>
    public Int32 CreateUserId { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>更新人</summary>
    public String UpdateUser { get; set; }

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
    public void Copy(EntityAutomationModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        TypePath = model.TypePath;
        Name = model.Name;
        Enable = model.Enable;
        Priority = model.Priority;
        TriggerKind = model.TriggerKind;
        TriggerConfig = model.TriggerConfig;
        GraphJson = model.GraphJson;
        HookToken = model.HookToken;
        Version = model.Version;
        CreateUser = model.CreateUser;
        CreateUserId = model.CreateUserId;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUser = model.UpdateUser;
        UpdateUserId = model.UpdateUserId;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
    }
    #endregion
}
