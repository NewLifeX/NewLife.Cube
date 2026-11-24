using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>多租户开关 E2E：开关可切换、管理员不受影响、注册自动入租户、非法租户被拒</summary>
public sealed class ApiTenantModeTests : ApiTestBase
{
    [Fact(DisplayName = "TC-API-TENANT-002 开启多租户后注册需带租户标识并自动成为成员，非法租户被拒")]
    [Trait("Category", "Tenant")]
    public async Task TC_API_TENANT_002_TenantMode()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        var code = "tm" + tag;
        var userName = "E2E-tm-" + tag;
        var tenantId = 0;
        try
        {
            await SetTenantAsync(true);

            // 管理员本身不属任何租户，开启后仍可正常访问管理端
            var adminList = await Admin.GetAsync("/api/Admin/User?pageSize=1");
            ApiAssert.Ok(adminList, "TC-API-TENANT-002 admin");

            var tenant = await Admin.PostJsonAsync("/api/Admin/Tenant", Json(new { name = "E2E租户" + tag, code, enable = true }));
            ApiAssert.Ok(tenant, "TC-API-TENANT-002 tenant");
            tenantId = ApiResult.GetInt32(tenant.Data?["id"]) ?? 0;
            Assert.True(tenantId > 0, $"创建租户未返回主键：{tenant.Content}");

            var headers = new Dictionary<String, String> { ["X-Tenant"] = code };
            using (var client = new ApiClient())
            {
                var register = await client.SendAsync(HttpMethod.Post, "/Auth/Register", Json(new { username = userName, password = "Test@123", confirmPassword = "Test@123" }), "application/json", headers);
                ApiAssert.Ok(register, "TC-API-TENANT-002 register");
            }

            var members = await Admin.GetAsync($"/api/Admin/TenantUser?tenantId={tenantId}");
            ApiAssert.Ok(members, "TC-API-TENANT-002 members");
            Assert.Equal(1, members.Page?["totalCount"]?.GetValue<Int32>());

            using (var member = new ApiClient())
            {
                var login = await member.SendAsync(HttpMethod.Post, "/Auth/Login", Json(new { username = userName, password = "Test@123" }), "application/json", headers);
                ApiAssert.Ok(login, "TC-API-TENANT-002 member login");

                var info = await member.SendAsync(HttpMethod.Get, "/Auth/Info", null, null, headers);
                ApiAssert.Ok(info, "TC-API-TENANT-002 member info");
            }

            using (var outsider = new ApiClient())
            {
                var bad = await outsider.SendAsync(HttpMethod.Post, "/Auth/Login", Json(new { username = userName, password = "Test@123" }), "application/json", new Dictionary<String, String> { ["X-Tenant"] = "not-exist" });
                Assert.NotEqual(0, bad.Code);
                Assert.False(String.IsNullOrWhiteSpace(bad.Message), "非法租户应给出提示");
            }
        }
        finally
        {
            await SetTenantAsync(false);
            if (tenantId > 0)
            {
                foreach (var row in (await Admin.GetAsync($"/api/Admin/TenantUser?tenantId={tenantId}")).Rows ?? new JsonArray())
                    await Admin.DeleteAsync($"/api/Admin/TenantUser/{ApiResult.GetInt32(row?["id"])}");
                await Admin.DeleteAsync($"/api/Admin/Tenant/{tenantId}");
            }
            ApiDatabaseHelper.DeleteUserByName(userName);

            var check = await Admin.GetAsync("/api/Admin/Cube");
            Assert.False(ApiResult.GetBoolean(check.Data?["enableTenant"]), "用例结束必须关闭多租户开关，避免影响其它用例");
        }
    }

    /// <summary>切换系统级多租户开关（与后台配置页保存接口同一路径）</summary>
    private async Task SetTenantAsync(Boolean enabled)
    {
        var get = await Admin.GetAsync("/api/Admin/Cube");
        ApiAssert.Ok(get, "读取魔方设置");

        var settings = get.Data as JsonObject ?? throw new InvalidOperationException("魔方设置响应不是对象：" + get.Content);
        settings["enableTenant"] = enabled;

        var put = await Admin.PutJsonAsync("/api/Admin/Cube", settings.ToJsonString());
        ApiAssert.Ok(put, "保存魔方设置");
        Assert.Equal(enabled, ApiResult.GetBoolean(put.Data?["enableTenant"]));
    }
}
