using NewLife.Cube.Models;
using NewLife.Log;
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
    public static ApiResponse<T> ToOkApiResponse<T>(this T data, String successMessage = "操作成功")
    {
        return new ApiResponse<T>
        {
            Code = CubeCode.Success.ToInt(),
            Message = successMessage,
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
        if (code == CubeCode.Success.ToInt()) code = CubeCode.Failed.ToInt();
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
            tipMessage = code.GetDescription() ?? String.Empty;//则使用code的描述
            if (String.IsNullOrWhiteSpace(tipMessage))// 如果code的描述为空
                tipMessage = code.ToString();//则直接使用变量名
        }
        if (!Enum.IsDefined(code.GetType(), code))
            return ToFailApiResponse(data, tipMessage);//如果返回码定义错了，则用服务默认的失败返回码，消息带过去 

        return ToFailApiResponse(data, code.ToInt(), tipMessage);
    }

    /// <summary>操作失败</summary>
    public static ApiResponse<T> ToFailApiResponse<T>(this T data, String failMessage) => ToFailApiResponse(data, CubeCode.Failed, failMessage);
    /// <summary>内部错误</summary>
    public static ApiResponse<T> ToErrorApiResponse<T>(this T data, String errorMessage) => ToFailApiResponse(data, CubeCode.Exception, errorMessage);
    /// <summary>请求参数错误</summary>
    public static ApiResponse<T> ToParaApiResponse<T>(this T data, String paraName,
         String defaultTip = @"请求参数错误")
    {
        var msg = defaultTip;
        if (!String.IsNullOrWhiteSpace(paraName)) msg = $"{defaultTip}:{paraName}";
        return ToFailApiResponse(data, CubeCode.ParamError, msg);
    }
    /// <returns>强制登出</returns>
    public static ApiResponse<T> ToLogOffApiResponse<T>(this T data, String message = "请重新登录") => ToFailApiResponse(data, CubeCode.LogOff, message);
}