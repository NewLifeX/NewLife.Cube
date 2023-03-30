namespace NewLife.Cube;

/// <summary>响应模型</summary>
/// <typeparam name="T"></typeparam>
public class ResponseModel<T>
{
    /// <summary>代码</summary>
    public Int32 Code { get; set; }

    /// <summary>响应数据</summary>
    public T Data { get; set; }

    /// <summary>跟踪编号</summary>
    public String TraceId { get; set; }
}
