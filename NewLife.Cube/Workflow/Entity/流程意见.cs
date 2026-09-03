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

/// <summary>流程意见。审批动作与意见内容，附件走 Attachment</summary>
[Serializable]
[DataObject]
[Description("流程意见。审批动作与意见内容，附件走 Attachment")]
[BindIndex("IX_WorkflowComment_InstanceId", false, "InstanceId")]
[BindIndex("IX_WorkflowComment_TaskId", false, "TaskId")]
[BindTable("WorkflowComment", Description = "流程意见。审批动作与意见内容，附件走 Attachment", ConnName = "Workflow", DbType = DatabaseType.None)]
public partial class WorkflowComment : IEntity<WorkflowCommentModel>
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

    private Int64 _TaskId;
    /// <summary>任务。0=发起意见</summary>
    [DisplayName("任务")]
    [Description("任务。0=发起意见")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("TaskId", "任务。0=发起意见", "")]
    public Int64 TaskId { get => _TaskId; set { if (OnPropertyChanging("TaskId", value)) { _TaskId = value; OnPropertyChanged("TaskId"); } } }

    private String _Action;
    /// <summary>动作。start/approve/reject/addSign/cc/rollback/withdraw/transfer/timeout</summary>
    [DisplayName("动作")]
    [Description("动作。start/approve/reject/addSign/cc/rollback/withdraw/transfer/timeout")]
    [DataObjectField(false, false, true, 16)]
    [BindColumn("Action", "动作。start/approve/reject/addSign/cc/rollback/withdraw/transfer/timeout", "")]
    public String Action { get => _Action; set { if (OnPropertyChanging("Action", value)) { _Action = value; OnPropertyChanged("Action"); } } }

    private String _Content;
    /// <summary>意见内容</summary>
    [DisplayName("意见内容")]
    [Description("意见内容")]
    [DataObjectField(false, false, true, 1000)]
    [BindColumn("Content", "意见内容", "")]
    public String Content { get => _Content; set { if (OnPropertyChanging("Content", value)) { _Content = value; OnPropertyChanged("Content"); } } }

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
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(WorkflowCommentModel model)
    {
        Id = model.Id;
        InstanceId = model.InstanceId;
        TaskId = model.TaskId;
        Action = model.Action;
        Content = model.Content;
        CreateUser = model.CreateUser;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
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
            "TaskId" => _TaskId,
            "Action" => _Action,
            "Content" => _Content,
            "CreateUser" => _CreateUser,
            "CreateUserID" => _CreateUserID,
            "CreateIP" => _CreateIP,
            "CreateTime" => _CreateTime,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToLong(); break;
                case "InstanceId": _InstanceId = value.ToLong(); break;
                case "TaskId": _TaskId = value.ToLong(); break;
                case "Action": _Action = Convert.ToString(value); break;
                case "Content": _Content = Convert.ToString(value); break;
                case "CreateUser": _CreateUser = Convert.ToString(value); break;
                case "CreateUserID": _CreateUserID = value.ToInt(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
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
    public static WorkflowComment FindById(Int64 id)
    {
        if (id < 0) return null;

        return Find(_.Id == id);
    }

    /// <summary>根据流程实例查找</summary>
    /// <param name="instanceId">流程实例</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowComment> FindAllByInstanceId(Int64 instanceId)
    {
        if (instanceId < 0) return [];

        return FindAll(_.InstanceId == instanceId);
    }

    /// <summary>根据任务查找</summary>
    /// <param name="taskId">任务</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowComment> FindAllByTaskId(Int64 taskId)
    {
        if (taskId < 0) return [];

        return FindAll(_.TaskId == taskId);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="instanceId">流程实例</param>
    /// <param name="taskId">任务。0=发起意见</param>
    /// <param name="start">编号开始</param>
    /// <param name="end">编号结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowComment> Search(Int64 instanceId, Int64 taskId, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (instanceId >= 0) exp &= _.InstanceId == instanceId;
        if (taskId >= 0) exp &= _.TaskId == taskId;
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
    /// <summary>取得流程意见字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>流程实例</summary>
        public static readonly Field InstanceId = FindByName("InstanceId");

        /// <summary>任务。0=发起意见</summary>
        public static readonly Field TaskId = FindByName("TaskId");

        /// <summary>动作。start/approve/reject/addSign/cc/rollback/withdraw/transfer/timeout</summary>
        public static readonly Field Action = FindByName("Action");

        /// <summary>意见内容</summary>
        public static readonly Field Content = FindByName("Content");

        /// <summary>创建者</summary>
        public static readonly Field CreateUser = FindByName("CreateUser");

        /// <summary>创建用户</summary>
        public static readonly Field CreateUserID = FindByName("CreateUserID");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得流程意见字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>流程实例</summary>
        public const String InstanceId = "InstanceId";

        /// <summary>任务。0=发起意见</summary>
        public const String TaskId = "TaskId";

        /// <summary>动作。start/approve/reject/addSign/cc/rollback/withdraw/transfer/timeout</summary>
        public const String Action = "Action";

        /// <summary>意见内容</summary>
        public const String Content = "Content";

        /// <summary>创建者</summary>
        public const String CreateUser = "CreateUser";

        /// <summary>创建用户</summary>
        public const String CreateUserID = "CreateUserID";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";
    }
    #endregion
}
