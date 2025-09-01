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

/// <summary>用户统计</summary>
[Serializable]
[DataObject]
[Description("用户统计")]
[BindIndex("IU_UserStat_Date", true, "Date")]
[BindTable("UserStat", Description = "用户统计", ConnName = "Membership", DbType = DatabaseType.None)]
public partial class UserStat : IEntity<UserStatModel>
{
    #region 属性
    private Int32 _ID;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("ID", "编号", "")]
    public Int32 ID { get => _ID; set { if (OnPropertyChanging("ID", value)) { _ID = value; OnPropertyChanged("ID"); } } }

    private DateTime _Date;
    /// <summary>统计日期</summary>
    [DisplayName("统计日期")]
    [Description("统计日期")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("Date", "统计日期", "", ItemType = "date", DataScale = "time")]
    public DateTime Date { get => _Date; set { if (OnPropertyChanging("Date", value)) { _Date = value; OnPropertyChanged("Date"); } } }

    private Int32 _Total;
    /// <summary>总数。总用户数</summary>
    [DisplayName("总数")]
    [Description("总数。总用户数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Total", "总数。总用户数", "")]
    public Int32 Total { get => _Total; set { if (OnPropertyChanging("Total", value)) { _Total = value; OnPropertyChanged("Total"); } } }

    private Int32 _Logins;
    /// <summary>登录数。总登录数</summary>
    [DisplayName("登录数")]
    [Description("登录数。总登录数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Logins", "登录数。总登录数", "")]
    public Int32 Logins { get => _Logins; set { if (OnPropertyChanging("Logins", value)) { _Logins = value; OnPropertyChanged("Logins"); } } }

    private Int32 _OAuths;
    /// <summary>OAuth登录。OAuth总登录数</summary>
    [DisplayName("OAuth登录")]
    [Description("OAuth登录。OAuth总登录数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("OAuths", "OAuth登录。OAuth总登录数", "")]
    public Int32 OAuths { get => _OAuths; set { if (OnPropertyChanging("OAuths", value)) { _OAuths = value; OnPropertyChanged("OAuths"); } } }

    private Int32 _MaxOnline;
    /// <summary>最大在线。最大在线用户数</summary>
    [DisplayName("最大在线")]
    [Description("最大在线。最大在线用户数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("MaxOnline", "最大在线。最大在线用户数", "")]
    public Int32 MaxOnline { get => _MaxOnline; set { if (OnPropertyChanging("MaxOnline", value)) { _MaxOnline = value; OnPropertyChanged("MaxOnline"); } } }

    private Int32 _Actives;
    /// <summary>活跃。今天活跃用户数</summary>
    [DisplayName("活跃")]
    [Description("活跃。今天活跃用户数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Actives", "活跃。今天活跃用户数", "")]
    public Int32 Actives { get => _Actives; set { if (OnPropertyChanging("Actives", value)) { _Actives = value; OnPropertyChanged("Actives"); } } }

    private Int32 _ActivesT7;
    /// <summary>7天活跃。7天活跃用户数</summary>
    [DisplayName("7天活跃")]
    [Description("7天活跃。7天活跃用户数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ActivesT7", "7天活跃。7天活跃用户数", "")]
    public Int32 ActivesT7 { get => _ActivesT7; set { if (OnPropertyChanging("ActivesT7", value)) { _ActivesT7 = value; OnPropertyChanged("ActivesT7"); } } }

    private Int32 _ActivesT30;
    /// <summary>30天活跃。30天活跃用户数</summary>
    [DisplayName("30天活跃")]
    [Description("30天活跃。30天活跃用户数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ActivesT30", "30天活跃。30天活跃用户数", "")]
    public Int32 ActivesT30 { get => _ActivesT30; set { if (OnPropertyChanging("ActivesT30", value)) { _ActivesT30 = value; OnPropertyChanged("ActivesT30"); } } }

    private Int32 _News;
    /// <summary>新用户。今天注册新用户数</summary>
    [DisplayName("新用户")]
    [Description("新用户。今天注册新用户数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("News", "新用户。今天注册新用户数", "")]
    public Int32 News { get => _News; set { if (OnPropertyChanging("News", value)) { _News = value; OnPropertyChanged("News"); } } }

    private Int32 _NewsT7;
    /// <summary>7天注册。7天内注册新用户数</summary>
    [DisplayName("7天注册")]
    [Description("7天注册。7天内注册新用户数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("NewsT7", "7天注册。7天内注册新用户数", "")]
    public Int32 NewsT7 { get => _NewsT7; set { if (OnPropertyChanging("NewsT7", value)) { _NewsT7 = value; OnPropertyChanged("NewsT7"); } } }

    private Int32 _NewsT30;
    /// <summary>30天注册。30天注册新用户数</summary>
    [DisplayName("30天注册")]
    [Description("30天注册。30天注册新用户数")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("NewsT30", "30天注册。30天注册新用户数", "")]
    public Int32 NewsT30 { get => _NewsT30; set { if (OnPropertyChanging("NewsT30", value)) { _NewsT30 = value; OnPropertyChanged("NewsT30"); } } }

    private Int32 _OnlineTime;
    /// <summary>在线时间。累计在线总时间，秒</summary>
    [DisplayName("在线时间")]
    [Description("在线时间。累计在线总时间，秒")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("OnlineTime", "在线时间。累计在线总时间，秒", "", ItemType = "TimeSpan:d'.'hh':'mm':'ss")]
    public Int32 OnlineTime { get => _OnlineTime; set { if (OnPropertyChanging("OnlineTime", value)) { _OnlineTime = value; OnPropertyChanged("OnlineTime"); } } }

    private DateTime _CreateTime;
    /// <summary>创建时间</summary>
    [Category("扩展")]
    [DisplayName("创建时间")]
    [Description("创建时间")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("CreateTime", "创建时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

    private DateTime _UpdateTime;
    /// <summary>更新时间</summary>
    [Category("扩展")]
    [DisplayName("更新时间")]
    [Description("更新时间")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("UpdateTime", "更新时间", "")]
    public DateTime UpdateTime { get => _UpdateTime; set { if (OnPropertyChanging("UpdateTime", value)) { _UpdateTime = value; OnPropertyChanged("UpdateTime"); } } }

    private String _Remark;
    /// <summary>详细信息</summary>
    [Category("扩展")]
    [DisplayName("详细信息")]
    [Description("详细信息")]
    [DataObjectField(false, false, true, 1000)]
    [BindColumn("Remark", "详细信息", "")]
    public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(UserStatModel model)
    {
        ID = model.ID;
        Date = model.Date;
        Total = model.Total;
        Logins = model.Logins;
        OAuths = model.OAuths;
        MaxOnline = model.MaxOnline;
        Actives = model.Actives;
        ActivesT7 = model.ActivesT7;
        ActivesT30 = model.ActivesT30;
        News = model.News;
        NewsT7 = model.NewsT7;
        NewsT30 = model.NewsT30;
        OnlineTime = model.OnlineTime;
        CreateTime = model.CreateTime;
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
            "Date" => _Date,
            "Total" => _Total,
            "Logins" => _Logins,
            "OAuths" => _OAuths,
            "MaxOnline" => _MaxOnline,
            "Actives" => _Actives,
            "ActivesT7" => _ActivesT7,
            "ActivesT30" => _ActivesT30,
            "News" => _News,
            "NewsT7" => _NewsT7,
            "NewsT30" => _NewsT30,
            "OnlineTime" => _OnlineTime,
            "CreateTime" => _CreateTime,
            "UpdateTime" => _UpdateTime,
            "Remark" => _Remark,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "ID": _ID = value.ToInt(); break;
                case "Date": _Date = value.ToDateTime(); break;
                case "Total": _Total = value.ToInt(); break;
                case "Logins": _Logins = value.ToInt(); break;
                case "OAuths": _OAuths = value.ToInt(); break;
                case "MaxOnline": _MaxOnline = value.ToInt(); break;
                case "Actives": _Actives = value.ToInt(); break;
                case "ActivesT7": _ActivesT7 = value.ToInt(); break;
                case "ActivesT30": _ActivesT30 = value.ToInt(); break;
                case "News": _News = value.ToInt(); break;
                case "NewsT7": _NewsT7 = value.ToInt(); break;
                case "NewsT30": _NewsT30 = value.ToInt(); break;
                case "OnlineTime": _OnlineTime = value.ToInt(); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
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
    /// <summary>根据统计日期查找</summary>
    /// <param name="date">统计日期</param>
    /// <returns>实体对象</returns>
    public static UserStat FindByDate(DateTime date)
    {
        if (date.Year < 1000) return null;

        return Find(_.Date == date);
    }

    /// <summary>根据统计日期查找</summary>
    /// <param name="date">统计日期</param>
    /// <returns>实体列表</returns>
    public static IList<UserStat> FindAllByDate(DateTime date)
    {
        if (date.Year < 1000) return [];

        return FindAll(_.Date == date);
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
        if (start == end) return Delete(_.Date == start);

        return Delete(_.Date.Between(start, end), maximumRows);
    }
    #endregion

    #region 字段名
    /// <summary>取得用户统计字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field ID = FindByName("ID");

        /// <summary>统计日期</summary>
        public static readonly Field Date = FindByName("Date");

        /// <summary>总数。总用户数</summary>
        public static readonly Field Total = FindByName("Total");

        /// <summary>登录数。总登录数</summary>
        public static readonly Field Logins = FindByName("Logins");

        /// <summary>OAuth登录。OAuth总登录数</summary>
        public static readonly Field OAuths = FindByName("OAuths");

        /// <summary>最大在线。最大在线用户数</summary>
        public static readonly Field MaxOnline = FindByName("MaxOnline");

        /// <summary>活跃。今天活跃用户数</summary>
        public static readonly Field Actives = FindByName("Actives");

        /// <summary>7天活跃。7天活跃用户数</summary>
        public static readonly Field ActivesT7 = FindByName("ActivesT7");

        /// <summary>30天活跃。30天活跃用户数</summary>
        public static readonly Field ActivesT30 = FindByName("ActivesT30");

        /// <summary>新用户。今天注册新用户数</summary>
        public static readonly Field News = FindByName("News");

        /// <summary>7天注册。7天内注册新用户数</summary>
        public static readonly Field NewsT7 = FindByName("NewsT7");

        /// <summary>30天注册。30天注册新用户数</summary>
        public static readonly Field NewsT30 = FindByName("NewsT30");

        /// <summary>在线时间。累计在线总时间，秒</summary>
        public static readonly Field OnlineTime = FindByName("OnlineTime");

        /// <summary>创建时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>更新时间</summary>
        public static readonly Field UpdateTime = FindByName("UpdateTime");

        /// <summary>详细信息</summary>
        public static readonly Field Remark = FindByName("Remark");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得用户统计字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String ID = "ID";

        /// <summary>统计日期</summary>
        public const String Date = "Date";

        /// <summary>总数。总用户数</summary>
        public const String Total = "Total";

        /// <summary>登录数。总登录数</summary>
        public const String Logins = "Logins";

        /// <summary>OAuth登录。OAuth总登录数</summary>
        public const String OAuths = "OAuths";

        /// <summary>最大在线。最大在线用户数</summary>
        public const String MaxOnline = "MaxOnline";

        /// <summary>活跃。今天活跃用户数</summary>
        public const String Actives = "Actives";

        /// <summary>7天活跃。7天活跃用户数</summary>
        public const String ActivesT7 = "ActivesT7";

        /// <summary>30天活跃。30天活跃用户数</summary>
        public const String ActivesT30 = "ActivesT30";

        /// <summary>新用户。今天注册新用户数</summary>
        public const String News = "News";

        /// <summary>7天注册。7天内注册新用户数</summary>
        public const String NewsT7 = "NewsT7";

        /// <summary>30天注册。30天注册新用户数</summary>
        public const String NewsT30 = "NewsT30";

        /// <summary>在线时间。累计在线总时间，秒</summary>
        public const String OnlineTime = "OnlineTime";

        /// <summary>创建时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>更新时间</summary>
        public const String UpdateTime = "UpdateTime";

        /// <summary>详细信息</summary>
        public const String Remark = "Remark";
    }
    #endregion
}
