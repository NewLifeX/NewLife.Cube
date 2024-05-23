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

/// <summary>访问规则。控制系统访问的安全访问规则，放行或拦截或限流</summary>
[Serializable]
[DataObject]
[Description("访问规则。控制系统访问的安全访问规则，放行或拦截或限流")]
[BindIndex("IU_AccessRule_Name", true, "Name")]
[BindTable("AccessRule", Description = "访问规则。控制系统访问的安全访问规则，放行或拦截或限流", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class AccessRule
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private String _Name;
    /// <summary>名称</summary>
    [DisplayName("名称")]
    [Description("名称")]
    [DataObjectField(false, false, false, 50)]
    [BindColumn("Name", "名称", "", Master = true)]
    public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

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

    private String _Url;
    /// <summary>URL路径。支持*模糊匹配，多个逗号隔开</summary>
    [DisplayName("URL路径")]
    [Description("URL路径。支持*模糊匹配，多个逗号隔开")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Url", "URL路径。支持*模糊匹配，多个逗号隔开", "")]
    public String Url { get => _Url; set { if (OnPropertyChanging("Url", value)) { _Url = value; OnPropertyChanged("Url"); } } }

    private String _UserAgent;
    /// <summary>用户代理。支持*模糊匹配，多个逗号隔开</summary>
    [DisplayName("用户代理")]
    [Description("用户代理。支持*模糊匹配，多个逗号隔开")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("UserAgent", "用户代理。支持*模糊匹配，多个逗号隔开", "")]
    public String UserAgent { get => _UserAgent; set { if (OnPropertyChanging("UserAgent", value)) { _UserAgent = value; OnPropertyChanged("UserAgent"); } } }

    private String _IP;
    /// <summary>来源IP。支持*模糊匹配，多个逗号隔开</summary>
    [DisplayName("来源IP")]
    [Description("来源IP。支持*模糊匹配，多个逗号隔开")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("IP", "来源IP。支持*模糊匹配，多个逗号隔开", "")]
    public String IP { get => _IP; set { if (OnPropertyChanging("IP", value)) { _IP = value; OnPropertyChanged("IP"); } } }

    private String _LoginedUser;
    /// <summary>登录用户。支持*模糊匹配，多个逗号隔开</summary>
    [DisplayName("登录用户")]
    [Description("登录用户。支持*模糊匹配，多个逗号隔开")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("LoginedUser", "登录用户。支持*模糊匹配，多个逗号隔开", "")]
    public String LoginedUser { get => _LoginedUser; set { if (OnPropertyChanging("LoginedUser", value)) { _LoginedUser = value; OnPropertyChanged("LoginedUser"); } } }

    private AccessActionKinds _ActionKind;
    /// <summary>动作。放行/拦截/限流</summary>
    [DisplayName("动作")]
    [Description("动作。放行/拦截/限流")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ActionKind", "动作。放行/拦截/限流", "")]
    public AccessActionKinds ActionKind { get => _ActionKind; set { if (OnPropertyChanging("ActionKind", value)) { _ActionKind = value; OnPropertyChanged("ActionKind"); } } }

    private Int32 _BlockCode;
    /// <summary>拦截代码。拦截时返回Http代码，如404/500/302等</summary>
    [DisplayName("拦截代码")]
    [Description("拦截代码。拦截时返回Http代码，如404/500/302等")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("BlockCode", "拦截代码。拦截时返回Http代码，如404/500/302等", "")]
    public Int32 BlockCode { get => _BlockCode; set { if (OnPropertyChanging("BlockCode", value)) { _BlockCode = value; OnPropertyChanged("BlockCode"); } } }

    private String _BlockContent;
    /// <summary>拦截内容。拦截时返回内容，返回302时此处调目标地址</summary>
    [DisplayName("拦截内容")]
    [Description("拦截内容。拦截时返回内容，返回302时此处调目标地址")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("BlockContent", "拦截内容。拦截时返回内容，返回302时此处调目标地址", "")]
    public String BlockContent { get => _BlockContent; set { if (OnPropertyChanging("BlockContent", value)) { _BlockContent = value; OnPropertyChanged("BlockContent"); } } }

    private LimitDimensions _LimitDimension;
    /// <summary>限流维度。IP/用户</summary>
    [DisplayName("限流维度")]
    [Description("限流维度。IP/用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("LimitDimension", "限流维度。IP/用户", "")]
    public LimitDimensions LimitDimension { get => _LimitDimension; set { if (OnPropertyChanging("LimitDimension", value)) { _LimitDimension = value; OnPropertyChanged("LimitDimension"); } } }

    private Int32 _LimitCycle;
    /// <summary>限流时间。限流时的考察时间，期间累加规则触发次数，如600秒</summary>
    [DisplayName("限流时间")]
    [Description("限流时间。限流时的考察时间，期间累加规则触发次数，如600秒")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("LimitCycle", "限流时间。限流时的考察时间，期间累加规则触发次数，如600秒", "", ItemType = "TimeSpan")]
    public Int32 LimitCycle { get => _LimitCycle; set { if (OnPropertyChanging("LimitCycle", value)) { _LimitCycle = value; OnPropertyChanged("LimitCycle"); } } }

    private Int32 _LimitTimes;
    /// <summary>限流次数。限流考察期间达到该阈值时，执行拦截</summary>
    [DisplayName("限流次数")]
    [Description("限流次数。限流考察期间达到该阈值时，执行拦截")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("LimitTimes", "限流次数。限流考察期间达到该阈值时，执行拦截", "")]
    public Int32 LimitTimes { get => _LimitTimes; set { if (OnPropertyChanging("LimitTimes", value)) { _LimitTimes = value; OnPropertyChanged("LimitTimes"); } } }

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
            "Id" => _Id,
            "Name" => _Name,
            "Enable" => _Enable,
            "Priority" => _Priority,
            "Url" => _Url,
            "UserAgent" => _UserAgent,
            "IP" => _IP,
            "LoginedUser" => _LoginedUser,
            "ActionKind" => _ActionKind,
            "BlockCode" => _BlockCode,
            "BlockContent" => _BlockContent,
            "LimitDimension" => _LimitDimension,
            "LimitCycle" => _LimitCycle,
            "LimitTimes" => _LimitTimes,
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
                case "Name": _Name = Convert.ToString(value); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "Priority": _Priority = value.ToInt(); break;
                case "Url": _Url = Convert.ToString(value); break;
                case "UserAgent": _UserAgent = Convert.ToString(value); break;
                case "IP": _IP = Convert.ToString(value); break;
                case "LoginedUser": _LoginedUser = Convert.ToString(value); break;
                case "ActionKind": _ActionKind = (AccessActionKinds)value.ToInt(); break;
                case "BlockCode": _BlockCode = value.ToInt(); break;
                case "BlockContent": _BlockContent = Convert.ToString(value); break;
                case "LimitDimension": _LimitDimension = (LimitDimensions)value.ToInt(); break;
                case "LimitCycle": _LimitCycle = value.ToInt(); break;
                case "LimitTimes": _LimitTimes = value.ToInt(); break;
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

    #region 字段名
    /// <summary>取得访问规则字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>名称</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>优先级。较大优先</summary>
        public static readonly Field Priority = FindByName("Priority");

        /// <summary>URL路径。支持*模糊匹配，多个逗号隔开</summary>
        public static readonly Field Url = FindByName("Url");

        /// <summary>用户代理。支持*模糊匹配，多个逗号隔开</summary>
        public static readonly Field UserAgent = FindByName("UserAgent");

        /// <summary>来源IP。支持*模糊匹配，多个逗号隔开</summary>
        public static readonly Field IP = FindByName("IP");

        /// <summary>登录用户。支持*模糊匹配，多个逗号隔开</summary>
        public static readonly Field LoginedUser = FindByName("LoginedUser");

        /// <summary>动作。放行/拦截/限流</summary>
        public static readonly Field ActionKind = FindByName("ActionKind");

        /// <summary>拦截代码。拦截时返回Http代码，如404/500/302等</summary>
        public static readonly Field BlockCode = FindByName("BlockCode");

        /// <summary>拦截内容。拦截时返回内容，返回302时此处调目标地址</summary>
        public static readonly Field BlockContent = FindByName("BlockContent");

        /// <summary>限流维度。IP/用户</summary>
        public static readonly Field LimitDimension = FindByName("LimitDimension");

        /// <summary>限流时间。限流时的考察时间，期间累加规则触发次数，如600秒</summary>
        public static readonly Field LimitCycle = FindByName("LimitCycle");

        /// <summary>限流次数。限流考察期间达到该阈值时，执行拦截</summary>
        public static readonly Field LimitTimes = FindByName("LimitTimes");

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

    /// <summary>取得访问规则字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>名称</summary>
        public const String Name = "Name";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>优先级。较大优先</summary>
        public const String Priority = "Priority";

        /// <summary>URL路径。支持*模糊匹配，多个逗号隔开</summary>
        public const String Url = "Url";

        /// <summary>用户代理。支持*模糊匹配，多个逗号隔开</summary>
        public const String UserAgent = "UserAgent";

        /// <summary>来源IP。支持*模糊匹配，多个逗号隔开</summary>
        public const String IP = "IP";

        /// <summary>登录用户。支持*模糊匹配，多个逗号隔开</summary>
        public const String LoginedUser = "LoginedUser";

        /// <summary>动作。放行/拦截/限流</summary>
        public const String ActionKind = "ActionKind";

        /// <summary>拦截代码。拦截时返回Http代码，如404/500/302等</summary>
        public const String BlockCode = "BlockCode";

        /// <summary>拦截内容。拦截时返回内容，返回302时此处调目标地址</summary>
        public const String BlockContent = "BlockContent";

        /// <summary>限流维度。IP/用户</summary>
        public const String LimitDimension = "LimitDimension";

        /// <summary>限流时间。限流时的考察时间，期间累加规则触发次数，如600秒</summary>
        public const String LimitCycle = "LimitCycle";

        /// <summary>限流次数。限流考察期间达到该阈值时，执行拦截</summary>
        public const String LimitTimes = "LimitTimes";

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
