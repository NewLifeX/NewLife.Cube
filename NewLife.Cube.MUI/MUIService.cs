using Microsoft.Extensions.FileProviders;
using NewLife.Cube.Extensions;

namespace NewLife.Cube.MUI;

/// <summary>MUI前端皮肤服务</summary>
public static class MUIService
{
    /// <summary>使用MUI前端皮肤。UseCube 已自动发现并启用皮肤包，通常无需调用；本方法仅用于强制指定 MUI 皮肤</summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public static WebApplication UseMUI(this WebApplication app, IWebHostEnvironment env) => app.UseCubeSkin(env, "MUI");
}

/// <summary>MUI前端皮肤。由魔方自动发现，多个皮肤包共存时可通过设置项 SpaSkin 切换</summary>
internal sealed class MUISkin : ICubeSkin
{
    /// <summary>皮肤标识</summary>
    public String Name => "MUI";

    /// <summary>显示名称</summary>
    public String DisplayName => "React + Material UI";

    /// <summary>创建静态资源文件提供者。前端编译产物编译时嵌入程序集，无需物理文件</summary>
    /// <returns></returns>
    public IFileProvider CreateFileProvider() => new CubeEmbeddedFileProvider(typeof(MUISkin).Assembly, "NewLife.Cube.MUI.wwwroot");
}
