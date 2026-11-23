using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Entity;

/// <summary>通知记录。站内信/短信/邮件/APP推送/Webhook/钉钉/企业微信等</summary>
[Serializable]
[DataObject]
[Description("通知记录。站内信/短信/邮件/APP推送/Webhook/钉钉/企业微信等")]
[BindIndex("IX_NotificationRecord_UserId_Channel_Deleted_Read_Id", false, "UserId,Channel,Deleted,Read,Id")]
[BindIndex("IX_NotificationRecord_TenantId_Channel_Id", false, "TenantId,Channel,Id")]
[BindIndex("IX_NotificationRecord_TenantId_BizId_Channel", false, "TenantId,BizId,Channel")]
[BindIndex("IX_NotificationRecord_Status_NextRetryTime", false, "Status,NextRetryTime")]
[BindTable("NotificationRecord", Description = "通知记录。站内信/短信/邮件/APP推送/Webhook/钉钉/企业微信等", ConnName = "Log", DbType = DatabaseType.None)]
public partial class NotificationRecord : IEntity<NotificationRecordModel>
{
    #region 属性
    private Int64 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, false, false, 0)]
    [BindColumn("Id", "编号", "", DataScale = "time")]
    public Int64 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int32 _TenantId;
    /// <summary>租户</summary>
    [DisplayName("租户")]
    [Description("租户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("TenantId", "租户", "")]
    public Int32 TenantId { get => _TenantId; set { if (OnPropertyChanging("TenantId", value)) { _TenantId = value; OnPropertyChanged("TenantId"); } } }

    private String _BizId;
    /// <summary>业务消息。同一BizId共享已读/处理状态</summary>
    [DisplayName("业务消息")]
    [Description("业务消息。同一BizId共享已读/处理状态")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("BizId", "业务消息。同一BizId共享已读/处理状态", "")]
    public String BizId { get => _BizId; set { if (OnPropertyChanging("BizId", value)) { _BizId = value; OnPropertyChanged("BizId"); } } }

    private Boolean _Shared;
    /// <summary>共享读状态。团队任一收件人已读即全员已读</summary>
    [DisplayName("共享读状态")]
    [Description("共享读状态。团队任一收件人已读即全员已读")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Shared", "共享读状态。团队任一收件人已读即全员已读", "")]
    public Boolean Shared { get => _Shared; set { if (OnPropertyChanging("Shared", value)) { _Shared = value; OnPropertyChanged("Shared"); } } }

    private Boolean _CancelOnRead;
    /// <summary>已读后停止后续渠道。true 表示任一渠道已读即取消未发送渠道</summary>
    [DisplayName("已读后停止后续渠道")]
    [Description("已读后停止后续渠道。true 表示任一渠道已读即取消未发送渠道")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CancelOnRead", "已读后停止后续渠道。true 表示任一渠道已读即取消未发送渠道", "")]
    public Boolean CancelOnRead { get => _CancelOnRead; set { if (OnPropertyChanging("CancelOnRead", value)) { _CancelOnRead = value; OnPropertyChanged("CancelOnRead"); } } }

    private String _Action;
    /// <summary>操作。Notify/Alert等</summary>
    [DisplayName("操作")]
    [Description("操作。Notify/Alert等")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Action", "操作。Notify/Alert等", "")]
    public String Action { get => _Action; set { if (OnPropertyChanging("Action", value)) { _Action = value; OnPropertyChanged("Action"); } } }

    private String _Channel;
    /// <summary>渠道。InApp/Sms/Mail/AppPush/Webhook/DingTalk/WeCom等</summary>
    [DisplayName("渠道")]
    [Description("渠道。InApp/Sms/Mail/AppPush/Webhook/DingTalk/WeCom等")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Channel", "渠道。InApp/Sms/Mail/AppPush/Webhook/DingTalk/WeCom等", "")]
    public String Channel { get => _Channel; set { if (OnPropertyChanging("Channel", value)) { _Channel = value; OnPropertyChanged("Channel"); } } }

    private Int32 _ConfigId;
    /// <summary>渠道编号</summary>
    [DisplayName("渠道编号")]
    [Description("渠道编号")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ConfigId", "渠道编号", "")]
    public Int32 ConfigId { get => _ConfigId; set { if (OnPropertyChanging("ConfigId", value)) { _ConfigId = value; OnPropertyChanged("ConfigId"); } } }

    private String _ConfigName;
    /// <summary>渠道配置</summary>
    [DisplayName("渠道配置")]
    [Description("渠道配置")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("ConfigName", "渠道配置", "")]
    public String ConfigName { get => _ConfigName; set { if (OnPropertyChanging("ConfigName", value)) { _ConfigName = value; OnPropertyChanged("ConfigName"); } } }

    private String _Provider;
    /// <summary>提供者。渠道实现标识</summary>
    [DisplayName("提供者")]
    [Description("提供者。渠道实现标识")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Provider", "提供者。渠道实现标识", "")]
    public String Provider { get => _Provider; set { if (OnPropertyChanging("Provider", value)) { _Provider = value; OnPropertyChanged("Provider"); } } }

    private Int32 _UserId;
    /// <summary>用户。站内信/已知用户</summary>
    [DisplayName("用户")]
    [Description("用户。站内信/已知用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UserId", "用户。站内信/已知用户", "")]
    public Int32 UserId { get => _UserId; set { if (OnPropertyChanging("UserId", value)) { _UserId = value; OnPropertyChanged("UserId"); } } }

    private String _Target;
    /// <summary>目标。手机号/邮箱/openid/机器人地址等</summary>
    [DisplayName("目标")]
    [Description("目标。手机号/邮箱/openid/机器人地址等")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Target", "目标。手机号/邮箱/openid/机器人地址等", "")]
    public String Target { get => _Target; set { if (OnPropertyChanging("Target", value)) { _Target = value; OnPropertyChanged("Target"); } } }

    private String _Title;
    /// <summary>标题。站内信/邮件主题</summary>
    [DisplayName("标题")]
    [Description("标题。站内信/邮件主题")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Title", "标题。站内信/邮件主题", "", Master = true)]
    public String Title { get => _Title; set { if (OnPropertyChanging("Title", value)) { _Title = value; OnPropertyChanged("Title"); } } }

    private String _Content;
    /// <summary>内容。站内信正文/邮件正文，支持长文本</summary>
    [DisplayName("内容")]
    [Description("内容。站内信正文/邮件正文，支持长文本")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("Content", "内容。站内信正文/邮件正文，支持长文本", "")]
    public String Content { get => _Content; set { if (OnPropertyChanging("Content", value)) { _Content = value; OnPropertyChanged("Content"); } } }

    private String _Category;
    /// <summary>分类。System/Business/Security/Marketing/Todo等</summary>
    [DisplayName("分类")]
    [Description("分类。System/Business/Security/Marketing/Todo等")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Category", "分类。System/Business/Security/Marketing/Todo等", "")]
    public String Category { get => _Category; set { if (OnPropertyChanging("Category", value)) { _Category = value; OnPropertyChanged("Category"); } } }

    private String _Level;
    /// <summary>级别。Info/Success/Warning/Error/Urgent</summary>
    [DisplayName("级别")]
    [Description("级别。Info/Success/Warning/Error/Urgent")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Level", "级别。Info/Success/Warning/Error/Urgent", "")]
    public String Level { get => _Level; set { if (OnPropertyChanging("Level", value)) { _Level = value; OnPropertyChanged("Level"); } } }

    private String _Link;
    /// <summary>跳转。点击通知打开的前端地址</summary>
    [DisplayName("跳转")]
    [Description("跳转。点击通知打开的前端地址")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Link", "跳转。点击通知打开的前端地址", "")]
    public String Link { get => _Link; set { if (OnPropertyChanging("Link", value)) { _Link = value; OnPropertyChanged("Link"); } } }

    private String _Data;
    /// <summary>数据。JSON业务载荷，供模板渲染与跳转</summary>
    [DisplayName("数据")]
    [Description("数据。JSON业务载荷，供模板渲染与跳转")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("Data", "数据。JSON业务载荷，供模板渲染与跳转", "")]
    public String Data { get => _Data; set { if (OnPropertyChanging("Data", value)) { _Data = value; OnPropertyChanged("Data"); } } }

    private String _TemplateCode;
    /// <summary>模板。通知模板编码</summary>
    [DisplayName("模板")]
    [Description("模板。通知模板编码")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("TemplateCode", "模板。通知模板编码", "")]
    public String TemplateCode { get => _TemplateCode; set { if (OnPropertyChanging("TemplateCode", value)) { _TemplateCode = value; OnPropertyChanged("TemplateCode"); } } }

    private String _Language;
    /// <summary>语言。渲染时使用的语言</summary>
    [DisplayName("语言")]
    [Description("语言。渲染时使用的语言")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Language", "语言。渲染时使用的语言", "")]
    public String Language { get => _Language; set { if (OnPropertyChanging("Language", value)) { _Language = value; OnPropertyChanged("Language"); } } }

    private Int32 _SenderId;
    /// <summary>发送人。0表示系统</summary>
    [DisplayName("发送人")]
    [Description("发送人。0表示系统")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("SenderId", "发送人。0表示系统", "")]
    public Int32 SenderId { get => _SenderId; set { if (OnPropertyChanging("SenderId", value)) { _SenderId = value; OnPropertyChanged("SenderId"); } } }

    private Boolean _Read;
    /// <summary>已读。站内信/弹窗</summary>
    [DisplayName("已读")]
    [Description("已读。站内信/弹窗")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Read", "已读。站内信/弹窗", "")]
    public Boolean Read { get => _Read; set { if (OnPropertyChanging("Read", value)) { _Read = value; OnPropertyChanged("Read"); } } }

    private DateTime _ReadTime;
    /// <summary>已读时间</summary>
    [DisplayName("已读时间")]
    [Description("已读时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("ReadTime", "已读时间", "")]
    public DateTime ReadTime { get => _ReadTime; set { if (OnPropertyChanging("ReadTime", value)) { _ReadTime = value; OnPropertyChanged("ReadTime"); } } }

    private Int32 _ReadBy;
    /// <summary>已读人。实际触发已读的用户</summary>
    [DisplayName("已读人")]
    [Description("已读人。实际触发已读的用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ReadBy", "已读人。实际触发已读的用户", "")]
    public Int32 ReadBy { get => _ReadBy; set { if (OnPropertyChanging("ReadBy", value)) { _ReadBy = value; OnPropertyChanged("ReadBy"); } } }

    private String _ReadChannel;
    /// <summary>已读渠道。InApp/AppPush/Sms/Mail/Batch</summary>
    [DisplayName("已读渠道")]
    [Description("已读渠道。InApp/AppPush/Sms/Mail/Batch")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("ReadChannel", "已读渠道。InApp/AppPush/Sms/Mail/Batch", "")]
    public String ReadChannel { get => _ReadChannel; set { if (OnPropertyChanging("ReadChannel", value)) { _ReadChannel = value; OnPropertyChanged("ReadChannel"); } } }

    private Boolean _Handled;
    /// <summary>已处理。业务确认，不等于已读</summary>
    [DisplayName("已处理")]
    [Description("已处理。业务确认，不等于已读")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Handled", "已处理。业务确认，不等于已读", "")]
    public Boolean Handled { get => _Handled; set { if (OnPropertyChanging("Handled", value)) { _Handled = value; OnPropertyChanged("Handled"); } } }

    private Int32 _HandledBy;
    /// <summary>处理人</summary>
    [DisplayName("处理人")]
    [Description("处理人")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("HandledBy", "处理人", "")]
    public Int32 HandledBy { get => _HandledBy; set { if (OnPropertyChanging("HandledBy", value)) { _HandledBy = value; OnPropertyChanged("HandledBy"); } } }

    private DateTime _HandledTime;
    /// <summary>处理时间</summary>
    [DisplayName("处理时间")]
    [Description("处理时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("HandledTime", "处理时间", "")]
    public DateTime HandledTime { get => _HandledTime; set { if (OnPropertyChanging("HandledTime", value)) { _HandledTime = value; OnPropertyChanged("HandledTime"); } } }

    private String _HandleResult;
    /// <summary>处理结果</summary>
    [DisplayName("处理结果")]
    [Description("处理结果")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("HandleResult", "处理结果", "")]
    public String HandleResult { get => _HandleResult; set { if (OnPropertyChanging("HandleResult", value)) { _HandleResult = value; OnPropertyChanged("HandleResult"); } } }

    private Boolean _Deleted;
    /// <summary>已删除。站内信软删除</summary>
    [DisplayName("已删除")]
    [Description("已删除。站内信软删除")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Deleted", "已删除。站内信软删除", "")]
    public Boolean Deleted { get => _Deleted; set { if (OnPropertyChanging("Deleted", value)) { _Deleted = value; OnPropertyChanged("Deleted"); } } }

    private Int32 _Status;
    /// <summary>状态。0待发送/1发送中/2成功/3失败/4跳过</summary>
    [DisplayName("状态")]
    [Description("状态。0待发送/1发送中/2成功/3失败/4跳过")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Status", "状态。0待发送/1发送中/2成功/3失败/4跳过", "")]
    public Int32 Status { get => _Status; set { if (OnPropertyChanging("Status", value)) { _Status = value; OnPropertyChanged("Status"); } } }

    private Int32 _Attempts;
    /// <summary>尝试次数</summary>
    [DisplayName("尝试次数")]
    [Description("尝试次数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Attempts", "尝试次数", "")]
    public Int32 Attempts { get => _Attempts; set { if (OnPropertyChanging("Attempts", value)) { _Attempts = value; OnPropertyChanged("Attempts"); } } }

    private DateTime _NextRetryTime;
    /// <summary>下次重试时间</summary>
    [DisplayName("下次重试时间")]
    [Description("下次重试时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("NextRetryTime", "下次重试时间", "")]
    public DateTime NextRetryTime { get => _NextRetryTime; set { if (OnPropertyChanging("NextRetryTime", value)) { _NextRetryTime = value; OnPropertyChanged("NextRetryTime"); } } }

    private DateTime _SentTime;
    /// <summary>发送完成时间</summary>
    [DisplayName("发送完成时间")]
    [Description("发送完成时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("SentTime", "发送完成时间", "")]
    public DateTime SentTime { get => _SentTime; set { if (OnPropertyChanging("SentTime", value)) { _SentTime = value; OnPropertyChanged("SentTime"); } } }

    private DateTime _ExpireTime;
    /// <summary>过期时间。过期后不再展示与发送</summary>
    [DisplayName("过期时间")]
    [Description("过期时间。过期后不再展示与发送")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("ExpireTime", "过期时间。过期后不再展示与发送", "")]
    public DateTime ExpireTime { get => _ExpireTime; set { if (OnPropertyChanging("ExpireTime", value)) { _ExpireTime = value; OnPropertyChanged("ExpireTime"); } } }

    private Boolean _Success;
    /// <summary>成功</summary>
    [DisplayName("成功")]
    [Description("成功")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Success", "成功", "")]
    public Boolean Success { get => _Success; set { if (OnPropertyChanging("Success", value)) { _Success = value; OnPropertyChanged("Success"); } } }

    private String _Result;
    /// <summary>结果。包含错误信息</summary>
    [DisplayName("结果")]
    [Description("结果。包含错误信息")]
    [DataObjectField(false, false, true, 1000)]
    [BindColumn("Result", "结果。包含错误信息", "")]
    public String Result { get => _Result; set { if (OnPropertyChanging("Result", value)) { _Result = value; OnPropertyChanged("Result"); } } }

    private String _TraceId;
    /// <summary>追踪。链路追踪</summary>
    [Category("扩展")]
    [DisplayName("追踪")]
    [Description("追踪。链路追踪")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("TraceId", "追踪。链路追踪", "")]
    public String TraceId { get => _TraceId; set { if (OnPropertyChanging("TraceId", value)) { _TraceId = value; OnPropertyChanged("TraceId"); } } }

    private String _CreateIP;
    /// <summary>创建地址</summary>
    [Category("扩展")]
    [DisplayName("创建地址")]
    [Description("创建地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateIP", "创建地址", "")]
    public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

    private DateTime _CreateTime;
    /// <summary>创建时间</summary>
    [Category("扩展")]
    [DisplayName("创建时间")]
    [Description("创建时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

    private DateTime _UpdateTime;
    /// <summary>更新时间</summary>
    [Category("扩展")]
    [DisplayName("更新时间")]
    [Description("更新时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("UpdateTime", "更新时间", "")]
    public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

    private String _UpdateIP;
    /// <summary>更新地址</summary>
    [Category("扩展")]
    [DisplayName("更新地址")]
    [Description("更新地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UpdateIP", "更新地址", "")]
    public String UpdateIP { get => _UpdateIP; set { if (OnPropertyChanging("UpdateIP", value)) { _UpdateIP = value; OnPropertyChanged("UpdateIP"); } } }

    private String _Remark;
    /// <summary>备注</summary>
    [Category("扩展")]
    [DisplayName("备注")]
    [Description("备注")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Remark", "备注", "")]
    public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
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

    #region 获取/设置 字段值
    /// <summary>获取/设置 字段值</summary>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public override Object this[String name]
    {
        get => name switch
        {
            "Id" => _Id,
            "TenantId" => _TenantId,
            "BizId" => _BizId,
            "Shared" => _Shared,
            "CancelOnRead" => _CancelOnRead,
            "Action" => _Action,
            "Channel" => _Channel,
            "ConfigId" => _ConfigId,
            "ConfigName" => _ConfigName,
            "Provider" => _Provider,
            "UserId" => _UserId,
            "Target" => _Target,
            "Title" => _Title,
            "Content" => _Content,
            "Category" => _Category,
            "Level" => _Level,
            "Link" => _Link,
            "Data" => _Data,
            "TemplateCode" => _TemplateCode,
            "Language" => _Language,
            "SenderId" => _SenderId,
            "Read" => _Read,
            "ReadTime" => _ReadTime,
            "ReadBy" => _ReadBy,
            "ReadChannel" => _ReadChannel,
            "Handled" => _Handled,
            "HandledBy" => _HandledBy,
            "HandledTime" => _HandledTime,
            "HandleResult" => _HandleResult,
            "Deleted" => _Deleted,
            "Status" => _Status,
            "Attempts" => _Attempts,
            "NextRetryTime" => _NextRetryTime,
            "SentTime" => _SentTime,
            "ExpireTime" => _ExpireTime,
            "Success" => _Success,
            "Result" => _Result,
            "TraceId" => _TraceId,
            "CreateIP" => _CreateIP,
            "CreateTime" => _CreateTime,
            "UpdateTime" => _UpdateTime,
            "UpdateIP" => _UpdateIP,
            "Remark" => _Remark,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToLong(); break;
                case "TenantId": _TenantId = value.ToInt(); break;
                case "BizId": _BizId = Convert.ToString(value); break;
                case "Shared": _Shared = value.ToBoolean(); break;
                case "CancelOnRead": _CancelOnRead = value.ToBoolean(); break;
                case "Action": _Action = Convert.ToString(value); break;
                case "Channel": _Channel = Convert.ToString(value); break;
                case "ConfigId": _ConfigId = value.ToInt(); break;
                case "ConfigName": _ConfigName = Convert.ToString(value); break;
                case "Provider": _Provider = Convert.ToString(value); break;
                case "UserId": _UserId = value.ToInt(); break;
                case "Target": _Target = Convert.ToString(value); break;
                case "Title": _Title = Convert.ToString(value); break;
                case "Content": _Content = Convert.ToString(value); break;
                case "Category": _Category = Convert.ToString(value); break;
                case "Level": _Level = Convert.ToString(value); break;
                case "Link": _Link = Convert.ToString(value); break;
                case "Data": _Data = Convert.ToString(value); break;
                case "TemplateCode": _TemplateCode = Convert.ToString(value); break;
                case "Language": _Language = Convert.ToString(value); break;
                case "SenderId": _SenderId = value.ToInt(); break;
                case "Read": _Read = value.ToBoolean(); break;
                case "ReadTime": _ReadTime = value.ToDateTime(); break;
                case "ReadBy": _ReadBy = value.ToInt(); break;
                case "ReadChannel": _ReadChannel = Convert.ToString(value); break;
                case "Handled": _Handled = value.ToBoolean(); break;
                case "HandledBy": _HandledBy = value.ToInt(); break;
                case "HandledTime": _HandledTime = value.ToDateTime(); break;
                case "HandleResult": _HandleResult = Convert.ToString(value); break;
                case "Deleted": _Deleted = value.ToBoolean(); break;
                case "Status": _Status = value.ToInt(); break;
                case "Attempts": _Attempts = value.ToInt(); break;
                case "NextRetryTime": _NextRetryTime = value.ToDateTime(); break;
                case "SentTime": _SentTime = value.ToDateTime(); break;
                case "ExpireTime": _ExpireTime = value.ToDateTime(); break;
                case "Success": _Success = value.ToBoolean(); break;
                case "Result": _Result = Convert.ToString(value); break;
                case "TraceId": _TraceId = Convert.ToString(value); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                case "Remark": _Remark = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion

    #region 关联映射
    #endregion

    #region 扩展查询
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static NotificationRecord FindById(Int64 id)
    {
        if (id < 0) return null;

        return Find(_.Id == id);
    }

    /// <summary>根据租户、渠道查找</summary>
    /// <param name="tenantId">租户</param>
    /// <param name="channel">渠道</param>
    /// <returns>实体列表</returns>
    public static IList<NotificationRecord> FindAllByTenantIdAndChannel(Int32 tenantId, String channel)
    {
        if (tenantId < 0) return [];
        if (channel.IsNullOrEmpty()) return [];

        return FindAll(_.TenantId == tenantId & _.Channel == channel);
    }

    /// <summary>根据租户、业务消息、渠道查找</summary>
    /// <param name="tenantId">租户</param>
    /// <param name="bizId">业务消息</param>
    /// <param name="channel">渠道</param>
    /// <returns>实体列表</returns>
    public static IList<NotificationRecord> FindAllByTenantIdAndBizIdAndChannel(Int32 tenantId, String bizId, String channel)
    {
        if (tenantId < 0) return [];
        if (bizId.IsNullOrEmpty()) return [];
        if (channel.IsNullOrEmpty()) return [];

        return FindAll(_.TenantId == tenantId & _.BizId == bizId & _.Channel == channel);
    }

    /// <summary>根据租户查找</summary>
    /// <param name="tenantId">租户</param>
    /// <returns>实体列表</returns>
    public static IList<NotificationRecord> FindAllByTenantId(Int32 tenantId)
    {
        if (tenantId < 0) return [];

        return FindAll(_.TenantId == tenantId);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="tenantId">租户</param>
    /// <param name="bizId">业务消息。同一BizId共享已读/处理状态</param>
    /// <param name="channel">渠道。InApp/Sms/Mail/AppPush/Webhook/DingTalk/WeCom等</param>
    /// <param name="userId">用户。站内信/已知用户</param>
    /// <param name="read">已读。站内信/弹窗</param>
    /// <param name="deleted">已删除。站内信软删除</param>
    /// <param name="status">状态。0待发送/1发送中/2成功/3失败/4跳过</param>
    /// <param name="nextRetryTime">下次重试时间</param>
    /// <param name="shared">共享读状态。团队任一收件人已读即全员已读</param>
    /// <param name="cancelOnRead">已读后停止后续渠道。true 表示任一渠道已读即取消未发送渠道</param>
    /// <param name="handled">已处理。业务确认，不等于已读</param>
    /// <param name="success">成功</param>
    /// <param name="start">编号开始</param>
    /// <param name="end">编号结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<NotificationRecord> Search(Int32 tenantId, String bizId, String channel, Int32 userId, Boolean? read, Boolean? deleted, Int32 status, DateTime nextRetryTime, Boolean? shared, Boolean? cancelOnRead, Boolean? handled, Boolean? success, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (tenantId >= 0) exp &= _.TenantId == tenantId;
        if (!bizId.IsNullOrEmpty()) exp &= _.BizId == bizId;
        if (!channel.IsNullOrEmpty()) exp &= _.Channel == channel;
        if (userId >= 0) exp &= _.UserId == userId;
        if (read != null) exp &= _.Read == read;
        if (deleted != null) exp &= _.Deleted == deleted;
        if (status >= 0) exp &= _.Status == status;
        if (shared != null) exp &= _.Shared == shared;
        if (cancelOnRead != null) exp &= _.CancelOnRead == cancelOnRead;
        if (handled != null) exp &= _.Handled == handled;
        if (success != null) exp &= _.Success == success;
        exp &= _.Id.Between(start, end, Meta.Factory.Snow);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 数据清理
    /// <summary>清理指定时间段内的数据</summary>
    /// <param name="start">开始时间。未指定时清理小于指定时间的所有数据</param>
    /// <param name="end">结束时间</param>
    /// <param name="maximumRows">最大删除行数。清理历史数据时，避免一次性删除过多导致数据库IO跟不上，0表示所有</param>
    /// <returns>清理行数</returns>
    public static Int32 DeleteWith(DateTime start, DateTime end, Int32 maximumRows = 0)
    {
        return Delete(_.Id.Between(start, end, Meta.Factory.Snow), maximumRows);
    }
    #endregion

    #region 字段名
    /// <summary>取得通知记录字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>租户</summary>
        public static readonly Field TenantId = FindByName("TenantId");

        /// <summary>业务消息。同一BizId共享已读/处理状态</summary>
        public static readonly Field BizId = FindByName("BizId");

        /// <summary>共享读状态。团队任一收件人已读即全员已读</summary>
        public static readonly Field Shared = FindByName("Shared");

        /// <summary>已读后停止后续渠道。true 表示任一渠道已读即取消未发送渠道</summary>
        public static readonly Field CancelOnRead = FindByName("CancelOnRead");

        /// <summary>操作。Notify/Alert等</summary>
        public static readonly Field Action = FindByName("Action");

        /// <summary>渠道。InApp/Sms/Mail/AppPush/Webhook/DingTalk/WeCom等</summary>
        public static readonly Field Channel = FindByName("Channel");

        /// <summary>渠道编号</summary>
        public static readonly Field ConfigId = FindByName("ConfigId");

        /// <summary>渠道配置</summary>
        public static readonly Field ConfigName = FindByName("ConfigName");

        /// <summary>提供者。渠道实现标识</summary>
        public static readonly Field Provider = FindByName("Provider");

        /// <summary>用户。站内信/已知用户</summary>
        public static readonly Field UserId = FindByName("UserId");

        /// <summary>目标。手机号/邮箱/openid/机器人地址等</summary>
        public static readonly Field Target = FindByName("Target");

        /// <summary>标题。站内信/邮件主题</summary>
        public static readonly Field Title = FindByName("Title");

        /// <summary>内容。站内信正文/邮件正文，支持长文本</summary>
        public static readonly Field Content = FindByName("Content");

        /// <summary>分类。System/Business/Security/Marketing/Todo等</summary>
        public static readonly Field Category = FindByName("Category");

        /// <summary>级别。Info/Success/Warning/Error/Urgent</summary>
        public static readonly Field Level = FindByName("Level");

        /// <summary>跳转。点击通知打开的前端地址</summary>
        public static readonly Field Link = FindByName("Link");

        /// <summary>数据。JSON业务载荷，供模板渲染与跳转</summary>
        public static readonly Field Data = FindByName("Data");

        /// <summary>模板。通知模板编码</summary>
        public static readonly Field TemplateCode = FindByName("TemplateCode");

        /// <summary>语言。渲染时使用的语言</summary>
        public static readonly Field Language = FindByName("Language");

        /// <summary>发送人。0表示系统</summary>
        public static readonly Field SenderId = FindByName("SenderId");

        /// <summary>已读。站内信/弹窗</summary>
        public static readonly Field Read = FindByName("Read");

        /// <summary>已读时间</summary>
        public static readonly Field ReadTime = FindByName("ReadTime");

        /// <summary>已读人。实际触发已读的用户</summary>
        public static readonly Field ReadBy = FindByName("ReadBy");

        /// <summary>已读渠道。InApp/AppPush/Sms/Mail/Batch</summary>
        public static readonly Field ReadChannel = FindByName("ReadChannel");

        /// <summary>已处理。业务确认，不等于已读</summary>
        public static readonly Field Handled = FindByName("Handled");

        /// <summary>处理人</summary>
        public static readonly Field HandledBy = FindByName("HandledBy");

        /// <summary>处理时间</summary>
        public static readonly Field HandledTime = FindByName("HandledTime");

        /// <summary>处理结果</summary>
        public static readonly Field HandleResult = FindByName("HandleResult");

        /// <summary>已删除。站内信软删除</summary>
        public static readonly Field Deleted = FindByName("Deleted");

        /// <summary>状态。0待发送/1发送中/2成功/3失败/4跳过</summary>
        public static readonly Field Status = FindByName("Status");

        /// <summary>尝试次数</summary>
        public static readonly Field Attempts = FindByName("Attempts");

        /// <summary>下次重试时间</summary>
        public static readonly Field NextRetryTime = FindByName("NextRetryTime");

        /// <summary>发送完成时间</summary>
        public static readonly Field SentTime = FindByName("SentTime");

        /// <summary>过期时间。过期后不再展示与发送</summary>
        public static readonly Field ExpireTime = FindByName("ExpireTime");

        /// <summary>成功</summary>
        public static readonly Field Success = FindByName("Success");

        /// <summary>结果。包含错误信息</summary>
        public static readonly Field Result = FindByName("Result");

        /// <summary>追踪。链路追踪</summary>
        public static readonly Field TraceId = FindByName("TraceId");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        /// <summary>备注</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得通知记录字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>租户</summary>
        public const String TenantId = "TenantId";

        /// <summary>业务消息。同一BizId共享已读/处理状态</summary>
        public const String BizId = "BizId";

        /// <summary>共享读状态。团队任一收件人已读即全员已读</summary>
        public const String Shared = "Shared";

        /// <summary>已读后停止后续渠道。true 表示任一渠道已读即取消未发送渠道</summary>
        public const String CancelOnRead = "CancelOnRead";

        /// <summary>操作。Notify/Alert等</summary>
        public const String Action = "Action";

        /// <summary>渠道。InApp/Sms/Mail/AppPush/Webhook/DingTalk/WeCom等</summary>
        public const String Channel = "Channel";

        /// <summary>渠道编号</summary>
        public const String ConfigId = "ConfigId";

        /// <summary>渠道配置</summary>
        public const String ConfigName = "ConfigName";

        /// <summary>提供者。渠道实现标识</summary>
        public const String Provider = "Provider";

        /// <summary>用户。站内信/已知用户</summary>
        public const String UserId = "UserId";

        /// <summary>目标。手机号/邮箱/openid/机器人地址等</summary>
        public const String Target = "Target";

        /// <summary>标题。站内信/邮件主题</summary>
        public const String Title = "Title";

        /// <summary>内容。站内信正文/邮件正文，支持长文本</summary>
        public const String Content = "Content";

        /// <summary>分类。System/Business/Security/Marketing/Todo等</summary>
        public const String Category = "Category";

        /// <summary>级别。Info/Success/Warning/Error/Urgent</summary>
        public const String Level = "Level";

        /// <summary>跳转。点击通知打开的前端地址</summary>
        public const String Link = "Link";

        /// <summary>数据。JSON业务载荷，供模板渲染与跳转</summary>
        public const String Data = "Data";

        /// <summary>模板。通知模板编码</summary>
        public const String TemplateCode = "TemplateCode";

        /// <summary>语言。渲染时使用的语言</summary>
        public const String Language = "Language";

        /// <summary>发送人。0表示系统</summary>
        public const String SenderId = "SenderId";

        /// <summary>已读。站内信/弹窗</summary>
        public const String Read = "Read";

        /// <summary>已读时间</summary>
        public const String ReadTime = "ReadTime";

        /// <summary>已读人。实际触发已读的用户</summary>
        public const String ReadBy = "ReadBy";

        /// <summary>已读渠道。InApp/AppPush/Sms/Mail/Batch</summary>
        public const String ReadChannel = "ReadChannel";

        /// <summary>已处理。业务确认，不等于已读</summary>
        public const String Handled = "Handled";

        /// <summary>处理人</summary>
        public const String HandledBy = "HandledBy";

        /// <summary>处理时间</summary>
        public const String HandledTime = "HandledTime";

        /// <summary>处理结果</summary>
        public const String HandleResult = "HandleResult";

        /// <summary>已删除。站内信软删除</summary>
        public const String Deleted = "Deleted";

        /// <summary>状态。0待发送/1发送中/2成功/3失败/4跳过</summary>
        public const String Status = "Status";

        /// <summary>尝试次数</summary>
        public const String Attempts = "Attempts";

        /// <summary>下次重试时间</summary>
        public const String NextRetryTime = "NextRetryTime";

        /// <summary>发送完成时间</summary>
        public const String SentTime = "SentTime";

        /// <summary>过期时间。过期后不再展示与发送</summary>
        public const String ExpireTime = "ExpireTime";

        /// <summary>成功</summary>
        public const String Success = "Success";

        /// <summary>结果。包含错误信息</summary>
        public const String Result = "Result";

        /// <summary>追踪。链路追踪</summary>
        public const String TraceId = "TraceId";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";

        /// <summary>备注</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
