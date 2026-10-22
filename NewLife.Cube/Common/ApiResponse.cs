using System.Text.Json.Serialization;
using NewLife.Web;

namespace NewLife.Cube;

/// <summary>响应模型</summary>
public interface IApiResponse
{
    /// <summary>代码。0表示成功，其它为错误代码</summary>
    Int32 Code { get; set; }

    /// <summary>消息内容。成功或错误时的提示文本</summary>
    String Message { get; set; }
}

/// <summary>字段级验证错误</summary>
public class FieldError
{
    /// <summary>字段名（与前端表单字段名对应）</summary>
    public String Field { get; set; }

    /// <summary>错误消息。例如"XX字段不可以为空"</summary>
    public String Message { get; set; }

    /// <summary>错误详情。例如异常堆栈信息</summary>
    public String? Error { get; set; } = null;
}

/// <summary>响应模型</summary>
/// <typeparam name="T"></typeparam>
public class ApiResponse<T> : IApiResponse
{
    /// <summary>代码。0表示成功，其它为错误代码</summary>
    public Int32 Code { get; set; }

    /// <summary>消息内容。成功或错误时的提示文本；为 null 时不输出</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public String Message { get; set; }

    /// <summary>响应数据。始终输出（可为 null）</summary>
    public T Data { get; set; }

    /// <summary>跟踪编号。为 null 时不输出</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public String TraceId { get; set; }

    /// <summary>字段级验证错误列表。每个元素对应一个字段的验证错误；无字段错误时不输出该字段</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<FieldError> FieldErrors { get; set; }
}

/// <summary>列表响应模型</summary>
/// <typeparam name="T"></typeparam>
public class ApiListResponse<T> : ApiResponse<IList<T>>
{
    /// <summary>分页信息。非列表响应或未分页时不输出</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PageModel Page { get; set; }

    /// <summary>统计行数据。无统计时不输出</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T Stat { get; set; }
}