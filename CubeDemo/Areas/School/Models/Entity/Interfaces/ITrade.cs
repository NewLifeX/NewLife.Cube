using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.School.Entity;

/// <summary>交易</summary>
public partial interface ITrade
{
    #region 属性
    /// <summary>订单编号</summary>
    Int64 Id { get; set; }

    /// <summary>租户</summary>
    Int32 TenantId { get; set; }

    /// <summary>节点号</summary>
    Int32 NodeId { get; set; }

    /// <summary>订单号</summary>
    String Tid { get; set; }

    /// <summary>状态</summary>
    Int32 Status { get; set; }

    /// <summary>是否支付</summary>
    Int32 PayStatus { get; set; }

    /// <summary>是否发货</summary>
    Int32 ShipStatus { get; set; }

    /// <summary>收货人电话</summary>
    String CreateIPReceiverPhone { get; set; }

    /// <summary>收货人手机号</summary>
    String ReceiverMobile { get; set; }

    /// <summary>收货省</summary>
    String ReceiverState { get; set; }

    /// <summary>收货人区</summary>
    String ReceiverCity { get; set; }

    /// <summary>收货区</summary>
    String ReceiverDistrict { get; set; }

    /// <summary>收货地址</summary>
    String ReceiverAddress { get; set; }

    /// <summary>买家昵称</summary>
    String BuyerName { get; set; }

    /// <summary>创建时间</summary>
    Int32 Created { get; set; }

    /// <summary>是否发送过</summary>
    Int32 Modified { get; set; }

    /// <summary>更新者</summary>
    Int32 IsSend { get; set; }

    /// <summary>错误原因</summary>
    String ErrorMsg { get; set; }
    #endregion
}
