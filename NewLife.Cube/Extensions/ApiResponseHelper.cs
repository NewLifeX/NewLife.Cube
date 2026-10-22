using NewLife.Cube.Models;
using NewLife.Log;
using NewLife.Web;
namespace NewLife.Cube.Extensions;

/// <summary>ApiResult返回扩展</summary>
/// <remarks>扩展方法封装</remarks>
public static partial class ApiResponseHelper
{
    /// <summary>成功返回</summary>
    /// <typeparam name="T">Data部分泛型</typeparam>
    /// <param name="data">Data部分</param>
    /// <param name="successMessage">成功信息</param>
    /// <returns></returns>
    public static ApiResponse<T> ToOkApiResponse<T>(this T data, String? successMessage = null)
    {
        return new ApiResponse<T>
        {
            Code = CubeCode.Ok.ToInt(),
            Message = successMessage ?? L.T("Operation succeeded"),
            Data = data,
            TraceId = DefaultSpan.Current?.TraceId,
        };
    }

    /// <summary>基础错误返回</summary>
    /// <param name="data">Data数据</param>
    /// <param name="failMessage">错误信息</param>
    /// <param name="code">自定义Code</param>
    /// <typeparam name="T">Data类型</typeparam>
    /// <returns></returns>
    public static ApiResponse<T> ToFailApiResponse<T>(this T data, Int32 code, String failMessage)
    {
        //自定义的code不能是成功状态码
        if (code == CubeCode.Ok.ToInt()) code = CubeCode.Failed.ToInt();
        return new ApiResponse<T>
        {
            Code = code,
            Message = failMessage,
            Data = data,
            TraceId = DefaultSpan.Current?.TraceId,
        };
    }

    /// <summary>业务逻辑中包含的失败、需要定义返回码 </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="code">业务码定义</param>
    /// <param name="tipMessage">message提示词</param>
    /// <returns></returns>
    public static ApiResponse<T> ToFailApiResponse<T>(this T data, Enum code, String tipMessage = "")
    {
        if (String.IsNullOrWhiteSpace(tipMessage))// 如果tipMessage为空
        {
            tipMessage = L.T(code.GetDescription() + "");//则使用code的描述
            if (String.IsNullOrWhiteSpace(tipMessage))// 如果code的描述为空
                tipMessage = code.ToString();//则直接使用变量名
        }
        if (!Enum.IsDefined(code.GetType(), code))
            return ToFailApiResponse(data, tipMessage);//如果返回码定义错了，则用服务默认的失败返回码，消息带过去 

        return ToFailApiResponse(data, code.ToInt(), tipMessage);
    }

    /// <summary>操作失败</summary>
    public static ApiResponse<T> ToFailApiResponse<T>(this T data, String failMessage) => ToFailApiResponse(data, CubeCode.Failed, failMessage);

    /// <summary>请求参数错误</summary>
    public static ApiResponse<T> ToParaApiResponse<T>(this T data, String paraName,
         String? defaultTip = null)
    {
        var msg = defaultTip ?? L.T("Invalid request parameter");
        if (!String.IsNullOrWhiteSpace(paraName)) msg = $"{msg}:{paraName}";
        return ToFailApiResponse(data, CubeCode.ParamError, msg);
    }
    /// <summary>内部错误</summary>
    public static ApiResponse<T> ToRemotingErrorApiResponse<T>(this T data, String errorMessage) => ToFailApiResponse(data, CubeCode.RemotingError, errorMessage);
    /// <returns>强制登出</returns>
    public static ApiResponse<T> ToLogOffApiResponse<T>(this T data, String? message = null) => ToFailApiResponse(data, CubeCode.LogOff, message ?? L.T("Please sign in again"));
    /// <summary>内部错误</summary>
    public static ApiResponse<T> ToErrorApiResponse<T>(this T data, String errorMessage) => ToFailApiResponse(data, CubeCode.Exception, errorMessage);

    /// <summary>成功返回列表（带分页信息）。Data=IList&lt;T&gt;，Page=PageModel。</summary>
    public static ApiListResponse<T> ToOkListApiResponse<T>(this IList<T> data, PageModel page, String? successMessage = null)
    {
        return new ApiListResponse<T>
        {
            Code = CubeCode.Ok.ToInt(),
            Message = successMessage ?? L.T("Operation succeeded"),
            Data = data,
            Page = page,
            TraceId = DefaultSpan.Current?.TraceId,
        };
    }

    /// <summary>成功返回列表（带分页信息+统计行）。Data=IList&lt;T&gt;，Page=PageModel，Stat=统计行。</summary>
    public static ApiListResponse<T> ToOkListApiResponse<T>(this IList<T> data, PageModel page, T stat, String? successMessage = null)
    {
        return new ApiListResponse<T>
        {
            Code = CubeCode.Ok.ToInt(),
            Message = successMessage ?? L.T("Operation succeeded"),
            Data = data,
            Page = page,
            Stat = stat,
            TraceId = DefaultSpan.Current?.TraceId,
        };
    }

    /// <summary>失败返回列表</summary>
    public static ApiListResponse<T> ToFailListApiResponse<T>(this IList<T> data, Int32 code, String failMessage)
    {
        if (code == CubeCode.Ok.ToInt()) code = CubeCode.Failed.ToInt();
        return new ApiListResponse<T>
        {
            Code = code,
            Message = failMessage,
            Data = data,
            TraceId = DefaultSpan.Current?.TraceId,
        };
    }
}