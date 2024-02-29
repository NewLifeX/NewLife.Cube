using System.ComponentModel;
using NewLife.Cube.Entity;
using NewLife.Log;

namespace NewLife.Cube.Jobs;

/// <summary>Http作业参数</summary>
public class HttpJobArgument
{
    /// <summary>请求方法。Get/Post</summary>
    [DisplayName("请求方法")]
    [Description("Get/Post")]
    public String Method { get; set; }

    /// <summary>请求地址</summary>
    [DisplayName("请求地址")]
    public String Url { get; set; }

    /// <summary>请求参数</summary>
    [DisplayName("请求参数")]
    [Description("字符串提交，一般是Json")]
    public String Body { get; set; }
}

/// <summary>HTTP服务</summary>
[DisplayName("发起Http请求")]
[Description("Http请求指定Url")]
[CronJob("RunHttp", "25 0 0 * * ? *")]
public class HttpService : CubeJobBase<HttpJobArgument>
{
    private readonly ITracer _tracer;

    /// <summary>实例化HTTP服务，用于定期请求指定Url地址</summary>
    /// <param name="tracer"></param>
    public HttpService(ITracer tracer) => _tracer = tracer;

    /// <summary>执行作业</summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected override async Task<String> OnExecute(HttpJobArgument argument)
    {
        if (argument.Url.IsNullOrEmpty()) throw new ArgumentNullException(nameof(argument.Url));

        using var span = _tracer?.NewSpan("RunHttp", argument);

        var client = new HttpClient();

        if (argument.Method.EqualIgnoreCase("Get"))
        {
            var rs = await client.GetStringAsync(argument.Url);

            return !rs.IsNullOrEmpty() && rs.Length > 500 ? rs[..500] : rs;
        }
        else
        {
            var res = await client.PostAsync(argument.Url, new StringContent(argument.Body));
            var rs = await res.Content.ReadAsStringAsync();

            return !rs.IsNullOrEmpty() && rs.Length > 500 ? rs[..500] : rs;
        }
    }
}
