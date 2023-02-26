namespace NewLife.Web;

/// <summary>Http上下文</summary>
public static class HttpContext
{
    private static IHttpContextAccessor _accessor;

    /// <summary>当前Http上下文</summary>
    public static Microsoft.AspNetCore.Http.HttpContext Current => _accessor?.HttpContext;

    /// <summary>设置Http上下文访问器</summary>
    /// <param name="accessor"></param>
    public static void Configure(IHttpContextAccessor accessor) => _accessor = accessor;
}

/// <summary>Http上下文扩展</summary>
public static class StaticHttpContextExtensions
{
    /// <summary>配置静态Http上下文访问器</summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseStaticHttpContext(this IApplicationBuilder app)
    {
        var accessor = app.ApplicationServices.GetService<IHttpContextAccessor>();
        HttpContext.Configure(accessor);

        return app;
    }
}