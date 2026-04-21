namespace NewLife.Cube.Models;

/// <summary>服务操作结果</summary>
public class ServiceResult
{
    /// <summary>是否成功</summary>
    public Boolean IsSuccess { get; set; }
    /// <summary>消息</summary>
    public String Message { get; set; } = String.Empty;

    /// <summary>MFA 挂起令牌。非空时表示登录层通过但需要完成二步验证，前端应跳转输入 MFA 验证码页并携此令牌调用 POST /Mfa/Verify</summary>
    public String MfaToken { get; set; }
}

/// <summary>服务操作结果</summary>
public class ServiceResult<T> : ServiceResult
{
    /// <summary>数据</summary>
    public T Data { get; set; } = default(T);
} 