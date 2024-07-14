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

/// <summary>应用日志。用于OAuthServer的子系统</summary>
[Serializable]
[DataObject]
[Description("应用日志。用于OAuthServer的子系统")]
[BindIndex("IX_AppLog_AppId", false, "AppId")]
[BindTable("AppLog", Description = "应用日志。用于OAuthServer的子系统", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class AppLog
{
    #region 属性
    private Int64 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, false, false, 0)]
    [BindColumn("Id", "编号", "", DataScale = "time")]
    public Int64 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int32 _AppId;
    /// <summary>应用</summary>
    [DisplayName("应用")]
    [Description("应用")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("AppId", "应用", "")]
    public Int32 AppId { get => _AppId; set { if (OnPropertyChanging("AppId", value)) { _AppId = value; OnPropertyChanged("AppId"); } } }

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

    private String _ClientId;
    /// <summary>应用标识</summary>
    [DisplayName("应用标识")]
    [Description("应用标识")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("ClientId", "应用标识", "")]
    public String ClientId { get => _ClientId; set { if (OnPropertyChanging("ClientId", value)) { _ClientId = value; OnPropertyChanged("ClientId"); } } }

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
    [DataObjectField(false, false, true, 200)]
    [BindColumn("TraceId", "追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链", "")]
    public String TraceId { get => _TraceId; set { if (OnPropertyChanging("TraceId", value)) { _TraceId = value; OnPropertyChanged("TraceId"); } } }

    private String _CreateUser;
    /// <summary>创建者。可以是设备编码等唯一使用者标识</summary>
    [Category("扩展")]
    [DisplayName("创建者")]
    [Description("创建者。可以是设备编码等唯一使用者标识")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateUser", "创建者。可以是设备编码等唯一使用者标识", "")]
    public String CreateUser { get => _CreateUser; set { if (OnPropertyChanging("CreateUser", value)) { _CreateUser = value; OnPropertyChanged("CreateUser"); } } }

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
    [Category("扩展")]
    [DisplayName("备注")]
    [Description("备注")]
    [DataObjectField(false, false, true, 2000)]
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
            "Id" => _Id,
            "AppId" => _AppId,
            "Action" => _Action,
            "Success" => _Success,
            "ClientId" => _ClientId,
            "RedirectUri" => _RedirectUri,
            "ResponseType" => _ResponseType,
            "Scope" => _Scope,
            "State" => _State,
            "AccessToken" => _AccessToken,
            "RefreshToken" => _RefreshToken,
            "TraceId" => _TraceId,
            "CreateUser" => _CreateUser,
            "CreateIP" => _CreateIP,
            "CreateTime" => _CreateTime,
            "UpdateIP" => _UpdateIP,
            "UpdateTime" => _UpdateTime,
            "Remark" => _Remark,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToLong(); break;
                case "AppId": _AppId = value.ToInt(); break;
                case "Action": _Action = Convert.ToString(value); break;
                case "Success": _Success = value.ToBoolean(); break;
                case "ClientId": _ClientId = Convert.ToString(value); break;
                case "RedirectUri": _RedirectUri = Convert.ToString(value); break;
                case "ResponseType": _ResponseType = Convert.ToString(value); break;
                case "Scope": _Scope = Convert.ToString(value); break;
                case "State": _State = Convert.ToString(value); break;
                case "AccessToken": _AccessToken = Convert.ToString(value); break;
                case "RefreshToken": _RefreshToken = Convert.ToString(value); break;
                case "TraceId": _TraceId = Convert.ToString(value); break;
                case "CreateUser": _CreateUser = Convert.ToString(value); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
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

    #region 扩展查询
    #endregion

    #region 数据清理
    /// <summary>清理指定时间段内的数据</summary>
    /// <param name="start">开始时间。未指定时清理小于指定时间的所有数据</param>
    /// <param name="end">结束时间</param>
    /// <returns>清理行数</returns>
    public static Int32 DeleteWith(DateTime start, DateTime end)
    {
        return Delete(_.Id.Between(start, end, Meta.Factory.Snow));
    }
    #endregion

    #region 字段名
    /// <summary>取得应用日志字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>应用</summary>
        public static readonly Field AppId = FindByName("AppId");

        /// <summary>操作</summary>
        public static readonly Field Action = FindByName("Action");

        /// <summary>成功</summary>
        public static readonly Field Success = FindByName("Success");

        /// <summary>应用标识</summary>
        public static readonly Field ClientId = FindByName("ClientId");

        /// <summary>回调地址</summary>
        public static readonly Field RedirectUri = FindByName("RedirectUri");

        /// <summary>响应类型。默认code</summary>
        public static readonly Field ResponseType = FindByName("ResponseType");

        /// <summary>授权域</summary>
        public static readonly Field Scope = FindByName("Scope");

        /// <summary>状态数据</summary>
        public static readonly Field State = FindByName("State");

        /// <summary>访问令牌</summary>
        public static readonly Field AccessToken = FindByName("AccessToken");

        /// <summary>刷新令牌</summary>
        public static readonly Field RefreshToken = FindByName("RefreshToken");

        /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
        public static readonly Field TraceId = FindByName("TraceId");

        /// <summary>创建者。可以是设备编码等唯一使用者标识</summary>
        public static readonly Field CreateUser = FindByName("CreateUser");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>备注</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得应用日志字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>应用</summary>
        public const String AppId = "AppId";

        /// <summary>操作</summary>
        public const String Action = "Action";

        /// <summary>成功</summary>
        public const String Success = "Success";

        /// <summary>应用标识</summary>
        public const String ClientId = "ClientId";

        /// <summary>回调地址</summary>
        public const String RedirectUri = "RedirectUri";

        /// <summary>响应类型。默认code</summary>
        public const String ResponseType = "ResponseType";

        /// <summary>授权域</summary>
        public const String Scope = "Scope";

        /// <summary>状态数据</summary>
        public const String State = "State";

        /// <summary>访问令牌</summary>
        public const String AccessToken = "AccessToken";

        /// <summary>刷新令牌</summary>
        public const String RefreshToken = "RefreshToken";

        /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
        public const String TraceId = "TraceId";

        /// <summary>创建者。可以是设备编码等唯一使用者标识</summary>
        public const String CreateUser = "CreateUser";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>备注</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
