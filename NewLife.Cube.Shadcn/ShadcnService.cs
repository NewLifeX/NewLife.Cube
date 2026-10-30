using Microsoft.Extensions.FileProviders;
using NewLife.Cube.Extensions;

namespace NewLife.Cube.Shadcn;

/// <summary>Shadcn前端皮肤服务</summary>
public static class ShadcnService
{
    /// <summary>使用Shadcn前端皮肤。UseCube 已自动发现并启用皮肤包，通常无需调用；本方法仅用于强制指定 Shadcn 皮肤</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public static WebApplication UseShadcn(this WebApplication app, IWebHostEnvironment env) => app.UseCubeSkin(env, "Shadcn");
}

/// <summary>Shadcn前端皮肤。由魔方自动发现，多个皮肤包共存时可通过设置项 SpaSkin 切换</summary>
internal sealed class ShadcnSkin : ICubeSkin
{
    /// <summary>皮肤标识</summary>
    public String Name => "Shadcn";

    /// <summary>显示名称</summary>
    public String DisplayName => "React + Shadcn UI";

    /// <summary>创建静态资源文件提供者。前端编译产物编译时嵌入程序集，无需物理文件</summary>
    /// <returns></returns>
    public IFileProvider CreateFileProvider() => new CubeEmbeddedFileProvider(typeof(ShadcnSkin).Assembly, "NewLife.Cube.Shadcn.wwwroot");
}
