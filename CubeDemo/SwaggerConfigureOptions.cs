using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using NewLife;
using Swashbuckle.AspNetCore.SwaggerGen;
#if NET10_0_OR_GREATER
using Microsoft.OpenApi;
#else
using Microsoft.OpenApi.Models;
#endif

namespace CubeDemo;

/// <summary>自动为每个文档分组引入Swagger</summary>
public class SwaggerConfigureOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiDescriptionGroupCollectionProvider provider;

    public SwaggerConfigureOptions(IApiDescriptionGroupCollectionProvider provider) => this.provider = provider;

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
                    info = new OpenApiInfo
                    {
                        Title = area.GetType().GetDisplayName(),
                        Description = area.GetType().GetDescription()?.Replace("\n", "<br/>")
                    };
                    break;
                }
            }

            options.SwaggerDoc(description.GroupName, info);
        }
    }
}