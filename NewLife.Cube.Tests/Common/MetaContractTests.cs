using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using XCode.Membership;
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

    private sealed class NoTimeMetaController : ReadOnlyEntityController<User>
    {
        protected override Boolean EnableTimeRange => false;

        public String Serialize(Object data) => OnJsonSerialize(data);
    }

    [Fact(DisplayName = "关闭主时间区间时不输出 range 查询项")]
    public void EntityMeta_DisableTimeRange_OmitsRange()
    {
        var http = new DefaultHttpContext();
        var controller = new NoTimeMetaController
        {
            ControllerContext = new ControllerContext { HttpContext = http, RouteData = new RouteData() },
        };

        var json = controller.Serialize(controller.Meta().Data);

        Assert.Contains("\"search\":[", json);
        Assert.DoesNotContain("\"kind\":\"range\"", json);
    }

    private sealed class CustomMetaController : ReadOnlyEntityController<User>
    {
        public String Serialize(Object data) => OnJsonSerialize(data);

        public override ApiResponse<Object> Meta()
        {
            var response = base.Meta();
            if (response?.Data is IDictionary<String, Object> data) data["custom"] = new { mode = "m2" };
            return response;
        }

        public Object CustomAction() => new { ok = true, kind = "m3" };
    }

    [Fact(DisplayName = "B3 可重写 Meta 追加自定义块，B4 自定义 Action 可独立调用")]
    public void EntityMeta_CanBeExtendedAndCustomActionWorks()
    {
        var http = new DefaultHttpContext();
        var controller = new CustomMetaController
        {
            ControllerContext = new ControllerContext { HttpContext = http, RouteData = new RouteData() },
        };

        var response = controller.Meta();
        var json = controller.Serialize(response.Data);
        var action = controller.CustomAction();

        Assert.Contains("\"custom\":{", json);
        Assert.Contains("\"mode\":\"m2\"", json);
        Assert.NotNull(action);
    }
}
