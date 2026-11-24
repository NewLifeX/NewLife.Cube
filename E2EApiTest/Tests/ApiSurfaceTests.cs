using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>管理端接口面冒烟：逐一访问所有实体控制器与配置页，确保不会出现 500 / 415 之类的基础故障</summary>
/// <remarks>单个接口的细则断言由其他用例负责，本组用例只守住“接口存在、可绑定参数、不崩”这条底线</remarks>
public class ApiSurfaceTests : ApiTestBase
{
    /// <summary>管理端实体控制器清单（与 Areas/Admin/Controllers 下的实体控制器一一对应）</summary>
    private static readonly String[] _entities =
    [
        "AccessRule", "Department", "Log", "MailConfig", "Menu", "NotificationRecord",
        "OAuthConfig", "OAuthLog", "Parameter", "Role", "SmsConfig", "Tenant",
        "TenantUser", "User", "UserConnect", "UserOnline", "UserStat", "UserToken",
    ];

    /// <summary>配置页控制器清单（ConfigController 派生）</summary>
    private static readonly String[] _configs = ["Core", "Cube", "Star", "Sys", "XCode"];

    [Fact(DisplayName = "TC-API-SURF-001 全部管理端实体列表接口可访问且不返回服务端错误")]
    [Trait("Category", "Surface")]
    public async Task EntityList_ShouldNotFail()
    {
        var failures = new List<String>();
        foreach (var name in _entities)
        {
            var rs = await Admin.GetAsync($"/api/Admin/{name}?pageIndex=1&pageSize=1");
            Record(failures, $"{name} 列表", rs);

            // 有数据时必须回传分页对象，避免“只返数组没有总数”的契约漂移
            if (rs.Code == 0 && rs.Page == null) failures.Add($"{name} 列表 缺少 page 分页信息");
        }

        Assert.True(failures.Count == 0, "以下实体列表接口异常：\n" + String.Join("\n", failures));
    }

    [Fact(DisplayName = "TC-API-SURF-002 全部管理端实体表单元数据接口可访问且字段完整")]
    [Trait("Category", "Surface")]
    public async Task EntityMeta_ShouldNotFail()
    {
        var failures = new List<String>();
        foreach (var name in _entities)
        {
            var rs = await Admin.GetAsync($"/api/Admin/{name}/meta");
            Record(failures, $"{name} 表单元数据", rs);
            if (rs.Code != 0) continue;

            if (String.IsNullOrEmpty(ApiResult.GetString(rs.Data?["entity"]?["name"])))
                failures.Add($"{name} 表单元数据 缺少 entity.name");
            if (String.IsNullOrEmpty(ApiResult.GetString(rs.Data?["entity"]?["primaryKey"])))
                failures.Add($"{name} 表单元数据 缺少 entity.primaryKey");

            foreach (var key in new[] { "list", "form", "search" })
            {
                if (rs.Data?[key] is not System.Text.Json.Nodes.JsonArray arr || arr.Count == 0)
                {
                    failures.Add($"{name} 表单元数据 {key} 为空");
                    continue;
                }

                foreach (var field in arr)
                {
                    if (String.IsNullOrEmpty(ApiResult.GetString(field?["name"]))) failures.Add($"{name} 表单元数据 {key} 存在无名字段");
                }
            }
        }

        Assert.True(failures.Count == 0, "以下实体元数据接口异常：\n" + String.Join("\n", failures));
    }

    [Fact(DisplayName = "TC-API-SURF-003 实体详情查询不存在的编号按契约返回失败提示而非空响应")]
    [Trait("Category", "Surface")]
    public async Task EntityDetail_Missing_ShouldFail()
    {
        // 文档《WebAPI接口规范》206 行约定：详情/修改的记录不存在抛 XException → code=500 且带 message，仅删除返回 404
        var failures = new List<String>();
        foreach (var name in _entities)
        {
            var rs = await Admin.GetAsync($"/api/Admin/{name}/99999999");
            if (rs.Code != 500 || String.IsNullOrWhiteSpace(rs.Message))
                failures.Add($"{name} 详情 HTTP={rs.StatusCode} code={rs.Code} {Truncate(rs.Message)}");
        }

        Assert.True(failures.Count == 0, "以下实体详情接口未按契约返回失败提示：\n" + String.Join("\n", failures));
    }

    [Fact(DisplayName = "TC-API-SURF-004 全部配置页表单结构接口可访问且字段完整")]
    [Trait("Category", "Surface")]
    public async Task ConfigSchema_ShouldNotFail()
    {
        var failures = new List<String>();
        foreach (var name in _configs)
        {
            var rs = await Admin.GetAsync($"/api/Admin/{name}/GetFormSchema");
            Record(failures, $"{name} 配置表单结构", rs);
            if (rs.Code != 0) continue;

            // 该动作返回 String，统一信封把它再包一层：data 是转义后的 JSON 文本，需要二次解析
            if (rs.Data is not System.Text.Json.Nodes.JsonValue value || !value.TryGetValue<String>(out var text))
            {
                failures.Add($"{name} 配置表单结构 data 不是 JSON 文本：{Truncate(rs.Content)}");
                continue;
            }

            var schema = System.Text.Json.Nodes.JsonNode.Parse(text);
            if (schema?["fields"] is not System.Text.Json.Nodes.JsonArray fields || fields.Count == 0)
                failures.Add($"{name} 配置表单结构 缺少 fields：{Truncate(text)}");
            else if (String.IsNullOrEmpty(ApiResult.GetString(schema["entity"])))
                failures.Add($"{name} 配置表单结构 缺少 entity");
        }

        Assert.True(failures.Count == 0, "以下配置页表单结构接口异常：\n" + String.Join("\n", failures));
    }

    [Fact(DisplayName = "TC-API-SURF-005 数据库管理接口缺少连接名返回参数错误，正常连接名返回结构")]
    [Trait("Category", "Surface")]
    public async Task DbTools_ShouldValidateParam()
    {
        foreach (var action in new[] { "tables", "entities", "diff" })
        {
            var missing = await Admin.GetAsync($"/api/Admin/Db/{action}");
            ApiAssert.Fail(missing, -2, $"TC-API-SURF-005 {action} 缺参数");

            var rs = await Admin.GetAsync($"/api/Admin/Db/{action}?name=Cube");
            ApiAssert.Ok(rs, $"TC-API-SURF-005 {action}");
            Assert.NotNull(rs.Rows);
            Assert.True(rs.Rows!.Count > 0, $"{action} 未返回数据");
        }

        var download = await Admin.GetAsync("/api/Admin/Db/Download");
        ApiAssert.Fail(download, -2, "TC-API-SURF-005 download");

        var list = await Admin.GetAsync("/api/Admin/Db");
        ApiAssert.Ok(list, "TC-API-SURF-005 index");
        Assert.True(list.Rows?.Count > 0, "数据库列表为空");
    }

    /// <summary>记录非预期响应：只允许 200 且业务码为 0，或 401/403 这类鉴权拒绝</summary>
    /// <param name="failures">失败明细收集器</param>
    /// <param name="label">接口标签</param>
    /// <param name="rs">响应</param>
    private static void Record(List<String> failures, String label, ApiResult rs)
    {
        if (rs.Code == 0 && rs.StatusCode == 200) return;
        if (rs.StatusCode is 401 or 403 && rs.Code is 401 or 403) return;

        var sb = new StringBuilder();
        sb.Append(label).Append(" HTTP=").Append(rs.StatusCode).Append(" code=").Append(rs.Code);
        if (!String.IsNullOrEmpty(rs.Message)) sb.Append(' ').Append(Truncate(rs.Message));
        failures.Add(sb.ToString());
    }

    private static String Truncate(String? text) => text == null ? "" : text.Length <= 160 ? text : text[..160] + "...";
}
