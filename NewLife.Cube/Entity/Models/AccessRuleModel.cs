using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>访问规则。控制系统访问的安全访问规则，放行或拦截或限流</summary>
public partial class AccessRuleModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>优先级。较大优先</summary>
    public Int32 Priority { get; set; }

    /// <summary>URL路径。支持*模糊匹配，多个逗号隔开</summary>
    public String Url { get; set; }

    /// <summary>用户代理。支持*模糊匹配，多个逗号隔开</summary>
    public String UserAgent { get; set; }

    /// <summary>来源IP。支持*模糊匹配，多个逗号隔开</summary>
    public String IP { get; set; }

    /// <summary>登录用户。支持*模糊匹配，多个逗号隔开</summary>
    public String LoginedUser { get; set; }

    /// <summary>动作。放行/拦截/限流</summary>
    public AccessActionKinds ActionKind { get; set; }

    /// <summary>拦截代码。拦截时返回Http代码，如404/500/302等</summary>
    public Int32 BlockCode { get; set; }

    /// <summary>拦截内容。拦截时返回内容，返回302时此处调目标地址</summary>
    public String BlockContent { get; set; }

    /// <summary>限流维度。IP/用户</summary>
    public LimitDimensions LimitDimension { get; set; }

    /// <summary>限流时间。限流时的考察时间，期间累加规则触发次数，如600秒</summary>
    public Int32 LimitCycle { get; set; }

    /// <summary>限流次数。限流考察期间达到该阈值时，执行拦截</summary>
    public Int32 LimitTimes { get; set; }

    /// <summary>创建者</summary>
    public Int32 CreateUserID { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>更新者</summary>
    public Int32 UpdateUserID { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>更新地址</summary>
    public String UpdateIP { get; set; }

    /// <summary>内容</summary>
    public String Remark { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(AccessRuleModel model)
    {
        Id = model.Id;
        Name = model.Name;
        Enable = model.Enable;
        Priority = model.Priority;
        Url = model.Url;
        UserAgent = model.UserAgent;
        IP = model.IP;
        LoginedUser = model.LoginedUser;
        ActionKind = model.ActionKind;
        BlockCode = model.BlockCode;
        BlockContent = model.BlockContent;
        LimitDimension = model.LimitDimension;
        LimitCycle = model.LimitCycle;
        LimitTimes = model.LimitTimes;
        CreateUserID = model.CreateUserID;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserID = model.UpdateUserID;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
        Remark = model.Remark;
    }
    #endregion
}
