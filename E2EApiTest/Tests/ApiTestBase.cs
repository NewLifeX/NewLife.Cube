using System;
using System.Text.Json;
using System.Threading.Tasks;
using E2EApiTest.Fixtures;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>API E2E 测试集合。共享同一个 CubeDemo 进程，串行执行避免数据竞争</summary>
[CollectionDefinition("E2EApi")]
public class ApiTestCollection : ICollectionFixture<ApiAppFixture> { }

/// <summary>API 测试基类：提供管理员客户端与 JSON 序列化辅助</summary>
[Collection("E2EApi")]
public abstract class ApiTestBase : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>管理员客户端（已登录，携带 Cookie 会话与 Bearer 令牌）</summary>
    protected ApiClient Admin { get; private set; } = null!;

    /// <summary>初始化：创建客户端并登录管理员</summary>
    public virtual async Task InitializeAsync()
    {
        Admin = new ApiClient();
        await Admin.LoginAsync();
    }

    /// <summary>清理：释放客户端</summary>
    public virtual Task DisposeAsync()
    {
        Admin.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>匿名客户端（独立 Cookie 容器，不带任何令牌）</summary>
    protected static ApiClient CreateAnonymous() => new();

    /// <summary>把对象序列化为 camelCase JSON</summary>
    protected static String Json(Object value) => JsonSerializer.Serialize(value, _jsonOptions);

    /// <summary>使用既有令牌访问，不带会话 Cookie（用于单独验证 Bearer / X-Token / 查询串）</summary>
    protected static async Task<ApiClient> CreateTokenOnlyClientAsync(String mode)
    {
        using var login = new ApiClient();
        await login.LoginAsync();

        return new ApiClient { Token = login.Token, TokenMode = mode };
    }

    /// <summary>生成带时间戳与随机数的唯一名称，避免并发与历史数据干扰</summary>
    protected static String NewName(String tag) => $"E2E-{tag}-{DateTime.Now:HHmmssfff}-{Random.Shared.Next(1000, 9999)}";
}
