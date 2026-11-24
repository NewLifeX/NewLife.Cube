using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>桌面端上下文服务接口 E2E：地区级联、头像、页面配置与布局保存</summary>
public sealed class ApiCubeServiceTests : ApiTestBase
{
    [Fact(DisplayName = "TC-API-CUBE-001 AI 浮窗配置返回开关、配色与图表库地址")]
    [Trait("Category", "Service")]
    public async Task TC_API_CUBE_001_GetAiConfig()
    {
        var rs = await Admin.GetAsync("/Cube/GetAiConfig");

        ApiAssert.Ok(rs, "TC-API-CUBE-001");
        Assert.NotNull(rs.Data?["aiSwitch"]);
        Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(rs.Data?["aiPrimaryColor"])), "缺少主色");
        Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(rs.Data?["aiSecondaryColor"])), "缺少辅色");
        Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(rs.Data?["mermaidUrl"])), "缺少 Mermaid 地址");
    }

    [Fact(DisplayName = "TC-API-CUBE-002 地区根节点可读，不存在地区返回带提示的失败")]
    [Trait("Category", "Service")]
    public async Task TC_API_CUBE_002_GetArea()
    {
        var root = await Admin.GetAsync("/Cube/GetArea?id=0");
        ApiAssert.Ok(root, "TC-API-CUBE-002 root");
        Assert.Equal(0, ApiResult.GetInt32(root.Data?["id"]));

        var missing = await Admin.GetAsync("/Cube/GetArea?id=999999");
        ApiAssert.Fail(missing, 500, "TC-API-CUBE-002 missing");
        Assert.False(String.IsNullOrWhiteSpace(missing.Message), "错误提示应写入 message 而不是 data");
        Assert.Null(missing.Data);
    }

    [Fact(DisplayName = "TC-API-CUBE-003 地区子级与父级接口字段投影一致")]
    [Trait("Category", "Service")]
    public async Task TC_API_CUBE_003_AreaTree()
    {
        var childs = await Admin.GetAsync("/Cube/AreaChilds?id=0");
        ApiAssert.Ok(childs, "TC-API-CUBE-003 childs");
        Assert.NotNull(childs.Rows);

        var parents = await Admin.GetAsync("/Cube/AreaParents?id=0&isContainSelf=true");
        ApiAssert.Ok(parents, "TC-API-CUBE-003 parents");
        var rows = parents.Rows;
        Assert.NotNull(rows);
        Assert.True(rows!.Count > 0, "isContainSelf=true 时应包含自身");

        // 同数组内元素必须使用同一套精简字段，不能混入实体的 createTime 等字段
        foreach (var row in rows)
        {
            foreach (var key in row!.AsObject().Select(e => e.Key))
                Assert.True(key is "id" or "name" or "fullName" or "parentId" or "level" or "bigArea", $"地区元素出现多余字段 {key}");
        }

        var all = await Admin.GetAsync("/Cube/AreaAllParents?id=0");
        ApiAssert.Ok(all, "TC-API-CUBE-003 allParents");
        Assert.NotNull(all.Rows);

        var missing = await Admin.GetAsync("/Cube/AreaParents?id=999999");
        ApiAssert.Fail(missing, 500, "TC-API-CUBE-003 missing");
        Assert.False(String.IsNullOrWhiteSpace(missing.Message), "错误提示应写入 message 而不是 data");
    }

    [Fact(DisplayName = "TC-API-CUBE-004 头像接口对存在的用户返回图片、对不存在用户返回失败提示")]
    [Trait("Category", "Service")]
    public async Task TC_API_CUBE_004_Avatar()
    {
        var ok = await Admin.GetAsync("/Cube/Avatar?id=1");
        Assert.Equal(200, ok.StatusCode);
        Assert.StartsWith("image/", ok.ContentType ?? "");

        var missing = await Admin.GetAsync("/Cube/Avatar?id=99999999");
        ApiAssert.Fail(missing, 500, "TC-API-CUBE-004");
        Assert.False(String.IsNullOrWhiteSpace(missing.Message), "缺少错误提示");
    }

    [Fact(DisplayName = "TC-API-CUBE-005 页面配置缺少 page 参数返回参数错误而非服务端错误")]
    [Trait("Category", "Service")]
    public async Task TC_API_CUBE_005_PageConfigMissingParam()
    {
        var get = await Admin.GetAsync("/Cube/GetPageConfig?kind=Default");
        ApiAssert.Fail(get, -2, "TC-API-CUBE-005 get");

        var set = await Admin.PostJsonAsync("/Cube/SetPageConfig?kind=Default", Json(new { columns = new[] { "Id" } }));
        ApiAssert.Fail(set, -2, "TC-API-CUBE-005 set");
    }

    [Fact(DisplayName = "TC-API-CUBE-006 页面配置保存后可原样读回")]
    [Trait("Category", "Service")]
    public async Task TC_API_CUBE_006_PageConfigRoundtrip()
    {
        var page = "e2e/page-" + DateTime.Now.ToString("HHmmssfff");
        var marker = "e2e-marker-" + Random.Shared.Next(1000, 9999);

        var set = await Admin.PostJsonAsync($"/Cube/SetPageConfig?kind=Default&page={page}", Json(new { columns = new[] { marker } }));
        ApiAssert.Ok(set, "TC-API-CUBE-006 set");
        Assert.True(ApiResult.GetInt32(set.Data) > 0, "应返回配置编号");

        var get = await Admin.GetAsync($"/Cube/GetPageConfig?kind=Default&page={page}");
        ApiAssert.Ok(get, "TC-API-CUBE-006 get");
        Assert.Contains(marker, get.Data?["columns"]?.ToJsonString() ?? "");
    }

    [Fact(DisplayName = "TC-API-CUBE-007 布局保存仅允许系统布局类别，越权类别返回带提示的拒绝")]
    [Trait("Category", "Service")]
    public async Task TC_API_CUBE_007_SaveLayout()
    {
        var name = NewName("layout");
        var ok = await Admin.SendAsync(HttpMethod.Post, $"/Cube/SaveLayout?userid=1&category=LayoutSetting&name={name}&value=%7B%7D", "");
        ApiAssert.Ok(ok, "TC-API-CUBE-007 ok");

        var denied = await Admin.SendAsync(HttpMethod.Post, $"/Cube/SaveLayout?userid=1&category=Other&name={name}&value=%7B%7D", "");
        ApiAssert.Fail(denied, 203, "TC-API-CUBE-007 denied");
        Assert.False(String.IsNullOrWhiteSpace(denied.Message), "拒绝提示应写入 message 而不是 data");
    }

    [Fact(DisplayName = "TC-API-CUBE-008 工作台布局接口空布局时返回带提示的拒绝")]
    [Trait("Category", "Service")]
    public async Task TC_API_CUBE_008_WidgetLayout()
    {
        var get = await Admin.GetAsync("/api/Admin/Index/GetWidgetLayout");
        ApiAssert.Ok(get, "TC-API-CUBE-008 get");

        var save = await Admin.PostJsonAsync("/api/Admin/Index/SaveWidgetLayout", "{}");

        // 空布局被拒绝时必须给出 message，前端才能提示用户
        Assert.NotEqual(0, save.Code);
        Assert.False(String.IsNullOrWhiteSpace(save.Message), "拒绝提示应写入 message 而不是 data");
    }
}
