using System.ComponentModel;
using NewLife.Configuration;
using XCode.Configuration;

namespace IoTServer;

/// <summary>配置</summary>
[Config("IoTServer")]
public class IoTSetting : Config<IoTSetting>
{
    #region 静态
    static IoTSetting() => Provider = new DbConfigProvider { UserId = 0, Category = "IoTServer" };
    #endregion

    #region 属性
    /// <summary>RPC服务端口。默认1882</summary>
    [Description("RPC服务端口。默认1882")]
    [Category("默认")]
    public Int32 RpcPort { get; set; } = 1882;

    /// <summary>MQTT服务端口。默认1883</summary>
    [Description("MQTT服务端口。默认1883")]
    [Category("默认")]
    public Int32 MqttPort { get; set; } = 1883;

    /// <summary>MQTT证书地址。设置了才启用安全连接，默认为空</summary>
    [Description("MQTT证书地址。设置了才启用安全连接，默认为空")]
    [Category("默认")]
    public String MqttCertPath { get; set; }

    /// <summary>MMQTT证书密码</summary>
    [Description("MQTT证书密码")]
    [Category("默认")]
    public String MqttCertPassword { get; set; }

    /// <summary>LoRaWAN服务端口。默认1680</summary>
    [Description("RPC服务端口。默认1680")]
    [Category("默认")]
    public Int32 LoRaPort { get; set; } = 1680;

    /// <summary>IoT服务端。物联网服务端地址，用于IoTWeb调用IoTServer的接口</summary>
    [Description("IoT服务端。物联网服务端地址，用于IoTWeb调用IoTServer的接口")]
    [Category("默认")]
    public String IoTServer { get; set; } = "http://localhost:1881";
    #endregion

    #region 设备管理
    /// <summary>令牌密钥。用于生成JWT令牌的算法和密钥，如HS256:ABCD1234</summary>
    [Description("令牌密钥。用于生成JWT令牌的算法和密钥，如HS256:ABCD1234")]
    [Category("设备管理")]
    public String TokenSecret { get; set; }

    /// <summary>令牌有效期。默认2*3600秒</summary>
    [Description("令牌有效期。默认2*3600秒")]
    [Category("设备管理")]
    public Int32 TokenExpire { get; set; } = 2 * 3600;

    /// <summary>会话超时。设备客户端超过该时间没有心跳或数据传输时踢下线，默认300秒</summary>
    [Description("会话超时。设备客户端超过该时间没有心跳或数据传输时踢下线，默认300秒")]
    [Category("设备管理")]
    public Int32 SessionTimeout { get; set; } = 300;

    /// <summary>自动注册。允许客户端自动注册，默认true</summary>
    [Description("自动注册。允许客户端自动注册，默认true")]
    [Category("设备管理")]
    public Boolean AutoRegister { get; set; } = true;

    /// <summary>启用子设备在线。子设备上线时写入在线表，默认false</summary>
    [Description("启用子设备在线。子设备上线时写入在线表，默认false")]
    [Category("设备管理")]
    public Boolean UseChildDeviceOnline { get; set; }
    #endregion

    #region 数据存储
    /// <summary>最大上传延迟时间。采集时间距离接收时间的最大差值，超过该值时取接收时间作为采集时间，默认86400秒，即24小时</summary>
    [Description("最大上传延迟时间。采集时间距离接收时间的最大差值，超过该值时取接收时间作为采集时间，默认86400秒，即24小时")]
    [Category("数据存储")]
    public Int32 MaxUploadDelay { get; set; } = 86400;

    /// <summary>存储历史数据。保存设备历史数据到关系型数据库，按天分表，数据量很大，默认true</summary>
    [Description("存储历史数据。保存设备历史数据到关系型数据库，按天分表，数据量很大，默认true")]
    [Category("数据存储")]
    public Boolean StoreData { get; set; } = true;

    /// <summary>历史数据保留时间。保留时长决定占用的磁盘空间，不影响性能，默认30天</summary>
    [Description("历史数据保留时间。保留时长决定占用的磁盘空间，不影响性能，默认30天")]
    [Category("数据存储")]
    public Int32 DataRetention { get; set; } = 30;

    /// <summary>存储属性数据。保存设备属性数据（即点位最新数据）到数据库，默认true</summary>
    [Description("存储属性数据。保存设备属性数据（即点位最新数据）到数据库，默认true")]
    [Category("数据存储")]
    public Boolean StoreProperty { get; set; } = true;

    /// <summary>存储状态数据。保存设备属性数据（即点位最新数据）到Redis，默认true</summary>
    [Description("存储状态数据。保存设备属性数据（即点位最新数据）到Redis，默认true")]
    [Category("数据存储")]
    public Boolean StoreStatus { get; set; } = true;

    /// <summary>存储分段数据。为每个属性点位计算分段并保存到数据库，默认true</summary>
    [Description("存储分段数据。为每个属性点位计算分段并保存到数据库，默认true")]
    [Category("数据存储")]
    public Boolean StoreSegment { get; set; } = true;

    /// <summary>启用数据队列。推送历史数据到数据队列，后端消费写入时序库、推送三方接口或者做实时计算，默认true</summary>
    [Description("启用数据队列。推送历史数据到Redis消息队列，后端消费写入时序库、推送三方接口或者做实时计算，默认true")]
    [Category("数据存储")]
    public Boolean DataQueue { get; set; } = true;

    /// <summary>启用事件队列。推送事件数据到Redis消息队列，后端消费推送三方接口或者做设备联动，默认true</summary>
    [Description("启用事件队列。推送事件数据到Redis消息队列，后端消费推送三方接口或者做设备联动，默认true")]
    [Category("数据存储")]
    public Boolean EventQueue { get; set; } = true;
    #endregion


}