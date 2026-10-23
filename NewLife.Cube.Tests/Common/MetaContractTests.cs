using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>meta 契约测试（单对象页，不依赖 XCode 数据库）</summary>
public class MetaContractTests
{
    private sealed class TestConfig
    {
        [System.ComponentModel.DisplayName("名称")]
        public String Name { get; set; } = "test";
    }

    private sealed class TestObjectController : ObjectController<TestConfig>
    {
        protected override TestConfig Value { get; set; } = new();

        public String Serialize(Object data) => OnJsonSerialize(data);
    }

    private sealed class UiObjectController : ObjectController<TestConfig>
    {
        protected override TestConfig Value { get; set; } = new();

        protected override IDictionary<String, Object> OnGetPageUi() => new Dictionary<String, Object>
        {
            ["density"] = "compact",
        };

        public String Serialize(Object data) => OnJsonSerialize(data);
    }

    [Fact(DisplayName = "单对象页 meta 返回 entity / form")]
    public void ObjectMeta_ReturnsFormOnly()
    {
        var http = new DefaultHttpContext();
        var controller = new TestObjectController
        {
            ControllerContext = new ControllerContext { HttpContext = http, RouteData = new RouteData() },
        };

        var response = controller.Meta();
        var json = controller.Serialize(response.Data);

        Assert.DoesNotContain("\"version\":", json);
        Assert.DoesNotContain("\"enableKey\":", json);
        Assert.Contains("\"requiredPermissions\":", json);
        Assert.Contains("\"entity\":{", json);
        Assert.Contains("\"form\":[", json);
        Assert.DoesNotContain("\"fields\":", json);
        Assert.DoesNotContain("\"list\":", json);
        Assert.DoesNotContain("\"search\":", json);
    }

    [Fact(DisplayName = "单对象页原样下发 ui 扩展键")]
    public void ObjectMeta_EmitsUiExtension()
    {
        var http = new DefaultHttpContext();
        var controller = new UiObjectController
        {
            ControllerContext = new ControllerContext { HttpContext = http, RouteData = new RouteData() },
        };

        var response = controller.Meta();
        var json = controller.Serialize(response.Data);

        Assert.Contains("\"ui\":{", json);
        Assert.Contains("\"density\":\"compact\"", json);
    }
}
