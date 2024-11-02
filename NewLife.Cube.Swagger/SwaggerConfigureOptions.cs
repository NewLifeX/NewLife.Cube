using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NewLife.Reflection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NewLife.Cube.Swagger;

/// <summary>自动为每个文档分组引入Swagger</summary>
public class SwaggerConfigureOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiDescriptionGroupCollectionProvider provider;

    /// <summary>实例化</summary>
    /// <param name="provider"></param>
    public SwaggerConfigureOptions(IApiDescriptionGroupCollectionProvider provider) => this.provider = provider;

    /// <summary>自动配置添加分组文档</summary>
    /// <param name="options"></param>
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiDescriptionGroups.Items)
        {
            if (description.GroupName.IsNullOrEmpty()) continue;

            // 遍历控制器，找到区域读取其描述
            OpenApiInfo? info = null;
            foreach (var apiDesc in description.Items)
            {
                if (apiDesc.ActionDescriptor is not ControllerActionDescriptor controller) continue;

                var area = controller.ControllerTypeInfo.GetCustomAttribute<AreaAttribute>();
                if (area != null)
                {
                    var type = area.GetType();
                    var asm = AssemblyX.Create(type.Assembly);
                    info = new OpenApiInfo
                    {
                        Title = type.GetDisplayName(),
                        Description = type.GetDescription()?.Replace("\n", "<br/>"),
                        Version = asm.FileVersion,
                    };
                    break;
                }
            }

            options.SwaggerDoc(description.GroupName, info);
        }
    }
}