using System.ComponentModel;

namespace NewLife.Cube.Models;

/// <!-- Yann -->
/// <summary>通用业务返回码/响应码定义：根据业务情况返回</summary>
/// <remarks> 大于0的返回码均为业务码，可以由业务系统自定义；正数码必须按分段约定分配（见《WebAPI接口规范》36.4.1），
/// 1~999 为保留段（含与 ApiCode 重合的 400/401/403/404/429/500），业务从 1000 起按模块分段 </remarks>
/// <returns> CODE : 0 表示成功；-1 表示通用错误，其他为特殊信息码；  </returns>
public enum CubeCode
{
    /// <summary> 0：操作成功 </summary>
    /// <remarks> 请求/操作成功：一般无Msg信息</remarks>
    [Description("Operation succeeded")]
    Ok = 0,

    /// <summary>-1：操作失败：请求(或处理)业务失败、不符合预期</summary>
    /// <remarks></remarks>
    [Description("Operation failed")]
    Failed = -1,

    /// <summary>参数异常：请求参数不完整或不正确</summary>
    /// <remarks>请求参数不合理，
    /// 如： 更新数据时，未传递ID或ID=0；
    ///      传递number类型的值超过enum定义范围等场景</remarks>
    [Description("Invalid parameters")]
    ParamError = -2,

    /// <summary>远程调用异常：程序内部调用其他模块时发生异常错误</summary>
    /// <remarks>远程调用其他接口时产生的错误，如HTTP请求/PRC调用/Connection连接等</remarks>
    [Description("Remote call failed")]
    RemotingError = -3,

    /// <summary>前置进行登出/注销操作</summary>
    /// <remarks>强制登出，如认证失败、缓存过期、密码修改后的重新登录场景 </remarks>
    [Description("Logged off")]
    LogOff = -4,

    /// <summary>内部错误：程序报错时的返回码</summary>
    /// <remarks>程序异常</remarks>
    [Description("Internal error")]
    Exception = -5,

    /// <summary>需要图形验证码：登录/注册/发码时要求输入图片验证码</summary>
    /// <remarks>data 返回 { captchaRequired, captchaId, image, captchaUrl }，前端据此展示验证码输入</remarks>
    [Description("Captcha required")]
    CaptchaRequired = -6,

    /// <summary>需要二步验证：账号密码正确但用户已开启 MFA</summary>
    /// <remarks>data 返回 { mfaToken }，前端指引用户输入 TOTP 后调用 /Mfa/Verify 完成登录。旧版仅用 message="mfa_required:{token}" 传递，保留兼容</remarks>
    [Description("MFA required")]
    MfaRequired = -7,
}
