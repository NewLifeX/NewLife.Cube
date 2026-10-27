using System;
using NewLife.Cube;
using NewLife.Cube.AI;
using Xunit;

namespace NewLife.Cube.Tests.AI;

/// <summary>AI 服务接口契约测试。验证服务商未注册时显式失败，已注册服务商按配置建客户端并复用，配置变更时重建</summary>
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

    [Fact(DisplayName = "AI 服务：已注册服务商按配置建客户端并复用实例")]
    public void Client_RegisteredProvider_CreatedAndReused()
    {
        var setting = new CubeSetting
        {
            AIProvider = "NewLifeAI",
            AIModel = "test-model",
            AIEndpoint = "https://ai.example.com/v1",
            AIApiKey = "test-key",
        };
        var service = new AIService(setting);

        var first = service.Client;
        var second = service.Client;

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact(DisplayName = "AI 服务：服务商为空时回退默认服务商")]
    public void Client_EmptyProvider_UsesDefault()
    {
        var setting = new CubeSetting
        {
            AIProvider = "",
            AIModel = "test-model",
            AIEndpoint = "https://ai.example.com/v1",
            AIApiKey = "test-key",
        };
        var service = new AIService(setting);

        Assert.NotNull(service.Client);
    }

    [Fact(DisplayName = "AI 服务：配置变更后重建客户端")]
    public void Client_ConfigChanged_Rebuilds()
    {
        var setting = new CubeSetting
        {
            AIProvider = "NewLifeAI",
            AIModel = "model-a",
            AIEndpoint = "https://ai.example.com/v1",
            AIApiKey = "test-key",
        };
        var service = new AIService(setting);
        var first = service.Client;

        setting.AIModel = "model-b";
        var second = service.Client;

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
    }
}
