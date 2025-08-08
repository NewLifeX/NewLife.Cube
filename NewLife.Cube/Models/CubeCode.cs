using System.ComponentModel;

namespace NewLife.Cube.Models;

/// <!-- Yann -->
/// <summary>通用业务返回码/响应码定义：根据业务情况返回</summary>
/// <remarks> 大于0的返回码均为业务码，可以由业务系统自定义 </remarks>
/// <returns> CODE : 0 表示成功；-1 表示通用错误，其他为特殊信息码；  </returns>
public enum CubeCode
{
    /// <summary> 0：操作成功 </summary>
    /// <remarks> 请求/操作成功：一般无Msg信息</remarks>
    [Description("操作成功")]
    Ok = 0,

    /// <summary>-1：操作失败：请求(或处理)业务失败、不符合预期</summary>
    /// <remarks></remarks>
    [Description("操作失败")]
    Failed = -1,

    /// <summary>参数异常：请求参数不完整或不正确</summary>
    /// <remarks>请求参数不合理，
    /// 如： 更新数据时，未传递ID或ID=0；
    ///      传递number类型的值超过enum定义范围等场景</remarks>
    [Description("参数异常")]
    ParamError = -2,

    /// <summary>远程调用异常：程序内部调用其他模块时发生异常错误</summary>
    /// <remarks>远程调用其他接口时产生的错误，如HTTP请求/PRC调用/Connection连接等</remarks>
    [Description("远程调用异常")]
    RemotingError = -3,

    /// <summary>前置进行登出/注销操作</summary>
    /// <remarks>强制登出，如认证失败、缓存过期、密码修改后的重新登录场景 </remarks>
    [Description("登出")]
    LogOff = -4,

    /// <summary>内部错误：程序报错时的返回码</summary>
    /// <remarks>程序异常</remarks>
    [Description("内部错误")]
    Exception = -5,
}
