using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NewLife.Cube.Entity;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NewLife.Cube.Swagger;

/// <summary>Swagger服务</summary>
public static class SwaggerService
{
    /// <summary>添加魔方Swagger服务</summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCubeSwagger(this IServiceCollection services)
    {
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerConfigureOptions>();
        services.AddSwaggerGen(options =>
        {
            // 解决 NewLife.Setting 与 XCode.Setting 冲突的问题
            options.CustomSchemaIds(type => type.FullName);

            var xml = "NewLife.Cube.xml".GetFullPath();
            if (File.Exists(xml)) options.IncludeXmlComments(xml, true);

            options.SwaggerDoc("v1", new OpenApiInfo { Title = "第三代魔方", Description = "第三代魔方WebApi接口，用于前后端分离。" });
            //options.SwaggerDoc("Basic", new OpenApiInfo { Version = "basic", Title = "基础模块" });
            //options.SwaggerDoc("Admin", new OpenApiInfo { Version = "admin", Title = "系统管理" });
            //options.SwaggerDoc("Cube", new OpenApiInfo { Version = "cube", Title = "魔方管理" });

            options.DocInclusionPredicate((docName, apiDesc) =>
            {
                if (apiDesc.ActionDescriptor is not ControllerActionDescriptor controller) return false;

                var groups = controller.ControllerTypeInfo.GetCustomAttributes(true).OfType<IApiDescriptionGroupNameProvider>().Select(e => e.GroupName).ToList();

                if (docName == "v1" && (groups == null || groups.Count == 0)) return true;

                return groups != null && groups.Any(e => e == docName);
            });

            var oauthConfigs = OAuthConfig.GetValids(GrantTypes.AuthorizationCode);
            if (oauthConfigs.Count > 0)
            {
                var cfg = oauthConfigs[0];
                var flow = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri(cfg.Server),
                    TokenUrl = new Uri(!cfg.AccessServer.IsNullOrEmpty() ? cfg.AccessServer : cfg.Server),
                    //Scopes = new Dictionary<String, String>
                    //{
                    //    { "api1", "Access to API #1" }
                    //}
                };
                options.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows { AuthorizationCode = flow }
                });

                //options.OperationFilter<AuthorizeCheckOperationFilter>();
            }
            else
            {
                // 定义JwtBearer认证方式
                options.AddSecurityDefinition("JwtBearer", new OpenApiSecurityScheme()
                {
                    Description = "输入登录成功后取得的令牌",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer"
                });
                // 声明一个Scheme，注意下面的Id要和上面AddSecurityDefinition中的参数name一致
                var scheme = new OpenApiSecurityScheme()
                {
                    Reference = new OpenApiReference() { Type = ReferenceType.SecurityScheme, Id = "JwtBearer" }
                };
                // 注册全局认证（所有的接口都可以使用认证）
                options.AddSecurityRequirement(new OpenApiSecurityRequirement() { [scheme] = [] });
            }
        });

        return services;
    }

    /// <summary>使用魔方Swagger服务</summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseCubeSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        //app.UseSwaggerUI();
        app.UseSwaggerUI(options =>
        {
            //options.SwaggerEndpoint("/swagger/Basic/swagger.json", "Basic");
            //options.SwaggerEndpoint("/swagger/Admin/swagger.json", "Admin");
            //options.SwaggerEndpoint("/swagger/Cube/swagger.json", "Cube");
            //options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            // 设置路由前缀为空，直接访问站点根目录即可看到SwaggerUI
            options.RoutePrefix = String.Empty;
            var groups = app.ApplicationServices.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items;
            foreach (var description in groups)
            {
                var group = description.GroupName;
                if (group.IsNullOrEmpty()) group = "v1";
                options.SwaggerEndpoint($"/swagger/{group}/swagger.json", group);
            }

            // 设置OAuth2认证
            var oauthConfigs = OAuthConfig.GetValids(GrantTypes.AuthorizationCode);
            if (oauthConfigs.Count > 0)
            {
                var cfg = oauthConfigs[0];
                //options.OAuthConfigObject = new()
                //{
                //    AppName = cfg.Name,
                //    ClientId = cfg.AppId,
                //    ClientSecret = cfg.Secret,
                //};
                options.OAuthClientId(cfg.AppId);
                options.OAuthClientSecret(cfg.Secret);
                if (!cfg.Scope.IsNullOrEmpty()) options.OAuthScopes(cfg.Scope.Split(","));
            }
        });

        return app;
    }
}
