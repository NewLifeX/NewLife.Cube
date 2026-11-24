using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>租户与成员关系 E2E：创建、成员绑定、成员存在时的删除保护</summary>
public sealed class ApiTenantTests : ApiTestBase
{
    [Fact(DisplayName = "TC-API-TENANT-001 租户可创建、成员可绑定并按租户过滤，成员未清空时禁止删除")]
    [Trait("Category", "Tenant")]
    public async Task TC_API_TENANT_001_MemberGuard()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        var userName = "E2E-tu-" + tag;
        var tenantId = 0;
        var tenantUserId = 0;
        try
        {
            var tenant = await Admin.PostJsonAsync("/api/Admin/Tenant", Json(new { name = "E2E租户" + tag, code = "t" + tag }));
            ApiAssert.Ok(tenant, "TC-API-TENANT-001 tenant");
            tenantId = ApiResult.GetInt32(tenant.Data?["id"]) ?? 0;
            Assert.True(tenantId > 0, $"创建租户未返回主键：{tenant.Content}");

            var register = await Admin.PostJsonAsync("/Auth/Register", Json(new { username = userName, password = "Test@123", confirmPassword = "Test@123" }));
            ApiAssert.Ok(register, "TC-API-TENANT-001 register");

            var users = await Admin.GetAsync($"/api/Admin/User?name={userName}");
            ApiAssert.Ok(users, "TC-API-TENANT-001 user list");
            var userId = ApiResult.GetInt32(users.Rows?.FirstOrDefault()?["id"]) ?? 0;
            Assert.True(userId > 0, $"未查到新用户：{users.Content}");

            var member = await Admin.PostJsonAsync("/api/Admin/TenantUser", Json(new { tenantId, userId, enable = true }));
            ApiAssert.Ok(member, "TC-API-TENANT-001 member");
            tenantUserId = ApiResult.GetInt32(member.Data?["id"]) ?? 0;
            Assert.True(tenantUserId > 0, $"绑定成员未返回主键：{member.Content}");

            var scoped = await Admin.GetAsync($"/api/Admin/TenantUser?tenantId={tenantId}");
            ApiAssert.Ok(scoped, "TC-API-TENANT-001 scoped");
            Assert.Equal(1, scoped.Page?["totalCount"]?.GetValue<int>());

            // 仍有成员的租户不能直接删除，避免留下孤儿关系
            var blocked = await Admin.DeleteAsync($"/api/Admin/Tenant/{tenantId}");
            Assert.NotEqual(0, blocked.Code);
            Assert.False(String.IsNullOrWhiteSpace(blocked.Message), "应提示先移除成员");

            var removeMember = await Admin.DeleteAsync($"/api/Admin/TenantUser/{tenantUserId}");
            ApiAssert.Ok(removeMember, "TC-API-TENANT-001 remove member");
            tenantUserId = 0;

            var deleted = await Admin.DeleteAsync($"/api/Admin/Tenant/{tenantId}");
            ApiAssert.Ok(deleted, "TC-API-TENANT-001 delete tenant");
            tenantId = 0;
        }
        finally
        {
            if (tenantUserId > 0) await Admin.DeleteAsync($"/api/Admin/TenantUser/{tenantUserId}");
            if (tenantId > 0) await Admin.DeleteAsync($"/api/Admin/Tenant/{tenantId}");
            ApiDatabaseHelper.DeleteUserByName(userName);
        }
    }
}
