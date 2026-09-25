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

namespace NewLife.Cube.Workflow.Entity;

/// <summary>流程定义。按实体路径配置的审批图草稿与发布快照</summary>
[Serializable]
[DataObject]
[Description("流程定义。按实体路径配置的审批图草稿与发布快照")]
[BindIndex("IX_WorkflowDefinition_TenantId_TypePath_Enable", false, "TenantId,TypePath,Enable")]
[BindTable("WorkflowDefinition", Description = "流程定义。按实体路径配置的审批图草稿与发布快照", ConnName = "Workflow", DbType = DatabaseType.None)]
public partial class WorkflowDefinition : IEntity<WorkflowDefinitionModel>
{
    #region 属性
    private Int64 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, false, false, 0)]
    [BindColumn("Id", "编号", "", DataScale = "time")]
    public Int64 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int32 _TenantId;
    /// <summary>租户。0=平台</summary>
    [DisplayName("租户")]
    [Description("租户。0=平台")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("TenantId", "租户。0=平台", "")]
    public Int32 TenantId { get => _TenantId; set { if (OnPropertyChanging("TenantId", value)) { _TenantId = value; OnPropertyChanged("TenantId"); } } }

    private String _TypePath;
    /// <summary>实体路径。与 GetPage 一致，禁止空=全部实体</summary>
    [DisplayName("实体路径")]
    [Description("实体路径。与 GetPage 一致，禁止空=全部实体")]
    [DataObjectField(false, false, false, 100)]
    [BindColumn("TypePath", "实体路径。与 GetPage 一致，禁止空=全部实体", "")]
    public String TypePath { get => _TypePath; set { if (OnPropertyChanging("TypePath", value)) { _TypePath = value; OnPropertyChanged("TypePath"); } } }

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
    [BindColumn("Enable", "启用", "", DefaultValue = "true")]
    public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

    private Boolean _Published;
    /// <summary>已发布。仅已发布定义可发起</summary>
    [DisplayName("已发布")]
    [Description("已发布。仅已发布定义可发起")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Published", "已发布。仅已发布定义可发起", "")]
    public Boolean Published { get => _Published; set { if (OnPropertyChanging("Published", value)) { _Published = value; OnPropertyChanged("Published"); } } }

    private Int32 _Version;
    /// <summary>版本。每次发布加一</summary>
    [DisplayName("版本")]
    [Description("版本。每次发布加一")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Version", "版本。每次发布加一", "", DefaultValue = "1")]
    public Int32 Version { get => _Version; set { if (OnPropertyChanging("Version", value)) { _Version = value; OnPropertyChanged("Version"); } } }

    private String _LockPolicy;
    /// <summary>锁策略。full 全锁 / nodeFields 节点字段</summary>
    [DisplayName("锁策略")]
    [Description("锁策略。full 全锁 / nodeFields 节点字段")]
    [DataObjectField(false, false, true, 16)]
    [BindColumn("LockPolicy", "锁策略。full 全锁 / nodeFields 节点字段", "", DefaultValue = "full")]
    public String LockPolicy { get => _LockPolicy; set { if (OnPropertyChanging("LockPolicy", value)) { _LockPolicy = value; OnPropertyChanged("LockPolicy"); } } }

    private String _StartFilter;
    /// <summary>发起过滤。ViewFilter 同构 JSON，空表示全记录可发起</summary>
    [DisplayName("发起过滤")]
    [Description("发起过滤。ViewFilter 同构 JSON，空表示全记录可发起")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("StartFilter", "发起过滤。ViewFilter 同构 JSON，空表示全记录可发起", "", ItemType = "json")]
    public String StartFilter { get => _StartFilter; set { if (OnPropertyChanging("StartFilter", value)) { _StartFilter = value; OnPropertyChanged("StartFilter"); } } }

    private String _GraphJson;
    /// <summary>设计草稿。FlowGram 图 JSON</summary>
    [DisplayName("设计草稿")]
    [Description("设计草稿。FlowGram 图 JSON")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("GraphJson", "设计草稿。FlowGram 图 JSON", "", ItemType = "json")]
    public String GraphJson { get => _GraphJson; set { if (OnPropertyChanging("GraphJson", value)) { _GraphJson = value; OnPropertyChanged("GraphJson"); } } }

    private String _PublishedGraphJson;
    /// <summary>发布快照。发起时再拷到实例</summary>
    [DisplayName("发布快照")]
    [Description("发布快照。发起时再拷到实例")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("PublishedGraphJson", "发布快照。发起时再拷到实例", "", ItemType = "json")]
    public String PublishedGraphJson { get => _PublishedGraphJson; set { if (OnPropertyChanging("PublishedGraphJson", value)) { _PublishedGraphJson = value; OnPropertyChanged("PublishedGraphJson"); } } }

    private String _CreateUser;
    /// <summary>创建者</summary>
    [Category("扩展")]
    [DisplayName("创建者")]
    [Description("创建者")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateUser", "创建者", "")]
    public String CreateUser { get => _CreateUser; set { if (OnPropertyChanging("CreateUser", value)) { _CreateUser = value; OnPropertyChanged("CreateUser"); } } }

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

    private String _UpdateUser;
    /// <summary>更新者</summary>
    [Category("扩展")]
    [DisplayName("更新者")]
    [Description("更新者")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UpdateUser", "更新者", "")]
    public String UpdateUser { get => _UpdateUser; set { if (OnPropertyChanging("UpdateUser", value)) { _UpdateUser = value; OnPropertyChanged("UpdateUser"); } } }

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
    public void Copy(WorkflowDefinitionModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        TypePath = model.TypePath;
        Name = model.Name;
        Enable = model.Enable;
        Published = model.Published;
        Version = model.Version;
        LockPolicy = model.LockPolicy;
        StartFilter = model.StartFilter;
        GraphJson = model.GraphJson;
        PublishedGraphJson = model.PublishedGraphJson;
        CreateUser = model.CreateUser;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateUser = model.UpdateUser;
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
            "TenantId" => _TenantId,
            "TypePath" => _TypePath,
            "Name" => _Name,
            "Enable" => _Enable,
            "Published" => _Published,
            "Version" => _Version,
            "LockPolicy" => _LockPolicy,
            "StartFilter" => _StartFilter,
            "GraphJson" => _GraphJson,
            "PublishedGraphJson" => _PublishedGraphJson,
            "CreateUser" => _CreateUser,
            "CreateUserID" => _CreateUserID,
            "CreateIP" => _CreateIP,
            "CreateTime" => _CreateTime,
            "UpdateUser" => _UpdateUser,
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
                case "Id": _Id = value.ToLong(); break;
                case "TenantId": _TenantId = value.ToInt(); break;
                case "TypePath": _TypePath = Convert.ToString(value); break;
                case "Name": _Name = Convert.ToString(value); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "Published": _Published = value.ToBoolean(); break;
                case "Version": _Version = value.ToInt(); break;
                case "LockPolicy": _LockPolicy = Convert.ToString(value); break;
                case "StartFilter": _StartFilter = Convert.ToString(value); break;
                case "GraphJson": _GraphJson = Convert.ToString(value); break;
                case "PublishedGraphJson": _PublishedGraphJson = Convert.ToString(value); break;
                case "CreateUser": _CreateUser = Convert.ToString(value); break;
                case "CreateUserID": _CreateUserID = value.ToInt(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "UpdateUser": _UpdateUser = Convert.ToString(value); break;
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
    /// <summary>租户</summary>
    [XmlIgnore, IgnoreDataMember, ScriptIgnore]
    public XCode.Membership.Tenant Tenant => Extends.Get(nameof(Tenant), k => XCode.Membership.Tenant.FindById(TenantId));

    /// <summary>租户</summary>
    [Map(nameof(TenantId), typeof(XCode.Membership.Tenant), "Id")]
    public String TenantName => Tenant?.ToString();

    #endregion

    #region 扩展查询
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static WorkflowDefinition FindById(Int64 id)
    {
        if (id < 0) return null;

        return Find(_.Id == id);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="tenantId">租户。0=平台</param>
    /// <param name="typePath">实体路径。与 GetPage 一致，禁止空=全部实体</param>
    /// <param name="enable">启用</param>
    /// <param name="published">已发布。仅已发布定义可发起</param>
    /// <param name="start">编号开始</param>
    /// <param name="end">编号结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowDefinition> Search(Int32 tenantId, String typePath, Boolean? enable, Boolean? published, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (tenantId >= 0) exp &= _.TenantId == tenantId;
        if (!typePath.IsNullOrEmpty()) exp &= _.TypePath == typePath;
        if (enable != null) exp &= _.Enable == enable;
        if (published != null) exp &= _.Published == published;
        exp &= _.Id.Between(start, end, Meta.Factory.Snow);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 数据清理
    /// <summary>清理指定时间段内的数据</summary>
    /// <param name="start">开始时间。未指定时清理小于指定时间的所有数据</param>
    /// <param name="end">结束时间</param>
    /// <param name="maximumRows">最大删除行数。清理历史数据时，避免一次性删除过多导致数据库IO跟不上，0表示所有</param>
    /// <returns>清理行数</returns>
    public static Int32 DeleteWith(DateTime start, DateTime end, Int32 maximumRows = 0)
    {
        return Delete(_.Id.Between(start, end, Meta.Factory.Snow), maximumRows);
    }
    #endregion

    #region 字段名
    /// <summary>取得流程定义字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>租户。0=平台</summary>
        public static readonly Field TenantId = FindByName("TenantId");

        /// <summary>实体路径。与 GetPage 一致，禁止空=全部实体</summary>
        public static readonly Field TypePath = FindByName("TypePath");

        /// <summary>名称</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>已发布。仅已发布定义可发起</summary>
        public static readonly Field Published = FindByName("Published");

        /// <summary>版本。每次发布加一</summary>
        public static readonly Field Version = FindByName("Version");

        /// <summary>锁策略。full 全锁 / nodeFields 节点字段</summary>
        public static readonly Field LockPolicy = FindByName("LockPolicy");

        /// <summary>发起过滤。ViewFilter 同构 JSON，空表示全记录可发起</summary>
        public static readonly Field StartFilter = FindByName("StartFilter");

        /// <summary>设计草稿。FlowGram 图 JSON</summary>
        public static readonly Field GraphJson = FindByName("GraphJson");

        /// <summary>发布快照。发起时再拷到实例</summary>
        public static readonly Field PublishedGraphJson = FindByName("PublishedGraphJson");

        /// <summary>创建者</summary>
        public static readonly Field CreateUser = FindByName("CreateUser");

        /// <summary>创建用户</summary>
        public static readonly Field CreateUserID = FindByName("CreateUserID");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>更新者</summary>
        public static readonly Field UpdateUser = FindByName("UpdateUser");

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

    /// <summary>取得流程定义字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>租户。0=平台</summary>
        public const String TenantId = "TenantId";

        /// <summary>实体路径。与 GetPage 一致，禁止空=全部实体</summary>
        public const String TypePath = "TypePath";

        /// <summary>名称</summary>
        public const String Name = "Name";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>已发布。仅已发布定义可发起</summary>
        public const String Published = "Published";

        /// <summary>版本。每次发布加一</summary>
        public const String Version = "Version";

        /// <summary>锁策略。full 全锁 / nodeFields 节点字段</summary>
        public const String LockPolicy = "LockPolicy";

        /// <summary>发起过滤。ViewFilter 同构 JSON，空表示全记录可发起</summary>
        public const String StartFilter = "StartFilter";

        /// <summary>设计草稿。FlowGram 图 JSON</summary>
        public const String GraphJson = "GraphJson";

        /// <summary>发布快照。发起时再拷到实例</summary>
        public const String PublishedGraphJson = "PublishedGraphJson";

        /// <summary>创建者</summary>
        public const String CreateUser = "CreateUser";

        /// <summary>创建用户</summary>
        public const String CreateUserID = "CreateUserID";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>更新者</summary>
        public const String UpdateUser = "UpdateUser";

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
