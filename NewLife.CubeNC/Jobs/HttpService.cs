using NewLife.Cube.Entity;
using NewLife.Log;

namespace NewLife.Cube.Jobs;

/// <summary>Http作业参数</summary>
public class HttpJobArgument
{
    /// <summary>请求方法</summary>
    public String Method { get; set; }

    /// <summary>请求地址</summary>
    public String Url { get; set; }

    /// <summary>请求参数</summary>
    public String Body { get; set; }
}

/// <summary>HTTP服务</summary>
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

            return !rs.IsNullOrEmpty() && rs.Length > 50 ? rs[..50] : rs;
        }
        else
        {
            var res = await client.PostAsync(argument.Url, new StringContent(argument.Body));
            var rs = await res.Content.ReadAsStringAsync();

            return !rs.IsNullOrEmpty() && rs.Length > 50 ? rs[..50] : rs;
        }
    }
}
