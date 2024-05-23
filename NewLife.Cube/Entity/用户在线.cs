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

/// <summary>用户在线</summary>
[Serializable]
[DataObject]
[Description("用户在线")]
[BindIndex("IX_UserOnline_UserID", false, "UserID")]
[BindIndex("IX_UserOnline_SessionID", false, "SessionID")]
[BindIndex("IX_UserOnline_CreateTime", false, "CreateTime")]
[BindTable("UserOnline", Description = "用户在线", ConnName = "Log", DbType = DatabaseType.None)]
public partial class UserOnline
{
    #region 属性
    private Int32 _ID;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("ID", "编号", "")]
    public Int32 ID { get => _ID; set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } } }

    private Int32 _UserID;
    /// <summary>用户。当前登录人</summary>
    [DisplayName("用户")]
    [Description("用户。当前登录人")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UserID", "用户。当前登录人", "")]
    public Int32 UserID { get => _UserID; set { if (OnPropertyChanging("UserID", value)) { _UserID = value; OnPropertyChanged("UserID"); } } }

    private String _Name;
    /// <summary>名称。当前登录人，或根据设备标识推算出来的使用人</summary>
    [DisplayName("名称")]
    [Description("名称。当前登录人，或根据设备标识推算出来的使用人")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Name", "名称。当前登录人，或根据设备标识推算出来的使用人", "", Master = true)]
    public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

    private String _SessionID;
    /// <summary>会话。Web的SessionID或Server的会话编号</summary>
    [DisplayName("会话")]
    [Description("会话。Web的SessionID或Server的会话编号")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("SessionID", "会话。Web的SessionID或Server的会话编号", "")]
    public String SessionID { get => _SessionID; set { if (OnPropertyChanging("SessionID", value)) { _SessionID = value; OnPropertyChanged("SessionID"); } } }

    private String _OAuthProvider;
    /// <summary>登录方。OAuth提供商，从哪个渠道登录</summary>
    [DisplayName("登录方")]
    [Description("登录方。OAuth提供商，从哪个渠道登录")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("OAuthProvider", "登录方。OAuth提供商，从哪个渠道登录", "")]
    public String OAuthProvider { get => _OAuthProvider; set { if (OnPropertyChanging("OAuthProvider", value)) { _OAuthProvider = value; OnPropertyChanged("OAuthProvider"); } } }

    private Int32 _Times;
    /// <summary>次数</summary>
    [DisplayName("次数")]
    [Description("次数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Times", "次数", "")]
    public Int32 Times { get => _Times; set { if (OnPropertyChanging("Times", value)) { _Times = value; OnPropertyChanged("Times"); } } }

    private String _Page;
    /// <summary>页面</summary>
    [DisplayName("页面")]
    [Description("页面")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Page", "页面", "")]
    public String Page { get => _Page; set { if (OnPropertyChanging("Page", value)) { _Page = value; OnPropertyChanged("Page"); } } }

    private String _Platform;
    /// <summary>平台。操作系统平台，Windows/Linux/Android等</summary>
    [DisplayName("平台")]
    [Description("平台。操作系统平台，Windows/Linux/Android等")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Platform", "平台。操作系统平台，Windows/Linux/Android等", "")]
    public String Platform { get => _Platform; set { if (OnPropertyChanging("Platform", value)) { _Platform = value; OnPropertyChanged("Platform"); } } }

    private String _OS;
    /// <summary>系统。操作系统，带版本</summary>
    [DisplayName("系统")]
    [Description("系统。操作系统，带版本")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("OS", "系统。操作系统，带版本", "")]
    public String OS { get => _OS; set { if (OnPropertyChanging("OS", value)) { _OS = value; OnPropertyChanged("OS"); } } }

    private String _Device;
    /// <summary>设备。手机品牌型号</summary>
    [DisplayName("设备")]
    [Description("设备。手机品牌型号")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Device", "设备。手机品牌型号", "")]
    public String Device { get => _Device; set { if (OnPropertyChanging("Device", value)) { _Device = value; OnPropertyChanged("Device"); } } }

    private String _Brower;
    /// <summary>用户代理。浏览器名称，带版本</summary>
    [DisplayName("用户代理")]
    [Description("用户代理。浏览器名称，带版本")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Brower", "用户代理。浏览器名称，带版本", "")]
    public String Brower { get => _Brower; set { if (OnPropertyChanging("Brower", value)) { _Brower = value; OnPropertyChanged("Brower"); } } }

    private String _NetType;
    /// <summary>网络。微信访问时，感知到WIFI或4G网络</summary>
    [DisplayName("网络")]
    [Description("网络。微信访问时，感知到WIFI或4G网络")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("NetType", "网络。微信访问时，感知到WIFI或4G网络", "")]
    public String NetType { get => _NetType; set { if (OnPropertyChanging("NetType", value)) { _NetType = value; OnPropertyChanged("NetType"); } } }

    private String _DeviceId;
    /// <summary>设备标识。唯一标识设备，位于浏览器Cookie，重装后改变</summary>
    [DisplayName("设备标识")]
    [Description("设备标识。唯一标识设备，位于浏览器Cookie，重装后改变")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("DeviceId", "设备标识。唯一标识设备，位于浏览器Cookie，重装后改变", "")]
    public String DeviceId { get => _DeviceId; set { if (OnPropertyChanging("DeviceId", value)) { _DeviceId = value; OnPropertyChanged("DeviceId"); } } }

    private String _Status;
    /// <summary>状态</summary>
    [DisplayName("状态")]
    [Description("状态")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Status", "状态", "")]
    public String Status { get => _Status; set { if (OnPropertyChanging("Status", value)) { _Status = value; OnPropertyChanged("Status"); } } }

    private Int32 _OnlineTime;
    /// <summary>在线时间。本次在线总时间，秒</summary>
    [DisplayName("在线时间")]
    [Description("在线时间。本次在线总时间，秒")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("OnlineTime", "在线时间。本次在线总时间，秒", "", ItemType = "TimeSpan")]
    public Int32 OnlineTime { get => _OnlineTime; set { if (OnPropertyChanging("OnlineTime", value)) { _OnlineTime = value; OnPropertyChanged("OnlineTime"); } } }

    private DateTime _LastError;
    /// <summary>最后错误</summary>
    [DisplayName("最后错误")]
    [Description("最后错误")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("LastError", "最后错误", "")]
    public DateTime LastError { get => _LastError; set { if (OnPropertyChanging("LastError", value)) { _LastError = value; OnPropertyChanged("LastError"); } } }

    private String _Address;
    /// <summary>地址。根据IP计算</summary>
    [DisplayName("地址")]
    [Description("地址。根据IP计算")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Address", "地址。根据IP计算", "")]
    public String Address { get => _Address; set { if (OnPropertyChanging("Address", value)) { _Address = value; OnPropertyChanged("Address"); } } }

    private String _TraceId;
    /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
    [Category("扩展")]
    [DisplayName("追踪")]
    [Description("追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("TraceId", "追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链", "")]
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
    [DataObjectField(false, false, false, 0)]
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
    /// <summary>修改时间</summary>
    [Category("扩展")]
    [DisplayName("修改时间")]
    [Description("修改时间")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UpdateTime", "修改时间", "")]
    public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }
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
            "UserID" => _UserID,
            "Name" => _Name,
            "SessionID" => _SessionID,
            "OAuthProvider" => _OAuthProvider,
            "Times" => _Times,
            "Page" => _Page,
            "Platform" => _Platform,
            "OS" => _OS,
            "Device" => _Device,
            "Brower" => _Brower,
            "NetType" => _NetType,
            "DeviceId" => _DeviceId,
            "Status" => _Status,
            "OnlineTime" => _OnlineTime,
            "LastError" => _LastError,
            "Address" => _Address,
            "TraceId" => _TraceId,
            "CreateIP" => _CreateIP,
            "CreateTime" => _CreateTime,
            "UpdateIP" => _UpdateIP,
            "UpdateTime" => _UpdateTime,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "ID": _ID = value.ToInt(); break;
                case "UserID": _UserID = value.ToInt(); break;
                case "Name": _Name = Convert.ToString(value); break;
                case "SessionID": _SessionID = Convert.ToString(value); break;
                case "OAuthProvider": _OAuthProvider = Convert.ToString(value); break;
                case "Times": _Times = value.ToInt(); break;
                case "Page": _Page = Convert.ToString(value); break;
                case "Platform": _Platform = Convert.ToString(value); break;
                case "OS": _OS = Convert.ToString(value); break;
                case "Device": _Device = Convert.ToString(value); break;
                case "Brower": _Brower = Convert.ToString(value); break;
                case "NetType": _NetType = Convert.ToString(value); break;
                case "DeviceId": _DeviceId = Convert.ToString(value); break;
                case "Status": _Status = Convert.ToString(value); break;
                case "OnlineTime": _OnlineTime = value.ToInt(); break;
                case "LastError": _LastError = value.ToDateTime(); break;
                case "Address": _Address = Convert.ToString(value); break;
                case "TraceId": _TraceId = Convert.ToString(value); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion

    #region 关联映射
    #endregion

    #region 字段名
    /// <summary>取得用户在线字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field ID = FindByName("ID");

        /// <summary>用户。当前登录人</summary>
        public static readonly Field UserID = FindByName("UserID");

        /// <summary>名称。当前登录人，或根据设备标识推算出来的使用人</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>会话。Web的SessionID或Server的会话编号</summary>
        public static readonly Field SessionID = FindByName("SessionID");

        /// <summary>登录方。OAuth提供商，从哪个渠道登录</summary>
        public static readonly Field OAuthProvider = FindByName("OAuthProvider");

        /// <summary>次数</summary>
        public static readonly Field Times = FindByName("Times");

        /// <summary>页面</summary>
        public static readonly Field Page = FindByName("Page");

        /// <summary>平台。操作系统平台，Windows/Linux/Android等</summary>
        public static readonly Field Platform = FindByName("Platform");

        /// <summary>系统。操作系统，带版本</summary>
        public static readonly Field OS = FindByName("OS");

        /// <summary>设备。手机品牌型号</summary>
        public static readonly Field Device = FindByName("Device");

        /// <summary>用户代理。浏览器名称，带版本</summary>
        public static readonly Field Brower = FindByName("Brower");

        /// <summary>网络。微信访问时，感知到WIFI或4G网络</summary>
        public static readonly Field NetType = FindByName("NetType");

        /// <summary>设备标识。唯一标识设备，位于浏览器Cookie，重装后改变</summary>
        public static readonly Field DeviceId = FindByName("DeviceId");

        /// <summary>状态</summary>
        public static readonly Field Status = FindByName("Status");

        /// <summary>在线时间。本次在线总时间，秒</summary>
        public static readonly Field OnlineTime = FindByName("OnlineTime");

        /// <summary>最后错误</summary>
        public static readonly Field LastError = FindByName("LastError");

        /// <summary>地址。根据IP计算</summary>
        public static readonly Field Address = FindByName("Address");

        /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
        public static readonly Field TraceId = FindByName("TraceId");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        /// <summary>修改时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得用户在线字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String ID = "ID";

        /// <summary>用户。当前登录人</summary>
        public const String UserID = "UserID";

        /// <summary>名称。当前登录人，或根据设备标识推算出来的使用人</summary>
        public const String Name = "Name";

        /// <summary>会话。Web的SessionID或Server的会话编号</summary>
        public const String SessionID = "SessionID";

        /// <summary>登录方。OAuth提供商，从哪个渠道登录</summary>
        public const String OAuthProvider = "OAuthProvider";

        /// <summary>次数</summary>
        public const String Times = "Times";

        /// <summary>页面</summary>
        public const String Page = "Page";

        /// <summary>平台。操作系统平台，Windows/Linux/Android等</summary>
        public const String Platform = "Platform";

        /// <summary>系统。操作系统，带版本</summary>
        public const String OS = "OS";

        /// <summary>设备。手机品牌型号</summary>
        public const String Device = "Device";

        /// <summary>用户代理。浏览器名称，带版本</summary>
        public const String Brower = "Brower";

        /// <summary>网络。微信访问时，感知到WIFI或4G网络</summary>
        public const String NetType = "NetType";

        /// <summary>设备标识。唯一标识设备，位于浏览器Cookie，重装后改变</summary>
        public const String DeviceId = "DeviceId";

        /// <summary>状态</summary>
        public const String Status = "Status";

        /// <summary>在线时间。本次在线总时间，秒</summary>
        public const String OnlineTime = "OnlineTime";

        /// <summary>最后错误</summary>
        public const String LastError = "LastError";

        /// <summary>地址。根据IP计算</summary>
        public const String Address = "Address";

        /// <summary>追踪。链路追踪，用于APM性能追踪定位，还原该事件的调用链</summary>
        public const String TraceId = "TraceId";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";

        /// <summary>修改时间</summary>
        public const String UpdateTime = "UpdateTime";
    }
    #endregion
}
