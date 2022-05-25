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
    /// <summary>OAuth日志。用于记录OAuth客户端请求，同时Id作为state，避免向OAuthServer泄漏本机Url</summary>
    [Serializable]
    [DataObject]
    [Description("OAuth日志。用于记录OAuth客户端请求，同时Id作为state，避免向OAuthServer泄漏本机Url")]
    [BindIndex("IX_OAuthLog_Provider", false, "Provider")]
    [BindIndex("IX_OAuthLog_ConnectId", false, "ConnectId")]
    [BindIndex("IX_OAuthLog_UserId", false, "UserId")]
    [BindTable("OAuthLog", Description = "OAuth日志。用于记录OAuth客户端请求，同时Id作为state，避免向OAuthServer泄漏本机Url", ConnName = "Cube", DbType = DatabaseType.None)]
    public partial class OAuthLog
    {
        #region 属性
        private Int64 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, false, false, 0)]
        [BindColumn("Id", "编号", "")]
        public Int64 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String _Provider;
        /// <summary>提供商</summary>
        [DisplayName("提供商")]
        [Description("提供商")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Provider", "提供商", "")]
        public String Provider { get => _Provider; set { if (OnPropertyChanging("Provider", value)) { _Provider = value; OnPropertyChanged("Provider"); } } }

        private Int32 _ConnectId;
        /// <summary>链接</summary>
        [DisplayName("链接")]
        [Description("链接")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("ConnectId", "链接", "")]
        public Int32 ConnectId { get => _ConnectId; set { if (OnPropertyChanging("ConnectId", value)) { _ConnectId = value; OnPropertyChanged("ConnectId"); } } }

        private Int32 _UserId;
        /// <summary>用户</summary>
        [DisplayName("用户")]
        [Description("用户")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("UserId", "用户", "")]
        public Int32 UserId { get => _UserId; set { if (OnPropertyChanging("UserId", value)) { _UserId = value; OnPropertyChanged("UserId"); } } }

        private String _Action;
        /// <summary>操作</summary>
        [DisplayName("操作")]
        [Description("操作")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Action", "操作", "")]
        public String Action { get => _Action; set { if (OnPropertyChanging("Action", value)) { _Action = value; OnPropertyChanged("Action"); } } }

        private Boolean _Success;
        /// <summary>成功</summary>
        [DisplayName("成功")]
        [Description("成功")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Success", "成功", "")]
        public Boolean Success { get => _Success; set { if (OnPropertyChanging("Success", value)) { _Success = value; OnPropertyChanged("Success"); } } }

        private String _RedirectUri;
        /// <summary>回调地址</summary>
        [DisplayName("回调地址")]
        [Description("回调地址")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("RedirectUri", "回调地址", "")]
        public String RedirectUri { get => _RedirectUri; set { if (OnPropertyChanging("RedirectUri", value)) { _RedirectUri = value; OnPropertyChanged("RedirectUri"); } } }

        private String _ResponseType;
        /// <summary>响应类型。默认code</summary>
        [DisplayName("响应类型")]
        [Description("响应类型。默认code")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("ResponseType", "响应类型。默认code", "")]
        public String ResponseType { get => _ResponseType; set { if (OnPropertyChanging("ResponseType", value)) { _ResponseType = value; OnPropertyChanged("ResponseType"); } } }

        private String _Scope;
        /// <summary>授权域</summary>
        [DisplayName("授权域")]
        [Description("授权域")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Scope", "授权域", "")]
        public String Scope { get => _Scope; set { if (OnPropertyChanging("Scope", value)) { _Scope = value; OnPropertyChanged("Scope"); } } }

        private String _State;
        /// <summary>状态数据</summary>
        [DisplayName("状态数据")]
        [Description("状态数据")]
        [DataObjectField(false, false, true, 500)]
        [BindColumn("State", "状态数据", "")]
        public String State { get => _State; set { if (OnPropertyChanging("State", value)) { _State = value; OnPropertyChanged("State"); } } }

        private String _Source;
        /// <summary>来源</summary>
        [DisplayName("来源")]
        [Description("来源")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("Source", "来源", "")]
        public String Source { get => _Source; set { if (OnPropertyChanging("Source", value)) { _Source = value; OnPropertyChanged("Source"); } } }

        private String _AccessToken;
        /// <summary>访问令牌</summary>
        [DisplayName("访问令牌")]
        [Description("访问令牌")]
        [DataObjectField(false, false, true, 2000)]
        [BindColumn("AccessToken", "访问令牌", "")]
        public String AccessToken { get => _AccessToken; set { if (OnPropertyChanging("AccessToken", value)) { _AccessToken = value; OnPropertyChanged("AccessToken"); } } }

        private String _RefreshToken;
        /// <summary>刷新令牌</summary>
        [DisplayName("刷新令牌")]
        [Description("刷新令牌")]
        [DataObjectField(false, false, true, 2000)]
        [BindColumn("RefreshToken", "刷新令牌", "")]
        public String RefreshToken { get => _RefreshToken; set { if (OnPropertyChanging("RefreshToken", value)) { _RefreshToken = value; OnPropertyChanged("RefreshToken"); } } }

        private String _TraceId;
        /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
        [DisplayName("追踪")]
        [Description("追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("TraceId", "追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链", "")]
        public String TraceId { get => _TraceId; set { if (OnPropertyChanging("TraceId", value)) { _TraceId = value; OnPropertyChanged("TraceId"); } } }

        private String _Remark;
        /// <summary>详细信息</summary>
        [DisplayName("详细信息")]
        [Description("详细信息")]
        [DataObjectField(false, false, true, 2000)]
        [BindColumn("Remark", "详细信息", "")]
        public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }

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
                    case "Id": return _Id;
                    case "Provider": return _Provider;
                    case "ConnectId": return _ConnectId;
                    case "UserId": return _UserId;
                    case "Action": return _Action;
                    case "Success": return _Success;
                    case "RedirectUri": return _RedirectUri;
                    case "ResponseType": return _ResponseType;
                    case "Scope": return _Scope;
                    case "State": return _State;
                    case "Source": return _Source;
                    case "AccessToken": return _AccessToken;
                    case "RefreshToken": return _RefreshToken;
                    case "TraceId": return _TraceId;
                    case "Remark": return _Remark;
                    case "CreateIP": return _CreateIP;
                    case "CreateTime": return _CreateTime;
                    case "UpdateTime": return _UpdateTime;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToLong(); break;
                    case "Provider": _Provider = Convert.ToString(value); break;
                    case "ConnectId": _ConnectId = value.ToInt(); break;
                    case "UserId": _UserId = value.ToInt(); break;
                    case "Action": _Action = Convert.ToString(value); break;
                    case "Success": _Success = value.ToBoolean(); break;
                    case "RedirectUri": _RedirectUri = Convert.ToString(value); break;
                    case "ResponseType": _ResponseType = Convert.ToString(value); break;
                    case "Scope": _Scope = Convert.ToString(value); break;
                    case "State": _State = Convert.ToString(value); break;
                    case "Source": _Source = Convert.ToString(value); break;
                    case "AccessToken": _AccessToken = Convert.ToString(value); break;
                    case "RefreshToken": _RefreshToken = Convert.ToString(value); break;
                    case "TraceId": _TraceId = Convert.ToString(value); break;
                    case "Remark": _Remark = Convert.ToString(value); break;
                    case "CreateIP": _CreateIP = Convert.ToString(value); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得OAuth日志字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field Id = FindByName("Id");

            /// <summary>提供商</summary>
            public static readonly Field Provider = FindByName("Provider");

            /// <summary>链接</summary>
            public static readonly Field ConnectId = FindByName("ConnectId");

            /// <summary>用户</summary>
            public static readonly Field UserId = FindByName("UserId");

            /// <summary>操作</summary>
            public static readonly Field Action = FindByName("Action");

            /// <summary>成功</summary>
            public static readonly Field Success = FindByName("Success");

            /// <summary>回调地址</summary>
            public static readonly Field RedirectUri = FindByName("RedirectUri");

            /// <summary>响应类型。默认code</summary>
            public static readonly Field ResponseType = FindByName("ResponseType");

            /// <summary>授权域</summary>
            public static readonly Field Scope = FindByName("Scope");

            /// <summary>状态数据</summary>
            public static readonly Field State = FindByName("State");

            /// <summary>来源</summary>
            public static readonly Field Source = FindByName("Source");

            /// <summary>访问令牌</summary>
            public static readonly Field AccessToken = FindByName("AccessToken");

            /// <summary>刷新令牌</summary>
            public static readonly Field RefreshToken = FindByName("RefreshToken");

            /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
            public static readonly Field TraceId = FindByName("TraceId");

            /// <summary>详细信息</summary>
            public static readonly Field Remark = FindByName("Remark");

            /// <summary>创建地址</summary>
            public static readonly Field CreateIP = FindByName("CreateIP");

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName("CreateTime");

            /// <summary>更新时间</summary>
            public static readonly Field UpdateTime = FindByName("UpdateTime");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得OAuth日志字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String Id = "Id";

            /// <summary>提供商</summary>
            public const String Provider = "Provider";

            /// <summary>链接</summary>
            public const String ConnectId = "ConnectId";

            /// <summary>用户</summary>
            public const String UserId = "UserId";

            /// <summary>操作</summary>
            public const String Action = "Action";

            /// <summary>成功</summary>
            public const String Success = "Success";

            /// <summary>回调地址</summary>
            public const String RedirectUri = "RedirectUri";

            /// <summary>响应类型。默认code</summary>
            public const String ResponseType = "ResponseType";

            /// <summary>授权域</summary>
            public const String Scope = "Scope";

            /// <summary>状态数据</summary>
            public const String State = "State";

            /// <summary>来源</summary>
            public const String Source = "Source";

            /// <summary>访问令牌</summary>
            public const String AccessToken = "AccessToken";

            /// <summary>刷新令牌</summary>
            public const String RefreshToken = "RefreshToken";

            /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
            public const String TraceId = "TraceId";

            /// <summary>详细信息</summary>
            public const String Remark = "Remark";

            /// <summary>创建地址</summary>
            public const String CreateIP = "CreateIP";

            /// <summary>创建时间</summary>
            public const String CreateTime = "CreateTime";

            /// <summary>更新时间</summary>
            public const String UpdateTime = "UpdateTime";
        }
        #endregion
    }
}