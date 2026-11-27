#nullable disable
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

/// <summary>用户链接。第三方绑定</summary>
[Serializable]
[DataObject]
[Description("用户链接。第三方绑定")]
[BindIndex("IU_UserConnect_Provider_OpenId", true, "Provider,OpenId")]
[BindIndex("IX_UserConnect_UserId", false, "UserId")]
[BindIndex("IX_UserConnect_OpenId", false, "OpenId")]
[BindIndex("IX_UserConnect_UnionId", false, "UnionId")]
[BindIndex("IX_UserConnect_DeviceId", false, "DeviceId")]
[BindTable("UserConnect", Description = "用户链接。第三方绑定", ConnName = "Membership", DbType = DatabaseType.None)]
public partial class UserConnect : IEntity<UserConnectModel>
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private String _Provider;
    /// <summary>提供商</summary>
    [DisplayName("提供商")]
    [Description("提供商")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Provider", "提供商", "")]
    public String Provider { get => _Provider; set { if (OnPropertyChanging("Provider", value)) { _Provider = value; OnPropertyChanged("Provider"); } } }

    private Int32 _UserId;
    /// <summary>用户。本地用户</summary>
    [DisplayName("用户")]
    [Description("用户。本地用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UserId", "用户。本地用户", "")]
    public Int32 UserId { get => _UserId; set { if (OnPropertyChanging("UserId", value)) { _UserId = value; OnPropertyChanged("UserId"); } } }

    private String _OpenId;
    /// <summary>身份标识。用户名、OpenId</summary>
    [DisplayName("身份标识")]
    [Description("身份标识。用户名、OpenId")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("OpenId", "身份标识。用户名、OpenId", "")]
    public String OpenId { get => _OpenId; set { if (OnPropertyChanging("OpenId", value)) { _OpenId = value; OnPropertyChanged("OpenId"); } } }

    private String _UnionId;
    /// <summary>全局标识。跨应用统一</summary>
    [DisplayName("全局标识")]
    [Description("全局标识。跨应用统一")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("UnionId", "全局标识。跨应用统一", "")]
    public String UnionId { get => _UnionId; set { if (OnPropertyChanging("UnionId", value)) { _UnionId = value; OnPropertyChanged("UnionId"); } } }

    private Int64 _LinkId;
    /// <summary>用户编号。第三方用户编号</summary>
    [DisplayName("用户编号")]
    [Description("用户编号。第三方用户编号")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("LinkId", "用户编号。第三方用户编号", "")]
    public Int64 LinkId { get => _LinkId; set { if (OnPropertyChanging("LinkId", value)) { _LinkId = value; OnPropertyChanged("LinkId"); } } }

    private String _NickName;
    /// <summary>昵称</summary>
    [DisplayName("昵称")]
    [Description("昵称")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("NickName", "昵称", "")]
    public String NickName { get => _NickName; set { if (OnPropertyChanging("NickName", value)) { _NickName = value; OnPropertyChanged("NickName"); } } }

    private String _DeviceId;
    /// <summary>设备标识。企业微信用于唯一标识设备，重装后改变</summary>
    [DisplayName("设备标识")]
    [Description("设备标识。企业微信用于唯一标识设备，重装后改变")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("DeviceId", "设备标识。企业微信用于唯一标识设备，重装后改变", "")]
    public String DeviceId { get => _DeviceId; set { if (OnPropertyChanging("DeviceId", value)) { _DeviceId = value; OnPropertyChanged("DeviceId"); } } }

    private String _Avatar;
    /// <summary>头像</summary>
    [DisplayName("头像")]
    [Description("头像")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Avatar", "头像", "", ShowIn = "Auto,-List,-Search")]
    public String Avatar { get => _Avatar; set { if (OnPropertyChanging("Avatar", value)) { _Avatar = value; OnPropertyChanged("Avatar"); } } }

    private String _AccessToken;
    /// <summary>访问令牌</summary>
    [DisplayName("访问令牌")]
    [Description("访问令牌")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("AccessToken", "访问令牌", "", ItemType = "password", ShowIn = "Auto,-List,-Search")]
    public String AccessToken { get => _AccessToken; set { if (OnPropertyChanging("AccessToken", value)) { _AccessToken = value; OnPropertyChanged("AccessToken"); } } }

    private String _RefreshToken;
    /// <summary>刷新令牌</summary>
    [DisplayName("刷新令牌")]
    [Description("刷新令牌")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("RefreshToken", "刷新令牌", "", ItemType = "password", ShowIn = "Auto,-List,-Search")]
    public String RefreshToken { get => _RefreshToken; set { if (OnPropertyChanging("RefreshToken", value)) { _RefreshToken = value; OnPropertyChanged("RefreshToken"); } } }

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

    private Int32 _CreateUserId;
    /// <summary>创建用户</summary>
    [Category("扩展")]
    [DisplayName("创建用户")]
    [Description("创建用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CreateUserId", "创建用户", "")]
    public Int32 CreateUserId { get => _CreateUserId; set { if (OnPropertyChanging("CreateUserId", value)) { _CreateUserId = value; OnPropertyChanged("CreateUserId"); } } }

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

    private Int32 _UpdateUserId;
    /// <summary>更新用户</summary>
    [Category("扩展")]
    [DisplayName("更新用户")]
    [Description("更新用户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UpdateUserId", "更新用户", "")]
    public Int32 UpdateUserId { get => _UpdateUserId; set { if (OnPropertyChanging("UpdateUserId", value)) { _UpdateUserId = value; OnPropertyChanged("UpdateUserId"); } } }

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
    [DataObjectField(false, false, true, 5000)]
    [BindColumn("Remark", "备注", "")]
    public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(UserConnectModel model)
    {
        Id = model.Id;
        Provider = model.Provider;
        UserId = model.UserId;
        OpenId = model.OpenId;
        UnionId = model.UnionId;
        LinkId = model.LinkId;
        NickName = model.NickName;
        DeviceId = model.DeviceId;
        Avatar = model.Avatar;
        AccessToken = model.AccessToken;
        RefreshToken = model.RefreshToken;
        Expire = model.Expire;
        Enable = model.Enable;
        CreateUserId = model.CreateUserId;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateUserId = model.UpdateUserId;
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
            "Id" => _Id,
            "Provider" => _Provider,
            "UserId" => _UserId,
            "OpenId" => _OpenId,
            "UnionId" => _UnionId,
            "LinkId" => _LinkId,
            "NickName" => _NickName,
            "DeviceId" => _DeviceId,
            "Avatar" => _Avatar,
            "AccessToken" => _AccessToken,
            "RefreshToken" => _RefreshToken,
            "Expire" => _Expire,
            "Enable" => _Enable,
            "CreateUserId" => _CreateUserId,
            "CreateIP" => _CreateIP,
            "CreateTime" => _CreateTime,
            "UpdateUserId" => _UpdateUserId,
            "UpdateIP" => _UpdateIP,
            "UpdateTime" => _UpdateTime,
            "Remark" => _Remark,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToInt(); break;
                case "Provider": _Provider = Convert.ToString(value); break;
                case "UserId": _UserId = value.ToInt(); break;
                case "OpenId": _OpenId = Convert.ToString(value); break;
                case "UnionId": _UnionId = Convert.ToString(value); break;
                case "LinkId": _LinkId = value.ToLong(); break;
                case "NickName": _NickName = Convert.ToString(value); break;
                case "DeviceId": _DeviceId = Convert.ToString(value); break;
                case "Avatar": _Avatar = Convert.ToString(value); break;
                case "AccessToken": _AccessToken = Convert.ToString(value); break;
                case "RefreshToken": _RefreshToken = Convert.ToString(value); break;
                case "Expire": _Expire = value.ToDateTime(); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "CreateUserId": _CreateUserId = value.ToInt(); break;
                case "CreateIP": _CreateIP = Convert.ToString(value); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "UpdateUserId": _UpdateUserId = value.ToInt(); break;
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
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static UserConnect FindById(Int32 id)
    {
        if (id < 0) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

        // 单对象缓存
        return Meta.SingleCache[id];

        //return Find(_.Id == id);
    }

    /// <summary>根据提供商、身份标识查找</summary>
    /// <param name="provider">提供商</param>
    /// <param name="openId">身份标识</param>
    /// <returns>实体对象</returns>
    public static UserConnect FindByProviderAndOpenId(String provider, String openId)
    {
        if (provider.IsNullOrEmpty()) return null;
        if (openId.IsNullOrEmpty()) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Provider.EqualIgnoreCase(provider) && e.OpenId.EqualIgnoreCase(openId));

        return Find(_.Provider == provider & _.OpenId == openId);
    }

    /// <summary>根据提供商查找</summary>
    /// <param name="provider">提供商</param>
    /// <returns>实体列表</returns>
    public static IList<UserConnect> FindAllByProvider(String provider)
    {
        if (provider.IsNullOrEmpty()) return [];

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.Provider.EqualIgnoreCase(provider));

        return FindAll(_.Provider == provider);
    }

    /// <summary>根据用户查找</summary>
    /// <param name="userId">用户</param>
    /// <returns>实体列表</returns>
    public static IList<UserConnect> FindAllByUserId(Int32 userId)
    {
        if (userId < 0) return [];

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.UserId == userId);

        return FindAll(_.UserId == userId);
    }

    /// <summary>根据身份标识查找</summary>
    /// <param name="openId">身份标识</param>
    /// <returns>实体列表</returns>
    public static IList<UserConnect> FindAllByOpenId(String openId)
    {
        if (openId.IsNullOrEmpty()) return [];

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.OpenId.EqualIgnoreCase(openId));

        return FindAll(_.OpenId == openId);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="provider">提供商</param>
    /// <param name="userId">用户。本地用户</param>
    /// <param name="openId">身份标识。用户名、OpenId</param>
    /// <param name="unionId">全局标识。跨应用统一</param>
    /// <param name="deviceId">设备标识。企业微信用于唯一标识设备，重装后改变</param>
    /// <param name="enable">启用</param>
    /// <param name="start">更新时间开始</param>
    /// <param name="end">更新时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<UserConnect> Search(String provider, Int32 userId, String openId, String unionId, String deviceId, Boolean? enable, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (!provider.IsNullOrEmpty()) exp &= _.Provider == provider;
        if (userId >= 0) exp &= _.UserId == userId;
        if (!openId.IsNullOrEmpty()) exp &= _.OpenId == openId;
        if (!unionId.IsNullOrEmpty()) exp &= _.UnionId == unionId;
        if (!deviceId.IsNullOrEmpty()) exp &= _.DeviceId == deviceId;
        if (enable != null) exp &= _.Enable == enable;
        exp &= _.UpdateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 字段名
    /// <summary>取得用户链接字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>提供商</summary>
        public static readonly Field Provider = FindByName("Provider");

        /// <summary>用户。本地用户</summary>
        public static readonly Field UserId = FindByName("UserId");

        /// <summary>身份标识。用户名、OpenId</summary>
        public static readonly Field OpenId = FindByName("OpenId");

        /// <summary>全局标识。跨应用统一</summary>
        public static readonly Field UnionId = FindByName("UnionId");

        /// <summary>用户编号。第三方用户编号</summary>
        public static readonly Field LinkId = FindByName("LinkId");

        /// <summary>昵称</summary>
        public static readonly Field NickName = FindByName("NickName");

        /// <summary>设备标识。企业微信用于唯一标识设备，重装后改变</summary>
        public static readonly Field DeviceId = FindByName("DeviceId");

        /// <summary>头像</summary>
        public static readonly Field Avatar = FindByName("Avatar");

        /// <summary>访问令牌</summary>
        public static readonly Field AccessToken = FindByName("AccessToken");

        /// <summary>刷新令牌</summary>
        public static readonly Field RefreshToken = FindByName("RefreshToken");

        /// <summary>过期时间</summary>
        public static readonly Field Expire = FindByName("Expire");

        /// <summary>启用</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>创建用户</summary>
        public static readonly Field CreateUserId = FindByName("CreateUserId");

        /// <summary>创建地址</summary>
        public static readonly Field CreateIP = FindByName("CreateIP");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>更新用户</summary>
        public static readonly Field UpdateUserId = FindByName("UpdateUserId");

        /// <summary>更新地址</summary>
        public static readonly Field UpdateIP = FindByName("UpdateIP");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>备注</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得用户链接字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>提供商</summary>
        public const String Provider = "Provider";

        /// <summary>用户。本地用户</summary>
        public const String UserId = "UserId";

        /// <summary>身份标识。用户名、OpenId</summary>
        public const String OpenId = "OpenId";

        /// <summary>全局标识。跨应用统一</summary>
        public const String UnionId = "UnionId";

        /// <summary>用户编号。第三方用户编号</summary>
        public const String LinkId = "LinkId";

        /// <summary>昵称</summary>
        public const String NickName = "NickName";

        /// <summary>设备标识。企业微信用于唯一标识设备，重装后改变</summary>
        public const String DeviceId = "DeviceId";

        /// <summary>头像</summary>
        public const String Avatar = "Avatar";

        /// <summary>访问令牌</summary>
        public const String AccessToken = "AccessToken";

        /// <summary>刷新令牌</summary>
        public const String RefreshToken = "RefreshToken";

        /// <summary>过期时间</summary>
        public const String Expire = "Expire";

        /// <summary>启用</summary>
        public const String Enable = "Enable";

        /// <summary>创建用户</summary>
        public const String CreateUserId = "CreateUserId";

        /// <summary>创建地址</summary>
        public const String CreateIP = "CreateIP";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>更新用户</summary>
        public const String UpdateUserId = "UpdateUserId";

        /// <summary>更新地址</summary>
        public const String UpdateIP = "UpdateIP";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>备注</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
