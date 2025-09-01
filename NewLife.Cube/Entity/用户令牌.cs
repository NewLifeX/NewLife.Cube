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

/// <summary>用户令牌。授权指定用户访问接口数据，支持有效期</summary>
[Serializable]
[DataObject]
[Description("用户令牌。授权指定用户访问接口数据，支持有效期")]
[BindIndex("IU_UserToken_Token", true, "Token")]
[BindIndex("IX_UserToken_UserID", false, "UserID")]
[BindTable("UserToken", Description = "用户令牌。授权指定用户访问接口数据，支持有效期", ConnName = "Membership", DbType = DatabaseType.None)]
public partial class UserToken : IEntity<UserTokenModel>
{
    #region 属性
    private Int32 _ID;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("ID", "编号", "")]
    public Int32 ID { get => _ID; set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } } }

    private String _Token;
    /// <summary>令牌</summary>
    [DisplayName("令牌")]
    [Description("令牌")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Token", "令牌", "")]
    public String Token { get => _Token; set { if (OnPropertyChanging("Token", value)) { _Token = value; OnPropertyChanged("Token"); } } }

    private String _Url;
    /// <summary>地址。锁定该令牌只能访问该资源路径</summary>
    [DisplayName("地址")]
    [Description("地址。锁定该令牌只能访问该资源路径")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Url", "地址。锁定该令牌只能访问该资源路径", "")]
    public String Url { get => _Url; set { if (OnPropertyChanging("Url", value)) { _Url = value; OnPropertyChanged("Url"); } } }

    private Int32 _UserID;
    /// <summary>用户。本地用户</summary>
    [DisplayName("用户")]
    [Description("用户。本地用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UserID", "用户。本地用户", "")]
    public Int32 UserID { get => _UserID; set { if (OnPropertyChanging("UserID", value)) { _UserID = value; OnPropertyChanged("UserID"); } } }

    private DateTime _Expire;
    /// <summary>过期时间</summary>
    [DisplayName("过期时间")]
    [Description("过期时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("Expire", "过期时间", "")]
    public DateTime Expire { get => _Expire; set { if (OnPropertyChanging("Expire", value)) { _Expire = value; OnPropertyChanged("Expire"); } } }

    private Boolean _Enable;
    /// <summary>启用</summary>
    [DisplayName("启用")]
    [Description("启用")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Enable", "启用", "")]
    public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

    private Int32 _Times;
    /// <summary>次数。该令牌使用次数</summary>
    [DisplayName("次数")]
    [Description("次数。该令牌使用次数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Times", "次数。该令牌使用次数", "")]
    public Int32 Times { get => _Times; set { if (OnPropertyChanging("Times", value)) { _Times = value; OnPropertyChanged("Times"); } } }

    private String _FirstIP;
    /// <summary>首次地址</summary>
    [DisplayName("首次地址")]
    [Description("首次地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("FirstIP", "首次地址", "")]
    public String FirstIP { get => _FirstIP; set { if (OnPropertyChanging("FirstIP", value)) { _FirstIP = value; OnPropertyChanged("FirstIP"); } } }

    private DateTime _FirstTime;
    /// <summary>首次时间</summary>
    [DisplayName("首次时间")]
    [Description("首次时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("FirstTime", "首次时间", "")]
    public DateTime FirstTime { get => _FirstTime; set { if (OnPropertyChanging("FirstTime", value)) { _FirstTime = value; OnPropertyChanged("FirstTime"); } } }

    private String _LastIP;
    /// <summary>最后地址</summary>
    [DisplayName("最后地址")]
    [Description("最后地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("LastIP", "最后地址", "")]
    public String LastIP { get => _LastIP; set { if (OnPropertyChanging("LastIP", value)) { _LastIP = value; OnPropertyChanged("LastIP"); } } }

    private DateTime _LastTime;
    /// <summary>最后时间</summary>
    [DisplayName("最后时间")]
    [Description("最后时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("LastTime", "最后时间", "")]
    public DateTime LastTime { get => _LastTime; set { if (OnPropertyChanging("LastTime", value)) { _LastTime = value; OnPropertyChanged("LastTime"); } } }

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
    public void Copy(UserTokenModel model)
    {
        ID = model.ID;
        Token = model.Token;
        Url = model.Url;
        UserID = model.UserID;
        Expire = model.Expire;
        Enable = model.Enable;
        Times = model.Times;
        FirstIP = model.FirstIP;
        FirstTime = model.FirstTime;
        LastIP = model.LastIP;
        LastTime = model.LastTime;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateUserID = model.UpdateUserID;
        UpdateIP = model.UpdateIP;
        UpdateTime = model.UpdateTime;
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
            "ID" => _ID,
            "Token" => _Token,
            "Url" => _Url,
            "UserID" => _UserID,
            "Expire" => _Expire,
            "Enable" => _Enable,
            "Times" => _Times,
            "FirstIP" => _FirstIP,
            "FirstTime" => _FirstTime,
            "LastIP" => _LastIP,
            "LastTime" => _LastTime,
            "CreateUserID" => _CreateUserID,
            "CreateIP" => _CreateIP,
            "CreateTime" => _CreateTime,
            "UpdateUserID" => _UpdateUserID,
            "UpdateIP" => _UpdateIP,
            "UpdateTime" => _UpdateTime,
            "Remark" => _Remark,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "ID": _ID = value.ToInt(); break;
                case "Token": _Token = Convert.ToString(value); break;
                case "Url": _Url = Convert.ToString(value); break;
                case "UserID": _UserID = value.ToInt(); break;
                case "Expire": _Expire = value.ToDateTime(); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "Times": _Times = value.ToInt(); break;
                case "FirstIP": _FirstIP = Convert.ToString(value); break;
                case "FirstTime": _FirstTime = value.ToDateTime(); break;
                case "LastIP": _LastIP = Convert.ToString(value); break;
                case "LastTime": _LastTime = value.ToDateTime(); break;
                case "CreateUserID": _CreateUserID = value.ToInt(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "UpdateUserID": _UpdateUserID = value.ToInt(); break;
                case "UpdateIP": _UpdateIP = Convert.ToString(value); break;
                case "UpdateTime": _UpdateTime = value.ToDateTime(); break;
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
    /// <param name="token">令牌</param>
    /// <param name="userId">用户。本地用户</param>
    /// <param name="enable">启用</param>
    /// <param name="start">更新时间开始</param>
    /// <param name="end">更新时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<UserToken> Search(String token, Int32 userId, Boolean? enable, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (!token.IsNullOrEmpty()) exp &= _.Token == token;
        if (userId >= 0) exp &= _.UserID == userId;
        if (enable != null) exp &= _.Enable == enable;
        exp &= _.UpdateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 字段名
    /// <summary>取得用户令牌字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field ID = FindByName("ID");

        /// <summary>令牌</summary>
        public static readonly Field Token = FindByName("Token");

        /// <summary>地址。锁定该令牌只能访问该资源路径</summary>
        public static readonly Field Url = FindByName("Url");

        /// <summary>用户。本地用户</summary>
        public static readonly Field UserID = FindByName("UserID");

        /// <summary>过期时间</summary>
        public static readonly Field Expire = FindByName("Expire");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>次数。该令牌使用次数</summary>
        public static readonly Field Times = FindByName("Times");

        /// <summary>首次地址</summary>
        public static readonly Field FirstIP = FindByName("FirstIP");

        /// <summary>首次时间</summary>
        public static readonly Field FirstTime = FindByName("FirstTime");

        /// <summary>最后地址</summary>
        public static readonly Field LastIP = FindByName("LastIP");

        /// <summary>最后时间</summary>
        public static readonly Field LastTime = FindByName("LastTime");

        /// <summary>创建用户</summary>
        public static readonly Field CreateUserID = FindByName("CreateUserID");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>更新用户</summary>
        public static readonly Field UpdateUserID = FindByName("UpdateUserID");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>备注</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得用户令牌字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String ID = "ID";

        /// <summary>令牌</summary>
        public const String Token = "Token";

        /// <summary>地址。锁定该令牌只能访问该资源路径</summary>
        public const String Url = "Url";

        /// <summary>用户。本地用户</summary>
        public const String UserID = "UserID";

        /// <summary>过期时间</summary>
        public const String Expire = "Expire";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>次数。该令牌使用次数</summary>
        public const String Times = "Times";

        /// <summary>首次地址</summary>
        public const String FirstIP = "FirstIP";

        /// <summary>首次时间</summary>
        public const String FirstTime = "FirstTime";

        /// <summary>最后地址</summary>
        public const String LastIP = "LastIP";

        /// <summary>最后时间</summary>
        public const String LastTime = "LastTime";

        /// <summary>创建用户</summary>
        public const String CreateUserID = "CreateUserID";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>更新用户</summary>
        public const String UpdateUserID = "UpdateUserID";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>备注</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
