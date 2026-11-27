#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>通知记录。站内信/短信/邮件/APP推送/Webhook/钉钉/企业微信等</summary>
public partial class NotificationRecordModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int64 Id { get; set; }

    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }

    /// <summary>业务消息。同一BizId共享已读/处理状态</summary>
    public String BizId { get; set; }

    /// <summary>共享读状态。团队任一收件人已读即全员已读</summary>
    public Boolean Shared { get; set; }

    /// <summary>已读后停止后续渠道。true 表示任一渠道已读即取消未发送渠道</summary>
    public Boolean CancelOnRead { get; set; }

    /// <summary>操作。Notify/Alert等</summary>
    public String Action { get; set; }

    /// <summary>渠道。InApp/Sms/Mail/AppPush/Webhook/DingTalk/WeCom等</summary>
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

    /// <summary>内容。站内信正文/邮件正文，支持长文本</summary>
    public String Content { get; set; }

    /// <summary>分类。System/Business/Security/Marketing/Todo等</summary>
    public String Category { get; set; }

    /// <summary>级别。Info/Success/Warning/Error/Urgent</summary>
    public String Level { get; set; }

    /// <summary>跳转。点击通知打开的前端地址</summary>
    public String Link { get; set; }

    /// <summary>数据。JSON业务载荷，供模板渲染与跳转</summary>
    public String Data { get; set; }

    /// <summary>模板。通知模板编码</summary>
    public String TemplateCode { get; set; }

    /// <summary>语言。渲染时使用的语言</summary>
    public String Language { get; set; }

    /// <summary>发送人。0表示系统</summary>
    public Int32 SenderId { get; set; }

    /// <summary>已读。站内信/弹窗</summary>
    public Boolean Read { get; set; }

    /// <summary>已读时间</summary>
    public DateTime ReadTime { get; set; }

    /// <summary>已读人。实际触发已读的用户</summary>
    public Int32 ReadBy { get; set; }

    /// <summary>已读渠道。InApp/AppPush/Sms/Mail/Batch</summary>
    public String ReadChannel { get; set; }

    /// <summary>已处理。业务确认，不等于已读</summary>
    public Boolean Handled { get; set; }

    /// <summary>处理人</summary>
    public Int32 HandledBy { get; set; }

    /// <summary>处理时间</summary>
    public DateTime HandledTime { get; set; }

    /// <summary>处理结果</summary>
    public String HandleResult { get; set; }

    /// <summary>已删除。站内信软删除</summary>
    public Boolean Deleted { get; set; }

    /// <summary>状态。0待发送/1发送中/2成功/3失败/4跳过</summary>
    public Int32 Status { get; set; }

    /// <summary>尝试次数</summary>
    public Int32 Attempts { get; set; }

    /// <summary>下次重试时间</summary>
    public DateTime NextRetryTime { get; set; }

    /// <summary>发送完成时间</summary>
    public DateTime SentTime { get; set; }

    /// <summary>过期时间。过期后不再展示与发送</summary>
    public DateTime ExpireTime { get; set; }

    /// <summary>成功</summary>
    public Boolean Success { get; set; }

    /// <summary>结果。包含错误信息</summary>
    public String Result { get; set; }

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
        BizId = model.BizId;
        Shared = model.Shared;
        CancelOnRead = model.CancelOnRead;
        Action = model.Action;
        Channel = model.Channel;
        ConfigId = model.ConfigId;
        ConfigName = model.ConfigName;
        Provider = model.Provider;
        UserId = model.UserId;
        Target = model.Target;
        Title = model.Title;
        Content = model.Content;
        Category = model.Category;
        Level = model.Level;
        Link = model.Link;
        Data = model.Data;
        TemplateCode = model.TemplateCode;
        Language = model.Language;
        SenderId = model.SenderId;
        Read = model.Read;
        ReadTime = model.ReadTime;
        ReadBy = model.ReadBy;
        ReadChannel = model.ReadChannel;
        Handled = model.Handled;
        HandledBy = model.HandledBy;
        HandledTime = model.HandledTime;
        HandleResult = model.HandleResult;
        Deleted = model.Deleted;
        Status = model.Status;
        Attempts = model.Attempts;
        NextRetryTime = model.NextRetryTime;
        SentTime = model.SentTime;
        ExpireTime = model.ExpireTime;
        Success = model.Success;
        Result = model.Result;
        TraceId = model.TraceId;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
        Remark = model.Remark;
    }
    #endregion
}
