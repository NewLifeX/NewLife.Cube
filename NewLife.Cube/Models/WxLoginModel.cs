namespace NewLife.Cube.Models;

/// <summary>微信登录请求参数模型</summary>
public class WxLoginModel
{
    /// <summary>登录凭证。小程序为wx.login()返回的code，APP为微信SDK返回的授权码</summary>
    public String Code { get; set; }

    /// <summary>对应小程序/APP的AppId</summary>
    public String AppId { get; set; }
}
