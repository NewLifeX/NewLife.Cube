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

/// <summary>流程主体。实例关联的业务记录，一批最多 100 条</summary>
[Serializable]
[DataObject]
[Description("流程主体。实例关联的业务记录，一批最多 100 条")]
[BindIndex("IX_WorkflowSubject_InstanceId", false, "InstanceId")]
[BindIndex("IX_WorkflowSubject_TypePath_EntityKey", false, "TypePath,EntityKey")]
[BindTable("WorkflowSubject", Description = "流程主体。实例关联的业务记录，一批最多 100 条", ConnName = "Workflow", DbType = DatabaseType.None)]
public partial class WorkflowSubject : IEntity<WorkflowSubjectModel>
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

    private String _TypePath;
    /// <summary>实体路径。必须等于实例 TypePath</summary>
    [DisplayName("实体路径")]
    [Description("实体路径。必须等于实例 TypePath")]
    [DataObjectField(false, false, true, 100)]
    [BindColumn("TypePath", "实体路径。必须等于实例 TypePath", "")]
    public String TypePath { get => _TypePath; set { if (OnPropertyChanging("TypePath", value)) { _TypePath = value; OnPropertyChanged("TypePath"); } } }

    private String _EntityKey;
    /// <summary>业务主键。归一化后的单主键字符串</summary>
    [DisplayName("业务主键")]
    [Description("业务主键。归一化后的单主键字符串")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("EntityKey", "业务主键。归一化后的单主键字符串", "")]
    public String EntityKey { get => _EntityKey; set { if (OnPropertyChanging("EntityKey", value)) { _EntityKey = value; OnPropertyChanged("EntityKey"); } } }

    private String _Title;
    /// <summary>标题。发起时快照显示名</summary>
    [DisplayName("标题")]
    [Description("标题。发起时快照显示名")]
    [DataObjectField(false, false, true, 100)]
    [BindColumn("Title", "标题。发起时快照显示名", "", Master = true)]
    public String Title { get => _Title; set { if (OnPropertyChanging("Title", value)) { _Title = value; OnPropertyChanged("Title"); } } }

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
    public void Copy(WorkflowSubjectModel model)
    {
        Id = model.Id;
        InstanceId = model.InstanceId;
        TypePath = model.TypePath;
        EntityKey = model.EntityKey;
        Title = model.Title;
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
            "TypePath" => _TypePath,
            "EntityKey" => _EntityKey,
            "Title" => _Title,
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
                case "TypePath": _TypePath = Convert.ToString(value); break;
                case "EntityKey": _EntityKey = Convert.ToString(value); break;
                case "Title": _Title = Convert.ToString(value); break;
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
    public static WorkflowSubject FindById(Int64 id)
    {
        if (id < 0) return null;

        return Find(_.Id == id);
    }

    /// <summary>根据流程实例查找</summary>
    /// <param name="instanceId">流程实例</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowSubject> FindAllByInstanceId(Int64 instanceId)
    {
        if (instanceId < 0) return [];

        return FindAll(_.InstanceId == instanceId);
    }

    /// <summary>根据实体路径、业务主键查找</summary>
    /// <param name="typePath">实体路径</param>
    /// <param name="entityKey">业务主键</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowSubject> FindAllByTypePathAndEntityKey(String typePath, String entityKey)
    {
        if (typePath.IsNullOrEmpty()) return [];
        if (entityKey.IsNullOrEmpty()) return [];

        return FindAll(_.TypePath == typePath & _.EntityKey == entityKey);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="instanceId">流程实例</param>
    /// <param name="typePath">实体路径。必须等于实例 TypePath</param>
    /// <param name="entityKey">业务主键。归一化后的单主键字符串</param>
    /// <param name="start">编号开始</param>
    /// <param name="end">编号结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowSubject> Search(Int64 instanceId, String typePath, String entityKey, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (instanceId >= 0) exp &= _.InstanceId == instanceId;
        if (!typePath.IsNullOrEmpty()) exp &= _.TypePath == typePath;
        if (!entityKey.IsNullOrEmpty()) exp &= _.EntityKey == entityKey;
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
    /// <summary>取得流程主体字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>流程实例</summary>
        public static readonly Field InstanceId = FindByName("InstanceId");

        /// <summary>实体路径。必须等于实例 TypePath</summary>
        public static readonly Field TypePath = FindByName("TypePath");

        /// <summary>业务主键。归一化后的单主键字符串</summary>
        public static readonly Field EntityKey = FindByName("EntityKey");

        /// <summary>标题。发起时快照显示名</summary>
        public static readonly Field Title = FindByName("Title");

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

    /// <summary>取得流程主体字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>流程实例</summary>
        public const String InstanceId = "InstanceId";

        /// <summary>实体路径。必须等于实例 TypePath</summary>
        public const String TypePath = "TypePath";

        /// <summary>业务主键。归一化后的单主键字符串</summary>
        public const String EntityKey = "EntityKey";

        /// <summary>标题。发起时快照显示名</summary>
        public const String Title = "Title";

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
