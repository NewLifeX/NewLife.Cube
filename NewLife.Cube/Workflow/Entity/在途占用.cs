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

/// <summary>在途占用。同一业务键同时只有一条在途审批</summary>
[Serializable]
[DataObject]
[Description("在途占用。同一业务键同时只有一条在途审批")]
[BindIndex("IU_WorkflowOccupancy_TypePath_EntityKey", true, "TypePath,EntityKey")]
[BindTable("WorkflowOccupancy", Description = "在途占用。同一业务键同时只有一条在途审批", ConnName = "Workflow", DbType = DatabaseType.None)]
public partial class WorkflowOccupancy : IEntity<WorkflowOccupancyModel>
{
    #region 属性
    private Int64 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int64 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private String _TypePath;
    /// <summary>实体路径</summary>
    [DisplayName("实体路径")]
    [Description("实体路径")]
    [DataObjectField(false, false, false, 100)]
    [BindColumn("TypePath", "实体路径", "")]
    public String TypePath { get => _TypePath; set { if (OnPropertyChanging("TypePath", value)) { _TypePath = value; OnPropertyChanged("TypePath"); } } }

    private String _EntityKey;
    /// <summary>业务主键</summary>
    [DisplayName("业务主键")]
    [Description("业务主键")]
    [DataObjectField(false, false, false, 50)]
    [BindColumn("EntityKey", "业务主键", "")]
    public String EntityKey { get => _EntityKey; set { if (OnPropertyChanging("EntityKey", value)) { _EntityKey = value; OnPropertyChanged("EntityKey"); } } }

    private Int64 _InstanceId;
    /// <summary>流程实例</summary>
    [DisplayName("流程实例")]
    [Description("流程实例")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("InstanceId", "流程实例", "")]
    public Int64 InstanceId { get => _InstanceId; set { if (OnPropertyChanging("InstanceId", value)) { _InstanceId = value; OnPropertyChanged("InstanceId"); } } }

    private DateTime _CreateTime;
    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    [Description("创建时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(WorkflowOccupancyModel model)
    {
        Id = model.Id;
        TypePath = model.TypePath;
        EntityKey = model.EntityKey;
        InstanceId = model.InstanceId;
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
            "TypePath" => _TypePath,
            "EntityKey" => _EntityKey,
            "InstanceId" => _InstanceId,
            "CreateTime" => _CreateTime,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToLong(); break;
                case "TypePath": _TypePath = Convert.ToString(value); break;
                case "EntityKey": _EntityKey = Convert.ToString(value); break;
                case "InstanceId": _InstanceId = value.ToLong(); break;
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
    public static WorkflowOccupancy FindById(Int64 id)
    {
        if (id < 0) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

        // 单对象缓存
        return Meta.SingleCache[id];

        //return Find(_.Id == id);
    }

    /// <summary>根据实体路径、业务主键查找</summary>
    /// <param name="typePath">实体路径</param>
    /// <param name="entityKey">业务主键</param>
    /// <returns>实体对象</returns>
    public static WorkflowOccupancy FindByTypePathAndEntityKey(String typePath, String entityKey)
    {
        if (typePath.IsNullOrEmpty()) return null;
        if (entityKey.IsNullOrEmpty()) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.TypePath.EqualIgnoreCase(typePath) && e.EntityKey.EqualIgnoreCase(entityKey));

        return Find(_.TypePath == typePath & _.EntityKey == entityKey);
    }

    /// <summary>根据实体路径查找</summary>
    /// <param name="typePath">实体路径</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowOccupancy> FindAllByTypePath(String typePath)
    {
        if (typePath.IsNullOrEmpty()) return [];

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.TypePath.EqualIgnoreCase(typePath));

        return FindAll(_.TypePath == typePath);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="typePath">实体路径</param>
    /// <param name="entityKey">业务主键</param>
    /// <param name="start">创建时间开始</param>
    /// <param name="end">创建时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<WorkflowOccupancy> Search(String typePath, String entityKey, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (!typePath.IsNullOrEmpty()) exp &= _.TypePath == typePath;
        if (!entityKey.IsNullOrEmpty()) exp &= _.EntityKey == entityKey;
        exp &= _.CreateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 字段名
    /// <summary>取得在途占用字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>实体路径</summary>
        public static readonly Field TypePath = FindByName("TypePath");

        /// <summary>业务主键</summary>
        public static readonly Field EntityKey = FindByName("EntityKey");

        /// <summary>流程实例</summary>
        public static readonly Field InstanceId = FindByName("InstanceId");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得在途占用字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>实体路径</summary>
        public const String TypePath = "TypePath";

        /// <summary>业务主键</summary>
        public const String EntityKey = "EntityKey";

        /// <summary>流程实例</summary>
        public const String InstanceId = "InstanceId";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";
    }
    #endregion
}
