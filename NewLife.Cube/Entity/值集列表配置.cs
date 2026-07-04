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

/// <summary>值集列表配置。列表型值集的数据源配置</summary>
[Serializable]
[DataObject]
[Description("值集列表配置。列表型值集的数据源配置")]
[BindIndex("IU_LovListConfig_LovDefId", true, "LovDefId")]
[BindTable("LovListConfig", Description = "值集列表配置。列表型值集的数据源配置", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class LovListConfig : IEntity<LovListConfigModel>
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int32 _LovDefId;
    /// <summary>值集定义。关联LovDefinition</summary>
    [DisplayName("值集定义")]
    [Description("值集定义。关联LovDefinition")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("LovDefId", "值集定义。关联LovDefinition", "")]
    public Int32 LovDefId { get => _LovDefId; set { if (OnPropertyChanging("LovDefId", value)) { _LovDefId = value; OnPropertyChanged("LovDefId"); } } }

    private String _RequestUrl;
    /// <summary>请求地址。数据接口地址，仅后端可见</summary>
    [DisplayName("请求地址")]
    [Description("请求地址。数据接口地址，仅后端可见")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("RequestUrl", "请求地址。数据接口地址，仅后端可见", "")]
    public String RequestUrl { get => _RequestUrl; set { if (OnPropertyChanging("RequestUrl", value)) { _RequestUrl = value; OnPropertyChanged("RequestUrl"); } } }

    private String _Method;
    /// <summary>请求方式。GET/POST</summary>
    [DisplayName("请求方式")]
    [Description("请求方式。GET/POST")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Method", "请求方式。GET/POST", "")]
    public String Method { get => _Method; set { if (OnPropertyChanging("Method", value)) { _Method = value; OnPropertyChanged("Method"); } } }

    private Boolean _Pageable;
    /// <summary>是否分页</summary>
    [DisplayName("是否分页")]
    [Description("是否分页")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Pageable", "是否分页", "")]
    public Boolean Pageable { get => _Pageable; set { if (OnPropertyChanging("Pageable", value)) { _Pageable = value; OnPropertyChanged("Pageable"); } } }

    private String _PageNumField;
    /// <summary>页码字段名。分页时页码参数名</summary>
    [DisplayName("页码字段名")]
    [Description("页码字段名。分页时页码参数名")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("PageNumField", "页码字段名。分页时页码参数名", "")]
    public String PageNumField { get => _PageNumField; set { if (OnPropertyChanging("PageNumField", value)) { _PageNumField = value; OnPropertyChanged("PageNumField"); } } }

    private String _PageSizeField;
    /// <summary>页量字段名。分页时每页条数参数名</summary>
    [DisplayName("页量字段名")]
    [Description("页量字段名。分页时每页条数参数名")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("PageSizeField", "页量字段名。分页时每页条数参数名", "")]
    public String PageSizeField { get => _PageSizeField; set { if (OnPropertyChanging("PageSizeField", value)) { _PageSizeField = value; OnPropertyChanged("PageSizeField"); } } }

    private String _DataPath;
    /// <summary>数据路径。从响应中提取数据列表的JSON路径</summary>
    [DisplayName("数据路径")]
    [Description("数据路径。从响应中提取数据列表的JSON路径")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("DataPath", "数据路径。从响应中提取数据列表的JSON路径", "")]
    public String DataPath { get => _DataPath; set { if (OnPropertyChanging("DataPath", value)) { _DataPath = value; OnPropertyChanged("DataPath"); } } }

    private String _TotalPath;
    /// <summary>总量路径。从响应中提取总数的JSON路径</summary>
    [DisplayName("总量路径")]
    [Description("总量路径。从响应中提取总数的JSON路径")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("TotalPath", "总量路径。从响应中提取总数的JSON路径", "")]
    public String TotalPath { get => _TotalPath; set { if (OnPropertyChanging("TotalPath", value)) { _TotalPath = value; OnPropertyChanged("TotalPath"); } } }

    private String _FixedParams;
    /// <summary>固定参数。每次请求附加的固定参数，JSON格式</summary>
    [DisplayName("固定参数")]
    [Description("固定参数。每次请求附加的固定参数，JSON格式")]
    [DataObjectField(false, false, true, 2000)]
    [BindColumn("FixedParams", "固定参数。每次请求附加的固定参数，JSON格式", "")]
    public String FixedParams { get => _FixedParams; set { if (OnPropertyChanging("FixedParams", value)) { _FixedParams = value; OnPropertyChanged("FixedParams"); } } }

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
    public void Copy(LovListConfigModel model)
    {
        Id = model.Id;
        LovDefId = model.LovDefId;
        RequestUrl = model.RequestUrl;
        Method = model.Method;
        Pageable = model.Pageable;
        PageNumField = model.PageNumField;
        PageSizeField = model.PageSizeField;
        DataPath = model.DataPath;
        TotalPath = model.TotalPath;
        FixedParams = model.FixedParams;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateUserID = model.UpdateUserID;
        UpdateIP = model.UpdateIP;
        UpdateTime = model.UpdateTime;
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
            "LovDefId" => _LovDefId,
            "RequestUrl" => _RequestUrl,
            "Method" => _Method,
            "Pageable" => _Pageable,
            "PageNumField" => _PageNumField,
            "PageSizeField" => _PageSizeField,
            "DataPath" => _DataPath,
            "TotalPath" => _TotalPath,
            "FixedParams" => _FixedParams,
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
                case "Id": _Id = value.ToInt(); break;
                case "LovDefId": _LovDefId = value.ToInt(); break;
                case "RequestUrl": _RequestUrl = Convert.ToString(value); break;
                case "Method": _Method = Convert.ToString(value); break;
                case "Pageable": _Pageable = value.ToBoolean(); break;
                case "PageNumField": _PageNumField = Convert.ToString(value); break;
                case "PageSizeField": _PageSizeField = Convert.ToString(value); break;
                case "DataPath": _DataPath = Convert.ToString(value); break;
                case "TotalPath": _TotalPath = Convert.ToString(value); break;
                case "FixedParams": _FixedParams = Convert.ToString(value); break;
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

    #region 扩展查询
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static LovListConfig FindById(Int32 id)
    {
        if (id < 0) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

        // 单对象缓存
        return Meta.SingleCache[id];

        //return Find(_.Id == id);
    }

    /// <summary>根据值集定义查找</summary>
    /// <param name="lovDefId">值集定义</param>
    /// <returns>实体对象</returns>
    public static LovListConfig FindByLovDefId(Int32 lovDefId)
    {
        if (lovDefId < 0) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.LovDefId == lovDefId);

        return Find(_.LovDefId == lovDefId);
    }
    #endregion

    #region 字段名
    /// <summary>取得值集列表配置字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>值集定义。关联LovDefinition</summary>
        public static readonly Field LovDefId = FindByName("LovDefId");

        /// <summary>请求地址。数据接口地址，仅后端可见</summary>
        public static readonly Field RequestUrl = FindByName("RequestUrl");

        /// <summary>请求方式。GET/POST</summary>
        public static readonly Field Method = FindByName("Method");

        /// <summary>是否分页</summary>
        public static readonly Field Pageable = FindByName("Pageable");

        /// <summary>页码字段名。分页时页码参数名</summary>
        public static readonly Field PageNumField = FindByName("PageNumField");

        /// <summary>页量字段名。分页时每页条数参数名</summary>
        public static readonly Field PageSizeField = FindByName("PageSizeField");

        /// <summary>数据路径。从响应中提取数据列表的JSON路径</summary>
        public static readonly Field DataPath = FindByName("DataPath");

        /// <summary>总量路径。从响应中提取总数的JSON路径</summary>
        public static readonly Field TotalPath = FindByName("TotalPath");

        /// <summary>固定参数。每次请求附加的固定参数，JSON格式</summary>
        public static readonly Field FixedParams = FindByName("FixedParams");

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

    /// <summary>取得值集列表配置字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>值集定义。关联LovDefinition</summary>
        public const String LovDefId = "LovDefId";

        /// <summary>请求地址。数据接口地址，仅后端可见</summary>
        public const String RequestUrl = "RequestUrl";

        /// <summary>请求方式。GET/POST</summary>
        public const String Method = "Method";

        /// <summary>是否分页</summary>
        public const String Pageable = "Pageable";

        /// <summary>页码字段名。分页时页码参数名</summary>
        public const String PageNumField = "PageNumField";

        /// <summary>页量字段名。分页时每页条数参数名</summary>
        public const String PageSizeField = "PageSizeField";

        /// <summary>数据路径。从响应中提取数据列表的JSON路径</summary>
        public const String DataPath = "DataPath";

        /// <summary>总量路径。从响应中提取总数的JSON路径</summary>
        public const String TotalPath = "TotalPath";

        /// <summary>固定参数。每次请求附加的固定参数，JSON格式</summary>
        public const String FixedParams = "FixedParams";

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
