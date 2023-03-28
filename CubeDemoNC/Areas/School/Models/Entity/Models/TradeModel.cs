using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Data;

namespace NewLife.School.Entity;

/// <summary>交易</summary>
public partial class TradeModel : IModel
{
    #region 属性
    /// <summary>订单编号</summary>
    public Int64 Id { get; set; }

    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }

    /// <summary>节点号</summary>
    public Int32 NodeId { get; set; }

    /// <summary>订单号</summary>
    public String Tid { get; set; }

    /// <summary>状态</summary>
    public Int32 Status { get; set; }

    /// <summary>是否支付</summary>
    public Int32 PayStatus { get; set; }

    /// <summary>是否发货</summary>
    public Int32 ShipStatus { get; set; }

    /// <summary>收货人电话</summary>
    public String CreateIPReceiverPhone { get; set; }

    /// <summary>收货人手机号</summary>
    public String ReceiverMobile { get; set; }

    /// <summary>收货省</summary>
    public String ReceiverState { get; set; }

    /// <summary>收货人区</summary>
    public String ReceiverCity { get; set; }

    /// <summary>收货区</summary>
    public String ReceiverDistrict { get; set; }

    /// <summary>收货地址</summary>
    public String ReceiverAddress { get; set; }

    /// <summary>买家昵称</summary>
    public String BuyerName { get; set; }

    /// <summary>创建时间</summary>
    public Int32 Created { get; set; }

    /// <summary>是否发送过</summary>
    public Int32 Modified { get; set; }

    /// <summary>更新者</summary>
    public Int32 IsSend { get; set; }

    /// <summary>错误原因</summary>
    public String ErrorMsg { get; set; }
    #endregion

    #region 获取/设置 字段值
    /// <summary>获取/设置 字段值</summary>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public virtual Object this[String name]
    {
        get
        {
            return name switch
            {
                "Id" => Id,
                "TenantId" => TenantId,
                "NodeId" => NodeId,
                "Tid" => Tid,
                "Status" => Status,
                "PayStatus" => PayStatus,
                "ShipStatus" => ShipStatus,
                "CreateIPReceiverPhone" => CreateIPReceiverPhone,
                "ReceiverMobile" => ReceiverMobile,
                "ReceiverState" => ReceiverState,
                "ReceiverCity" => ReceiverCity,
                "ReceiverDistrict" => ReceiverDistrict,
                "ReceiverAddress" => ReceiverAddress,
                "BuyerName" => BuyerName,
                "Created" => Created,
                "Modified" => Modified,
                "IsSend" => IsSend,
                "ErrorMsg" => ErrorMsg,
                _ => null
            };
        }
        set
        {
            switch (name)
            {
                case "Id": Id = value.ToLong(); break;
                case "TenantId": TenantId = value.ToInt(); break;
                case "NodeId": NodeId = value.ToInt(); break;
                case "Tid": Tid = Convert.ToString(value); break;
                case "Status": Status = value.ToInt(); break;
                case "PayStatus": PayStatus = value.ToInt(); break;
                case "ShipStatus": ShipStatus = value.ToInt(); break;
                case "CreateIPReceiverPhone": CreateIPReceiverPhone = Convert.ToString(value); break;
                case "ReceiverMobile": ReceiverMobile = Convert.ToString(value); break;
                case "ReceiverState": ReceiverState = Convert.ToString(value); break;
                case "ReceiverCity": ReceiverCity = Convert.ToString(value); break;
                case "ReceiverDistrict": ReceiverDistrict = Convert.ToString(value); break;
                case "ReceiverAddress": ReceiverAddress = Convert.ToString(value); break;
                case "BuyerName": BuyerName = Convert.ToString(value); break;
                case "Created": Created = value.ToInt(); break;
                case "Modified": Modified = value.ToInt(); break;
                case "IsSend": IsSend = value.ToInt(); break;
                case "ErrorMsg": ErrorMsg = Convert.ToString(value); break;
            }
        }
    }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(ITrade model)
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
}
