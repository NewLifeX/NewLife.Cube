using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class App : IApp
    {
        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        [BindColumn("ID", "编号", "")]
        public Int32 ID { get { return _ID; } set { if (OnPropertyChanging(__.ID, value)) { _ID = value; OnPropertyChanged(__.ID); } } }

        private String _Name;
        /// <summary>名称。AppID</summary>
        [DisplayName("名称")]
        [Description("名称。AppID")]
        [DataObjectField(false, false, false, 50)]
        [BindColumn("Name", "名称。AppID", "", Master = true)]
        public String Name { get { return _Name; } set { if (OnPropertyChanging(__.Name, value)) { _Name = value; OnPropertyChanged(__.Name); } } }

        private String _DisplayName;
        /// <summary>显示名</summary>
        [DisplayName("显示名")]
        [Description("显示名")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("DisplayName", "显示名", "")]
        public String DisplayName { get { return _DisplayName; } set { if (OnPropertyChanging(__.DisplayName, value)) { _DisplayName = value; OnPropertyChanged(__.DisplayName); } } }

        private String _Secret;
        /// <summary>密钥。AppSecret</summary>
        [DisplayName("密钥")]
        [Description("密钥。AppSecret")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Secret", "密钥。AppSecret", "")]
        public String Secret { get { return _Secret; } set { if (OnPropertyChanging(__.Secret, value)) { _Secret = value; OnPropertyChanged(__.Secret); } } }

        private String _White;
        /// <summary>白名单</summary>
        [DisplayName("白名单")]
        [Description("白名单")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("White", "白名单", "")]
        public String White { get { return _White; } set { if (OnPropertyChanging(__.White, value)) { _White = value; OnPropertyChanged(__.White); } } }

        private String _Black;
        /// <summary>黑名单。黑名单优先于白名单</summary>
        [DisplayName("黑名单")]
        [Description("黑名单。黑名单优先于白名单")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Black", "黑名单。黑名单优先于白名单", "")]
        public String Black { get { return _Black; } set { if (OnPropertyChanging(__.Black, value)) { _Black = value; OnPropertyChanged(__.Black); } } }

        private Boolean _Enable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [Description("启用")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Enable", "启用", "")]
        public Boolean Enable { get { return _Enable; } set { if (OnPropertyChanging(__.Enable, value)) { _Enable = value; OnPropertyChanged(__.Enable); } } }

        private String _Urls;
        /// <summary>回调地址。用于限制回调地址安全性，多个地址逗号隔开</summary>
        [DisplayName("回调地址")]
        [Description("回调地址。用于限制回调地址安全性，多个地址逗号隔开")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("Urls", "回调地址。用于限制回调地址安全性，多个地址逗号隔开", "")]
        public String Urls { get { return _Urls; } set { if (OnPropertyChanging(__.Urls, value)) { _Urls = value; OnPropertyChanged(__.Urls); } } }

        private Int32 _Auths;
        /// <summary>次数</summary>
        [DisplayName("次数")]
        [Description("次数")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Auths", "次数", "")]
        public Int32 Auths { get { return _Auths; } set { if (OnPropertyChanging(__.Auths, value)) { _Auths = value; OnPropertyChanged(__.Auths); } } }

        private DateTime _LastAuth;
        /// <summary>最后请求</summary>
        [DisplayName("最后请求")]
        [Description("最后请求")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("LastAuth", "最后请求", "")]
        public DateTime LastAuth { get { return _LastAuth; } set { if (OnPropertyChanging(__.LastAuth, value)) { _LastAuth = value; OnPropertyChanged(__.LastAuth); } } }

        private String _Remark;
        /// <summary>内容</summary>
        [DisplayName("内容")]
        [Description("内容")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("Remark", "内容", "")]
        public String Remark { get { return _Remark; } set { if (OnPropertyChanging(__.Remark, value)) { _Remark = value; OnPropertyChanged(__.Remark); } } }

        private Int32 _CreateUserID;
        /// <summary>创建者</summary>
        [DisplayName("创建者")]
        [Description("创建者")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CreateUserID", "创建者", "")]
        public Int32 CreateUserID { get { return _CreateUserID; } set { if (OnPropertyChanging(__.CreateUserID, value)) { _CreateUserID = value; OnPropertyChanged(__.CreateUserID); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get { return _CreateTime; } set { if (OnPropertyChanging(__.CreateTime, value)) { _CreateTime = value; OnPropertyChanged(__.CreateTime); } } }

        private String _CreateIP;
        /// <summary>创建地址</summary>
        [DisplayName("创建地址")]
        [Description("创建地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("CreateIP", "创建地址", "")]
        public String CreateIP { get { return _CreateIP; } set { if (OnPropertyChanging(__.CreateIP, value)) { _CreateIP = value; OnPropertyChanged(__.CreateIP); } } }

        private Int32 _UpdateUserID;
        /// <summary>更新者</summary>
        [DisplayName("更新者")]
        [Description("更新者")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UpdateUserID", "更新者", "")]
        public Int32 UpdateUserID { get { return _UpdateUserID; } set { if (OnPropertyChanging(__.UpdateUserID, value)) { _UpdateUserID = value; OnPropertyChanged(__.UpdateUserID); } } }

        private DateTime _UpdateTime;
        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        [Description("更新时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("UpdateTime", "更新时间", "")]
        public DateTime UpdateTime { get { return _UpdateTime; } set { if (OnPropertyChanging(__.UpdateTime, value)) { _UpdateTime = value; OnPropertyChanged(__.UpdateTime); } } }

        private String _UpdateIP;
        /// <summary>更新地址</summary>
        [DisplayName("更新地址")]
        [Description("更新地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("UpdateIP", "更新地址", "")]
        public String UpdateIP { get { return _UpdateIP; } set { if (OnPropertyChanging(__.UpdateIP, value)) { _UpdateIP = value; OnPropertyChanged(__.UpdateIP); } } }
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
                    case __.ID : return _ID;
                    case __.Name : return _Name;
                    case __.DisplayName : return _DisplayName;
                    case __.Secret : return _Secret;
                    case __.White : return _White;
                    case __.Black : return _Black;
                    case __.Enable : return _Enable;
                    case __.Urls : return _Urls;
                    case __.Auths : return _Auths;
                    case __.LastAuth : return _LastAuth;
                    case __.Remark : return _Remark;
                    case __.CreateUserID : return _CreateUserID;
                    case __.CreateTime : return _CreateTime;
                    case __.CreateIP : return _CreateIP;
                    case __.UpdateUserID : return _UpdateUserID;
                    case __.UpdateTime : return _UpdateTime;
                    case __.UpdateIP : return _UpdateIP;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case __.ID : _ID = Convert.ToInt32(value); break;
                    case __.Name : _Name = Convert.ToString(value); break;
                    case __.DisplayName : _DisplayName = Convert.ToString(value); break;
                    case __.Secret : _Secret = Convert.ToString(value); break;
                    case __.White : _White = Convert.ToString(value); break;
                    case __.Black : _Black = Convert.ToString(value); break;
                    case __.Enable : _Enable = Convert.ToBoolean(value); break;
                    case __.Urls : _Urls = Convert.ToString(value); break;
                    case __.Auths : _Auths = Convert.ToInt32(value); break;
                    case __.LastAuth : _LastAuth = Convert.ToDateTime(value); break;
                    case __.Remark : _Remark = Convert.ToString(value); break;
                    case __.CreateUserID : _CreateUserID = Convert.ToInt32(value); break;
                    case __.CreateTime : _CreateTime = Convert.ToDateTime(value); break;
                    case __.CreateIP : _CreateIP = Convert.ToString(value); break;
                    case __.UpdateUserID : _UpdateUserID = Convert.ToInt32(value); break;
                    case __.UpdateTime : _UpdateTime = Convert.ToDateTime(value); break;
                    case __.UpdateIP : _UpdateIP = Convert.ToString(value); break;
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
            public static readonly Field ID = FindByName(__.ID);

            /// <summary>名称。AppID</summary>
            public static readonly Field Name = FindByName(__.Name);

            /// <summary>显示名</summary>
            public static readonly Field DisplayName = FindByName(__.DisplayName);

            /// <summary>密钥。AppSecret</summary>
            public static readonly Field Secret = FindByName(__.Secret);

            /// <summary>白名单</summary>
            public static readonly Field White = FindByName(__.White);

            /// <summary>黑名单。黑名单优先于白名单</summary>
            public static readonly Field Black = FindByName(__.Black);

            /// <summary>启用</summary>
            public static readonly Field Enable = FindByName(__.Enable);

            /// <summary>回调地址。用于限制回调地址安全性，多个地址逗号隔开</summary>
            public static readonly Field Urls = FindByName(__.Urls);

            /// <summary>次数</summary>
            public static readonly Field Auths = FindByName(__.Auths);

            /// <summary>最后请求</summary>
            public static readonly Field LastAuth = FindByName(__.LastAuth);

            /// <summary>内容</summary>
            public static readonly Field Remark = FindByName(__.Remark);

            /// <summary>创建者</summary>
            public static readonly Field CreateUserID = FindByName(__.CreateUserID);

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName(__.CreateTime);

            /// <summary>创建地址</summary>
            public static readonly Field CreateIP = FindByName(__.CreateIP);

            /// <summary>更新者</summary>
            public static readonly Field UpdateUserID = FindByName(__.UpdateUserID);

            /// <summary>更新时间</summary>
            public static readonly Field UpdateTime = FindByName(__.UpdateTime);

            /// <summary>更新地址</summary>
            public static readonly Field UpdateIP = FindByName(__.UpdateIP);

            static Field FindByName(String name) { return Meta.Table.FindByName(name); }
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

            /// <summary>白名单</summary>
            public const String White = "White";

            /// <summary>黑名单。黑名单优先于白名单</summary>
            public const String Black = "Black";

            /// <summary>启用</summary>
            public const String Enable = "Enable";

            /// <summary>回调地址。用于限制回调地址安全性，多个地址逗号隔开</summary>
            public const String Urls = "Urls";

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

    /// <summary>应用系统。用于OAuthServer的子系统接口</summary>
    public partial interface IApp
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称。AppID</summary>
        String Name { get; set; }

        /// <summary>显示名</summary>
        String DisplayName { get; set; }

        /// <summary>密钥。AppSecret</summary>
        String Secret { get; set; }

        /// <summary>白名单</summary>
        String White { get; set; }

        /// <summary>黑名单。黑名单优先于白名单</summary>
        String Black { get; set; }

        /// <summary>启用</summary>
        Boolean Enable { get; set; }

        /// <summary>回调地址。用于限制回调地址安全性，多个地址逗号隔开</summary>
        String Urls { get; set; }

        /// <summary>次数</summary>
        Int32 Auths { get; set; }

        /// <summary>最后请求</summary>
        DateTime LastAuth { get; set; }

        /// <summary>内容</summary>
        String Remark { get; set; }

        /// <summary>创建者</summary>
        Int32 CreateUserID { get; set; }

        /// <summary>创建时间</summary>
        DateTime CreateTime { get; set; }

        /// <summary>创建地址</summary>
        String CreateIP { get; set; }

        /// <summary>更新者</summary>
        Int32 UpdateUserID { get; set; }

        /// <summary>更新时间</summary>
        DateTime UpdateTime { get; set; }

        /// <summary>更新地址</summary>
        String UpdateIP { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
        #endregion
    }
}