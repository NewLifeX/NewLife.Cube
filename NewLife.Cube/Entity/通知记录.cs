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

/// <summary>通知记录。站内信/短信通知/邮件通知/公众号通知/钉钉机器人/企业微信机器人等</summary>
[Serializable]
[DataObject]
[Description("通知记录。站内信/短信通知/邮件通知/公众号通知/钉钉机器人/企业微信机器人等")]
[BindIndex("IX_NotificationRecord_TenantId_Channel_UserId_Target_Id", false, "TenantId,Channel,UserId,Target,Id")]
[BindIndex("IX_NotificationRecord_TenantId_Channel_Id", false, "TenantId,Channel,Id")]
[BindIndex("IX_NotificationRecord_Channel_Target_Id", false, "Channel,Target,Id")]
[BindIndex("IX_NotificationRecord_UserId_Read_Id", false, "UserId,Read,Id")]
[BindTable("NotificationRecord", Description = "通知记录。站内信/短信通知/邮件通知/公众号通知/钉钉机器人/企业微信机器人等", ConnName = "Log", DbType = DatabaseType.None)]
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

    private String _Action;
    /// <summary>操作。Notify/Alert等</summary>
    [DisplayName("操作")]
    [Description("操作。Notify/Alert等")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Action", "操作。Notify/Alert等", "")]
    public String Action { get => _Action; set { if (OnPropertyChanging("Action", value)) { _Action = value; OnPropertyChanged("Action"); } } }

    private String _Channel;
    /// <summary>渠道。InApp/Sms/Mail/WxMp/DingTalk/WeCom等</summary>
    [DisplayName("渠道")]
    [Description("渠道。InApp/Sms/Mail/WxMp/DingTalk/WeCom等")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Channel", "渠道。InApp/Sms/Mail/WxMp/DingTalk/WeCom等", "")]
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
    [DataObjectField(false, false, true, 200)]
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
    /// <summary>内容</summary>
    [DisplayName("内容")]
    [Description("内容")]
    [DataObjectField(false, false, true, 2000)]
    [BindColumn("Content", "内容", "")]
    public String Content { get => _Content; set { if (OnPropertyChanging("Content", value)) { _Content = value; OnPropertyChanged("Content"); } } }

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
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Result", "结果。包含错误信息", "")]
    public String Result { get => _Result; set { if (OnPropertyChanging("Result", value)) { _Result = value; OnPropertyChanged("Result"); } } }

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
            "Action" => _Action,
            "Channel" => _Channel,
            "ConfigId" => _ConfigId,
            "ConfigName" => _ConfigName,
            "Provider" => _Provider,
            "UserId" => _UserId,
            "Target" => _Target,
            "Title" => _Title,
            "Content" => _Content,
            "Success" => _Success,
            "Result" => _Result,
            "Read" => _Read,
            "ReadTime" => _ReadTime,
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
                case "Action": _Action = Convert.ToString(value); break;
                case "Channel": _Channel = Convert.ToString(value); break;
                case "ConfigId": _ConfigId = value.ToInt(); break;
                case "ConfigName": _ConfigName = Convert.ToString(value); break;
                case "Provider": _Provider = Convert.ToString(value); break;
                case "UserId": _UserId = value.ToInt(); break;
                case "Target": _Target = Convert.ToString(value); break;
                case "Title": _Title = Convert.ToString(value); break;
                case "Content": _Content = Convert.ToString(value); break;
                case "Success": _Success = value.ToBoolean(); break;
                case "Result": _Result = Convert.ToString(value); break;
                case "Read": _Read = value.ToBoolean(); break;
                case "ReadTime": _ReadTime = value.ToDateTime(); break;
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

    /// <summary>根据渠道、目标查找</summary>
    /// <param name="channel">渠道</param>
    /// <param name="target">目标</param>
    /// <returns>实体列表</returns>
    public static IList<NotificationRecord> FindAllByChannelAndTarget(String channel, String target)
    {
        if (channel.IsNullOrEmpty()) return [];
        if (target.IsNullOrEmpty()) return [];

        return FindAll(_.Channel == channel & _.Target == target);
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
    /// <param name="channel">渠道。InApp/Sms/Mail/WxMp/DingTalk/WeCom等</param>
    /// <param name="userId">用户。站内信/已知用户</param>
    /// <param name="target">目标。手机号/邮箱/openid/机器人地址等</param>
    /// <param name="read">已读。站内信/弹窗</param>
    /// <param name="success">成功</param>
    /// <param name="start">编号开始</param>
    /// <param name="end">编号结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<NotificationRecord> Search(Int32 tenantId, String channel, Int32 userId, String target, Boolean? read, Boolean? success, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (tenantId >= 0) exp &= _.TenantId == tenantId;
        if (!channel.IsNullOrEmpty()) exp &= _.Channel == channel;
        if (userId >= 0) exp &= _.UserId == userId;
        if (!target.IsNullOrEmpty()) exp &= _.Target == target;
        if (read != null) exp &= _.Read == read;
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

        /// <summary>操作。Notify/Alert等</summary>
        public static readonly Field Action = FindByName("Action");

        /// <summary>渠道。InApp/Sms/Mail/WxMp/DingTalk/WeCom等</summary>
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

        /// <summary>内容</summary>
        public static readonly Field Content = FindByName("Content");

        /// <summary>成功</summary>
        public static readonly Field Success = FindByName("Success");

        /// <summary>结果。包含错误信息</summary>
        public static readonly Field Result = FindByName("Result");

        /// <summary>已读。站内信/弹窗</summary>
        public static readonly Field Read = FindByName("Read");

        /// <summary>已读时间</summary>
        public static readonly Field ReadTime = FindByName("ReadTime");

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

        /// <summary>操作。Notify/Alert等</summary>
        public const String Action = "Action";

        /// <summary>渠道。InApp/Sms/Mail/WxMp/DingTalk/WeCom等</summary>
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

        /// <summary>内容</summary>
        public const String Content = "Content";

        /// <summary>成功</summary>
        public const String Success = "Success";

        /// <summary>结果。包含错误信息</summary>
        public const String Result = "Result";

        /// <summary>已读。站内信/弹窗</summary>
        public const String Read = "Read";

        /// <summary>已读时间</summary>
        public const String ReadTime = "ReadTime";

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
