using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using NewLife;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CubeDemo;

public class SwaggerConfigureOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiDescriptionGroupCollectionProvider provider;

    public SwaggerConfigureOptions(IApiDescriptionGroupCollectionProvider provider) => this.provider = provider;

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiDescriptionGroups.Items)
        {
            if(description.GroupName.IsNullOrEmpty()) continue;
            options.SwaggerDoc(description.GroupName, null);
        }
    }
}