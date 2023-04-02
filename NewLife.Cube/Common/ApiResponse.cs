using NewLife.Web;

namespace NewLife.Cube;

/// <summary>响应模型</summary>
public interface IApiResponse
{
    /// <summary>代码。0表示成功，其它为错误代码</summary>
    Int32 code { get; set; }

    /// <summary>消息内容。成功或错误时的提示文本</summary>
    String message { get; set; }
}

/// <summary>响应模型</summary>
/// <typeparam name="T"></typeparam>
public class ApiResponse<T> : IApiResponse
{
    /// <summary>代码。0表示成功，其它为错误代码</summary>
    public Int32 code { get; set; }

    /// <summary>消息内容。成功或错误时的提示文本</summary>
    public String message { get; set; }

    /// <summary>响应数据</summary>
    public T data { get; set; }

    /// <summary>跟踪编号</summary>
    public String traceId { get; set; }
}

/// <summary>列表响应模型</summary>
/// <typeparam name="T"></typeparam>
public class ApiListResponse<T> : ApiResponse<IList<T>>
{
    /// <summary>分页信息</summary>
    public PageModel page { get; set; }

    /// <summary>统计行数据</summary>
    public T stat { get; set; }
}