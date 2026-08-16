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

/// <summary>自动化运行。一次触发的执行队列与状态，ConnName=Log</summary>
[Serializable]
[DataObject]
[Description("自动化运行。一次触发的执行队列与状态，ConnName=Log")]
[BindIndex("IX_AutomationRun_AutomationId", false, "AutomationId")]
[BindIndex("IX_AutomationRun_TypePath_RecordKey", false, "TypePath,RecordKey")]
[BindIndex("IX_AutomationRun_Status_ResumeAt", false, "Status,ResumeAt")]
[BindTable("AutomationRun", Description = "自动化运行。一次触发的执行队列与状态，ConnName=Log", ConnName = "Log", DbType = DatabaseType.None)]
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
    /// <summary>规则编号</summary>
    [DisplayName("规则编号")]
    [Description("规则编号")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("AutomationId", "规则编号", "")]
    public Int64 AutomationId { get => _AutomationId; set { if (OnPropertyChanging("AutomationId", value)) { _AutomationId = value; OnPropertyChanged("AutomationId"); } } }

    private String _TypePath;
    /// <summary>实体路径</summary>
    [DisplayName("实体路径")]
    [Description("实体路径")]
    [DataObjectField(false, false, true, 100)]
    [BindColumn("TypePath", "实体路径", "")]
    public String TypePath { get => _TypePath; set { if (OnPropertyChanging("TypePath", value)) { _TypePath = value; OnPropertyChanged("TypePath"); } } }

    private String _RecordKey;
    /// <summary>记录主键。定时/Webhook 可空</summary>
    [DisplayName("记录主键")]
    [Description("记录主键。定时/Webhook 可空")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("RecordKey", "记录主键。定时/Webhook 可空", "")]
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
    /// <summary>调用深度。runAutomation +1</summary>
    [DisplayName("调用深度")]
    [Description("调用深度。runAutomation +1")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Depth", "调用深度。runAutomation +1", "")]
    public Int32 Depth { get => _Depth; set { if (OnPropertyChanging("Depth", value)) { _Depth = value; OnPropertyChanged("Depth"); } } }

    private DateTime _ResumeAt;
    /// <summary>延时续跑时间。非 waiting 为 MinValue</summary>
    [DisplayName("延时续跑时间")]
    [Description("延时续跑时间。非 waiting 为 MinValue")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("ResumeAt", "延时续跑时间。非 waiting 为 MinValue", "")]
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

    /// <summary>根据规则编号查找</summary>
    /// <param name="automationId">规则编号</param>
    /// <returns>实体列表</returns>
    public static IList<AutomationRun> FindAllByAutomationId(Int64 automationId)
    {
        if (automationId < 0) return [];

        return FindAll(_.AutomationId == automationId);
    }

    /// <summary>根据实体路径、记录主键查找</summary>
    /// <param name="typePath">实体路径</param>
    /// <param name="recordKey">记录主键</param>
    /// <returns>实体列表</returns>
    public static IList<AutomationRun> FindAllByTypePathAndRecordKey(String typePath, String recordKey)
    {
        if (typePath.IsNullOrEmpty()) return [];
        if (recordKey.IsNullOrEmpty()) return [];

        return FindAll(_.TypePath == typePath & _.RecordKey == recordKey);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="automationId">规则编号</param>
    /// <param name="typePath">实体路径</param>
    /// <param name="recordKey">记录主键。定时/Webhook 可空</param>
    /// <param name="status">状态。queued/running/waiting/succeeded/failed/cancelled</param>
    /// <param name="resumeAt">延时续跑时间。非 waiting 为 MinValue</param>
    /// <param name="start">编号开始</param>
    /// <param name="end">编号结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<AutomationRun> Search(Int64 automationId, String typePath, String recordKey, String status, DateTime resumeAt, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (automationId >= 0) exp &= _.AutomationId == automationId;
        if (!typePath.IsNullOrEmpty()) exp &= _.TypePath == typePath;
        if (!recordKey.IsNullOrEmpty()) exp &= _.RecordKey == recordKey;
        if (!status.IsNullOrEmpty()) exp &= _.Status == status;
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
    /// <summary>取得自动化运行字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>租户。复制自规则</summary>
        public static readonly Field TenantId = FindByName("TenantId");

        /// <summary>规则编号</summary>
        public static readonly Field AutomationId = FindByName("AutomationId");

        /// <summary>实体路径</summary>
        public static readonly Field TypePath = FindByName("TypePath");

        /// <summary>记录主键。定时/Webhook 可空</summary>
        public static readonly Field RecordKey = FindByName("RecordKey");

        /// <summary>触发种类</summary>
        public static readonly Field TriggerKind = FindByName("TriggerKind");

        /// <summary>状态。queued/running/waiting/succeeded/failed/cancelled</summary>
        public static readonly Field Status = FindByName("Status");

        /// <summary>调用深度。runAutomation +1</summary>
        public static readonly Field Depth = FindByName("Depth");

        /// <summary>延时续跑时间。非 waiting 为 MinValue</summary>
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

        /// <summary>规则编号</summary>
        public const String AutomationId = "AutomationId";

        /// <summary>实体路径</summary>
        public const String TypePath = "TypePath";

        /// <summary>记录主键。定时/Webhook 可空</summary>
        public const String RecordKey = "RecordKey";

        /// <summary>触发种类</summary>
        public const String TriggerKind = "TriggerKind";

        /// <summary>状态。queued/running/waiting/succeeded/failed/cancelled</summary>
        public const String Status = "Status";

        /// <summary>调用深度。runAutomation +1</summary>
        public const String Depth = "Depth";

        /// <summary>延时续跑时间。非 waiting 为 MinValue</summary>
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
