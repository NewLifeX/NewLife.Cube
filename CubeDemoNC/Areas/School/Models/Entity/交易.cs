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

namespace NewLife.School.Entity;

/// <summary>交易</summary>
[Serializable]
[DataObject]
[Description("交易")]
[BindIndex("IX_Trade_TenantId_NodeId", false, "TenantId,NodeId")]
[BindIndex("IX_Trade_NodeId", false, "NodeId")]
[BindTable("Trade", Description = "交易", ConnName = "Bill", DbType = DatabaseType.SqlServer)]
public partial class Trade : ITrade, IEntity<TradeModel>
{
    #region 属性
    private Int64 _Id;
    /// <summary>订单编号</summary>
    [DisplayName("订单编号")]
    [Description("订单编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "订单编号", "")]
    public Int64 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private Int32 _TenantId;
    /// <summary>租户</summary>
    [DisplayName("租户")]
    [Description("租户")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("TenantId", "租户", "")]
    public Int32 TenantId { get => _TenantId; set { if (OnPropertyChanging("TenantId", value)) { _TenantId = value; OnPropertyChanged("TenantId"); } } }

    private Int32 _NodeId;
    /// <summary>节点号</summary>
    [DisplayName("节点号")]
    [Description("节点号")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("NodeId", "节点号", "")]
    public Int32 NodeId { get => _NodeId; set { if (OnPropertyChanging("NodeId", value)) { _NodeId = value; OnPropertyChanged("NodeId"); } } }

    private String _Tid;
    /// <summary>订单号</summary>
    [DisplayName("订单号")]
    [Description("订单号")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Tid", "订单号", "")]
    public String Tid { get => _Tid; set { if (OnPropertyChanging("Tid", value)) { _Tid = value; OnPropertyChanged("Tid"); } } }

    private Int32 _Status;
    /// <summary>状态</summary>
    [DisplayName("状态")]
    [Description("状态")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Status", "状态", "")]
    public Int32 Status { get => _Status; set { if (OnPropertyChanging("Status", value)) { _Status = value; OnPropertyChanged("Status"); } } }

    private Int32 _PayStatus;
    /// <summary>是否支付</summary>
    [DisplayName("是否支付")]
    [Description("是否支付")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("PayStatus", "是否支付", "")]
    public Int32 PayStatus { get => _PayStatus; set { if (OnPropertyChanging("PayStatus", value)) { _PayStatus = value; OnPropertyChanged("PayStatus"); } } }

    private Int32 _ShipStatus;
    /// <summary>是否发货</summary>
    [DisplayName("是否发货")]
    [Description("是否发货")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("ShipStatus", "是否发货", "")]
    public Int32 ShipStatus { get => _ShipStatus; set { if (OnPropertyChanging("ShipStatus", value)) { _ShipStatus = value; OnPropertyChanged("ShipStatus"); } } }

    private String _CreateIPReceiverPhone;
    /// <summary>收货人电话</summary>
    [DisplayName("收货人电话")]
    [Description("收货人电话")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("CreateIPReceiverPhone", "收货人电话", "")]
    public String CreateIPReceiverPhone { get => _CreateIPReceiverPhone; set { if (OnPropertyChanging("CreateIPReceiverPhone", value)) { _CreateIPReceiverPhone = value; OnPropertyChanged("CreateIPReceiverPhone"); } } }

    private String _ReceiverMobile;
    /// <summary>收货人手机号</summary>
    [DisplayName("收货人手机号")]
    [Description("收货人手机号")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("ReceiverMobile", "收货人手机号", "")]
    public String ReceiverMobile { get => _ReceiverMobile; set { if (OnPropertyChanging("ReceiverMobile", value)) { _ReceiverMobile = value; OnPropertyChanged("ReceiverMobile"); } } }

    private String _ReceiverState;
    /// <summary>收货省</summary>
    [DisplayName("收货省")]
    [Description("收货省")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("ReceiverState", "收货省", "")]
    public String ReceiverState { get => _ReceiverState; set { if (OnPropertyChanging("ReceiverState", value)) { _ReceiverState = value; OnPropertyChanged("ReceiverState"); } } }

    private String _ReceiverCity;
    /// <summary>收货人区</summary>
    [DisplayName("收货人区")]
    [Description("收货人区")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("ReceiverCity", "收货人区", "")]
    public String ReceiverCity { get => _ReceiverCity; set { if (OnPropertyChanging("ReceiverCity", value)) { _ReceiverCity = value; OnPropertyChanged("ReceiverCity"); } } }

    private String _ReceiverDistrict;
    /// <summary>收货区</summary>
    [DisplayName("收货区")]
    [Description("收货区")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("Receiver_District", "收货区", "")]
    public String ReceiverDistrict { get => _ReceiverDistrict; set { if (OnPropertyChanging("ReceiverDistrict", value)) { _ReceiverDistrict = value; OnPropertyChanged("ReceiverDistrict"); } } }

    private String _ReceiverAddress;
    /// <summary>收货地址</summary>
    [DisplayName("收货地址")]
    [Description("收货地址")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("ReceiverAddress", "收货地址", "")]
    public String ReceiverAddress { get => _ReceiverAddress; set { if (OnPropertyChanging("ReceiverAddress", value)) { _ReceiverAddress = value; OnPropertyChanged("ReceiverAddress"); } } }

    private String _BuyerName;
    /// <summary>买家昵称</summary>
    [DisplayName("买家昵称")]
    [Description("买家昵称")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("BuyerName", "买家昵称", "")]
    public String BuyerName { get => _BuyerName; set { if (OnPropertyChanging("BuyerName", value)) { _BuyerName = value; OnPropertyChanged("BuyerName"); } } }

    private Int32 _Created;
    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    [Description("创建时间")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Created", "创建时间", "")]
    public Int32 Created { get => _Created; set { if (OnPropertyChanging("Created", value)) { _Created = value; OnPropertyChanged("Created"); } } }

    private Int32 _Modified;
    /// <summary>是否发送过</summary>
    [DisplayName("是否发送过")]
    [Description("是否发送过")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Modified", "是否发送过", "")]
    public Int32 Modified { get => _Modified; set { if (OnPropertyChanging("Modified", value)) { _Modified = value; OnPropertyChanged("Modified"); } } }

    private Int32 _IsSend;
    /// <summary>更新者</summary>
    [DisplayName("更新者")]
    [Description("更新者")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("IsSend", "更新者", "")]
    public Int32 IsSend { get => _IsSend; set { if (OnPropertyChanging("IsSend", value)) { _IsSend = value; OnPropertyChanged("IsSend"); } } }

    private String _ErrorMsg;
    /// <summary>错误原因</summary>
    [DisplayName("错误原因")]
    [Description("错误原因")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("ErrorMsg", "错误原因", "")]
    public String ErrorMsg { get => _ErrorMsg; set { if (OnPropertyChanging("ErrorMsg", value)) { _ErrorMsg = value; OnPropertyChanged("ErrorMsg"); } } }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(TradeModel model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        NodeId = model.NodeId;
        Tid = model.Tid;
        Status = model.Status;
        PayStatus = model.PayStatus;
        ShipStatus = model.ShipStatus;
        CreateIPReceiverPhone = model.CreateIPReceiverPhone;
        ReceiverMobile = model.ReceiverMobile;
        ReceiverState = model.ReceiverState;
        ReceiverCity = model.ReceiverCity;
        ReceiverDistrict = model.ReceiverDistrict;
        ReceiverAddress = model.ReceiverAddress;
        BuyerName = model.BuyerName;
        Created = model.Created;
        Modified = model.Modified;
        IsSend = model.IsSend;
        ErrorMsg = model.ErrorMsg;
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
            "NodeId" => _NodeId,
            "Tid" => _Tid,
            "Status" => _Status,
            "PayStatus" => _PayStatus,
            "ShipStatus" => _ShipStatus,
            "CreateIPReceiverPhone" => _CreateIPReceiverPhone,
            "ReceiverMobile" => _ReceiverMobile,
            "ReceiverState" => _ReceiverState,
            "ReceiverCity" => _ReceiverCity,
            "ReceiverDistrict" => _ReceiverDistrict,
            "ReceiverAddress" => _ReceiverAddress,
            "BuyerName" => _BuyerName,
            "Created" => _Created,
            "Modified" => _Modified,
            "IsSend" => _IsSend,
            "ErrorMsg" => _ErrorMsg,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToLong(); break;
                case "TenantId": _TenantId = value.ToInt(); break;
                case "NodeId": _NodeId = value.ToInt(); break;
                case "Tid": _Tid = Convert.ToString(value); break;
                case "Status": _Status = value.ToInt(); break;
                case "PayStatus": _PayStatus = value.ToInt(); break;
                case "ShipStatus": _ShipStatus = value.ToInt(); break;
                case "CreateIPReceiverPhone": _CreateIPReceiverPhone = Convert.ToString(value); break;
                case "ReceiverMobile": _ReceiverMobile = Convert.ToString(value); break;
                case "ReceiverState": _ReceiverState = Convert.ToString(value); break;
                case "ReceiverCity": _ReceiverCity = Convert.ToString(value); break;
                case "ReceiverDistrict": _ReceiverDistrict = Convert.ToString(value); break;
                case "ReceiverAddress": _ReceiverAddress = Convert.ToString(value); break;
                case "BuyerName": _BuyerName = Convert.ToString(value); break;
                case "Created": _Created = value.ToInt(); break;
                case "Modified": _Modified = value.ToInt(); break;
                case "IsSend": _IsSend = value.ToInt(); break;
                case "ErrorMsg": _ErrorMsg = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion

    #region 关联映射
    #endregion

    #region 字段名
    /// <summary>取得交易字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>订单编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>租户</summary>
        public static readonly Field TenantId = FindByName("TenantId");

        /// <summary>节点号</summary>
        public static readonly Field NodeId = FindByName("NodeId");

        /// <summary>订单号</summary>
        public static readonly Field Tid = FindByName("Tid");

        /// <summary>状态</summary>
        public static readonly Field Status = FindByName("Status");

        /// <summary>是否支付</summary>
        public static readonly Field PayStatus = FindByName("PayStatus");

        /// <summary>是否发货</summary>
        public static readonly Field ShipStatus = FindByName("ShipStatus");

        /// <summary>收货人电话</summary>
        public static readonly Field CreateIPReceiverPhone = FindByName("CreateIPReceiverPhone");

        /// <summary>收货人手机号</summary>
        public static readonly Field ReceiverMobile = FindByName("ReceiverMobile");

        /// <summary>收货省</summary>
        public static readonly Field ReceiverState = FindByName("ReceiverState");

        /// <summary>收货人区</summary>
        public static readonly Field ReceiverCity = FindByName("ReceiverCity");

        /// <summary>收货区</summary>
        public static readonly Field ReceiverDistrict = FindByName("ReceiverDistrict");

        /// <summary>收货地址</summary>
        public static readonly Field ReceiverAddress = FindByName("ReceiverAddress");

        /// <summary>买家昵称</summary>
        public static readonly Field BuyerName = FindByName("BuyerName");

        /// <summary>创建时间</summary>
        public static readonly Field Created = FindByName("Created");

        /// <summary>是否发送过</summary>
        public static readonly Field Modified = FindByName("Modified");

        /// <summary>更新者</summary>
        public static readonly Field IsSend = FindByName("IsSend");

        /// <summary>错误原因</summary>
        public static readonly Field ErrorMsg = FindByName("ErrorMsg");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得交易字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>订单编号</summary>
        public const String Id = "Id";

        /// <summary>租户</summary>
        public const String TenantId = "TenantId";

        /// <summary>节点号</summary>
        public const String NodeId = "NodeId";

        /// <summary>订单号</summary>
        public const String Tid = "Tid";

        /// <summary>状态</summary>
        public const String Status = "Status";

        /// <summary>是否支付</summary>
        public const String PayStatus = "PayStatus";

        /// <summary>是否发货</summary>
        public const String ShipStatus = "ShipStatus";

        /// <summary>收货人电话</summary>
        public const String CreateIPReceiverPhone = "CreateIPReceiverPhone";

        /// <summary>收货人手机号</summary>
        public const String ReceiverMobile = "ReceiverMobile";

        /// <summary>收货省</summary>
        public const String ReceiverState = "ReceiverState";

        /// <summary>收货人区</summary>
        public const String ReceiverCity = "ReceiverCity";

        /// <summary>收货区</summary>
        public const String ReceiverDistrict = "ReceiverDistrict";

        /// <summary>收货地址</summary>
        public const String ReceiverAddress = "ReceiverAddress";

        /// <summary>买家昵称</summary>
        public const String BuyerName = "BuyerName";

        /// <summary>创建时间</summary>
        public const String Created = "Created";

        /// <summary>是否发送过</summary>
        public const String Modified = "Modified";

        /// <summary>更新者</summary>
        public const String IsSend = "IsSend";

        /// <summary>错误原因</summary>
        public const String ErrorMsg = "ErrorMsg";
    }
    #endregion
}
