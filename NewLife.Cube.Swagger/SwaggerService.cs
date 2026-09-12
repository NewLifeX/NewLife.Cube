using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using NewLife.Cube.Entity;
using NewLife.Reflection;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using XCode.Membership;

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

            var asm = AssemblyX.Entry;
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "第三代魔方", Description = "第三代魔方WebApi接口，用于前后端分离。", Version = asm.FileVersion });
            //options.SwaggerDoc("Basic", new OpenApiInfo { Version = "basic", Title = "基础模块" });
            //options.SwaggerDoc("Admin", new OpenApiInfo { Version = "admin", Title = "系统管理" });
            //options.SwaggerDoc("Cube", new OpenApiInfo { Version = "cube", Title = "魔方管理" });

            options.DocInclusionPredicate((docName, apiDesc) =>
            {
                if (apiDesc.ActionDescriptor is not ControllerActionDescriptor controller) return false;

                // 跳过未显式绑定 HTTP 方法的 action（如 RPC 风格基类辅助方法），避免 Swagger 文档生成失败
                if (String.IsNullOrEmpty(apiDesc.HttpMethod)) return false;

                // v1 文档包含全部接口（含各区域分组），便于 Scalar 单页展示；其余分组文档按区域名过滤
                if (docName == "v1") return true;

                var groups = controller.ControllerTypeInfo.GetCustomAttributes(true).OfType<IApiDescriptionGroupNameProvider>().Select(e => e.GroupName).ToList();

                if (docName == "v1" && (groups == null || groups.Count == 0)) return true;

                return groups != null && groups.Any(e => e == docName);
            });

            var oauthConfigs = OAuthConfig.GetValids(TenantContext.CurrentId, GrantTypes.AuthorizationCode);
            // 优先使用配置了服务器地址的，否则回退第一条；全部无Server时回退到JwtBearer
            var cfg = oauthConfigs.FirstOrDefault(x => !String.IsNullOrWhiteSpace(x.Server)) ?? oauthConfigs.FirstOrDefault();
            if (cfg != null && !cfg.Server.IsNullOrEmpty())
            {
                var flow = new OpenApiOAuthFlow //Yann 这个授权地址不一定对吧？
                {
                    AuthorizationUrl = new Uri(cfg.Server + "/authorize"),
                    TokenUrl = new Uri((!cfg.AccessServer.IsNullOrEmpty() ? cfg.AccessServer : cfg.Server) + "/access_token"),
                    //Scopes = new Dictionary<String, String>
                    //{
                    //    { "api1", "Access to API #1" }
                    //}
                };
                options.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    In = ParameterLocation.Query,
                    Flows = new OpenApiOAuthFlows { AuthorizationCode = flow }
                });

                // 声明一个Scheme，注意下面的Id要和上面AddSecurityDefinition中的参数name一致
                var schemeRef = new OpenApiSecuritySchemeReference("OAuth2");
                // 注册全局认证（所有的接口都可以使用认证）
                options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement() { [schemeRef] = [] });
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
                    Scheme = "Bearer"
                });
                // 声明一个Scheme，注意下面的Id要和上面AddSecurityDefinition中的参数name一致
                var schemeRef = new OpenApiSecuritySchemeReference("JwtBearer");
                // 注册全局认证（所有的接口都可以使用认证）
                options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement() { [schemeRef] = [] });
            }
        });

        return services;
    }

    /// <summary>使用魔方Swagger服务</summary>
    /// <param name="app"></param>
    /// <param name="routePrefix">SwaggerUI路由前缀。默认空字符串（根路径）。使用Vue/React前端时建议设为"swagger"</param>
    /// <returns></returns>
    public static IApplicationBuilder UseCubeSwagger(this IApplicationBuilder app, String? routePrefix = null)
    {
        app.UseSwagger();
        //app.UseSwaggerUI();
        app.UseSwaggerUI(options =>
        {
            var asm = AssemblyX.Entry;
            options.DocumentTitle = !asm.Title.IsNullOrEmpty() ? asm.Title : "魔方Web开发平台";

            //options.SwaggerEndpoint("/swagger/Basic/swagger.json", "Basic");
            //options.SwaggerEndpoint("/swagger/Admin/swagger.json", "Admin");
            //options.SwaggerEndpoint("/swagger/Cube/swagger.json", "Cube");
            //options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            // 设置路由前缀，默认空字符串直接访问站点根目录即可看到SwaggerUI
            options.RoutePrefix = routePrefix ?? String.Empty;
            var groups = app.ApplicationServices.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items;
            foreach (var description in groups)
            {
                var group = description.GroupName;
                if (group.IsNullOrEmpty()) group = "v1";
                options.SwaggerEndpoint($"/swagger/{group}/swagger.json", group);
            }

            // 设置OAuth2认证
            var oauthConfigs = OAuthConfig.GetValids(TenantContext.CurrentId, GrantTypes.AuthorizationCode);
            // 与 AddCubeSwagger 保持一致的选取逻辑：优先有Server的，否则第一条
            var cfg = oauthConfigs.FirstOrDefault(x => !String.IsNullOrWhiteSpace(x.Server)) ?? oauthConfigs.FirstOrDefault();
            if (cfg != null)
            {
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

    /// <summary>使用魔方Scalar接口文档</summary>
    /// <param name="app"></param>
    /// <param name="title">Scalar界面标题</param>
    /// <returns></returns>
    public static WebApplication UseCubeScalar(this WebApplication app, String? title = null)
    {
        app.UseWhen(ctx => IsProtectedDocumentationPath(ctx.Request.Path), branch =>
        {
            branch.Use(async (ctx, next) =>
            {
            var userName = app.Configuration["Swagger:UserName"] ?? "newlife";
            var password = app.Configuration["Swagger:Password"] ?? "newlife@2026";
            if (!VerifyBasicAuth(ctx, userName, password))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.Headers.WWWAuthenticate = "Basic realm=\"Scalar\"";
                    return;
                }
                await next();
            });

            branch.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path.Equals("/scalar/config.js", StringComparison.OrdinalIgnoreCase))
                {
                    await using var stream = typeof(SwaggerService).Assembly.GetManifestResourceStream("NewLife.Cube.Swagger.Resources.scalar-config.js");
                    if (stream != null)
                    {
                        ctx.Response.ContentType = "application/javascript; charset=utf-8";
                        await stream.CopyToAsync(ctx.Response.Body);
                        return;
                    }
                }
                await next();
            });

            branch.UseRouting();
            branch.UseEndpoints(endpoints => endpoints.MapScalarApiReference(options =>
            {
                options.WithTitle(title ?? app.Environment.ApplicationName ?? "魔方接口文档");
                options.AddPreferredSecuritySchemes("JwtBearer");
                options.AddHttpAuthentication("JwtBearer", auth => auth.Token = app.Configuration["Scalar:JwtToken"] ?? String.Empty);
                options.WithJavaScriptConfiguration("/scalar/config.js");

                var names = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                var groups = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items;
                foreach (var group in groups)
                {
                    var name = String.IsNullOrEmpty(group.GroupName) ? "v1" : group.GroupName;
                    if (names.Add(name)) options.AddDocument(name, title: name, routePattern: $"/swagger/{name}/swagger.json", isDefault: name == "vTest1");
                }
                if (names.Count == 0) options.AddDocument("v1", title: "v1", routePattern: "/swagger/v1/swagger.json");
            }));
        });

        return app;
    }

    private static Boolean IsProtectedDocumentationPath(PathString path)
    {
        return path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase) ||
            (path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) && path.Value!.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
    }

    private static Boolean VerifyBasicAuth(HttpContext context, String userName, String password)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (String.IsNullOrEmpty(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return false;

        try
        {
            var bytes = Convert.FromBase64String(header.Substring(6).Trim());
            var pair = System.Text.Encoding.UTF8.GetString(bytes);
            var index = pair.IndexOf(':');
            return index > 0 && pair.Substring(0, index) == userName && pair.Substring(index + 1) == password;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
