using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Entity
{
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
        /// <summary>应用密钥。</summary>
        [DisplayName("应用密钥")]
        [Description("应用密钥。")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Secret", "应用密钥。", "")]
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

        private String _Scope;
        /// <summary>授权范围</summary>
        [DisplayName("授权范围")]
        [Description("授权范围")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Scope", "授权范围", "")]
        public String Scope { get => _Scope; set { if (OnPropertyChanging("Scope", value)) { _Scope = value; OnPropertyChanged("Scope"); } } }

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

        private String _AppUrl;
        /// <summary>应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址</summary>
        [DisplayName("应用地址")]
        [Description("应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("AppUrl", "应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址", "")]
        public String AppUrl { get => _AppUrl; set { if (OnPropertyChanging("AppUrl", value)) { _AppUrl = value; OnPropertyChanged("AppUrl"); } } }

        private Int32 _CreateUserID;
        /// <summary>创建者</summary>
        [DisplayName("创建者")]
        [Description("创建者")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CreateUserID", "创建者", "")]
        public Int32 CreateUserID { get => _CreateUserID; set { if (OnPropertyChanging("CreateUserID", value)) { _CreateUserID = value; OnPropertyChanged("CreateUserID"); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

        private String _CreateIP;
        /// <summary>创建地址</summary>
        [DisplayName("创建地址")]
        [Description("创建地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("CreateIP", "创建地址", "")]
        public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

        private Int32 _UpdateUserID;
        /// <summary>更新者</summary>
        [DisplayName("更新者")]
        [Description("更新者")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UpdateUserID", "更新者", "")]
        public Int32 UpdateUserID { get => _UpdateUserID; set { if (OnPropertyChanging("UpdateUserID", value)) { _UpdateUserID = value; OnPropertyChanged("UpdateUserID"); } } }

        private DateTime _UpdateTime;
        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        [Description("更新时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("UpdateTime", "更新时间", "")]
        public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

        private String _UpdateIP;
        /// <summary>更新地址</summary>
        [DisplayName("更新地址")]
        [Description("更新地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("UpdateIP", "更新地址", "")]
        public String UpdateIP { get => _UpdateIP; set { if (OnPropertyChanging("UpdateIP", value)) { _UpdateIP = value; OnPropertyChanged("UpdateIP"); } } }

        private String _Remark;
        /// <summary>内容</summary>
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
            get
            {
                switch (name)
                {
                    case "ID": return _ID;
                    case "Name": return _Name;
                    case "NickName": return _NickName;
                    case "Logo": return _Logo;
                    case "AppId": return _AppId;
                    case "Secret": return _Secret;
                    case "Server": return _Server;
                    case "AccessServer": return _AccessServer;
                    case "Scope": return _Scope;
                    case "Enable": return _Enable;
                    case "Debug": return _Debug;
                    case "AppUrl": return _AppUrl;
                    case "CreateUserID": return _CreateUserID;
                    case "CreateTime": return _CreateTime;
                    case "CreateIP": return _CreateIP;
                    case "UpdateUserID": return _UpdateUserID;
                    case "UpdateTime": return _UpdateTime;
                    case "UpdateIP": return _UpdateIP;
                    case "Remark": return _Remark;
                    default: return base[name];
                }
            }
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
                    case "Scope": _Scope = Convert.ToString(value); break;
                    case "Enable": _Enable = value.ToBoolean(); break;
                    case "Debug": _Debug = value.ToBoolean(); break;
                    case "AppUrl": _AppUrl = Convert.ToString(value); break;
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

            /// <summary>应用密钥。</summary>
            public static readonly Field Secret = FindByName("Secret");

            /// <summary>服务地址</summary>
            public static readonly Field Server = FindByName("Server");

            /// <summary>令牌服务地址。可以不同于验证地址的内网直达地址</summary>
            public static readonly Field AccessServer = FindByName("AccessServer");

            /// <summary>授权范围</summary>
            public static readonly Field Scope = FindByName("Scope");

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName("Enable");

            /// <summary>调试。设置处于调试状态，输出详细日志</summary>
            public static readonly Field Debug = FindByName("Debug");

            /// <summary>应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址</summary>
            public static readonly Field AppUrl = FindByName("AppUrl");

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

            /// <summary>应用密钥。</summary>
            public const String Secret = "Secret";

            /// <summary>服务地址</summary>
            public const String Server = "Server";

            /// <summary>令牌服务地址。可以不同于验证地址的内网直达地址</summary>
            public const String AccessServer = "AccessServer";

            /// <summary>授权范围</summary>
            public const String Scope = "Scope";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

            /// <summary>调试。设置处于调试状态，输出详细日志</summary>
            public const String Debug = "Debug";

            /// <summary>应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址</summary>
            public const String AppUrl = "AppUrl";

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
}