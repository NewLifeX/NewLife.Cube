using Microsoft.Extensions.FileProviders;
using NewLife.Cube;
using NewLife.Cube.Extensions;

namespace NewLife.Cube.Soybean;

/// <summary>Soybean前端皮肤。由魔方自动发现，通过设置项 SpaSkin 切换</summary>
public sealed class SoybeanSkin : ICubeSkin
{
    /// <summary>皮肤标识</summary>
    public String Name => "Soybean";

    /// <summary>显示名称</summary>
    public String DisplayName => "Vue3 + Soybean Admin";

    /// <summary>创建静态资源文件提供者</summary>
    public IFileProvider CreateFileProvider() => new CubeEmbeddedFileProvider(typeof(SoybeanSkin).Assembly, "NewLife.Cube.Soybean.wwwroot");
}
