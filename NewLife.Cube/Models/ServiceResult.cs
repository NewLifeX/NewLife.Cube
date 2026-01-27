namespace NewLife.Cube.Models;

/// <summary>服务操作结果</summary>
public class ServiceResult
{
    /// <summary>是否成功</summary>
    public Boolean IsSuccess { get; set; }
    /// <summary>消息</summary>
    public String Message { get; set; } = String.Empty;
}

/// <summary>服务操作结果</summary>
public class ServiceResult<T> : ServiceResult
{
    /// <summary>数据</summary>
    public T Data { get; set; } = default(T);
} 