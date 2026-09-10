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

/// <summary>流程任务。节点上的待办/已办任务</summary>
[Serializable]
[DataObject]
[Description("流程任务。节点上的待办/已办任务")]
[BindIndex("IX_WorkflowTask_AssigneeId_Status", false, "AssigneeId,Status")]
[BindIndex("IX_WorkflowTask_InstanceId_NodeId", false, "InstanceId,NodeId")]
[BindTable("WorkflowTask", Description = "流程任务。节点上的待办/已办任务", ConnName = "Workflow", DbType = DatabaseType.None)]
public partial class WorkflowTask : IEntity<WorkflowTaskModel>
{
    #region 属性
    private Int64 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, false, false, 0)]
    [BindColumn("Id", "编号", "", DataScale = "time")]
    public Int64 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int64 _InstanceId;
    /// <summary>流程实例</summary>
    [DisplayName("流程实例")]
    [Description("流程实例")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("InstanceId", "流程实例", "")]
    public Int64 InstanceId { get => _InstanceId; set { if (OnPropertyChanging("InstanceId", value)) { _InstanceId = value; OnPropertyChanged("InstanceId"); } } }

    private String _NodeId;
    /// <summary>节点。图节点 id</summary>
    [DisplayName("节点")]
    [Description("节点。图节点 id")]
    [DataObjectField(false, false, true, 64)]
    [BindColumn("NodeId", "节点。图节点 id", "")]
    public String NodeId { get => _NodeId; set { if (OnPropertyChanging("NodeId", value)) { _NodeId = value; OnPropertyChanged("NodeId"); } } }

    private String _Mode;
    /// <summary>签核模式。or/and/sequence</summary>
    [DisplayName("签核模式")]
    [Description("签核模式。or/and/sequence")]
    [DataObjectField(false, false, true, 16)]
    [BindColumn("Mode", "签核模式。or/and/sequence", "")]
    public String Mode { get => _Mode; set { if (OnPropertyChanging("Mode", value)) { _Mode = value; OnPropertyChanged("Mode"); } } }

    private Int32 _AssigneeId;
    /// <summary>办理人。0=未认领，候选人在 CandidateJson</summary>
    [DisplayName("办理人")]
    [Description("办理人。0=未认领，候选人在 CandidateJson")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("AssigneeId", "办理人。0=未认领，候选人在 CandidateJson", "")]
    public Int32 AssigneeId { get => _AssigneeId; set { if (OnPropertyChanging("AssigneeId", value)) { _AssigneeId = value; OnPropertyChanged("AssigneeId"); } } }

    private String _CandidateJson;
    /// <summary>候选人。用户 Id 数组</summary>
    [DisplayName("候选人")]
    [Description("候选人。用户 Id 数组")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("CandidateJson", "候选人。用户 Id 数组", "", ItemType = "json")]
    public String CandidateJson { get => _CandidateJson; set { if (OnPropertyChanging("CandidateJson", value)) { _CandidateJson = value; OnPropertyChanged("CandidateJson"); } } }

    private Int32 _SequenceIndex;
    /// <summary>依次签序号。从 0 起</summary>
    [DisplayName("依次签序号")]
    [Description("依次签序号。从 0 起")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("SequenceIndex", "依次签序号。从 0 起", "")]
    public Int32 SequenceIndex { get => _SequenceIndex; set { if (OnPropertyChanging("SequenceIndex", value)) { _SequenceIndex = value; OnPropertyChanged("SequenceIndex"); } } }

    private Boolean _Visible;
    /// <summary>可见。依次签未轮到为 false</summary>
    [DisplayName("可见")]
    [Description("可见。依次签未轮到为 false")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Visible", "可见。依次签未轮到为 false", "")]
    public Boolean Visible { get => _Visible; set { if (OnPropertyChanging("Visible", value)) { _Visible = value; OnPropertyChanged("Visible"); } } }

    private String _Status;
    /// <summary>状态。Pending/Active/Done/Rejected/Cancelled/Transferred</summary>
    [DisplayName("状态")]
    [Description("状态。Pending/Active/Done/Rejected/Cancelled/Transferred")]
    [DataObjectField(false, false, true, 16)]
    [BindColumn("Status", "状态。Pending/Active/Done/Rejected/Cancelled/Transferred", "")]
    public String Status { get => _Status; set { if (OnPropertyChanging("Status", value)) { _Status = value; OnPropertyChanged("Status"); } } }

    private DateTime _DueTime;
    /// <summary>截止时间</summary>
    [DisplayName("截止时间")]
    [Description("截止时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("DueTime", "截止时间", "")]
    public DateTime DueTime { get => _DueTime; set { if (OnPropertyChanging("DueTime", value)) { _DueTime = value; OnPropertyChanged("DueTime"); } } }

    private String _TimeoutAction;
    /// <summary>超时动作。pass/reject/transfer</summary>
    [DisplayName("超时动作")]
    [Description("超时动作。pass/reject/transfer")]
    [DataObjectField(false, false, true, 16)]
    [BindColumn("TimeoutAction", "超时动作。pass/reject/transfer", "")]
    public String TimeoutAction { get => _TimeoutAction; set { if (OnPropertyChanging("TimeoutAction", value)) { _TimeoutAction = value; OnPropertyChanged("TimeoutAction"); } } }

    private String _TimeoutTransferTo;
    /// <summary>超时转交。复用 to schema</summary>
    [DisplayName("超时转交")]
    [Description("超时转交。复用 to schema")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("TimeoutTransferTo", "超时转交。复用 to schema", "", ItemType = "json")]
    public String TimeoutTransferTo { get => _TimeoutTransferTo; set { if (OnPropertyChanging("TimeoutTransferTo", value)) { _TimeoutTransferTo = value; OnPropertyChanged("TimeoutTransferTo"); } } }

    private DateTime _ClaimTime;
    /// <summary>认领时间</summary>
    [DisplayName("认领时间")]
    [Description("认领时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("ClaimTime", "认领时间", "")]
    public DateTime ClaimTime { get => _ClaimTime; set { if (OnPropertyChanging("ClaimTime", value)) { _ClaimTime = value; OnPropertyChanged("ClaimTime"); } } }

    private DateTime _FinishTime;
    /// <summary>完成时间</summary>
    [DisplayName("完成时间")]
    [Description("完成时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("FinishTime", "完成时间", "")]
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
    public void Copy(WorkflowTaskModel model)
    {
        Id = model.Id;
        InstanceId = model.InstanceId;
        NodeId = model.NodeId;
        Mode = model.Mode;
        AssigneeId = model.AssigneeId;
        CandidateJson = model.CandidateJson;
        SequenceIndex = model.SequenceIndex;
        Visible = model.Visible;
        Status = model.Status;
        DueTime = model.DueTime;
        TimeoutAction = model.TimeoutAction;
        TimeoutTransferTo = model.TimeoutTransferTo;
        ClaimTime = model.ClaimTime;
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
            "InstanceId" => _InstanceId,
            "NodeId" => _NodeId,
            "Mode" => _Mode,
            "AssigneeId" => _AssigneeId,
            "CandidateJson" => _CandidateJson,
            "SequenceIndex" => _SequenceIndex,
            "Visible" => _Visible,
            "Status" => _Status,
            "DueTime" => _DueTime,
            "TimeoutAction" => _TimeoutAction,
            "TimeoutTransferTo" => _TimeoutTransferTo,
            "ClaimTime" => _ClaimTime,
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
                case "InstanceId": _InstanceId = value.ToLong(); break;
                case "NodeId": _NodeId = Convert.ToString(value); break;
                case "Mode": _Mode = Convert.ToString(value); break;
                case "AssigneeId": _AssigneeId = value.ToInt(); break;
                case "CandidateJson": _CandidateJson = Convert.ToString(value); break;
                case "SequenceIndex": _SequenceIndex = value.ToInt(); break;
                case "Visible": _Visible = value.ToBoolean(); break;
                case "Status": _Status = Convert.ToString(value); break;
                case "DueTime": _DueTime = value.ToDateTime(); break;
                case "TimeoutAction": _TimeoutAction = Convert.ToString(value); break;
                case "TimeoutTransferTo": _TimeoutTransferTo = Convert.ToString(value); break;
                case "ClaimTime": _ClaimTime = value.ToDateTime(); break;
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
    public static WorkflowTask FindById(Int64 id)
    {
        if (id < 0) return null;

        return Find(_.Id == id);
    }

    /// <summary>根据办理人、状态查找</summary>
    /// <param name="assigneeId">办理人</param>
    /// <param name="status">状态</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowTask> FindAllByAssigneeIdAndStatus(Int32 assigneeId, String status)
    {
        if (assigneeId < 0) return [];
        if (status.IsNullOrEmpty()) return [];

        return FindAll(_.AssigneeId == assigneeId & _.Status == status);
    }

    /// <summary>根据流程实例、节点查找</summary>
    /// <param name="instanceId">流程实例</param>
    /// <param name="nodeId">节点</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowTask> FindAllByInstanceIdAndNodeId(Int64 instanceId, String nodeId)
    {
        if (instanceId < 0) return [];
        if (nodeId.IsNullOrEmpty()) return [];

        return FindAll(_.InstanceId == instanceId & _.NodeId == nodeId);
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
    /// <summary>取得流程任务字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>流程实例</summary>
        public static readonly Field InstanceId = FindByName("InstanceId");

        /// <summary>节点。图节点 id</summary>
        public static readonly Field NodeId = FindByName("NodeId");

        /// <summary>签核模式。or/and/sequence</summary>
        public static readonly Field Mode = FindByName("Mode");

        /// <summary>办理人。0=未认领，候选人在 CandidateJson</summary>
        public static readonly Field AssigneeId = FindByName("AssigneeId");

        /// <summary>候选人。用户 Id 数组</summary>
        public static readonly Field CandidateJson = FindByName("CandidateJson");

        /// <summary>依次签序号。从 0 起</summary>
        public static readonly Field SequenceIndex = FindByName("SequenceIndex");

        /// <summary>可见。依次签未轮到为 false</summary>
        public static readonly Field Visible = FindByName("Visible");

        /// <summary>状态。Pending/Active/Done/Rejected/Cancelled/Transferred</summary>
        public static readonly Field Status = FindByName("Status");

        /// <summary>截止时间</summary>
        public static readonly Field DueTime = FindByName("DueTime");

        /// <summary>超时动作。pass/reject/transfer</summary>
        public static readonly Field TimeoutAction = FindByName("TimeoutAction");

        /// <summary>超时转交。复用 to schema</summary>
        public static readonly Field TimeoutTransferTo = FindByName("TimeoutTransferTo");

        /// <summary>认领时间</summary>
        public static readonly Field ClaimTime = FindByName("ClaimTime");

        /// <summary>完成时间</summary>
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

    /// <summary>取得流程任务字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>流程实例</summary>
        public const String InstanceId = "InstanceId";

        /// <summary>节点。图节点 id</summary>
        public const String NodeId = "NodeId";

        /// <summary>签核模式。or/and/sequence</summary>
        public const String Mode = "Mode";

        /// <summary>办理人。0=未认领，候选人在 CandidateJson</summary>
        public const String AssigneeId = "AssigneeId";

        /// <summary>候选人。用户 Id 数组</summary>
        public const String CandidateJson = "CandidateJson";

        /// <summary>依次签序号。从 0 起</summary>
        public const String SequenceIndex = "SequenceIndex";

        /// <summary>可见。依次签未轮到为 false</summary>
        public const String Visible = "Visible";

        /// <summary>状态。Pending/Active/Done/Rejected/Cancelled/Transferred</summary>
        public const String Status = "Status";

        /// <summary>截止时间</summary>
        public const String DueTime = "DueTime";

        /// <summary>超时动作。pass/reject/transfer</summary>
        public const String TimeoutAction = "TimeoutAction";

        /// <summary>超时转交。复用 to schema</summary>
        public const String TimeoutTransferTo = "TimeoutTransferTo";

        /// <summary>认领时间</summary>
        public const String ClaimTime = "ClaimTime";

        /// <summary>完成时间</summary>
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
