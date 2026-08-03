using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
using NewLife.Cube.Services;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// 覆盖 <see cref="LovAutoRegisterService"/> 中 <c>InferLovList</c> 的自动推断逻辑（私有方法，通过反射调用）。
/// 验证：
///   1) Name 为空时推断为 ControllerName.ActionName；
///   2) RequestUrl 从 [Route] / HTTP 方法特性推断，并自动拼接 ApiPrefixes；
///   3) Method 从 HTTP 方法特性推断（[HttpPost] → POST, [HttpGet] → GET, 无特性 → GET）。
/// </summary>
public class LovAutoRegisterInferenceTests
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

    #region 测试用控制器

    [Route("api/roles")]
    private class RouteController : ControllerBase
    {
        [HttpGet("list")]
        public IActionResult GetList() => Ok();
    }

    [Route("[controller]/[action]")]
    private class ProductsController : ControllerBase
    {
        [HttpPost]
        public IActionResult Search() => Ok();
    }

    private class NoRouteController : ControllerBase
    {
        public IActionResult Index() => Ok();
    }

    [Route("api/v1/[controller]")]
    private class OrdersController : ControllerBase
    {
        [HttpPut("{id}")]
        public IActionResult Update(Int32 id) => Ok();
    }

    private class MixedController : ControllerBase
    {
        [HttpPost("create")]
        public IActionResult Create() => Ok();
    }

    private class DeleteActionController : ControllerBase
    {
        [HttpDelete("{id}")]
        public IActionResult Delete(Int32 id) => Ok();
    }

    [Route("api/v2/[controller]")]
    private class PatchController : ControllerBase
    {
        [HttpPatch("{id}")]
        public IActionResult Patch(Int32 id) => Ok();
    }

    [Route("api/")]
    private class SubRouteController : ControllerBase
    {
        [HttpGet("items")]
        public IActionResult GetItems() => Ok();
    }

    #endregion

    #region InferLovList — Name 推断

    [Fact]
    public void InferName_FromControllerAction()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Role" };
        var method = typeof(RouteController).GetMethod(nameof(RouteController.GetList))!;

        // 调用 private static InferLovList
        CallInferLovList(attr, method);

        Assert.Equal("Route.GetList", attr.Name);
    }

    [Fact]
    public void InferName_ExplicitNameKept()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Role", Name = "MyCustomName" };
        var method = typeof(RouteController).GetMethod(nameof(RouteController.GetList))!;

        CallInferLovList(attr, method);

        Assert.Equal("MyCustomName", attr.Name);
    }

    #endregion

    #region InferRequestUrl — 路由推断

    [Fact]
    public void InferRequestUrl_FromRouteAndHttpGet()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Role" };
        var method = typeof(RouteController).GetMethod(nameof(RouteController.GetList))!;

        CallInferLovList(attr, method);

        // Controller [Route("api/roles")] + Action [HttpGet("list")] → /api/roles/list
        // 但 ApiPrefixes 默认 /api，所以最终是 /api/api/roles/list
        Assert.Contains("api/roles/list", attr.RequestUrl);
    }

    [Fact]
    public void InferRequestUrl_FromRouteWithToken()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Products" };
        var method = typeof(ProductsController).GetMethod(nameof(ProductsController.Search))!;

        CallInferLovList(attr, method);

        // [Route("[controller]/[action]")] → Products/Search
        // 默认 ApiPrefixes=/api → /api/Products/Search
        Assert.Contains("Products/Search", attr.RequestUrl);
    }

    [Fact]
    public void InferRequestUrl_NoRouteAttributes_Convention()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Home" };
        var method = typeof(NoRouteController).GetMethod(nameof(NoRouteController.Index))!;

        CallInferLovList(attr, method);

        // 无路由特性 → 约定路由 controller/action → /api/NoRoute/Index
        Assert.Contains("NoRoute/Index", attr.RequestUrl);
    }

    [Fact]
    public void InferRequestUrl_WithRouteAndHttpPut()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Orders" };
        var method = typeof(OrdersController).GetMethod(nameof(OrdersController.Update))!;

        CallInferLovList(attr, method);

        // [Route("api/v1/[controller]")] + [HttpPut("{id}")] → /api/v1/Orders/{id}
        // 默认 ApiPrefixes=/api → /api/api/v1/Orders/{id}
        Assert.Contains("api/v1/Orders/{id}", attr.RequestUrl);
    }

    [Fact]
    public void InferRequestUrl_HttpPostWithTemplate()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Mixed" };
        var method = typeof(MixedController).GetMethod(nameof(MixedController.Create))!;

        CallInferLovList(attr, method);

        // 无 Controller [Route] + Action [HttpPost("create")] → /api/Mixed/create
        Assert.Contains("create", attr.RequestUrl);
    }

    [Fact]
    public void InferRequestUrl_HttpDeleteWithTemplate()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Delete" };
        var method = typeof(DeleteActionController).GetMethod(nameof(DeleteActionController.Delete))!;

        CallInferLovList(attr, method);

        Assert.Contains("DeleteAction/{id}", attr.RequestUrl);
    }

    [Fact]
    public void InferRequestUrl_HttpPatchWithTemplate()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Patch" };
        var method = typeof(PatchController).GetMethod(nameof(PatchController.Patch))!;

        CallInferLovList(attr, method);

        // [Route("api/v2/[controller]")] + [HttpPatch("{id}")] → /api/api/v2/Patch/{id}
        Assert.Contains("api/v2/Patch/{id}", attr.RequestUrl);
    }

    [Fact]
    public void InferRequestUrl_SubRoute()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.SubRoute" };
        var method = typeof(SubRouteController).GetMethod(nameof(SubRouteController.GetItems))!;

        CallInferLovList(attr, method);

        // [Route("api/")] + [HttpGet("items")] → /api/api/items
        Assert.Contains("api/items", attr.RequestUrl);
    }

    #endregion

    #region InferHttpMethod — 请求方式推断

    [Fact]
    public void InferHttpMethod_NoHttpAttribute_DefaultsToGet()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.NoRoute" };
        var method = typeof(NoRouteController).GetMethod(nameof(NoRouteController.Index))!;

        CallInferLovList(attr, method);

        // 无 HTTP 方法特性 → 保持默认 GET
        Assert.Equal("GET", attr.Method);
    }

    [Fact]
    public void InferHttpMethod_HttpGet_StaysGet()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Role" };
        var method = typeof(RouteController).GetMethod(nameof(RouteController.GetList))!;

        CallInferLovList(attr, method);

        Assert.Equal("GET", attr.Method);
    }

    [Fact]
    public void InferHttpMethod_HttpPost_Inferred()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Products" };
        var method = typeof(ProductsController).GetMethod(nameof(ProductsController.Search))!;

        CallInferLovList(attr, method);

        Assert.Equal("POST", attr.Method);
    }

    [Fact]
    public void InferHttpMethod_HttpPut_Inferred()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Orders" };
        var method = typeof(OrdersController).GetMethod(nameof(OrdersController.Update))!;

        CallInferLovList(attr, method);

        Assert.Equal("PUT", attr.Method);
    }

    [Fact]
    public void InferHttpMethod_HttpDelete_Inferred()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Delete" };
        var method = typeof(DeleteActionController).GetMethod(nameof(DeleteActionController.Delete))!;

        CallInferLovList(attr, method);

        Assert.Equal("DELETE", attr.Method);
    }

    [Fact]
    public void InferHttpMethod_HttpPatch_Inferred()
    {
        var attr = new LovListAttribute { LovCode = "List.Test.Patch" };
        var method = typeof(PatchController).GetMethod(nameof(PatchController.Patch))!;

        CallInferLovList(attr, method);

        Assert.Equal("PATCH", attr.Method);
    }

    [Fact]
    public void InferHttpMethod_ExplicitMethodPrevails()
    {
        // 如果用户显式指定了 Method="POST"，即使方法标注 [HttpGet] 也不覆盖
        var attr = new LovListAttribute { LovCode = "List.Test.Role", Method = "POST" };
        var method = typeof(RouteController).GetMethod(nameof(RouteController.GetList))!;

        CallInferLovList(attr, method);

        // 用户显式指定 POST，不应被 [HttpGet] 覆盖
        Assert.Equal("POST", attr.Method);
    }

    #endregion

    #region 综合场景

    [Fact]
    public void InferAll_AllDefaults()
    {
        // 只有 LovCode，其余全部自动推断
        var attr = new LovListAttribute { LovCode = "List.Test.Products" };
        var method = typeof(ProductsController).GetMethod(nameof(ProductsController.Search))!;

        CallInferLovList(attr, method);

        // Name
        Assert.Equal("Products.Search", attr.Name);

        // RequestUrl — 含 API 前缀
        Assert.Contains("Products/Search", attr.RequestUrl);

        // Method — 从 [HttpPost] 推断
        Assert.Equal("POST", attr.Method);
    }

    [Fact]
    public void InferAll_ExplicitOverrides()
    {
        // 用户显式指定所有可推断属性，推断不应覆盖
        var attr = new LovListAttribute
        {
            LovCode = "List.Test.Products",
            Name = "ExplicitName",
            RequestUrl = "/explicit/url",
            Method = "DELETE",
        };
        var method = typeof(ProductsController).GetMethod(nameof(ProductsController.Search))!;

        CallInferLovList(attr, method);

        Assert.Equal("ExplicitName", attr.Name);
        Assert.Equal("/explicit/url", attr.RequestUrl);
        Assert.Equal("DELETE", attr.Method);
    }

    #endregion

    #region 辅助

    /// <summary>通过反射调用 <c>LovAutoRegisterService.InferLovList(LovListAttribute, MethodInfo)</c></summary>
    private static void CallInferLovList(LovListAttribute attr, MethodInfo method)
    {
        var type = typeof(LovAutoRegisterService);
        var methodInfo = type.GetMethod("InferLovList", PrivateStatic, [typeof(LovListAttribute), typeof(MethodInfo)]);
        Assert.NotNull(methodInfo);

        // 保存原有的 ApiPrefixes，测试完后恢复
        var set = CubeSetting.Current;
        var savedPrefixes = set.ApiPrefixes;
        try
        {
            set.ApiPrefixes = "/api";
            methodInfo!.Invoke(null, [attr, method]);
        }
        finally
        {
            set.ApiPrefixes = savedPrefixes;
        }
    }

    #endregion
}