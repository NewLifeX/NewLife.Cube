using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>通知记录。站内信/短信通知/邮件通知/公众号通知/钉钉机器人/企业微信机器人等</summary>
public partial class NotificationRecordModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }

    /// <summary>操作。Notify/Alert等</summary>
    public String Action { get; set; }

    /// <summary>渠道。InApp/Sms/Mail/WxMp/DingTalk/WeCom等</summary>
    public String Channel { get; set; }

    /// <summary>渠道编号</summary>
    public Int32 ConfigId { get; set; }

    /// <summary>渠道配置</summary>
    public String ConfigName { get; set; }

    /// <summary>提供者。渠道实现标识</summary>
    public String Provider { get; set; }

    /// <summary>用户。站内信/已知用户</summary>
    public Int32 UserId { get; set; }

    /// <summary>目标。手机号/邮箱/openid/机器人地址等</summary>
    public String Target { get; set; }

    /// <summary>标题。站内信/邮件主题</summary>
    public String Title { get; set; }

    /// <summary>内容</summary>
    public String Content { get; set; }

    /// <summary>成功</summary>
    public Boolean Success { get; set; }

    /// <summary>结果。包含错误信息</summary>
    public String Result { get; set; }

    /// <summary>已读。站内信/弹窗</summary>
    public Boolean Read { get; set; }

    /// <summary>已读时间</summary>
    public DateTime ReadTime { get; set; }

    /// <summary>追踪。链路追踪</summary>
    public String TraceId { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>更新地址</summary>
    public String UpdateIP { get; set; }

    /// <summary>备注</summary>
    public String Remark { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(NotificationRecordModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        Action = model.Action;
        Channel = model.Channel;
        ConfigId = model.ConfigId;
        ConfigName = model.ConfigName;
        Provider = model.Provider;
        UserId = model.UserId;
        Target = model.Target;
        Title = model.Title;
        Content = model.Content;
        Success = model.Success;
        Result = model.Result;
        Read = model.Read;
        ReadTime = model.ReadTime;
        TraceId = model.TraceId;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
        Remark = model.Remark;
    }
    #endregion
}
