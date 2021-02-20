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
    /// <summary>应用系统。用于OAuthServer的子系统</summary>
    [Serializable]
    [DataObject]
    [Description("应用系统。用于OAuthServer的子系统")]
    [BindIndex("IU_App_Name", true, "Name")]
    [BindTable("App", Description = "应用系统。用于OAuthServer的子系统", ConnName = "Cube", DbType = DatabaseType.None)]
    public partial class App
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
        /// <summary>名称。AppID</summary>
        [DisplayName("名称")]
        [Description("名称。AppID")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn("Name", "名称。AppID", "", Master = true)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

        private String _DisplayName;
        /// <summary>显示名</summary>
        [DisplayName("显示名")]
        [Description("显示名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("DisplayName", "显示名", "")]
        public String DisplayName { get => _DisplayName; set { if (OnPropertyChanging("DisplayName", value)) { _DisplayName = value; OnPropertyChanged("DisplayName"); } } }

        private String _Secret;
        /// <summary>密钥。AppSecret</summary>
        [DisplayName("密钥")]
        [Description("密钥。AppSecret")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Secret", "密钥。AppSecret", "")]
        public String Secret { get => _Secret; set { if (OnPropertyChanging("Secret", value)) { _Secret = value; OnPropertyChanged("Secret"); } } }

        private String _HomePage;
        /// <summary>首页</summary>
        [DisplayName("首页")]
        [Description("首页")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("HomePage", "首页", "")]
        public String HomePage { get => _HomePage; set { if (OnPropertyChanging("HomePage", value)) { _HomePage = value; OnPropertyChanged("HomePage"); } } }

        private String _Logo;
        /// <summary>图标。附件编号列表</summary>
        [DisplayName("图标")]
        [Description("图标。附件编号列表")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Logo", "图标。附件编号列表", "", ItemType = "image")]
        public String Logo { get => _Logo; set { if (OnPropertyChanging("Logo", value)) { _Logo = value; OnPropertyChanged("Logo"); } } }

        private String _White;
        /// <summary>白名单</summary>
        [DisplayName("白名单")]
        [Description("白名单")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("White", "白名单", "")]
        public String White { get => _White; set { if (OnPropertyChanging("White", value)) { _White = value; OnPropertyChanged("White"); } } }

        private String _Black;
        /// <summary>黑名单。黑名单优先于白名单</summary>
        [DisplayName("黑名单")]
        [Description("黑名单。黑名单优先于白名单")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Black", "黑名单。黑名单优先于白名单", "")]
        public String Black { get => _Black; set { if (OnPropertyChanging("Black", value)) { _Black = value; OnPropertyChanged("Black"); } } }

        private Boolean _Enable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [Description("启用")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Enable", "启用", "")]
        public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

        private Int32 _TokenExpire;
        /// <summary>有效期。访问令牌AccessToken的有效期，单位秒，默认使用全局设置</summary>
        [DisplayName("有效期")]
        [Description("有效期。访问令牌AccessToken的有效期，单位秒，默认使用全局设置")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("TokenExpire", "有效期。访问令牌AccessToken的有效期，单位秒，默认使用全局设置", "")]
        public Int32 TokenExpire { get => _TokenExpire; set { if (OnPropertyChanging("TokenExpire", value)) { _TokenExpire = value; OnPropertyChanged("TokenExpire"); } } }

        private String _Urls;
        /// <summary>回调地址。用于限制回调地址安全性，多个地址逗号隔开</summary>
        [DisplayName("回调地址")]
        [Description("回调地址。用于限制回调地址安全性，多个地址逗号隔开")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("Urls", "回调地址。用于限制回调地址安全性，多个地址逗号隔开", "")]
        public String Urls { get => _Urls; set { if (OnPropertyChanging("Urls", value)) { _Urls = value; OnPropertyChanged("Urls"); } } }

        private String _RoleIds;
        /// <summary>授权角色。只允许这些角色登录该系统，多个地址逗号隔开，未填写时表示不限制</summary>
        [DisplayName("授权角色")]
        [Description("授权角色。只允许这些角色登录该系统，多个地址逗号隔开，未填写时表示不限制")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("RoleIds", "授权角色。只允许这些角色登录该系统，多个地址逗号隔开，未填写时表示不限制", "")]
        public String RoleIds { get => _RoleIds; set { if (OnPropertyChanging("RoleIds", value)) { _RoleIds = value; OnPropertyChanged("RoleIds"); } } }

        private Int32 _Auths;
        /// <summary>次数</summary>
        [DisplayName("次数")]
        [Description("次数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Auths", "次数", "")]
        public Int32 Auths { get => _Auths; set { if (OnPropertyChanging("Auths", value)) { _Auths = value; OnPropertyChanged("Auths"); } } }

        private DateTime _LastAuth;
        /// <summary>最后请求</summary>
        [DisplayName("最后请求")]
        [Description("最后请求")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("LastAuth", "最后请求", "")]
        public DateTime LastAuth { get => _LastAuth; set { if (OnPropertyChanging("LastAuth", value)) { _LastAuth = value; OnPropertyChanged("LastAuth"); } } }

        private String _Remark;
        /// <summary>内容</summary>
        [DisplayName("内容")]
        [Description("内容")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("Remark", "内容", "")]
        public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }

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
                    case "DisplayName": return _DisplayName;
                    case "Secret": return _Secret;
                    case "HomePage": return _HomePage;
                    case "Logo": return _Logo;
                    case "White": return _White;
                    case "Black": return _Black;
                    case "Enable": return _Enable;
                    case "TokenExpire": return _TokenExpire;
                    case "Urls": return _Urls;
                    case "RoleIds": return _RoleIds;
                    case "Auths": return _Auths;
                    case "LastAuth": return _LastAuth;
                    case "Remark": return _Remark;
                    case "CreateUserID": return _CreateUserID;
                    case "CreateTime": return _CreateTime;
                    case "CreateIP": return _CreateIP;
                    case "UpdateUserID": return _UpdateUserID;
                    case "UpdateTime": return _UpdateTime;
                    case "UpdateIP": return _UpdateIP;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": _ID = value.ToInt(); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "DisplayName": _DisplayName = Convert.ToString(value); break;
                    case "Secret": _Secret = Convert.ToString(value); break;
                    case "HomePage": _HomePage = Convert.ToString(value); break;
                    case "Logo": _Logo = Convert.ToString(value); break;
                    case "White": _White = Convert.ToString(value); break;
                    case "Black": _Black = Convert.ToString(value); break;
                    case "Enable": _Enable = value.ToBoolean(); break;
                    case "TokenExpire": _TokenExpire = value.ToInt(); break;
                    case "Urls": _Urls = Convert.ToString(value); break;
                    case "RoleIds": _RoleIds = Convert.ToString(value); break;
                    case "Auths": _Auths = value.ToInt(); break;
                    case "LastAuth": _LastAuth = value.ToDateTime(); break;
                    case "Remark": _Remark = Convert.ToString(value); break;
                    case "CreateUserID": _CreateUserID = value.ToInt(); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "CreateIP": _CreateIP = Convert.ToString(value); break;
                    case "UpdateUserID": _UpdateUserID = value.ToInt(); break;
                    case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                    case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得应用系统字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field ID = FindByName("ID");

            /// <summary>名称。AppID</summary>
            public static readonly Field Name = FindByName("Name");

            /// <summary>显示名</summary>
            public static readonly Field DisplayName = FindByName("DisplayName");

            /// <summary>密钥。AppSecret</summary>
            public static readonly Field Secret = FindByName("Secret");

            /// <summary>首页</summary>
            public static readonly Field HomePage = FindByName("HomePage");

            /// <summary>图标。附件编号列表</summary>
            public static readonly Field Logo = FindByName("Logo");

            /// <summary>白名单</summary>
            public static readonly Field White = FindByName("White");

            /// <summary>黑名单。黑名单优先于白名单</summary>
            public static readonly Field Black = FindByName("Black");

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName("Enable");

            /// <summary>有效期。访问令牌AccessToken的有效期，单位秒，默认使用全局设置</summary>
            public static readonly Field TokenExpire = FindByName("TokenExpire");

            /// <summary>回调地址。用于限制回调地址安全性，多个地址逗号隔开</summary>
            public static readonly Field Urls = FindByName("Urls");

            /// <summary>授权角色。只允许这些角色登录该系统，多个地址逗号隔开，未填写时表示不限制</summary>
            public static readonly Field RoleIds = FindByName("RoleIds");

            /// <summary>次数</summary>
            public static readonly Field Auths = FindByName("Auths");

            /// <summary>最后请求</summary>
            public static readonly Field LastAuth = FindByName("LastAuth");

            /// <summary>内容</summary>
            public static readonly Field Remark = FindByName("Remark");

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

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得应用系统字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String ID = "ID";

            /// <summary>名称。AppID</summary>
            public const String Name = "Name";

            /// <summary>显示名</summary>
            public const String DisplayName = "DisplayName";

            /// <summary>密钥。AppSecret</summary>
            public const String Secret = "Secret";

            /// <summary>首页</summary>
            public const String HomePage = "HomePage";

            /// <summary>图标。附件编号列表</summary>
            public const String Logo = "Logo";

            /// <summary>白名单</summary>
            public const String White = "White";

            /// <summary>黑名单。黑名单优先于白名单</summary>
            public const String Black = "Black";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

            /// <summary>有效期。访问令牌AccessToken的有效期，单位秒，默认使用全局设置</summary>
            public const String TokenExpire = "TokenExpire";

            /// <summary>回调地址。用于限制回调地址安全性，多个地址逗号隔开</summary>
            public const String Urls = "Urls";

            /// <summary>授权角色。只允许这些角色登录该系统，多个地址逗号隔开，未填写时表示不限制</summary>
            public const String RoleIds = "RoleIds";

            /// <summary>次数</summary>
            public const String Auths = "Auths";

            /// <summary>最后请求</summary>
            public const String LastAuth = "LastAuth";

            /// <summary>内容</summary>
            public const String Remark = "Remark";

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
        }
        #endregion
    }
}