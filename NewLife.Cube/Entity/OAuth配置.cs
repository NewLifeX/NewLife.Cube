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

/// <summary>OAuth配置。需要连接的OAuth认证方</summary>
[Serializable]
[DataObject]
[Description("OAuth配置。需要连接的OAuth认证方")]
[BindIndex("IU_OAuthConfig_Name", true, "Name")]
[BindTable("OAuthConfig", Description = "OAuth配置。需要连接的OAuth认证方", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class OAuthConfig
{
    #region 属性
    private Int32 _ID;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("ID", "编号", "")]
    public Int32 ID { get => _ID; set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } } }

    private String _Name;
    /// <summary>名称。提供者名称</summary>
    [DisplayName("名称")]
    [Description("名称。提供者名称")]
    [DataObjectField(false, false, false, 50)]
    [BindColumn("Name", "名称。提供者名称", "", Master = true)]
    public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

    private String _NickName;
    /// <summary>昵称</summary>
    [DisplayName("昵称")]
    [Description("昵称")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("NickName", "昵称", "")]
    public String NickName { get => _NickName; set { if (OnPropertyChanging("NickName", value)) { _NickName = value; OnPropertyChanged("NickName"); } } }

    private String _Logo;
    /// <summary>图标</summary>
    [DisplayName("图标")]
    [Description("图标")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Logo", "图标", "")]
    public String Logo { get => _Logo; set { if (OnPropertyChanging("Logo", value)) { _Logo = value; OnPropertyChanged("Logo"); } } }

    private String _AppId;
    /// <summary>应用标识</summary>
    [DisplayName("应用标识")]
    [Description("应用标识")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("AppId", "应用标识", "")]
    public String AppId { get => _AppId; set { if (OnPropertyChanging("AppId", value)) { _AppId = value; OnPropertyChanged("AppId"); } } }

    private String _Secret;
    /// <summary>应用密钥</summary>
    [DisplayName("应用密钥")]
    [Description("应用密钥")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Secret", "应用密钥", "")]
    public String Secret { get => _Secret; set { if (OnPropertyChanging("Secret", value)) { _Secret = value; OnPropertyChanged("Secret"); } } }

    private String _Server;
    /// <summary>服务地址</summary>
    [DisplayName("服务地址")]
    [Description("服务地址")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Server", "服务地址", "")]
    public String Server { get => _Server; set { if (OnPropertyChanging("Server", value)) { _Server = value; OnPropertyChanged("Server"); } } }

    private String _AccessServer;
    /// <summary>令牌服务地址。可以不同于验证地址的内网直达地址</summary>
    [DisplayName("令牌服务地址")]
    [Description("令牌服务地址。可以不同于验证地址的内网直达地址")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("AccessServer", "令牌服务地址。可以不同于验证地址的内网直达地址", "")]
    public String AccessServer { get => _AccessServer; set { if (OnPropertyChanging("AccessServer", value)) { _AccessServer = value; OnPropertyChanged("AccessServer"); } } }

    private GrantTypes _GrantType;
    /// <summary>授权类型</summary>
    [DisplayName("授权类型")]
    [Description("授权类型")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("GrantType", "授权类型", "")]
    public GrantTypes GrantType { get => _GrantType; set { if (OnPropertyChanging("GrantType", value)) { _GrantType = value; OnPropertyChanged("GrantType"); } } }

    private String _Scope;
    /// <summary>授权范围</summary>
    [DisplayName("授权范围")]
    [Description("授权范围")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Scope", "授权范围", "")]
    public String Scope { get => _Scope; set { if (OnPropertyChanging("Scope", value)) { _Scope = value; OnPropertyChanged("Scope"); } } }

    private String _AuthUrl;
    /// <summary>验证地址。跳转SSO的验证地址</summary>
    [DisplayName("验证地址")]
    [Description("验证地址。跳转SSO的验证地址")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("AuthUrl", "验证地址。跳转SSO的验证地址", "")]
    public String AuthUrl { get => _AuthUrl; set { if (OnPropertyChanging("AuthUrl", value)) { _AuthUrl = value; OnPropertyChanged("AuthUrl"); } } }

    private String _AccessUrl;
    /// <summary>令牌地址。根据code换取令牌的地址</summary>
    [DisplayName("令牌地址")]
    [Description("令牌地址。根据code换取令牌的地址")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("AccessUrl", "令牌地址。根据code换取令牌的地址", "")]
    public String AccessUrl { get => _AccessUrl; set { if (OnPropertyChanging("AccessUrl", value)) { _AccessUrl = value; OnPropertyChanged("AccessUrl"); } } }

    private String _UserUrl;
    /// <summary>用户地址。根据令牌获取用户信息的地址</summary>
    [DisplayName("用户地址")]
    [Description("用户地址。根据令牌获取用户信息的地址")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("UserUrl", "用户地址。根据令牌获取用户信息的地址", "")]
    public String UserUrl { get => _UserUrl; set { if (OnPropertyChanging("UserUrl", value)) { _UserUrl = value; OnPropertyChanged("UserUrl"); } } }

    private String _AppUrl;
    /// <summary>应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址</summary>
    [DisplayName("应用地址")]
    [Description("应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("AppUrl", "应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址", "")]
    public String AppUrl { get => _AppUrl; set { if (OnPropertyChanging("AppUrl", value)) { _AppUrl = value; OnPropertyChanged("AppUrl"); } } }

    private Boolean _Enable;
    /// <summary>启用</summary>
    [DisplayName("启用")]
    [Description("启用")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Enable", "启用", "")]
    public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

    private Boolean _Debug;
    /// <summary>调试。设置处于调试状态，输出详细日志</summary>
    [DisplayName("调试")]
    [Description("调试。设置处于调试状态，输出详细日志")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Debug", "调试。设置处于调试状态，输出详细日志", "")]
    public Boolean Debug { get => _Debug; set { if (OnPropertyChanging("Debug", value)) { _Debug = value; OnPropertyChanged("Debug"); } } }

    private Boolean _Visible;
    /// <summary>可见。是否在登录页面可见，不可见的提供者只能使用应用内自动登录，例如微信公众号</summary>
    [DisplayName("可见")]
    [Description("可见。是否在登录页面可见，不可见的提供者只能使用应用内自动登录，例如微信公众号")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Visible", "可见。是否在登录页面可见，不可见的提供者只能使用应用内自动登录，例如微信公众号", "")]
    public Boolean Visible { get => _Visible; set { if (OnPropertyChanging("Visible", value)) { _Visible = value; OnPropertyChanged("Visible"); } } }

    private Boolean _AutoRegister;
    /// <summary>自动注册。SSO登录后，如果本地没有匹配用户，自动注册新用户，否则跳到登录页，在登录后绑定</summary>
    [DisplayName("自动注册")]
    [Description("自动注册。SSO登录后，如果本地没有匹配用户，自动注册新用户，否则跳到登录页，在登录后绑定")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("AutoRegister", "自动注册。SSO登录后，如果本地没有匹配用户，自动注册新用户，否则跳到登录页，在登录后绑定", "")]
    public Boolean AutoRegister { get => _AutoRegister; set { if (OnPropertyChanging("AutoRegister", value)) { _AutoRegister = value; OnPropertyChanged("AutoRegister"); } } }

    private String _AutoRole;
    /// <summary>自动角色。该渠道登录的用户，将会自动得到指定角色名，多个角色逗号隔开</summary>
    [DisplayName("自动角色")]
    [Description("自动角色。该渠道登录的用户，将会自动得到指定角色名，多个角色逗号隔开")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("AutoRole", "自动角色。该渠道登录的用户，将会自动得到指定角色名，多个角色逗号隔开", "")]
    public String AutoRole { get => _AutoRole; set { if (OnPropertyChanging("AutoRole", value)) { _AutoRole = value; OnPropertyChanged("AutoRole"); } } }

    private Int32 _Sort;
    /// <summary>排序。较大者在前面</summary>
    [DisplayName("排序")]
    [Description("排序。较大者在前面")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Sort", "排序。较大者在前面", "")]
    public Int32 Sort { get => _Sort; set { if (OnPropertyChanging("Sort", value)) { _Sort = value; OnPropertyChanged("Sort"); } } }

    private String _SecurityKey;
    /// <summary>安全密钥。公钥，用于RSA加密用户密码，在通信链路上保护用户密码安全，密钥前面可以增加keyName，形成keyName$keyValue，用于向服务端指示所使用的密钥标识，方便未来更换密钥。</summary>
    [DisplayName("安全密钥")]
    [Description("安全密钥。公钥，用于RSA加密用户密码，在通信链路上保护用户密码安全，密钥前面可以增加keyName，形成keyName$keyValue，用于向服务端指示所使用的密钥标识，方便未来更换密钥。")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("SecurityKey", "安全密钥。公钥，用于RSA加密用户密码，在通信链路上保护用户密码安全，密钥前面可以增加keyName，形成keyName$keyValue，用于向服务端指示所使用的密钥标识，方便未来更换密钥。", "")]
    public String SecurityKey { get => _SecurityKey; set { if (OnPropertyChanging("SecurityKey", value)) { _SecurityKey = value; OnPropertyChanged("SecurityKey"); } } }

    private String _FieldMap;
    /// <summary>字段映射。SSO用户字段如何映射到OAuthClient内部属性</summary>
    [DisplayName("字段映射")]
    [Description("字段映射。SSO用户字段如何映射到OAuthClient内部属性")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("FieldMap", "字段映射。SSO用户字段如何映射到OAuthClient内部属性", "")]
    public String FieldMap { get => _FieldMap; set { if (OnPropertyChanging("FieldMap", value)) { _FieldMap = value; OnPropertyChanged("FieldMap"); } } }

    private Boolean _FetchAvatar;
    /// <summary>抓取头像。是否抓取头像并保存到本地</summary>
    [DisplayName("抓取头像")]
    [Description("抓取头像。是否抓取头像并保存到本地")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("FetchAvatar", "抓取头像。是否抓取头像并保存到本地", "")]
    public Boolean FetchAvatar { get => _FetchAvatar; set { if (OnPropertyChanging("FetchAvatar", value)) { _FetchAvatar = value; OnPropertyChanged("FetchAvatar"); } } }

    private Boolean _IsDeleted;
    /// <summary>删除。是否已删除，可恢复</summary>
    [DisplayName("删除")]
    [Description("删除。是否已删除，可恢复")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("IsDeleted", "删除。是否已删除，可恢复", "")]
    public Boolean IsDeleted { get => _IsDeleted; set { if (OnPropertyChanging("IsDeleted", value)) { _IsDeleted = value; OnPropertyChanged("IsDeleted"); } } }

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
    /// <summary>内容</summary>
    [Category("扩展")]
    [DisplayName("内容")]
    [Description("内容")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Remark", "内容", "")]
    public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
    #endregion

    #region 获取/设置 字段值
    /// <summary>获取/设置 字段值</summary>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public override Object this[String name]
    {
        get => name switch
        {
            "ID" => _ID,
            "Name" => _Name,
            "NickName" => _NickName,
            "Logo" => _Logo,
            "AppId" => _AppId,
            "Secret" => _Secret,
            "Server" => _Server,
            "AccessServer" => _AccessServer,
            "GrantType" => _GrantType,
            "Scope" => _Scope,
            "AuthUrl" => _AuthUrl,
            "AccessUrl" => _AccessUrl,
            "UserUrl" => _UserUrl,
            "AppUrl" => _AppUrl,
            "Enable" => _Enable,
            "Debug" => _Debug,
            "Visible" => _Visible,
            "AutoRegister" => _AutoRegister,
            "AutoRole" => _AutoRole,
            "Sort" => _Sort,
            "SecurityKey" => _SecurityKey,
            "FieldMap" => _FieldMap,
            "FetchAvatar" => _FetchAvatar,
            "IsDeleted" => _IsDeleted,
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
                case "ID": _ID = value.ToInt(); break;
                case "Name": _Name = Convert.ToString(value); break;
                case "NickName": _NickName = Convert.ToString(value); break;
                case "Logo": _Logo = Convert.ToString(value); break;
                case "AppId": _AppId = Convert.ToString(value); break;
                case "Secret": _Secret = Convert.ToString(value); break;
                case "Server": _Server = Convert.ToString(value); break;
                case "AccessServer": _AccessServer = Convert.ToString(value); break;
                case "GrantType": _GrantType = (GrantTypes)value.ToInt(); break;
                case "Scope": _Scope = Convert.ToString(value); break;
                case "AuthUrl": _AuthUrl = Convert.ToString(value); break;
                case "AccessUrl": _AccessUrl = Convert.ToString(value); break;
                case "UserUrl": _UserUrl = Convert.ToString(value); break;
                case "AppUrl": _AppUrl = Convert.ToString(value); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "Debug": _Debug = value.ToBoolean(); break;
                case "Visible": _Visible = value.ToBoolean(); break;
                case "AutoRegister": _AutoRegister = value.ToBoolean(); break;
                case "AutoRole": _AutoRole = Convert.ToString(value); break;
                case "Sort": _Sort = value.ToInt(); break;
                case "SecurityKey": _SecurityKey = Convert.ToString(value); break;
                case "FieldMap": _FieldMap = Convert.ToString(value); break;
                case "FetchAvatar": _FetchAvatar = value.ToBoolean(); break;
                case "IsDeleted": _IsDeleted = value.ToBoolean(); break;
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
    #endregion

    #region 扩展查询
    #endregion

    #region 字段名
    /// <summary>取得OAuth配置字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field ID = FindByName("ID");

        /// <summary>名称。提供者名称</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>昵称</summary>
        public static readonly Field NickName = FindByName("NickName");

        /// <summary>图标</summary>
        public static readonly Field Logo = FindByName("Logo");

        /// <summary>应用标识</summary>
        public static readonly Field AppId = FindByName("AppId");

        /// <summary>应用密钥</summary>
        public static readonly Field Secret = FindByName("Secret");

        /// <summary>服务地址</summary>
        public static readonly Field Server = FindByName("Server");

        /// <summary>令牌服务地址。可以不同于验证地址的内网直达地址</summary>
        public static readonly Field AccessServer = FindByName("AccessServer");

        /// <summary>授权类型</summary>
        public static readonly Field GrantType = FindByName("GrantType");

        /// <summary>授权范围</summary>
        public static readonly Field Scope = FindByName("Scope");

        /// <summary>验证地址。跳转SSO的验证地址</summary>
        public static readonly Field AuthUrl = FindByName("AuthUrl");

        /// <summary>令牌地址。根据code换取令牌的地址</summary>
        public static readonly Field AccessUrl = FindByName("AccessUrl");

        /// <summary>用户地址。根据令牌获取用户信息的地址</summary>
        public static readonly Field UserUrl = FindByName("UserUrl");

        /// <summary>应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址</summary>
        public static readonly Field AppUrl = FindByName("AppUrl");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>调试。设置处于调试状态，输出详细日志</summary>
        public static readonly Field Debug = FindByName("Debug");

        /// <summary>可见。是否在登录页面可见，不可见的提供者只能使用应用内自动登录，例如微信公众号</summary>
        public static readonly Field Visible = FindByName("Visible");

        /// <summary>自动注册。SSO登录后，如果本地没有匹配用户，自动注册新用户，否则跳到登录页，在登录后绑定</summary>
        public static readonly Field AutoRegister = FindByName("AutoRegister");

        /// <summary>自动角色。该渠道登录的用户，将会自动得到指定角色名，多个角色逗号隔开</summary>
        public static readonly Field AutoRole = FindByName("AutoRole");

        /// <summary>排序。较大者在前面</summary>
        public static readonly Field Sort = FindByName("Sort");

        /// <summary>安全密钥。公钥，用于RSA加密用户密码，在通信链路上保护用户密码安全，密钥前面可以增加keyName，形成keyName$keyValue，用于向服务端指示所使用的密钥标识，方便未来更换密钥。</summary>
        public static readonly Field SecurityKey = FindByName("SecurityKey");

        /// <summary>字段映射。SSO用户字段如何映射到OAuthClient内部属性</summary>
        public static readonly Field FieldMap = FindByName("FieldMap");

        /// <summary>抓取头像。是否抓取头像并保存到本地</summary>
        public static readonly Field FetchAvatar = FindByName("FetchAvatar");

        /// <summary>删除。是否已删除，可恢复</summary>
        public static readonly Field IsDeleted = FindByName("IsDeleted");

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

        /// <summary>内容</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得OAuth配置字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String ID = "ID";

        /// <summary>名称。提供者名称</summary>
        public const String Name = "Name";

        /// <summary>昵称</summary>
        public const String NickName = "NickName";

        /// <summary>图标</summary>
        public const String Logo = "Logo";

        /// <summary>应用标识</summary>
        public const String AppId = "AppId";

        /// <summary>应用密钥</summary>
        public const String Secret = "Secret";

        /// <summary>服务地址</summary>
        public const String Server = "Server";

        /// <summary>令牌服务地址。可以不同于验证地址的内网直达地址</summary>
        public const String AccessServer = "AccessServer";

        /// <summary>授权类型</summary>
        public const String GrantType = "GrantType";

        /// <summary>授权范围</summary>
        public const String Scope = "Scope";

        /// <summary>验证地址。跳转SSO的验证地址</summary>
        public const String AuthUrl = "AuthUrl";

        /// <summary>令牌地址。根据code换取令牌的地址</summary>
        public const String AccessUrl = "AccessUrl";

        /// <summary>用户地址。根据令牌获取用户信息的地址</summary>
        public const String UserUrl = "UserUrl";

        /// <summary>应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址</summary>
        public const String AppUrl = "AppUrl";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>调试。设置处于调试状态，输出详细日志</summary>
        public const String Debug = "Debug";

        /// <summary>可见。是否在登录页面可见，不可见的提供者只能使用应用内自动登录，例如微信公众号</summary>
        public const String Visible = "Visible";

        /// <summary>自动注册。SSO登录后，如果本地没有匹配用户，自动注册新用户，否则跳到登录页，在登录后绑定</summary>
        public const String AutoRegister = "AutoRegister";

        /// <summary>自动角色。该渠道登录的用户，将会自动得到指定角色名，多个角色逗号隔开</summary>
        public const String AutoRole = "AutoRole";

        /// <summary>排序。较大者在前面</summary>
        public const String Sort = "Sort";

        /// <summary>安全密钥。公钥，用于RSA加密用户密码，在通信链路上保护用户密码安全，密钥前面可以增加keyName，形成keyName$keyValue，用于向服务端指示所使用的密钥标识，方便未来更换密钥。</summary>
        public const String SecurityKey = "SecurityKey";

        /// <summary>字段映射。SSO用户字段如何映射到OAuthClient内部属性</summary>
        public const String FieldMap = "FieldMap";

        /// <summary>抓取头像。是否抓取头像并保存到本地</summary>
        public const String FetchAvatar = "FetchAvatar";

        /// <summary>删除。是否已删除，可恢复</summary>
        public const String IsDeleted = "IsDeleted";

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

        /// <summary>内容</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
