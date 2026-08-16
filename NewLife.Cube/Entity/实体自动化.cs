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

/// <summary>实体自动化流程。按实体配置的触发与动作规则，GraphJson 存线性图</summary>
[Serializable]
[DataObject]
[DisplayName("实体自动化流程")]
[Description("实体自动化流程。按实体配置的触发与动作规则，GraphJson 存线性图")]
[BindIndex("IX_EntityAutomation_TenantId_TypePath_Enable", false, "TenantId,TypePath,Enable")]
[BindIndex("IU_EntityAutomation_HookToken", true, "HookToken")]
[BindTable("EntityAutomation", Description = "实体自动化流程。按实体配置的触发与动作规则，GraphJson 存线性图", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class EntityAutomation : IEntity<EntityAutomationModel>
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
    /// <summary>租户</summary>
    [DisplayName("租户")]
    [Description("租户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("TenantId", "租户", "")]
    public Int32 TenantId { get => _TenantId; set { if (OnPropertyChanging("TenantId", value)) { _TenantId = value; OnPropertyChanged("TenantId"); } } }

    private String _TypePath;
    /// <summary>实体路径。如 Admin/User</summary>
    [DisplayName("实体路径")]
    [Description("实体路径。如 Admin/User")]
    [DataObjectField(false, false, false, 100)]
    [BindColumn("TypePath", "实体路径。如 Admin/User", "")]
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
    [BindColumn("Enable", "启用", "")]
    public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

    private Int32 _Priority;
    /// <summary>优先级。越小越先</summary>
    [DisplayName("优先级")]
    [Description("优先级。越小越先")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Priority", "优先级。越小越先", "")]
    public Int32 Priority { get => _Priority; set { if (OnPropertyChanging("Priority", value)) { _Priority = value; OnPropertyChanged("Priority"); } } }

    private String _TriggerKind;
    /// <summary>触发种类</summary>
    [DisplayName("触发种类")]
    [Description("触发种类")]
    [DataObjectField(false, false, false, 32)]
    [BindColumn("TriggerKind", "触发种类", "")]
    public String TriggerKind { get => _TriggerKind; set { if (OnPropertyChanging("TriggerKind", value)) { _TriggerKind = value; OnPropertyChanged("TriggerKind"); } } }

    private String _TriggerConfig;
    /// <summary>触发配置。JSON</summary>
    [DisplayName("触发配置")]
    [Description("触发配置。JSON")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("TriggerConfig", "触发配置。JSON", "")]
    public String TriggerConfig { get => _TriggerConfig; set { if (OnPropertyChanging("TriggerConfig", value)) { _TriggerConfig = value; OnPropertyChanged("TriggerConfig"); } } }

    private String _GraphJson;
    /// <summary>流程图。nodes/edges JSON</summary>
    [DisplayName("流程图")]
    [Description("流程图。nodes/edges JSON")]
    [DataObjectField(false, false, true, -1)]
    [BindColumn("GraphJson", "流程图。nodes/edges JSON", "")]
    public String GraphJson { get => _GraphJson; set { if (OnPropertyChanging("GraphJson", value)) { _GraphJson = value; OnPropertyChanged("GraphJson"); } } }

    private String _HookToken;
    /// <summary>入站令牌。webhook 用</summary>
    [DisplayName("入站令牌")]
    [Description("入站令牌。webhook 用")]
    [DataObjectField(false, false, true, 64)]
    [BindColumn("HookToken", "入站令牌。webhook 用", "")]
    public String HookToken { get => _HookToken; set { if (OnPropertyChanging("HookToken", value)) { _HookToken = value; OnPropertyChanged("HookToken"); } } }

    private Int32 _Version;
    /// <summary>版本。乐观并发</summary>
    [DisplayName("版本")]
    [Description("版本。乐观并发")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Version", "版本。乐观并发", "")]
    public Int32 Version { get => _Version; set { if (OnPropertyChanging("Version", value)) { _Version = value; OnPropertyChanged("Version"); } } }

    private String _CreateUser;
    /// <summary>创建人</summary>
    [Category("扩展")]
    [DisplayName("创建人")]
    [Description("创建人")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateUser", "创建人", "")]
    public String CreateUser { get => _CreateUser; set { if (OnPropertyChanging("CreateUser", value)) { _CreateUser = value; OnPropertyChanged("CreateUser"); } } }

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

    private String _UpdateUser;
    /// <summary>更新人</summary>
    [Category("扩展")]
    [DisplayName("更新人")]
    [Description("更新人")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UpdateUser", "更新人", "")]
    public String UpdateUser { get => _UpdateUser; set { if (OnPropertyChanging("UpdateUser", value)) { _UpdateUser = value; OnPropertyChanged("UpdateUser"); } } }

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
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(EntityAutomationModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        TypePath = model.TypePath;
        Name = model.Name;
        Enable = model.Enable;
        Priority = model.Priority;
        TriggerKind = model.TriggerKind;
        TriggerConfig = model.TriggerConfig;
        GraphJson = model.GraphJson;
        HookToken = model.HookToken;
        Version = model.Version;
        CreateUser = model.CreateUser;
        CreateUserId = model.CreateUserId;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUser = model.UpdateUser;
        UpdateUserId = model.UpdateUserId;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
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
            "Priority" => _Priority,
            "TriggerKind" => _TriggerKind,
            "TriggerConfig" => _TriggerConfig,
            "GraphJson" => _GraphJson,
            "HookToken" => _HookToken,
            "Version" => _Version,
            "CreateUser" => _CreateUser,
            "CreateUserId" => _CreateUserId,
            "CreateTime" => _CreateTime,
            "CreateIP" => _CreateIP,
            "UpdateUser" => _UpdateUser,
            "UpdateUserId" => _UpdateUserId,
            "UpdateTime" => _UpdateTime,
            "UpdateIP" => _UpdateIP,
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
                case "Priority": _Priority = value.ToInt(); break;
                case "TriggerKind": _TriggerKind = Convert.ToString(value); break;
                case "TriggerConfig": _TriggerConfig = Convert.ToString(value); break;
                case "GraphJson": _GraphJson = Convert.ToString(value); break;
                case "HookToken": _HookToken = Convert.ToString(value); break;
                case "Version": _Version = value.ToInt(); break;
                case "CreateUser": _CreateUser = Convert.ToString(value); break;
                case "CreateUserId": _CreateUserId = value.ToInt(); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "UpdateUser": _UpdateUser = Convert.ToString(value); break;
                case "UpdateUserId": _UpdateUserId = value.ToInt(); break;
                case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
                case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
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
    public static EntityAutomation FindById(Int64 id)
    {
        if (id < 0) return null;

        return Find(_.Id == id);
    }

    /// <summary>根据入站令牌查找</summary>
    /// <param name="hookToken">入站令牌</param>
    /// <returns>实体对象</returns>
    public static EntityAutomation FindByHookToken(String hookToken)
    {
        if (hookToken.IsNullOrEmpty()) return null;

        return Find(_.HookToken == hookToken);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="tenantId">租户</param>
    /// <param name="typePath">实体路径。如 Admin/User</param>
    /// <param name="enable">启用</param>
    /// <param name="hookToken">入站令牌。webhook 用</param>
    /// <param name="start">编号开始</param>
    /// <param name="end">编号结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<EntityAutomation> Search(Int32 tenantId, String typePath, Boolean? enable, String hookToken, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (tenantId >= 0) exp &= _.TenantId == tenantId;
        if (!typePath.IsNullOrEmpty()) exp &= _.TypePath == typePath;
        if (enable != null) exp &= _.Enable == enable;
        if (!hookToken.IsNullOrEmpty()) exp &= _.HookToken == hookToken;
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
    /// <summary>取得实体自动化字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>租户</summary>
        public static readonly Field TenantId = FindByName("TenantId");

        /// <summary>实体路径。如 Admin/User</summary>
        public static readonly Field TypePath = FindByName("TypePath");

        /// <summary>名称</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>优先级。越小越先</summary>
        public static readonly Field Priority = FindByName("Priority");

        /// <summary>触发种类</summary>
        public static readonly Field TriggerKind = FindByName("TriggerKind");

        /// <summary>触发配置。JSON</summary>
        public static readonly Field TriggerConfig = FindByName("TriggerConfig");

        /// <summary>流程图。nodes/edges JSON</summary>
        public static readonly Field GraphJson = FindByName("GraphJson");

        /// <summary>入站令牌。webhook 用</summary>
        public static readonly Field HookToken = FindByName("HookToken");

        /// <summary>版本。乐观并发</summary>
        public static readonly Field Version = FindByName("Version");

        /// <summary>创建人</summary>
        public static readonly Field CreateUser = FindByName("CreateUser");

        /// <summary>创建者</summary>
        public static readonly Field CreateUserId = FindByName("CreateUserId");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>更新人</summary>
        public static readonly Field UpdateUser = FindByName("UpdateUser");

        /// <summary>更新者</summary>
        public static readonly Field UpdateUserId = FindByName("UpdateUserId");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得实体自动化字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>租户</summary>
        public const String TenantId = "TenantId";

        /// <summary>实体路径。如 Admin/User</summary>
        public const String TypePath = "TypePath";

        /// <summary>名称</summary>
        public const String Name = "Name";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>优先级。越小越先</summary>
        public const String Priority = "Priority";

        /// <summary>触发种类</summary>
        public const String TriggerKind = "TriggerKind";

        /// <summary>触发配置。JSON</summary>
        public const String TriggerConfig = "TriggerConfig";

        /// <summary>流程图。nodes/edges JSON</summary>
        public const String GraphJson = "GraphJson";

        /// <summary>入站令牌。webhook 用</summary>
        public const String HookToken = "HookToken";

        /// <summary>版本。乐观并发</summary>
        public const String Version = "Version";

        /// <summary>创建人</summary>
        public const String CreateUser = "CreateUser";

        /// <summary>创建者</summary>
        public const String CreateUserId = "CreateUserId";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>更新人</summary>
        public const String UpdateUser = "UpdateUser";

        /// <summary>更新者</summary>
        public const String UpdateUserId = "UpdateUserId";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";
    }
    #endregion
}
