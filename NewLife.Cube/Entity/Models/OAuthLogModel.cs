using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>OAuth日志。用于记录OAuth客户端请求，同时Id作为state，避免向OAuthServer泄漏本机Url</summary>
public partial class OAuthLogModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>提供商</summary>
    public String Provider { get; set; }

    /// <summary>链接</summary>
    public Int32 ConnectId { get; set; }

    /// <summary>用户</summary>
    public Int32 UserId { get; set; }

    /// <summary>操作</summary>
    public String Action { get; set; }

    /// <summary>成功</summary>
    public Boolean Success { get; set; }

    /// <summary>回调地址</summary>
    public String RedirectUri { get; set; }

    /// <summary>响应类型。默认code</summary>
    public String ResponseType { get; set; }

    /// <summary>授权域</summary>
    public String Scope { get; set; }

    /// <summary>状态数据</summary>
    public String State { get; set; }

    /// <summary>来源</summary>
    public String Source { get; set; }

    /// <summary>访问令牌</summary>
    public String AccessToken { get; set; }

    /// <summary>刷新令牌</summary>
    public String RefreshToken { get; set; }

    /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
    public String TraceId { get; set; }

    /// <summary>详细信息</summary>
    public String Remark { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(OAuthLogModel model)
    {
        Id = model.Id;
        Provider = model.Provider;
        ConnectId = model.ConnectId;
        UserId = model.UserId;
        Action = model.Action;
        Success = model.Success;
        RedirectUri = model.RedirectUri;
        ResponseType = model.ResponseType;
        Scope = model.Scope;
        State = model.State;
        Source = model.Source;
        AccessToken = model.AccessToken;
        RefreshToken = model.RefreshToken;
        TraceId = model.TraceId;
        Remark = model.Remark;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateTime = model.UpdateTime;
    }
    #endregion
}
