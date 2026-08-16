using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Entity;

/// <summary>自动化运行。一次自动化执行，含队列与延时续跑状态</summary>
[Serializable]
[DataObject]
[DisplayName("自动化运行")]
[Description("自动化运行。一次自动化执行，含队列与延时续跑状态")]
[BindIndex("IX_AutomationRun_AutomationId", false, "AutomationId")]
[BindIndex("IX_AutomationRun_TypePath_RecordKey", false, "TypePath,RecordKey")]
[BindIndex("IX_AutomationRun_Status_ResumeAt", false, "Status,ResumeAt")]
[BindTable("AutomationRun", Description = "自动化运行。一次自动化执行，含队列与延时续跑状态", ConnName = "Log", DbType = DatabaseType.None)]
public partial class AutomationRun : IEntity<AutomationRunModel>
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
    /// <summary>租户。复制自规则</summary>
    [DisplayName("租户")]
    [Description("租户。复制自规则")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("TenantId", "租户。复制自规则", "")]
    public Int32 TenantId { get => _TenantId; set { if (OnPropertyChanging("TenantId", value)) { _TenantId = value; OnPropertyChanged("TenantId"); } } }

    private Int64 _AutomationId;
    /// <summary>规则</summary>
    [DisplayName("规则")]
    [Description("规则")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("AutomationId", "规则", "")]
    public Int64 AutomationId { get => _AutomationId; set { if (OnPropertyChanging("AutomationId", value)) { _AutomationId = value; OnPropertyChanged("AutomationId"); } } }

    private String _TypePath;
    /// <summary>实体路径</summary>
    [DisplayName("实体路径")]
    [Description("实体路径")]
    [DataObjectField(false, false, true, 100)]
    [BindColumn("TypePath", "实体路径", "")]
    public String TypePath { get => _TypePath; set { if (OnPropertyChanging("TypePath", value)) { _TypePath = value; OnPropertyChanged("TypePath"); } } }

    private String _RecordKey;
    /// <summary>记录主键</summary>
    [DisplayName("记录主键")]
    [Description("记录主键")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("RecordKey", "记录主键", "")]
    public String RecordKey { get => _RecordKey; set { if (OnPropertyChanging("RecordKey", value)) { _RecordKey = value; OnPropertyChanged("RecordKey"); } } }

    private String _TriggerKind;
    /// <summary>触发种类</summary>
    [DisplayName("触发种类")]
    [Description("触发种类")]
    [DataObjectField(false, false, true, 32)]
    [BindColumn("TriggerKind", "触发种类", "")]
    public String TriggerKind { get => _TriggerKind; set { if (OnPropertyChanging("TriggerKind", value)) { _TriggerKind = value; OnPropertyChanged("TriggerKind"); } } }

    private String _Status;
    /// <summary>状态。queued/running/waiting/succeeded/failed/cancelled</summary>
    [DisplayName("状态")]
    [Description("状态。queued/running/waiting/succeeded/failed/cancelled")]
    [DataObjectField(false, false, true, 20)]
    [BindColumn("Status", "状态。queued/running/waiting/succeeded/failed/cancelled", "")]
    public String Status { get => _Status; set { if (OnPropertyChanging("Status", value)) { _Status = value; OnPropertyChanged("Status"); } } }

    private Int32 _Depth;
    /// <summary>深度。runAutomation 递增</summary>
    [DisplayName("深度")]
    [Description("深度。runAutomation 递增")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Depth", "深度。runAutomation 递增", "")]
    public Int32 Depth { get => _Depth; set { if (OnPropertyChanging("Depth", value)) { _Depth = value; OnPropertyChanged("Depth"); } } }

    private DateTime _ResumeAt;
    /// <summary>续跑时间。delay 到期</summary>
    [DisplayName("续跑时间")]
    [Description("续跑时间。delay 到期")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("ResumeAt", "续跑时间。delay 到期", "")]
    public DateTime ResumeAt { get => _ResumeAt; set { if (OnPropertyChanging("ResumeAt", value)) { _ResumeAt = value; OnPropertyChanged("ResumeAt"); } } }

    private String _NodeTrace;
    /// <summary>节点轨迹。JSON</summary>
    [DisplayName("节点轨迹")]
    [Description("节点轨迹。JSON")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("NodeTrace", "节点轨迹。JSON", "")]
    public String NodeTrace { get => _NodeTrace; set { if (OnPropertyChanging("NodeTrace", value)) { _NodeTrace = value; OnPropertyChanged("NodeTrace"); } } }

    private String _Error;
    /// <summary>错误摘要</summary>
    [DisplayName("错误摘要")]
    [Description("错误摘要")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Error", "错误摘要", "")]
    public String Error { get => _Error; set { if (OnPropertyChanging("Error", value)) { _Error = value; OnPropertyChanged("Error"); } } }

    private DateTime _CreateTime;
    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    [Description("创建时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

    private DateTime _UpdateTime;
    /// <summary>更新时间</summary>
    [DisplayName("更新时间")]
    [Description("更新时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("UpdateTime", "更新时间", "")]
    public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(AutomationRunModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        AutomationId = model.AutomationId;
        TypePath = model.TypePath;
        RecordKey = model.RecordKey;
        TriggerKind = model.TriggerKind;
        Status = model.Status;
        Depth = model.Depth;
        ResumeAt = model.ResumeAt;
        NodeTrace = model.NodeTrace;
        Error = model.Error;
        CreateTime = model.CreateTime;
        UpdateTime = model.UpdateTime;
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
            "AutomationId" => _AutomationId,
            "TypePath" => _TypePath,
            "RecordKey" => _RecordKey,
            "TriggerKind" => _TriggerKind,
            "Status" => _Status,
            "Depth" => _Depth,
            "ResumeAt" => _ResumeAt,
            "NodeTrace" => _NodeTrace,
            "Error" => _Error,
            "CreateTime" => _CreateTime,
            "UpdateTime" => _UpdateTime,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToLong(); break;
                case "TenantId": _TenantId = value.ToInt(); break;
                case "AutomationId": _AutomationId = value.ToLong(); break;
                case "TypePath": _TypePath = Convert.ToString(value); break;
                case "RecordKey": _RecordKey = Convert.ToString(value); break;
                case "TriggerKind": _TriggerKind = Convert.ToString(value); break;
                case "Status": _Status = Convert.ToString(value); break;
                case "Depth": _Depth = value.ToInt(); break;
                case "ResumeAt": _ResumeAt = value.ToDateTime(); break;
                case "NodeTrace": _NodeTrace = Convert.ToString(value); break;
                case "Error": _Error = Convert.ToString(value); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
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
    public static AutomationRun FindById(Int64 id)
    {
        if (id < 0) return null;

        return Find(_.Id == id);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="tenantId">租户。复制自规则</param>
    /// <param name="automationId">规则</param>
    /// <param name="typePath">实体路径</param>
    /// <param name="recordKey">记录主键</param>
    /// <param name="triggerKind">触发种类</param>
    /// <param name="status">状态。queued/running/waiting/succeeded/failed/cancelled</param>
    /// <param name="start">编号开始</param>
    /// <param name="end">编号结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<AutomationRun> Search(Int32 tenantId, Int64 automationId, String typePath, String recordKey, String triggerKind, String status, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (tenantId >= 0) exp &= _.TenantId == tenantId;
        if (automationId > 0) exp &= _.AutomationId == automationId;
        if (!typePath.IsNullOrEmpty()) exp &= _.TypePath == typePath;
        if (!recordKey.IsNullOrEmpty()) exp &= _.RecordKey == recordKey;
        if (!triggerKind.IsNullOrEmpty()) exp &= _.TriggerKind == triggerKind;
        if (!status.IsNullOrEmpty()) exp &= _.Status == status;
        exp &= _.Id.Between(start, end, Meta.Factory.Snow);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 字段名
    /// <summary>取得自动化运行字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>租户。复制自规则</summary>
        public static readonly Field TenantId = FindByName("TenantId");

        /// <summary>规则</summary>
        public static readonly Field AutomationId = FindByName("AutomationId");

        /// <summary>实体路径</summary>
        public static readonly Field TypePath = FindByName("TypePath");

        /// <summary>记录主键</summary>
        public static readonly Field RecordKey = FindByName("RecordKey");

        /// <summary>触发种类</summary>
        public static readonly Field TriggerKind = FindByName("TriggerKind");

        /// <summary>状态。queued/running/waiting/succeeded/failed/cancelled</summary>
        public static readonly Field Status = FindByName("Status");

        /// <summary>深度。runAutomation 递增</summary>
        public static readonly Field Depth = FindByName("Depth");

        /// <summary>续跑时间。delay 到期</summary>
        public static readonly Field ResumeAt = FindByName("ResumeAt");

        /// <summary>节点轨迹。JSON</summary>
        public static readonly Field NodeTrace = FindByName("NodeTrace");

        /// <summary>错误摘要</summary>
        public static readonly Field Error = FindByName("Error");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得自动化运行字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>租户。复制自规则</summary>
        public const String TenantId = "TenantId";

        /// <summary>规则</summary>
        public const String AutomationId = "AutomationId";

        /// <summary>实体路径</summary>
        public const String TypePath = "TypePath";

        /// <summary>记录主键</summary>
        public const String RecordKey = "RecordKey";

        /// <summary>触发种类</summary>
        public const String TriggerKind = "TriggerKind";

        /// <summary>状态。queued/running/waiting/succeeded/failed/cancelled</summary>
        public const String Status = "Status";

        /// <summary>深度。runAutomation 递增</summary>
        public const String Depth = "Depth";

        /// <summary>续跑时间。delay 到期</summary>
        public const String ResumeAt = "ResumeAt";

        /// <summary>节点轨迹。JSON</summary>
        public const String NodeTrace = "NodeTrace";

        /// <summary>错误摘要</summary>
        public const String Error = "Error";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";
    }
    #endregion
}
