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

/// <summary>邮件配置。邮件渠道配置，支持多租户多渠道</summary>
[Serializable]
[DataObject]
[Description("邮件配置。邮件渠道配置，支持多租户多渠道")]
[BindIndex("IU_MailConfig_TenantId_Name", true, "TenantId,Name")]
[BindIndex("IX_MailConfig_TenantId_Priority", false, "TenantId,Priority")]
[BindTable("MailConfig", Description = "邮件配置。邮件渠道配置，支持多租户多渠道", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class MailConfig : IEntity<MailConfigModel>
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int32 _TenantId;
    /// <summary>租户</summary>
    [DisplayName("租户")]
    [Description("租户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("TenantId", "租户", "")]
    public Int32 TenantId { get => _TenantId; set { if (OnPropertyChanging("TenantId", value)) { _TenantId = value; OnPropertyChanged("TenantId"); } } }

    private String _Provider;
    /// <summary>提供者。对应具体实现类标识，用于区分同类型多渠道</summary>
    [DisplayName("提供者")]
    [Description("提供者。对应具体实现类标识，用于区分同类型多渠道")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Provider", "提供者。对应具体实现类标识，用于区分同类型多渠道", "")]
    public String Provider { get => _Provider; set { if (OnPropertyChanging("Provider", value)) { _Provider = value; OnPropertyChanged("Provider"); } } }

    private String _Name;
    /// <summary>名称。渠道名称</summary>
    [DisplayName("名称")]
    [Description("名称。渠道名称")]
    [DataObjectField(false, false, false, 50)]
    [BindColumn("Name", "名称。渠道名称", "", Master = true)]
    public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

    private String _DisplayName;
    /// <summary>显示名</summary>
    [DisplayName("显示名")]
    [Description("显示名")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("DisplayName", "显示名", "")]
    public String DisplayName { get => _DisplayName; set { if (OnPropertyChanging("DisplayName", value)) { _DisplayName = value; OnPropertyChanged("DisplayName"); } } }

    private String _Server;
    /// <summary>服务器。SMTP服务器地址</summary>
    [DisplayName("服务器")]
    [Description("服务器。SMTP服务器地址")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Server", "服务器。SMTP服务器地址", "")]
    public String Server { get => _Server; set { if (OnPropertyChanging("Server", value)) { _Server = value; OnPropertyChanged("Server"); } } }

    private Int32 _Port;
    /// <summary>端口。SMTP端口，默认25，SSL465/587</summary>
    [DisplayName("端口")]
    [Description("端口。SMTP端口，默认25，SSL465/587")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Port", "端口。SMTP端口，默认25，SSL465/587", "")]
    public Int32 Port { get => _Port; set { if (OnPropertyChanging("Port", value)) { _Port = value; OnPropertyChanged("Port"); } } }

    private Boolean _EnableSsl;
    /// <summary>启用SSL。是否使用SSL加密连接</summary>
    [DisplayName("启用SSL")]
    [Description("启用SSL。是否使用SSL加密连接")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("EnableSsl", "启用SSL。是否使用SSL加密连接", "")]
    public Boolean EnableSsl { get => _EnableSsl; set { if (OnPropertyChanging("EnableSsl", value)) { _EnableSsl = value; OnPropertyChanged("EnableSsl"); } } }

    private String _UserName;
    /// <summary>用户名。SMTP登录账号</summary>
    [DisplayName("用户名")]
    [Description("用户名。SMTP登录账号")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UserName", "用户名。SMTP登录账号", "")]
    public String UserName { get => _UserName; set { if (OnPropertyChanging("UserName", value)) { _UserName = value; OnPropertyChanged("UserName"); } } }

    private String _Password;
    /// <summary>密码。SMTP登录密码或授权码</summary>
    [DisplayName("密码")]
    [Description("密码。SMTP登录密码或授权码")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Password", "密码。SMTP登录密码或授权码", "", ShowIn = "Auto,-List")]
    public String Password { get => _Password; set { if (OnPropertyChanging("Password", value)) { _Password = value; OnPropertyChanged("Password"); } } }

    private String _FromMail;
    /// <summary>发件人邮箱。发送邮件时的发件人地址</summary>
    [DisplayName("发件人邮箱")]
    [Description("发件人邮箱。发送邮件时的发件人地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("FromMail", "发件人邮箱。发送邮件时的发件人地址", "")]
    public String FromMail { get => _FromMail; set { if (OnPropertyChanging("FromMail", value)) { _FromMail = value; OnPropertyChanged("FromMail"); } } }

    private String _FromName;
    /// <summary>发件人名称。发送邮件时显示的发件人名称</summary>
    [DisplayName("发件人名称")]
    [Description("发件人名称。发送邮件时显示的发件人名称")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("FromName", "发件人名称。发送邮件时显示的发件人名称", "")]
    public String FromName { get => _FromName; set { if (OnPropertyChanging("FromName", value)) { _FromName = value; OnPropertyChanged("FromName"); } } }

    private Int32 _CodeLength;
    /// <summary>验证码长度。默认6位</summary>
    [DisplayName("验证码长度")]
    [Description("验证码长度。默认6位")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CodeLength", "验证码长度。默认6位", "")]
    public Int32 CodeLength { get => _CodeLength; set { if (OnPropertyChanging("CodeLength", value)) { _CodeLength = value; OnPropertyChanged("CodeLength"); } } }

    private Int32 _Expire;
    /// <summary>有效期。验证码有效期，单位秒，默认600秒(10分钟)</summary>
    [DisplayName("有效期")]
    [Description("有效期。验证码有效期，单位秒，默认600秒(10分钟)")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Expire", "有效期。验证码有效期，单位秒，默认600秒(10分钟)", "")]
    public Int32 Expire { get => _Expire; set { if (OnPropertyChanging("Expire", value)) { _Expire = value; OnPropertyChanged("Expire"); } } }

    private Boolean _Enable;
    /// <summary>启用</summary>
    [DisplayName("启用")]
    [Description("启用")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Enable", "启用", "")]
    public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

    private Int32 _Priority;
    /// <summary>优先级。较大优先</summary>
    [DisplayName("优先级")]
    [Description("优先级。较大优先")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Priority", "优先级。较大优先", "")]
    public Int32 Priority { get => _Priority; set { if (OnPropertyChanging("Priority", value)) { _Priority = value; OnPropertyChanged("Priority"); } } }

    private Int32 _CreateUserID;
    /// <summary>创建者</summary>
    [Category("扩展")]
    [DisplayName("创建者")]
    [Description("创建者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CreateUserID", "创建者", "")]
    public Int32 CreateUserID { get => _CreateUserID; set { if (OnPropertyChanging("CreateUserID", value)) { _CreateUserID = value; OnPropertyChanged("CreateUserID"); } } }

    private DateTime _CreateTime;
    /// <summary>创建时间</summary>
    [Category("扩展")]
    [DisplayName("创建时间")]
    [Description("创建时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

    private String _CreateIP;
    /// <summary>创建地址</summary>
    [Category("扩展")]
    [DisplayName("创建地址")]
    [Description("创建地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateIP", "创建地址", "")]
    public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

    private Int32 _UpdateUserID;
    /// <summary>更新者</summary>
    [Category("扩展")]
    [DisplayName("更新者")]
    [Description("更新者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UpdateUserID", "更新者", "")]
    public Int32 UpdateUserID { get => _UpdateUserID; set { if (OnPropertyChanging("UpdateUserID", value)) { _UpdateUserID = value; OnPropertyChanged("UpdateUserID"); } } }

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
    public void Copy(MailConfigModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        Provider = model.Provider;
        Name = model.Name;
        DisplayName = model.DisplayName;
        Server = model.Server;
        Port = model.Port;
        EnableSsl = model.EnableSsl;
        UserName = model.UserName;
        Password = model.Password;
        FromMail = model.FromMail;
        FromName = model.FromName;
        CodeLength = model.CodeLength;
        Expire = model.Expire;
        Enable = model.Enable;
        Priority = model.Priority;
        CreateUserID = model.CreateUserID;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserID = model.UpdateUserID;
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
            "Provider" => _Provider,
            "Name" => _Name,
            "DisplayName" => _DisplayName,
            "Server" => _Server,
            "Port" => _Port,
            "EnableSsl" => _EnableSsl,
            "UserName" => _UserName,
            "Password" => _Password,
            "FromMail" => _FromMail,
            "FromName" => _FromName,
            "CodeLength" => _CodeLength,
            "Expire" => _Expire,
            "Enable" => _Enable,
            "Priority" => _Priority,
            "CreateUserID" => _CreateUserID,
            "CreateTime" => _CreateTime,
            "CreateIP" => _CreateIP,
            "UpdateUserID" => _UpdateUserID,
            "UpdateTime" => _UpdateTime,
            "UpdateIP" => _UpdateIP,
            "Remark" => _Remark,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToInt(); break;
                case "TenantId": _TenantId = value.ToInt(); break;
                case "Provider": _Provider = Convert.ToString(value); break;
                case "Name": _Name = Convert.ToString(value); break;
                case "DisplayName": _DisplayName = Convert.ToString(value); break;
                case "Server": _Server = Convert.ToString(value); break;
                case "Port": _Port = value.ToInt(); break;
                case "EnableSsl": _EnableSsl = value.ToBoolean(); break;
                case "UserName": _UserName = Convert.ToString(value); break;
                case "Password": _Password = Convert.ToString(value); break;
                case "FromMail": _FromMail = Convert.ToString(value); break;
                case "FromName": _FromName = Convert.ToString(value); break;
                case "CodeLength": _CodeLength = value.ToInt(); break;
                case "Expire": _Expire = value.ToInt(); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "Priority": _Priority = value.ToInt(); break;
                case "CreateUserID": _CreateUserID = value.ToInt(); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "UpdateUserID": _UpdateUserID = value.ToInt(); break;
                case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                case "Remark": _Remark = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion

    #region 关联映射
    /// <summary>租户</summary>
    [XmlIgnore, IgnoreDataMember, ScriptIgnore]
    public XCode.Membership.Tenant Tenant => Extends.Get(nameof(Tenant), k => XCode.Membership.Tenant.FindById(TenantId));

    /// <summary>租户</summary>
    [Map(nameof(TenantId), typeof(XCode.Membership.Tenant), "Id")]
    public String TenantName => Tenant?.ToString();

    #endregion

    #region 扩展查询
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static MailConfig FindById(Int32 id)
    {
        if (id < 0) return null;

        // 实体缓存
        if (Meta.Session.Count < MaxCacheCount) return Meta.Cache.Find(e => e.Id == id);

        // 单对象缓存
        return Meta.SingleCache[id];

        //return Find(_.Id == id);
    }

    /// <summary>根据租户、名称查找</summary>
    /// <param name="tenantId">租户</param>
    /// <param name="name">名称</param>
    /// <returns>实体对象</returns>
    public static MailConfig FindByTenantIdAndName(Int32 tenantId, String name)
    {
        if (tenantId < 0) return null;
        if (name.IsNullOrEmpty()) return null;

        // 实体缓存
        if (Meta.Session.Count < MaxCacheCount) return Meta.Cache.Find(e => e.TenantId == tenantId && e.Name.EqualIgnoreCase(name));

        return Find(_.TenantId == tenantId & _.Name == name);
    }

    /// <summary>根据租户查找</summary>
    /// <param name="tenantId">租户</param>
    /// <returns>实体列表</returns>
    public static IList<MailConfig> FindAllByTenantId(Int32 tenantId)
    {
        if (tenantId < 0) return [];

        // 实体缓存
        if (Meta.Session.Count < MaxCacheCount) return Meta.Cache.FindAll(e => e.TenantId == tenantId);

        return FindAll(_.TenantId == tenantId);
    }

    /// <summary>根据租户、优先级查找</summary>
    /// <param name="tenantId">租户</param>
    /// <param name="priority">优先级</param>
    /// <returns>实体列表</returns>
    public static IList<MailConfig> FindAllByTenantIdAndPriority(Int32 tenantId, Int32 priority)
    {
        if (tenantId < 0) return [];
        if (priority < 0) return [];

        // 实体缓存
        if (Meta.Session.Count < MaxCacheCount) return Meta.Cache.FindAll(e => e.TenantId == tenantId && e.Priority == priority);

        return FindAll(_.TenantId == tenantId & _.Priority == priority);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="tenantId">租户</param>
    /// <param name="priority">优先级。较大优先</param>
    /// <param name="enableSsl">启用SSL。是否使用SSL加密连接</param>
    /// <param name="enable">启用</param>
    /// <param name="start">更新时间开始</param>
    /// <param name="end">更新时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<MailConfig> Search(Int32 tenantId, Int32 priority, Boolean? enableSsl, Boolean? enable, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (tenantId >= 0) exp &= _.TenantId == tenantId;
        if (priority >= 0) exp &= _.Priority == priority;
        if (enableSsl != null) exp &= _.EnableSsl == enableSsl;
        if (enable != null) exp &= _.Enable == enable;
        exp &= _.UpdateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 字段名
    /// <summary>取得邮件配置字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>租户</summary>
        public static readonly Field TenantId = FindByName("TenantId");

        /// <summary>提供者。对应具体实现类标识，用于区分同类型多渠道</summary>
        public static readonly Field Provider = FindByName("Provider");

        /// <summary>名称。渠道名称</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>显示名</summary>
        public static readonly Field DisplayName = FindByName("DisplayName");

        /// <summary>服务器。SMTP服务器地址</summary>
        public static readonly Field Server = FindByName("Server");

        /// <summary>端口。SMTP端口，默认25，SSL465/587</summary>
        public static readonly Field Port = FindByName("Port");

        /// <summary>启用SSL。是否使用SSL加密连接</summary>
        public static readonly Field EnableSsl = FindByName("EnableSsl");

        /// <summary>用户名。SMTP登录账号</summary>
        public static readonly Field UserName = FindByName("UserName");

        /// <summary>密码。SMTP登录密码或授权码</summary>
        public static readonly Field Password = FindByName("Password");

        /// <summary>发件人邮箱。发送邮件时的发件人地址</summary>
        public static readonly Field FromMail = FindByName("FromMail");

        /// <summary>发件人名称。发送邮件时显示的发件人名称</summary>
        public static readonly Field FromName = FindByName("FromName");

        /// <summary>验证码长度。默认6位</summary>
        public static readonly Field CodeLength = FindByName("CodeLength");

        /// <summary>有效期。验证码有效期，单位秒，默认600秒(10分钟)</summary>
        public static readonly Field Expire = FindByName("Expire");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>优先级。较大优先</summary>
        public static readonly Field Priority = FindByName("Priority");

        /// <summary>创建者</summary>
        public static readonly Field CreateUserID = FindByName("CreateUserID");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>更新者</summary>
        public static readonly Field UpdateUserID = FindByName("UpdateUserID");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        /// <summary>备注</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得邮件配置字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>租户</summary>
        public const String TenantId = "TenantId";

        /// <summary>提供者。对应具体实现类标识，用于区分同类型多渠道</summary>
        public const String Provider = "Provider";

        /// <summary>名称。渠道名称</summary>
        public const String Name = "Name";

        /// <summary>显示名</summary>
        public const String DisplayName = "DisplayName";

        /// <summary>服务器。SMTP服务器地址</summary>
        public const String Server = "Server";

        /// <summary>端口。SMTP端口，默认25，SSL465/587</summary>
        public const String Port = "Port";

        /// <summary>启用SSL。是否使用SSL加密连接</summary>
        public const String EnableSsl = "EnableSsl";

        /// <summary>用户名。SMTP登录账号</summary>
        public const String UserName = "UserName";

        /// <summary>密码。SMTP登录密码或授权码</summary>
        public const String Password = "Password";

        /// <summary>发件人邮箱。发送邮件时的发件人地址</summary>
        public const String FromMail = "FromMail";

        /// <summary>发件人名称。发送邮件时显示的发件人名称</summary>
        public const String FromName = "FromName";

        /// <summary>验证码长度。默认6位</summary>
        public const String CodeLength = "CodeLength";

        /// <summary>有效期。验证码有效期，单位秒，默认600秒(10分钟)</summary>
        public const String Expire = "Expire";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>优先级。较大优先</summary>
        public const String Priority = "Priority";

        /// <summary>创建者</summary>
        public const String CreateUserID = "CreateUserID";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>更新者</summary>
        public const String UpdateUserID = "UpdateUserID";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";

        /// <summary>备注</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
