using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>应用日志。用于OAuthServer的子系统</summary>
public partial class AppLogModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>应用</summary>
    public Int32 AppId { get; set; }

    /// <summary>操作</summary>
    public String Action { get; set; }

    /// <summary>成功</summary>
    public Boolean Success { get; set; }

    /// <summary>应用标识</summary>
    public String ClientId { get; set; }

    /// <summary>回调地址</summary>
    public String RedirectUri { get; set; }

    /// <summary>响应类型。默认code</summary>
    public String ResponseType { get; set; }

    /// <summary>授权域</summary>
    public String Scope { get; set; }

    /// <summary>状态数据</summary>
    public String State { get; set; }

    /// <summary>访问令牌</summary>
    public String AccessToken { get; set; }

    /// <summary>刷新令牌</summary>
    public String RefreshToken { get; set; }

    /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
    public String TraceId { get; set; }

    /// <summary>创建者。可以是设备编码等唯一使用者标识</summary>
    public String CreateUser { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

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
    public void Copy(AppLogModel model)
    {
        Id = model.Id;
        AppId = model.AppId;
        Action = model.Action;
        Success = model.Success;
        ClientId = model.ClientId;
        RedirectUri = model.RedirectUri;
        ResponseType = model.ResponseType;
        Scope = model.Scope;
        State = model.State;
        AccessToken = model.AccessToken;
        RefreshToken = model.RefreshToken;
        TraceId = model.TraceId;
        CreateUser = model.CreateUser;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateIP = model.UpdateIP;
        UpdateTime = model.UpdateTime;
        Remark = model.Remark;
    }
    #endregion
}
