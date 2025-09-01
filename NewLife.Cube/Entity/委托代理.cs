﻿using System;
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

/// <summary>委托代理。委托某人代理自己的用户权限，代理人下一次登录时将得到委托人的身份</summary>
[Serializable]
[DataObject]
[Description("委托代理。委托某人代理自己的用户权限，代理人下一次登录时将得到委托人的身份")]
[BindIndex("IX_PrincipalAgent_PrincipalId", false, "PrincipalId")]
[BindIndex("IX_PrincipalAgent_AgentId", false, "AgentId")]
[BindTable("PrincipalAgent", Description = "委托代理。委托某人代理自己的用户权限，代理人下一次登录时将得到委托人的身份", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class PrincipalAgent : IEntity<PrincipalAgentModel>
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int32 _PrincipalId;
    /// <summary>委托人。把自己的身份权限委托给别人</summary>
    [DisplayName("委托人")]
    [Description("委托人。把自己的身份权限委托给别人")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("PrincipalId", "委托人。把自己的身份权限委托给别人", "")]
    public Int32 PrincipalId { get => _PrincipalId; set { if (OnPropertyChanging("PrincipalId", value)) { _PrincipalId = value; OnPropertyChanged("PrincipalId"); } } }

    private Int32 _AgentId;
    /// <summary>代理人。代理获得别人身份权限</summary>
    [DisplayName("代理人")]
    [Description("代理人。代理获得别人身份权限")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("AgentId", "代理人。代理获得别人身份权限", "")]
    public Int32 AgentId { get => _AgentId; set { if (OnPropertyChanging("AgentId", value)) { _AgentId = value; OnPropertyChanged("AgentId"); } } }

    private Boolean _Enable;
    /// <summary>启用</summary>
    [DisplayName("启用")]
    [Description("启用")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Enable", "启用", "")]
    public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

    private Int32 _Times;
    /// <summary>次数。可用代理次数，0表示已用完，-1表示无限制</summary>
    [DisplayName("次数")]
    [Description("次数。可用代理次数，0表示已用完，-1表示无限制")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Times", "次数。可用代理次数，0表示已用完，-1表示无限制", "")]
    public Int32 Times { get => _Times; set { if (OnPropertyChanging("Times", value)) { _Times = value; OnPropertyChanged("Times"); } } }

    private DateTime _Expire;
    /// <summary>有效期。截止时间之前有效，不设置表示无限制</summary>
    [DisplayName("有效期")]
    [Description("有效期。截止时间之前有效，不设置表示无限制")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("Expire", "有效期。截止时间之前有效，不设置表示无限制", "")]
    public DateTime Expire { get => _Expire; set { if (OnPropertyChanging("Expire", value)) { _Expire = value; OnPropertyChanged("Expire"); } } }

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
    /// <summary>内容</summary>
    [Category("扩展")]
    [DisplayName("内容")]
    [Description("内容")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Remark", "内容", "")]
    public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(PrincipalAgentModel model)
    {
        Id = model.Id;
        PrincipalId = model.PrincipalId;
        AgentId = model.AgentId;
        Enable = model.Enable;
        Times = model.Times;
        Expire = model.Expire;
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
            "PrincipalId" => _PrincipalId,
            "AgentId" => _AgentId,
            "Enable" => _Enable,
            "Times" => _Times,
            "Expire" => _Expire,
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
                case "PrincipalId": _PrincipalId = value.ToInt(); break;
                case "AgentId": _AgentId = value.ToInt(); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "Times": _Times = value.ToInt(); break;
                case "Expire": _Expire = value.ToDateTime(); break;
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
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="principalId">委托人。把自己的身份权限委托给别人</param>
    /// <param name="agentId">代理人。代理获得别人身份权限</param>
    /// <param name="enable">启用</param>
    /// <param name="start">更新时间开始</param>
    /// <param name="end">更新时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<PrincipalAgent> Search(Int32 principalId, Int32 agentId, Boolean? enable, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (principalId >= 0) exp &= _.PrincipalId == principalId;
        if (agentId >= 0) exp &= _.AgentId == agentId;
        if (enable != null) exp &= _.Enable == enable;
        exp &= _.UpdateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 字段名
    /// <summary>取得委托代理字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>委托人。把自己的身份权限委托给别人</summary>
        public static readonly Field PrincipalId = FindByName("PrincipalId");

        /// <summary>代理人。代理获得别人身份权限</summary>
        public static readonly Field AgentId = FindByName("AgentId");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>次数。可用代理次数，0表示已用完，-1表示无限制</summary>
        public static readonly Field Times = FindByName("Times");

        /// <summary>有效期。截止时间之前有效，不设置表示无限制</summary>
        public static readonly Field Expire = FindByName("Expire");

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

        /// <summary>内容</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得委托代理字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>委托人。把自己的身份权限委托给别人</summary>
        public const String PrincipalId = "PrincipalId";

        /// <summary>代理人。代理获得别人身份权限</summary>
        public const String AgentId = "AgentId";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>次数。可用代理次数，0表示已用完，-1表示无限制</summary>
        public const String Times = "Times";

        /// <summary>有效期。截止时间之前有效，不设置表示无限制</summary>
        public const String Expire = "Expire";

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

        /// <summary>内容</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
