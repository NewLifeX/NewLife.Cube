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

/// <summary>用户链接。第三方绑定</summary>
[Serializable]
[DataObject]
[Description("用户链接。第三方绑定")]
[BindIndex("IU_UserConnect_Provider_OpenID", true, "Provider,OpenID")]
[BindIndex("IX_UserConnect_UserID", false, "UserID")]
[BindIndex("IX_UserConnect_OpenID", false, "OpenID")]
[BindIndex("IX_UserConnect_UnionID", false, "UnionID")]
[BindIndex("IX_UserConnect_DeviceId", false, "DeviceId")]
[BindTable("UserConnect", Description = "用户链接。第三方绑定", ConnName = "Membership", DbType = DatabaseType.None)]
public partial class UserConnect
{
    #region 属性
    private Int32 _ID;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("ID", "编号", "")]
    public Int32 ID { get => _ID; set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } } }

    private String _Provider;
    /// <summary>提供商</summary>
    [DisplayName("提供商")]
    [Description("提供商")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Provider", "提供商", "")]
    public String Provider { get => _Provider; set { if (OnPropertyChanging("Provider", value)) { _Provider = value; OnPropertyChanged("Provider"); } } }

    private Int32 _UserID;
    /// <summary>用户。本地用户</summary>
    [DisplayName("用户")]
    [Description("用户。本地用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UserID", "用户。本地用户", "")]
    public Int32 UserID { get => _UserID; set { if (OnPropertyChanging("UserID", value)) { _UserID = value; OnPropertyChanged("UserID"); } } }

    private String _OpenID;
    /// <summary>身份标识。用户名、OpenID</summary>
    [DisplayName("身份标识")]
    [Description("身份标识。用户名、OpenID")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("OpenID", "身份标识。用户名、OpenID", "")]
    public String OpenID { get => _OpenID; set { if (OnPropertyChanging("OpenID", value)) { _OpenID = value; OnPropertyChanged("OpenID"); } } }

    private String _UnionID;
    /// <summary>全局标识。跨应用统一</summary>
    [DisplayName("全局标识")]
    [Description("全局标识。跨应用统一")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UnionID", "全局标识。跨应用统一", "")]
    public String UnionID { get => _UnionID; set { if (OnPropertyChanging("UnionID", value)) { _UnionID = value; OnPropertyChanged("UnionID"); } } }

    private Int64 _LinkID;
    /// <summary>用户编号。第三方用户编号</summary>
    [DisplayName("用户编号")]
    [Description("用户编号。第三方用户编号")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("LinkID", "用户编号。第三方用户编号", "")]
    public Int64 LinkID { get => _LinkID; set { if (OnPropertyChanging("LinkID", value)) { _LinkID = value; OnPropertyChanged("LinkID"); } } }

    private String _NickName;
    /// <summary>昵称</summary>
    [DisplayName("昵称")]
    [Description("昵称")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("NickName", "昵称", "")]
    public String NickName { get => _NickName; set { if (OnPropertyChanging("NickName", value)) { _NickName = value; OnPropertyChanged("NickName"); } } }

    private String _DeviceId;
    /// <summary>设备标识。企业微信用于唯一标识设备，重装后改变</summary>
    [DisplayName("设备标识")]
    [Description("设备标识。企业微信用于唯一标识设备，重装后改变")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("DeviceId", "设备标识。企业微信用于唯一标识设备，重装后改变", "")]
    public String DeviceId { get => _DeviceId; set { if (OnPropertyChanging("DeviceId", value)) { _DeviceId = value; OnPropertyChanged("DeviceId"); } } }

    private String _Avatar;
    /// <summary>头像</summary>
    [DisplayName("头像")]
    [Description("头像")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Avatar", "头像", "")]
    public String Avatar { get => _Avatar; set { if (OnPropertyChanging("Avatar", value)) { _Avatar = value; OnPropertyChanged("Avatar"); } } }

    private String _AccessToken;
    /// <summary>访问令牌</summary>
    [DisplayName("访问令牌")]
    [Description("访问令牌")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("AccessToken", "访问令牌", "")]
    public String AccessToken { get => _AccessToken; set { if (OnPropertyChanging("AccessToken", value)) { _AccessToken = value; OnPropertyChanged("AccessToken"); } } }

    private String _RefreshToken;
    /// <summary>刷新令牌</summary>
    [DisplayName("刷新令牌")]
    [Description("刷新令牌")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("RefreshToken", "刷新令牌", "")]
    public String RefreshToken { get => _RefreshToken; set { if (OnPropertyChanging("RefreshToken", value)) { _RefreshToken = value; OnPropertyChanged("RefreshToken"); } } }

    private DateTime _Expire;
    /// <summary>过期时间</summary>
    [DisplayName("过期时间")]
    [Description("过期时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("Expire", "过期时间", "")]
    public DateTime Expire { get => _Expire; set { if (OnPropertyChanging("Expire", value)) { _Expire = value; OnPropertyChanged("Expire"); } } }

    private Boolean _Enable;
    /// <summary>启用</summary>
    [DisplayName("启用")]
    [Description("启用")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Enable", "启用", "")]
    public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

    private Int32 _CreateUserID;
    /// <summary>创建用户</summary>
    [Category("扩展")]
    [DisplayName("创建用户")]
    [Description("创建用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CreateUserID", "创建用户", "")]
    public Int32 CreateUserID { get => _CreateUserID; set { if (OnPropertyChanging("CreateUserID", value)) { _CreateUserID = value; OnPropertyChanged("CreateUserID"); } } }

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

    private Int32 _UpdateUserID;
    /// <summary>更新用户</summary>
    [Category("扩展")]
    [DisplayName("更新用户")]
    [Description("更新用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UpdateUserID", "更新用户", "")]
    public Int32 UpdateUserID { get => _UpdateUserID; set { if (OnPropertyChanging("UpdateUserID", value)) { _UpdateUserID = value; OnPropertyChanged("UpdateUserID"); } } }

    private String _UpdateIP;
    /// <summary>更新地址</summary>
    [Category("扩展")]
    [DisplayName("更新地址")]
    [Description("更新地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UpdateIP", "更新地址", "")]
    public String UpdateIP { get => _UpdateIP; set { if (OnPropertyChanging("UpdateIP", value)) { _UpdateIP = value; OnPropertyChanged("UpdateIP"); } } }

    private DateTime _UpdateTime;
    /// <summary>更新时间</summary>
    [Category("扩展")]
    [DisplayName("更新时间")]
    [Description("更新时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("UpdateTime", "更新时间", "")]
    public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

    private String _Remark;
    /// <summary>备注</summary>
    [DisplayName("备注")]
    [Description("备注")]
    [DataObjectField(false, false, true, 5000)]
    [BindColumn("Remark", "备注", "")]
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
            "Provider" => _Provider,
            "UserID" => _UserID,
            "OpenID" => _OpenID,
            "UnionID" => _UnionID,
            "LinkID" => _LinkID,
            "NickName" => _NickName,
            "DeviceId" => _DeviceId,
            "Avatar" => _Avatar,
            "AccessToken" => _AccessToken,
            "RefreshToken" => _RefreshToken,
            "Expire" => _Expire,
            "Enable" => _Enable,
            "CreateUserID" => _CreateUserID,
            "CreateIP" => _CreateIP,
            "CreateTime" => _CreateTime,
            "UpdateUserID" => _UpdateUserID,
            "UpdateIP" => _UpdateIP,
            "UpdateTime" => _UpdateTime,
            "Remark" => _Remark,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "ID": _ID = value.ToInt(); break;
                case "Provider": _Provider = Convert.ToString(value); break;
                case "UserID": _UserID = value.ToInt(); break;
                case "OpenID": _OpenID = Convert.ToString(value); break;
                case "UnionID": _UnionID = Convert.ToString(value); break;
                case "LinkID": _LinkID = value.ToLong(); break;
                case "NickName": _NickName = Convert.ToString(value); break;
                case "DeviceId": _DeviceId = Convert.ToString(value); break;
                case "Avatar": _Avatar = Convert.ToString(value); break;
                case "AccessToken": _AccessToken = Convert.ToString(value); break;
                case "RefreshToken": _RefreshToken = Convert.ToString(value); break;
                case "Expire": _Expire = value.ToDateTime(); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "CreateUserID": _CreateUserID = value.ToInt(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "UpdateUserID": _UpdateUserID = value.ToInt(); break;
                case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                case "Remark": _Remark = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion

    #region 关联映射
    #endregion

    #region 字段名
    /// <summary>取得用户链接字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field ID = FindByName("ID");

        /// <summary>提供商</summary>
        public static readonly Field Provider = FindByName("Provider");

        /// <summary>用户。本地用户</summary>
        public static readonly Field UserID = FindByName("UserID");

        /// <summary>身份标识。用户名、OpenID</summary>
        public static readonly Field OpenID = FindByName("OpenID");

        /// <summary>全局标识。跨应用统一</summary>
        public static readonly Field UnionID = FindByName("UnionID");

        /// <summary>用户编号。第三方用户编号</summary>
        public static readonly Field LinkID = FindByName("LinkID");

        /// <summary>昵称</summary>
        public static readonly Field NickName = FindByName("NickName");

        /// <summary>设备标识。企业微信用于唯一标识设备，重装后改变</summary>
        public static readonly Field DeviceId = FindByName("DeviceId");

        /// <summary>头像</summary>
        public static readonly Field Avatar = FindByName("Avatar");

        /// <summary>访问令牌</summary>
        public static readonly Field AccessToken = FindByName("AccessToken");

        /// <summary>刷新令牌</summary>
        public static readonly Field RefreshToken = FindByName("RefreshToken");

        /// <summary>过期时间</summary>
        public static readonly Field Expire = FindByName("Expire");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>创建用户</summary>
        public static readonly Field CreateUserID = FindByName("CreateUserID");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>更新用户</summary>
        public static readonly Field UpdateUserID = FindByName("UpdateUserID");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>备注</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得用户链接字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String ID = "ID";

        /// <summary>提供商</summary>
        public const String Provider = "Provider";

        /// <summary>用户。本地用户</summary>
        public const String UserID = "UserID";

        /// <summary>身份标识。用户名、OpenID</summary>
        public const String OpenID = "OpenID";

        /// <summary>全局标识。跨应用统一</summary>
        public const String UnionID = "UnionID";

        /// <summary>用户编号。第三方用户编号</summary>
        public const String LinkID = "LinkID";

        /// <summary>昵称</summary>
        public const String NickName = "NickName";

        /// <summary>设备标识。企业微信用于唯一标识设备，重装后改变</summary>
        public const String DeviceId = "DeviceId";

        /// <summary>头像</summary>
        public const String Avatar = "Avatar";

        /// <summary>访问令牌</summary>
        public const String AccessToken = "AccessToken";

        /// <summary>刷新令牌</summary>
        public const String RefreshToken = "RefreshToken";

        /// <summary>过期时间</summary>
        public const String Expire = "Expire";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>创建用户</summary>
        public const String CreateUserID = "CreateUserID";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>更新用户</summary>
        public const String UpdateUserID = "UpdateUserID";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>备注</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
