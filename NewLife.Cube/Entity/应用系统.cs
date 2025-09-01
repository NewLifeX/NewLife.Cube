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

/// <summary>应用系统。用于OAuthServer的子系统</summary>
[Serializable]
[DataObject]
[Description("应用系统。用于OAuthServer的子系统")]
[BindIndex("IU_OAuthApp_Name", true, "Name")]
[BindTable("OAuthApp", Description = "应用系统。用于OAuthServer的子系统", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class App : IEntity<AppModel>
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private String _Name;
    /// <summary>名称。AppID</summary>
    [DisplayName("名称")]
    [Description("名称。AppID")]
    [DataObjectField(false, false, false, 50)]
    [BindColumn("Name", "名称。AppID", "", Master = true)]
    public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

    private String _DisplayName;
    /// <summary>显示名</summary>
    [DisplayName("显示名")]
    [Description("显示名")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("DisplayName", "显示名", "")]
    public String DisplayName { get => _DisplayName; set { if (OnPropertyChanging("DisplayName", value)) { _DisplayName = value; OnPropertyChanged("DisplayName"); } } }

    private String _Secret;
    /// <summary>密钥。AppSecret</summary>
    [DisplayName("密钥")]
    [Description("密钥。AppSecret")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Secret", "密钥。AppSecret", "")]
    public String Secret { get => _Secret; set { if (OnPropertyChanging("Secret", value)) { _Secret = value; OnPropertyChanged("Secret"); } } }

    private String _Category;
    /// <summary>类别</summary>
    [DisplayName("类别")]
    [Description("类别")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Category", "类别", "")]
    public String Category { get => _Category; set { if (OnPropertyChanging("Category", value)) { _Category = value; OnPropertyChanged("Category"); } } }

    private Boolean _Enable;
    /// <summary>启用</summary>
    [DisplayName("启用")]
    [Description("启用")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Enable", "启用", "")]
    public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

    private String _HomePage;
    /// <summary>首页</summary>
    [DisplayName("首页")]
    [Description("首页")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("HomePage", "首页", "")]
    public String HomePage { get => _HomePage; set { if (OnPropertyChanging("HomePage", value)) { _HomePage = value; OnPropertyChanged("HomePage"); } } }

    private String _Logo;
    /// <summary>图标。附件路径</summary>
    [DisplayName("图标")]
    [Description("图标。附件路径")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Logo", "图标。附件路径", "", ItemType = "image")]
    public String Logo { get => _Logo; set { if (OnPropertyChanging("Logo", value)) { _Logo = value; OnPropertyChanged("Logo"); } } }

    private String _White;
    /// <summary>IP白名单。符合条件的来源IP才允许访问，支持*通配符，多个逗号隔开</summary>
    [Category("安全告警")]
    [DisplayName("IP白名单")]
    [Description("IP白名单。符合条件的来源IP才允许访问，支持*通配符，多个逗号隔开")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("White", "IP白名单。符合条件的来源IP才允许访问，支持*通配符，多个逗号隔开", "")]
    public String White { get => _White; set { if (OnPropertyChanging("White", value)) { _White = value; OnPropertyChanged("White"); } } }

    private String _Black;
    /// <summary>IP黑名单。符合条件的来源IP禁止访问，支持*通配符，多个逗号隔开</summary>
    [Category("安全告警")]
    [DisplayName("IP黑名单")]
    [Description("IP黑名单。符合条件的来源IP禁止访问，支持*通配符，多个逗号隔开")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Black", "IP黑名单。符合条件的来源IP禁止访问，支持*通配符，多个逗号隔开", "")]
    public String Black { get => _Black; set { if (OnPropertyChanging("Black", value)) { _Black = value; OnPropertyChanged("Black"); } } }

    private Int32 _TokenExpire;
    /// <summary>有效期。访问令牌AccessToken的有效期，单位秒，默认使用全局设置</summary>
    [DisplayName("有效期")]
    [Description("有效期。访问令牌AccessToken的有效期，单位秒，默认使用全局设置")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("TokenExpire", "有效期。访问令牌AccessToken的有效期，单位秒，默认使用全局设置", "", ItemType = "TimeSpan")]
    public Int32 TokenExpire { get => _TokenExpire; set { if (OnPropertyChanging("TokenExpire", value)) { _TokenExpire = value; OnPropertyChanged("TokenExpire"); } } }

    private String _Urls;
    /// <summary>回调地址。用于限制回调地址安全性，多个地址逗号隔开</summary>
    [DisplayName("回调地址")]
    [Description("回调地址。用于限制回调地址安全性，多个地址逗号隔开")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("Urls", "回调地址。用于限制回调地址安全性，多个地址逗号隔开", "")]
    public String Urls { get => _Urls; set { if (OnPropertyChanging("Urls", value)) { _Urls = value; OnPropertyChanged("Urls"); } } }

    private String _RoleIds;
    /// <summary>授权角色。只允许这些角色登录该系统，多个角色逗号隔开，未填写时表示不限制</summary>
    [DisplayName("授权角色")]
    [Description("授权角色。只允许这些角色登录该系统，多个角色逗号隔开，未填写时表示不限制")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("RoleIds", "授权角色。只允许这些角色登录该系统，多个角色逗号隔开，未填写时表示不限制", "")]
    public String RoleIds { get => _RoleIds; set { if (OnPropertyChanging("RoleIds", value)) { _RoleIds = value; OnPropertyChanged("RoleIds"); } } }

    private String _Scopes;
    /// <summary>能力集合。逗号分隔，password，client_credentials</summary>
    [DisplayName("能力集合")]
    [Description("能力集合。逗号分隔，password，client_credentials")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Scopes", "能力集合。逗号分隔，password，client_credentials", "")]
    public String Scopes { get => _Scopes; set { if (OnPropertyChanging("Scopes", value)) { _Scopes = value; OnPropertyChanged("Scopes"); } } }

    private String _OAuths;
    /// <summary>三方OAuth。本系统作为OAuthServer时，该应用前来验证时可用的第三方OAuth提供商，多个逗号隔开</summary>
    [DisplayName("三方OAuth")]
    [Description("三方OAuth。本系统作为OAuthServer时，该应用前来验证时可用的第三方OAuth提供商，多个逗号隔开")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("OAuths", "三方OAuth。本系统作为OAuthServer时，该应用前来验证时可用的第三方OAuth提供商，多个逗号隔开", "")]
    public String OAuths { get => _OAuths; set { if (OnPropertyChanging("OAuths", value)) { _OAuths = value; OnPropertyChanged("OAuths"); } } }

    private DateTime _Expired;
    /// <summary>过期时间。空表示永不过期</summary>
    [DisplayName("过期时间")]
    [Description("过期时间。空表示永不过期")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("Expired", "过期时间。空表示永不过期", "")]
    public DateTime Expired { get => _Expired; set { if (OnPropertyChanging("Expired", value)) { _Expired = value; OnPropertyChanged("Expired"); } } }

    private Int32 _Auths;
    /// <summary>次数</summary>
    [DisplayName("次数")]
    [Description("次数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Auths", "次数", "")]
    public Int32 Auths { get => _Auths; set { if (OnPropertyChanging("Auths", value)) { _Auths = value; OnPropertyChanged("Auths"); } } }

    private DateTime _LastAuth;
    /// <summary>最后请求</summary>
    [DisplayName("最后请求")]
    [Description("最后请求")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("LastAuth", "最后请求", "")]
    public DateTime LastAuth { get => _LastAuth; set { if (OnPropertyChanging("LastAuth", value)) { _LastAuth = value; OnPropertyChanged("LastAuth"); } } }

    private Int32 _CreateUserID;
    /// <summary>创建者</summary>
    [Category("扩展")]
    [DisplayName("创建者")]
    [Description("创建者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CreateUserID", "创建者", "")]
    public Int32 CreateUserID { get => _CreateUserID; set { if (OnPropertyChanging("CreateUserID", value)) { _CreateUserID = value; OnPropertyChanged("CreateUserID"); } } }

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

    private Int32 _UpdateUserID;
    /// <summary>更新者</summary>
    [Category("扩展")]
    [DisplayName("更新者")]
    [Description("更新者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UpdateUserID", "更新者", "")]
    public Int32 UpdateUserID { get => _UpdateUserID; set { if (OnPropertyChanging("UpdateUserID", value)) { _UpdateUserID = value; OnPropertyChanged("UpdateUserID"); } } }

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
    public void Copy(AppModel model)
    {
        Id = model.Id;
        Name = model.Name;
        DisplayName = model.DisplayName;
        Secret = model.Secret;
        Category = model.Category;
        Enable = model.Enable;
        HomePage = model.HomePage;
        Logo = model.Logo;
        White = model.White;
        Black = model.Black;
        TokenExpire = model.TokenExpire;
        Urls = model.Urls;
        RoleIds = model.RoleIds;
        Scopes = model.Scopes;
        OAuths = model.OAuths;
        Expired = model.Expired;
        Auths = model.Auths;
        LastAuth = model.LastAuth;
        CreateUserID = model.CreateUserID;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserID = model.UpdateUserID;
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
            "Name" => _Name,
            "DisplayName" => _DisplayName,
            "Secret" => _Secret,
            "Category" => _Category,
            "Enable" => _Enable,
            "HomePage" => _HomePage,
            "Logo" => _Logo,
            "White" => _White,
            "Black" => _Black,
            "TokenExpire" => _TokenExpire,
            "Urls" => _Urls,
            "RoleIds" => _RoleIds,
            "Scopes" => _Scopes,
            "OAuths" => _OAuths,
            "Expired" => _Expired,
            "Auths" => _Auths,
            "LastAuth" => _LastAuth,
            "CreateUserID" => _CreateUserID,
            "CreateTime" => _CreateTime,
            "CreateIP" => _CreateIP,
            "UpdateUserID" => _UpdateUserID,
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
                case "Name": _Name = Convert.ToString(value); break;
                case "DisplayName": _DisplayName = Convert.ToString(value); break;
                case "Secret": _Secret = Convert.ToString(value); break;
                case "Category": _Category = Convert.ToString(value); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "HomePage": _HomePage = Convert.ToString(value); break;
                case "Logo": _Logo = Convert.ToString(value); break;
                case "White": _White = Convert.ToString(value); break;
                case "Black": _Black = Convert.ToString(value); break;
                case "TokenExpire": _TokenExpire = value.ToInt(); break;
                case "Urls": _Urls = Convert.ToString(value); break;
                case "RoleIds": _RoleIds = Convert.ToString(value); break;
                case "Scopes": _Scopes = Convert.ToString(value); break;
                case "OAuths": _OAuths = Convert.ToString(value); break;
                case "Expired": _Expired = value.ToDateTime(); break;
                case "Auths": _Auths = value.ToInt(); break;
                case "LastAuth": _LastAuth = value.ToDateTime(); break;
                case "CreateUserID": _CreateUserID = value.ToInt(); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "UpdateUserID": _UpdateUserID = value.ToInt(); break;
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
    /// <param name="enable">启用</param>
    /// <param name="start">更新时间开始</param>
    /// <param name="end">更新时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<App> Search(Boolean? enable, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (enable != null) exp &= _.Enable == enable;
        exp &= _.UpdateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 字段名
    /// <summary>取得应用系统字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>名称。AppID</summary>
        public static readonly Field Name = FindByName("Name");

        /// <summary>显示名</summary>
        public static readonly Field DisplayName = FindByName("DisplayName");

        /// <summary>密钥。AppSecret</summary>
        public static readonly Field Secret = FindByName("Secret");

        /// <summary>类别</summary>
        public static readonly Field Category = FindByName("Category");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>首页</summary>
        public static readonly Field HomePage = FindByName("HomePage");

        /// <summary>图标。附件路径</summary>
        public static readonly Field Logo = FindByName("Logo");

        /// <summary>IP白名单。符合条件的来源IP才允许访问，支持*通配符，多个逗号隔开</summary>
        public static readonly Field White = FindByName("White");

        /// <summary>IP黑名单。符合条件的来源IP禁止访问，支持*通配符，多个逗号隔开</summary>
        public static readonly Field Black = FindByName("Black");

        /// <summary>有效期。访问令牌AccessToken的有效期，单位秒，默认使用全局设置</summary>
        public static readonly Field TokenExpire = FindByName("TokenExpire");

        /// <summary>回调地址。用于限制回调地址安全性，多个地址逗号隔开</summary>
        public static readonly Field Urls = FindByName("Urls");

        /// <summary>授权角色。只允许这些角色登录该系统，多个角色逗号隔开，未填写时表示不限制</summary>
        public static readonly Field RoleIds = FindByName("RoleIds");

        /// <summary>能力集合。逗号分隔，password，client_credentials</summary>
        public static readonly Field Scopes = FindByName("Scopes");

        /// <summary>三方OAuth。本系统作为OAuthServer时，该应用前来验证时可用的第三方OAuth提供商，多个逗号隔开</summary>
        public static readonly Field OAuths = FindByName("OAuths");

        /// <summary>过期时间。空表示永不过期</summary>
        public static readonly Field Expired = FindByName("Expired");

        /// <summary>次数</summary>
        public static readonly Field Auths = FindByName("Auths");

        /// <summary>最后请求</summary>
        public static readonly Field LastAuth = FindByName("LastAuth");

        /// <summary>创建者</summary>
        public static readonly Field CreateUserID = FindByName("CreateUserID");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>更新者</summary>
        public static readonly Field UpdateUserID = FindByName("UpdateUserID");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        /// <summary>内容</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得应用系统字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>名称。AppID</summary>
        public const String Name = "Name";

        /// <summary>显示名</summary>
        public const String DisplayName = "DisplayName";

        /// <summary>密钥。AppSecret</summary>
        public const String Secret = "Secret";

        /// <summary>类别</summary>
        public const String Category = "Category";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>首页</summary>
        public const String HomePage = "HomePage";

        /// <summary>图标。附件路径</summary>
        public const String Logo = "Logo";

        /// <summary>IP白名单。符合条件的来源IP才允许访问，支持*通配符，多个逗号隔开</summary>
        public const String White = "White";

        /// <summary>IP黑名单。符合条件的来源IP禁止访问，支持*通配符，多个逗号隔开</summary>
        public const String Black = "Black";

        /// <summary>有效期。访问令牌AccessToken的有效期，单位秒，默认使用全局设置</summary>
        public const String TokenExpire = "TokenExpire";

        /// <summary>回调地址。用于限制回调地址安全性，多个地址逗号隔开</summary>
        public const String Urls = "Urls";

        /// <summary>授权角色。只允许这些角色登录该系统，多个角色逗号隔开，未填写时表示不限制</summary>
        public const String RoleIds = "RoleIds";

        /// <summary>能力集合。逗号分隔，password，client_credentials</summary>
        public const String Scopes = "Scopes";

        /// <summary>三方OAuth。本系统作为OAuthServer时，该应用前来验证时可用的第三方OAuth提供商，多个逗号隔开</summary>
        public const String OAuths = "OAuths";

        /// <summary>过期时间。空表示永不过期</summary>
        public const String Expired = "Expired";

        /// <summary>次数</summary>
        public const String Auths = "Auths";

        /// <summary>最后请求</summary>
        public const String LastAuth = "LastAuth";

        /// <summary>创建者</summary>
        public const String CreateUserID = "CreateUserID";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>更新者</summary>
        public const String UpdateUserID = "UpdateUserID";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";

        /// <summary>内容</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
