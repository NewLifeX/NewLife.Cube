
# asp.net core移植

- 移植完成，正在完善

[TOC]

## 替换方案

|Asp. Net Mvc | ASP. NET Core|说明|
|:--|:--|:--:|
|HttpRuntime.AppDomainAppVirtualPath||据说已被干掉，用不到|
|HttpRuntime.AppDomainAppPath|(IHostingEnvironment)Env.ContentRootPath||
|HttpPostedFileBase|IFormFile||
|Request[key]|Request.Form[key] & Request.Query[key]|需要判断这两个，返回的类型StringValues，而不会是null|
|Request.IsAjaxRequest()|Request.Headers["x-requested-with"]=="XMLHttpRequest"||
|Request.QueryString[key]|Request.Query[key]||
|Request.RawUrl|Request.GetEncodedUrl()||
|Request.RouteData.GetRequiredString()|HttpContext.GetRouteValue()||
|Request.ServerVariables|Request.Headers||
|Request.Url.PathAndQuery|Request.GetEncodedPathAndQuery()||
|Request.UrlReferrer|Request.Headers["Referer"].FirstOrDefault()|
|Response.Output|new StreamWriter(HttpContext.Response.Body)||
|System.Runtime.Caching|Microsoft.Extensions.Caching.Memory||
||||
||||
||||

## 注意

- request.Form有时候是异常，不可读取

## 路由

- 其中url以及以前defaults参数的默认值可使用template代替，直接指定默认值
- id后一定要加问号，效果等同于`Asp. Net Mvc`的`id = UrlParameter.Optional`

```csharp
 app.UseMvc(routes =>
          {
              routes.MapRoute(
                  name: "default",
                  template: "{controller=Home}/{action=Index}/{id?}");
          });
```

## 区域

- [官方文档](https://docs.microsoft.com/zh-cn/aspnet/core/mvc/controllers/areas)
- `Asp. Net Mvc`中的区域会自动注册本区域文件夹里面的控制器作为区域的控制器， `Asp. Net Core Mvc`需要在控制器使用特性`[Area("Admin")]`指定区域，如果不指定区域就是和正常控制器一样，即使它位于区域文件夹
- `Asp. Net Core Mvc`一定要进行区域路由注册，否则无法匹配带有`Area`特性的控制器
- 在没有任何路由注册默认控制器为`Index`的情况下，如果有控制器名为`IndexController`，在`Asp. Net Mvc`中访问`/Admin/`，会匹配此控制器，但在`Asp. Net Core Mvc`中需要指定路由默认控制器有`Index`才能匹配
- 总的来说，`Asp. Net Core Mvc`什么都要指定都要设置，不能偷懒
- 路由注册示例

```csharp
 app.UseMvc(routes =>
          {
              //区域路由注册
              routes.MapRoute(
                  name: "areas",
                  template: "{area:exists}/{controller=Home}/{action=Index}{id?}"
              );
              routes.MapRoute(
                  name: "default",
                  template: "{controller=Home}/{action=Index}/{id?}");
          });
```

## 视图命名空间导入

- 由`web.config`换成`_ViewImports.cshtml`，使用`@using`

## http模块到中间件

- [中间件介绍](https://docs.microsoft.com/zh-cn/aspnet/core/fundamentals/middleware/index?view=aspnetcore-2.1&tabs=aspnetcore2x#middleware-writing-middleware)
- [http模块迁移到中间件](https://docs.microsoft.com/zh-cn/aspnet/core/migration/http-modules?view=aspnetcore-2.1#migrating-module-insertion-into-the-request-pipeline)

## 添加http上下文

- ConfigureServices

```csharp
//添加Http上下文访问器
StaticHttpContextExtensions.AddHttpContextAccessor(services);
```

- Configure

```csharp
//配置静态Http上下文访问器
app.UseStaticHttpContext();
```

## Razor视图

- [参考博客](https://www.cnblogs.com/tcjiaan/p/8412827.html)
- 分部页替换：`<partial name="_Login_Login"/>`或`@Html.PartialAsync("_Login_Login").Result`

### 导入命名空间

- `Views`文件夹下的`_ViewImports.cshtml`

### RazorOptions

```csharp
  services
      .AddMvc()
      .AddRazorOptions(opt =>
      {
          opt.ViewLocationFormats.Clear();
          opt.AreaViewLocationFormats.Clear();
          opt.ViewLocationFormats.Add("~/Views/{1}/{0}.cshtml");
          opt.ViewLocationFormats.Add("~/Views/Shared/{0}.cshtml");
          opt.AreaViewLocationFormats.Add("~/Areas/{2}/Views/{1}/{0}.cshtml");
          opt.AreaViewLocationFormats.Add("~/Areas/{2}/Views/Shared/{0}.cshtml");
      });
```

### 视图引擎



## 模型绑定

### IModelBinder

- [官网](https://docs.microsoft.com/zh-cn/aspnet/core/mvc/advanced/custom-model-binding)
- 继承自`Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder`
- `CreateModel`改为`BindModelAsync`方法
- `modelType`模型类型`bindingContext.ModelType`
- `controllerContext`为`bindingContext.ActionContext`
- 返回object改成

```csharp
  bindingContext.Result = ModelBindingResult.Success(entity);
  return Task.CompletedTask;
```

- 示例

```csharp
    public class EntityModelBinder:IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var modelType = bindingContext.ModelType;
            var controllerContext = bindingContext.ActionContext;
            if (modelType.As<IEntity>())
            {
                var fact = EntityFactory.CreateOperate(modelType);
                if (fact != null)
                {
                    bindingContext.Result = ModelBindingResult.Success(fact.Create());
                }
            }
            return Task.CompletedTask;
        }
```

### IModelBinderProvider

```csharp
    public class EntityModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context) => 
        context.Metadata.ModelType.As<IEntity>() ? new EntityModelBinder() : null;
    }
```

### 使用

- 放在第一位优先调用

```csharp
    services.AddMvc(opt =>
    {
        //模型绑定
        opt.ModelBinderProviders.Insert(0,new EntityModelBinderProvider());
    });
```

## 过滤器

## 上传文件大小限制

- https://stackoverflow.com/questions/38698350/increase-upload-file-size-in-asp-net-core

```csharp
.UseKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;
}
```

## 数据验证

- ValidateAntiForgeryToken

## 模型绑定验证

## 响应流写入、设置、推送

## 登录授权

- https://stackoverflow.com/questions/46853920/net-core-authentication
