using System;
using NewLife.Cube;
using NewLife.Cube.AI;
using Xunit;

namespace NewLife.Cube.Tests.AI;

/// <summary>AI 服务接口契约测试。验证服务商未注册时显式失败，而不是静默回退</summary>
public class AIServiceContractTests
{
    [Fact(DisplayName = "AI 服务：未注册的服务商直接抛参数异常")]
    public void Client_UnknownProvider_Throws()
    {
        var setting = new CubeSetting
        {
            AIProvider = "not-registered-provider-xyz",
            AIModel = "test-model",
            AIEndpoint = "https://example.com",
            AIApiKey = "test-key",
        };
        var service = new AIService(setting);

        var ex = Assert.ThrowsAny<Exception>(() => _ = service.Client);
        Assert.Contains("not-registered-provider-xyz", ex.Message);
    }
}
