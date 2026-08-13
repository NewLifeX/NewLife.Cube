using System;
using NewLife.Cube.AI;
using NewLife.Cube.Areas.Admin.Controllers;
using Xunit;

namespace XUnitTest;

/// <summary>非实体页面 AI 页面上下文测试 — 验证魔方设置/服务器信息/数据库信息等非实体页面实现 IPageDataContext，支持 AI 基础功能分析</summary>
/// <remarks>
/// 设计预期：除实体列表/表单页外，非实体页面也应支持 AI 各项基础功能分析。
/// 全局 /Ai/AiChat 端点解析目标控制器后，get_page_context 优先调用其服务端实现（IPageDataContext），
/// 否则浏览器采集兜底。本测试用类型级断言守护服务端上下文接入，避免依赖数据库。
/// </remarks>
public class IPageDataContextTests
{
    [Fact(DisplayName = "非实体页面控制器实现 IPageDataContext")]
    public void NonEntityControllers_Implement_IPageDataContext()
    {
        // 服务器信息页：服务端上下文提供权威服务器运行指标（无需浏览器采集）
        Assert.True(typeof(IPageDataContext).IsAssignableFrom(typeof(IndexController)),
            "IndexController 应实现 IPageDataContext（服务器信息页）");

        // 数据库信息页：服务端上下文提供数据库连接列表（不含连接字符串）
        Assert.True(typeof(IPageDataContext).IsAssignableFrom(typeof(DbController)),
            "DbController 应实现 IPageDataContext（数据库信息页）");

        // 魔方设置页：服务端上下文提供配置摘要（敏感配置不暴露）
        Assert.True(typeof(IPageDataContext).IsAssignableFrom(typeof(CubeController)),
            "CubeController 应实现 IPageDataContext（魔方设置页）");
    }

    [Fact(DisplayName = "魔方设置敏感字段过滤")]
    public void CubeSetting_SensitiveFields_Filtered()
    {
        // AiFormHelper.IsSensitiveField：ApiKey/Secret/Password/Token/连接串等不应向 AI 暴露
        // （ConfigController 表单 schema 与 CubeController 页面上下文共用同一过滤逻辑）
        foreach (var name in new[] { "AIApiKey", "JwtSecret", "AppSecret", "OAuthClientSecret", "DbPassword", "AccessToken", "ConnStr" })
        {
            Assert.True(AiFormHelper.IsSensitiveField(name), $"敏感字段 {name} 应被过滤");
        }

        foreach (var name in new[] { "Debug", "ShowRunTime", "AIModel", "AIProvider", "AISwitch", "UploadPath" })
        {
            Assert.False(AiFormHelper.IsSensitiveField(name), $"非敏感字段 {name} 不应被过滤");
        }
    }
}
