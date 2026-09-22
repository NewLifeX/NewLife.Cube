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

/// <summary>流程实例。一次审批运行，钉扎定义版本与图快照</summary>
[Serializable]
[DataObject]
[Description("流程实例。一次审批运行，钉扎定义版本与图快照")]
[BindIndex("IX_WorkflowInstance_DefinitionId", false, "DefinitionId")]
[BindIndex("IX_WorkflowInstance_TenantId_TypePath_Status", false, "TenantId,TypePath,Status")]
[BindIndex("IX_WorkflowInstance_StarterId_Status", false, "StarterId,Status")]
[BindTable("WorkflowInstance", Description = "流程实例。一次审批运行，钉扎定义版本与图快照", ConnName = "Workflow", DbType = DatabaseType.None)]
public partial class WorkflowInstance : IEntity<WorkflowInstanceModel>
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
    /// <summary>租户。拷贝定义</summary>
    [DisplayName("租户")]
    [Description("租户。拷贝定义")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("TenantId", "租户。拷贝定义", "")]
    public Int32 TenantId { get => _TenantId; set { if (OnPropertyChanging("TenantId", value)) { _TenantId = value; OnPropertyChanged("TenantId"); } } }

    private Int64 _DefinitionId;
    /// <summary>流程定义</summary>
    [DisplayName("流程定义")]
    [Description("流程定义")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("DefinitionId", "流程定义", "")]
    public Int64 DefinitionId { get => _DefinitionId; set { if (OnPropertyChanging("DefinitionId", value)) { _DefinitionId = value; OnPropertyChanged("DefinitionId"); } } }

    private Int32 _DefinitionVersion;
    /// <summary>定义版本。发起时钉扎</summary>
    [DisplayName("定义版本")]
    [Description("定义版本。发起时钉扎")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("DefinitionVersion", "定义版本。发起时钉扎", "")]
    public Int32 DefinitionVersion { get => _DefinitionVersion; set { if (OnPropertyChanging("DefinitionVersion", value)) { _DefinitionVersion = value; OnPropertyChanged("DefinitionVersion"); } } }

    private String _TypePath;
    /// <summary>实体路径</summary>
    [DisplayName("实体路径")]
    [Description("实体路径")]
    [DataObjectField(false, false, true, 100)]
    [BindColumn("TypePath", "实体路径", "")]
    public String TypePath { get => _TypePath; set { if (OnPropertyChanging("TypePath", value)) { _TypePath = value; OnPropertyChanged("TypePath"); } } }

    private String _GraphSnapshot;
    /// <summary>图快照。发起时复制 PublishedGraphJson，之后只读</summary>
    [DisplayName("图快照")]
    [Description("图快照。发起时复制 PublishedGraphJson，之后只读")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("GraphSnapshot", "图快照。发起时复制 PublishedGraphJson，之后只读", "", ItemType = "json")]
    public String GraphSnapshot { get => _GraphSnapshot; set { if (OnPropertyChanging("GraphSnapshot", value)) { _GraphSnapshot = value; OnPropertyChanged("GraphSnapshot"); } } }

    private String _Status;
    /// <summary>状态。Running/Approved/Rejected/Withdrawn/Cancelled</summary>
    [DisplayName("状态")]
    [Description("状态。Running/Approved/Rejected/Withdrawn/Cancelled")]
    [DataObjectField(false, false, true, 16)]
    [BindColumn("Status", "状态。Running/Approved/Rejected/Withdrawn/Cancelled", "")]
    public String Status { get => _Status; set { if (OnPropertyChanging("Status", value)) { _Status = value; OnPropertyChanged("Status"); } } }

    private Int32 _StarterId;
    /// <summary>发起人</summary>
    [DisplayName("发起人")]
    [Description("发起人")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("StarterId", "发起人", "")]
    public Int32 StarterId { get => _StarterId; set { if (OnPropertyChanging("StarterId", value)) { _StarterId = value; OnPropertyChanged("StarterId"); } } }

    private String _Title;
    /// <summary>标题。本次提交的显示标题，发起时填写</summary>
    [DisplayName("标题")]
    [Description("标题。本次提交的显示标题，发起时填写")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Title", "标题。本次提交的显示标题，发起时填写", "", Master = true)]
    public String Title { get => _Title; set { if (OnPropertyChanging("Title", value)) { _Title = value; OnPropertyChanged("Title"); } } }

    private String _StartComment;
    /// <summary>发起意见</summary>
    [DisplayName("发起意见")]
    [Description("发起意见")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("StartComment", "发起意见", "")]
    public String StartComment { get => _StartComment; set { if (OnPropertyChanging("StartComment", value)) { _StartComment = value; OnPropertyChanged("StartComment"); } } }

    private String _Summary;
    /// <summary>流程摘要。Markdown/富文本，发起时填写</summary>
    [DisplayName("流程摘要")]
    [Description("流程摘要。Markdown/富文本，发起时填写")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("Summary", "流程摘要。Markdown/富文本，发起时填写", "")]
    public String Summary { get => _Summary; set { if (OnPropertyChanging("Summary", value)) { _Summary = value; OnPropertyChanged("Summary"); } } }

    private DateTime _FinishTime;
    /// <summary>结束时间</summary>
    [DisplayName("结束时间")]
    [Description("结束时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("FinishTime", "结束时间", "")]
    public DateTime FinishTime { get => _FinishTime; set { if (OnPropertyChanging("FinishTime", value)) { _FinishTime = value; OnPropertyChanged("FinishTime"); } } }

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
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(WorkflowInstanceModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        DefinitionId = model.DefinitionId;
        DefinitionVersion = model.DefinitionVersion;
        TypePath = model.TypePath;
        GraphSnapshot = model.GraphSnapshot;
        Status = model.Status;
        StarterId = model.StarterId;
        Title = model.Title;
        StartComment = model.StartComment;
        Summary = model.Summary;
        FinishTime = model.FinishTime;
        CreateUser = model.CreateUser;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateUser = model.UpdateUser;
        UpdateUserID = model.UpdateUserID;
        UpdateIP = model.UpdateIP;
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
            "DefinitionId" => _DefinitionId,
            "DefinitionVersion" => _DefinitionVersion,
            "TypePath" => _TypePath,
            "GraphSnapshot" => _GraphSnapshot,
            "Status" => _Status,
            "StarterId" => _StarterId,
            "Title" => _Title,
            "StartComment" => _StartComment,
            "Summary" => _Summary,
            "FinishTime" => _FinishTime,
            "CreateUser" => _CreateUser,
            "CreateUserID" => _CreateUserID,
            "CreateIP" => _CreateIP,
            "CreateTime" => _CreateTime,
            "UpdateUser" => _UpdateUser,
            "UpdateUserID" => _UpdateUserID,
            "UpdateIP" => _UpdateIP,
            "UpdateTime" => _UpdateTime,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToLong(); break;
                case "TenantId": _TenantId = value.ToInt(); break;
                case "DefinitionId": _DefinitionId = value.ToLong(); break;
                case "DefinitionVersion": _DefinitionVersion = value.ToInt(); break;
                case "TypePath": _TypePath = Convert.ToString(value); break;
                case "GraphSnapshot": _GraphSnapshot = Convert.ToString(value); break;
                case "Status": _Status = Convert.ToString(value); break;
                case "StarterId": _StarterId = value.ToInt(); break;
                case "Title": _Title = Convert.ToString(value); break;
                case "StartComment": _StartComment = Convert.ToString(value); break;
                case "Summary": _Summary = Convert.ToString(value); break;
                case "FinishTime": _FinishTime = value.ToDateTime(); break;
                case "CreateUser": _CreateUser = Convert.ToString(value); break;
                case "CreateUserID": _CreateUserID = value.ToInt(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "UpdateUser": _UpdateUser = Convert.ToString(value); break;
                case "UpdateUserID": _UpdateUserID = value.ToInt(); break;
                case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
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
    public static WorkflowInstance FindById(Int64 id)
    {
        if (id < 0) return null;

        return Find(_.Id == id);
    }

    /// <summary>根据流程定义查找</summary>
    /// <param name="definitionId">流程定义</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowInstance> FindAllByDefinitionId(Int64 definitionId)
    {
        if (definitionId < 0) return [];

        return FindAll(_.DefinitionId == definitionId);
    }

    /// <summary>根据租户、实体路径、状态查找</summary>
    /// <param name="tenantId">租户</param>
    /// <param name="typePath">实体路径</param>
    /// <param name="status">状态</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowInstance> FindAllByTenantIdAndTypePathAndStatus(Int32 tenantId, String typePath, String status)
    {
        if (tenantId < 0) return [];
        if (typePath.IsNullOrEmpty()) return [];
        if (status.IsNullOrEmpty()) return [];

        return FindAll(_.TenantId == tenantId & _.TypePath == typePath & _.Status == status);
    }

    /// <summary>根据发起人、状态查找</summary>
    /// <param name="starterId">发起人</param>
    /// <param name="status">状态</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowInstance> FindAllByStarterIdAndStatus(Int32 starterId, String status)
    {
        if (starterId < 0) return [];
        if (status.IsNullOrEmpty()) return [];

        return FindAll(_.StarterId == starterId & _.Status == status);
    }
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
    /// <summary>取得流程实例字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>租户。拷贝定义</summary>
        public static readonly Field TenantId = FindByName("TenantId");

        /// <summary>流程定义</summary>
        public static readonly Field DefinitionId = FindByName("DefinitionId");

        /// <summary>定义版本。发起时钉扎</summary>
        public static readonly Field DefinitionVersion = FindByName("DefinitionVersion");

        /// <summary>实体路径</summary>
        public static readonly Field TypePath = FindByName("TypePath");

        /// <summary>图快照。发起时复制 PublishedGraphJson，之后只读</summary>
        public static readonly Field GraphSnapshot = FindByName("GraphSnapshot");

        /// <summary>状态。Running/Approved/Rejected/Withdrawn/Cancelled</summary>
        public static readonly Field Status = FindByName("Status");

        /// <summary>发起人</summary>
        public static readonly Field StarterId = FindByName("StarterId");

        /// <summary>标题。本次提交的显示标题，发起时填写</summary>
        public static readonly Field Title = FindByName("Title");

        /// <summary>发起意见</summary>
        public static readonly Field StartComment = FindByName("StartComment");

        /// <summary>流程摘要。Markdown/富文本，发起时填写</summary>
        public static readonly Field Summary = FindByName("Summary");

        /// <summary>结束时间</summary>
        public static readonly Field FinishTime = FindByName("FinishTime");

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

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得流程实例字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>租户。拷贝定义</summary>
        public const String TenantId = "TenantId";

        /// <summary>流程定义</summary>
        public const String DefinitionId = "DefinitionId";

        /// <summary>定义版本。发起时钉扎</summary>
        public const String DefinitionVersion = "DefinitionVersion";

        /// <summary>实体路径</summary>
        public const String TypePath = "TypePath";

        /// <summary>图快照。发起时复制 PublishedGraphJson，之后只读</summary>
        public const String GraphSnapshot = "GraphSnapshot";

        /// <summary>状态。Running/Approved/Rejected/Withdrawn/Cancelled</summary>
        public const String Status = "Status";

        /// <summary>发起人</summary>
        public const String StarterId = "StarterId";

        /// <summary>标题。本次提交的显示标题，发起时填写</summary>
        public const String Title = "Title";

        /// <summary>发起意见</summary>
        public const String StartComment = "StartComment";

        /// <summary>流程摘要。Markdown/富文本，发起时填写</summary>
        public const String Summary = "Summary";

        /// <summary>结束时间</summary>
        public const String FinishTime = "FinishTime";

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
    }
    #endregion
}
