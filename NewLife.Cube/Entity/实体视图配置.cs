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

/// <summary>实体视图配置。按用户与实体路径的视图类型与列布局</summary>
[Serializable]
[DataObject]
[Description("实体视图配置。按用户与实体路径的视图类型与列布局")]
[BindIndex("IU_EntityViewProfile_UserId_TypePath", true, "UserId,TypePath")]
[BindTable("EntityViewProfile", Description = "实体视图配置。按用户与实体路径的视图类型与列布局", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class EntityViewProfile : IEntity<EntityViewProfileModel>
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int32 _UserId;
    /// <summary>用户</summary>
    [DisplayName("用户")]
    [Description("用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UserId", "用户", "")]
    public Int32 UserId { get => _UserId; set { if (OnPropertyChanging("UserId", value)) { _UserId = value; OnPropertyChanged("UserId"); } } }

    private String _TypePath;
    /// <summary>实体路径。如 Admin/User</summary>
    [DisplayName("实体路径")]
    [Description("实体路径。如 Admin/User")]
    [DataObjectField(false, false, false, 200)]
    [BindColumn("TypePath", "实体路径。如 Admin/User", "")]
    public String TypePath { get => _TypePath; set { if (OnPropertyChanging("TypePath", value)) { _TypePath = value; OnPropertyChanged("TypePath"); } } }

    private String _View;
    /// <summary>视图。table/tree/card/gantt</summary>
    [DisplayName("视图")]
    [Description("视图。table/tree/card/gantt")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("View", "视图。table/tree/card/gantt", "")]
    public String View { get => _View; set { if (OnPropertyChanging("View", value)) { _View = value; OnPropertyChanged("View"); } } }

    private String _ColumnsJson;
    /// <summary>列布局。JSON 数组</summary>
    [DisplayName("列布局")]
    [Description("列布局。JSON 数组")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("ColumnsJson", "列布局。JSON 数组", "")]
    public String ColumnsJson { get => _ColumnsJson; set { if (OnPropertyChanging("ColumnsJson", value)) { _ColumnsJson = value; OnPropertyChanged("ColumnsJson"); } } }

    private String _GanttJson;
    /// <summary>甘特映射。JSON</summary>
    [DisplayName("甘特映射")]
    [Description("甘特映射。JSON")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("GanttJson", "甘特映射。JSON", "")]
    public String GanttJson { get => _GanttJson; set { if (OnPropertyChanging("GanttJson", value)) { _GanttJson = value; OnPropertyChanged("GanttJson"); } } }

    private String _CardJson;
    /// <summary>卡片映射。JSON</summary>
    [DisplayName("卡片映射")]
    [Description("卡片映射。JSON")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("CardJson", "卡片映射。JSON", "")]
    public String CardJson { get => _CardJson; set { if (OnPropertyChanging("CardJson", value)) { _CardJson = value; OnPropertyChanged("CardJson"); } } }

    private String _FiltersJson;
    /// <summary>筛选记忆。JSON</summary>
    [DisplayName("筛选记忆")]
    [Description("筛选记忆。JSON")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("FiltersJson", "筛选记忆。JSON", "")]
    public String FiltersJson { get => _FiltersJson; set { if (OnPropertyChanging("FiltersJson", value)) { _FiltersJson = value; OnPropertyChanged("FiltersJson"); } } }

    private Int32 _Version;
    /// <summary>版本。配置契约版本</summary>
    [DisplayName("版本")]
    [Description("版本。配置契约版本")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Version", "版本。配置契约版本", "")]
    public Int32 Version { get => _Version; set { if (OnPropertyChanging("Version", value)) { _Version = value; OnPropertyChanged("Version"); } } }

    private Int32 _CreateUserId;
    /// <summary>创建者</summary>
    [Category("扩展")]
    [DisplayName("创建者")]
    [Description("创建者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CreateUserId", "创建者", "")]
    public Int32 CreateUserId { get => _CreateUserId; set { if (OnPropertyChanging("CreateUserId", value)) { _CreateUserId = value; OnPropertyChanged("CreateUserId"); } } }

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

    private Int32 _UpdateUserId;
    /// <summary>更新者</summary>
    [Category("扩展")]
    [DisplayName("更新者")]
    [Description("更新者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UpdateUserId", "更新者", "")]
    public Int32 UpdateUserId { get => _UpdateUserId; set { if (OnPropertyChanging("UpdateUserId", value)) { _UpdateUserId = value; OnPropertyChanged("UpdateUserId"); } } }

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
    public void Copy(EntityViewProfileModel model)
    {
        Id = model.Id;
        UserId = model.UserId;
        TypePath = model.TypePath;
        View = model.View;
        ColumnsJson = model.ColumnsJson;
        GanttJson = model.GanttJson;
        CardJson = model.CardJson;
        FiltersJson = model.FiltersJson;
        Version = model.Version;
        CreateUserId = model.CreateUserId;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserId = model.UpdateUserId;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
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
            "UserId" => _UserId,
            "TypePath" => _TypePath,
            "View" => _View,
            "ColumnsJson" => _ColumnsJson,
            "GanttJson" => _GanttJson,
            "CardJson" => _CardJson,
            "FiltersJson" => _FiltersJson,
            "Version" => _Version,
            "CreateUserId" => _CreateUserId,
            "CreateTime" => _CreateTime,
            "CreateIP" => _CreateIP,
            "UpdateUserId" => _UpdateUserId,
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
                case "UserId": _UserId = value.ToInt(); break;
                case "TypePath": _TypePath = Convert.ToString(value); break;
                case "View": _View = Convert.ToString(value); break;
                case "ColumnsJson": _ColumnsJson = Convert.ToString(value); break;
                case "GanttJson": _GanttJson = Convert.ToString(value); break;
                case "CardJson": _CardJson = Convert.ToString(value); break;
                case "FiltersJson": _FiltersJson = Convert.ToString(value); break;
                case "Version": _Version = value.ToInt(); break;
                case "CreateUserId": _CreateUserId = value.ToInt(); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "UpdateUserId": _UpdateUserId = value.ToInt(); break;
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

    #region 扩展查询
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static EntityViewProfile FindById(Int32 id)
    {
        if (id < 0) return null;

        // 实体缓存
        if (Meta.Session.Count < MaxCacheCount) return Meta.Cache.Find(e => e.Id == id);

        // 单对象缓存
        return Meta.SingleCache[id];

        //return Find(_.Id == id);
    }

    /// <summary>根据用户、实体路径查找</summary>
    /// <param name="userId">用户</param>
    /// <param name="typePath">实体路径</param>
    /// <returns>实体对象</returns>
    public static EntityViewProfile FindByUserIdAndTypePath(Int32 userId, String typePath)
    {
        if (userId < 0) return null;
        if (typePath.IsNullOrEmpty()) return null;

        // 实体缓存
        if (Meta.Session.Count < MaxCacheCount) return Meta.Cache.Find(e => e.UserId == userId && e.TypePath.EqualIgnoreCase(typePath));

        return Find(_.UserId == userId & _.TypePath == typePath);
    }

    /// <summary>根据用户查找</summary>
    /// <param name="userId">用户</param>
    /// <returns>实体列表</returns>
    public static IList<EntityViewProfile> FindAllByUserId(Int32 userId)
    {
        if (userId < 0) return [];

        // 实体缓存
        if (Meta.Session.Count < MaxCacheCount) return Meta.Cache.FindAll(e => e.UserId == userId);

        return FindAll(_.UserId == userId);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="userId">用户</param>
    /// <param name="typePath">实体路径。如 Admin/User</param>
    /// <param name="start">更新时间开始</param>
    /// <param name="end">更新时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<EntityViewProfile> Search(Int32 userId, String typePath, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (userId >= 0) exp &= _.UserId == userId;
        if (!typePath.IsNullOrEmpty()) exp &= _.TypePath == typePath;
        exp &= _.UpdateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 字段名
    /// <summary>取得实体视图配置字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>用户</summary>
        public static readonly Field UserId = FindByName("UserId");

        /// <summary>实体路径。如 Admin/User</summary>
        public static readonly Field TypePath = FindByName("TypePath");

        /// <summary>视图。table/tree/card/gantt</summary>
        public static readonly Field View = FindByName("View");

        /// <summary>列布局。JSON 数组</summary>
        public static readonly Field ColumnsJson = FindByName("ColumnsJson");

        /// <summary>甘特映射。JSON</summary>
        public static readonly Field GanttJson = FindByName("GanttJson");

        /// <summary>卡片映射。JSON</summary>
        public static readonly Field CardJson = FindByName("CardJson");

        /// <summary>筛选记忆。JSON</summary>
        public static readonly Field FiltersJson = FindByName("FiltersJson");

        /// <summary>版本。配置契约版本</summary>
        public static readonly Field Version = FindByName("Version");

        /// <summary>创建者</summary>
        public static readonly Field CreateUserId = FindByName("CreateUserId");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>更新者</summary>
        public static readonly Field UpdateUserId = FindByName("UpdateUserId");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        /// <summary>备注</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得实体视图配置字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>用户</summary>
        public const String UserId = "UserId";

        /// <summary>实体路径。如 Admin/User</summary>
        public const String TypePath = "TypePath";

        /// <summary>视图。table/tree/card/gantt</summary>
        public const String View = "View";

        /// <summary>列布局。JSON 数组</summary>
        public const String ColumnsJson = "ColumnsJson";

        /// <summary>甘特映射。JSON</summary>
        public const String GanttJson = "GanttJson";

        /// <summary>卡片映射。JSON</summary>
        public const String CardJson = "CardJson";

        /// <summary>筛选记忆。JSON</summary>
        public const String FiltersJson = "FiltersJson";

        /// <summary>版本。配置契约版本</summary>
        public const String Version = "Version";

        /// <summary>创建者</summary>
        public const String CreateUserId = "CreateUserId";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>更新者</summary>
        public const String UpdateUserId = "UpdateUserId";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";

        /// <summary>备注</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
