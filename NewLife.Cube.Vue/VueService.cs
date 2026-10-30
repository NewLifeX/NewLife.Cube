using System.Reflection;
using Microsoft.Extensions.FileProviders;
using NewLife.Cube.Extensions;

namespace NewLife.Cube.Vue;

/// <summary>Vue前端皮肤服务</summary>
public static class VueService
{
    /// <summary>使用Vue前端皮肤。注册嵌入式静态资源与SPA回退路由，提供Vue3+Element Plus管理后台前端</summary>
    /// <remarks>多个皮肤包共存时请改用 <see cref="CubeSkinService.UseCubeSkin(WebApplication, IWebHostEnvironment, String)"/>，按设置项切换皮肤</remarks>
    /// <param name="app"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public static WebApplication UseVue(this WebApplication app, IWebHostEnvironment env) => app.UseCubeSkin(env, "Vue");
}

/// <summary>Vue前端皮肤。由魔方自动发现，多个皮肤包共存时可通过设置项 SpaSkin 切换</summary>
internal sealed class VueSkin : ICubeSkin
{
    /// <summary>皮肤标识</summary>
    public String Name => "Vue";

    /// <summary>显示名称</summary>
    public String DisplayName => "Vue3 + Element Plus";

    /// <summary>创建静态资源文件提供者。前端编译产物编译时嵌入程序集，无需物理文件</summary>
    /// <returns></returns>
    public IFileProvider CreateFileProvider()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return new CubeEmbeddedFileProvider(assembly, "NewLife.Cube.Vue.wwwroot");
    }
}
