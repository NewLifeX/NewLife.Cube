using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife.AI.Clients;
using NewLife.Cube;
using NewLife.Cube.AI;
using NewLife.Cube.AI.Jobs;
using NewLife.Cube.Entity;
using NewLife.Cube.Tests.Common;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// AI-2 / AI-3 定时作业测试。覆盖 AI 开关关闭时跳过、日志摘要与安全周报调用 AI 并推送站内信。
/// AI 服务用假实现注入，只验证提示词/数据与落库结果，不依赖外部大模型。
/// </summary>
[Collection("TenantAuth")]
public class AIJobsTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public AIJobsTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 假 AI 服务
    private sealed class FakeAI : IAIService
    {
        public String Prompt;
        public String Data;
        public Int32 Calls;
        public String Result = "AI-REPORT";

        public IChatClient Client => null;

        public Task<String> ChatAsync(String prompt, String data, CancellationToken cancellationToken = default)
        {
            Prompt = prompt;
            Data = data;
            Calls++;

            return Task.FromResult(Result);
        }

        public async IAsyncEnumerable<String> DiagnoseSystemStreamAsync(String sysInfoJson, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
    #endregion

    private static T WithSwitch<T>(Boolean on, Func<T> func)
    {
        var set = CubeSetting.Current;
        var old = set.AISwitch;
        set.AISwitch = on;
        try
        {
            return func();
        }
        finally
        {
            set.AISwitch = old;
        }
    }

    [Fact(DisplayName = "AI日志摘要_开关关闭时跳过且不写站内信")]
    public async Task LogSummary_Disabled_Skips()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        var before = NotificationRecord.FindAll(NotificationRecord._.Action == "AI_LogSummary").Count;

        var ai = new FakeAI();
        var job = new AILogSummaryJob(ai, null);
        var rs = await WithSwitch(false, () => job.Execute(null));

        Assert.False(rs.IsNullOrEmpty());
        Assert.Equal(0, ai.Calls);
        Assert.Equal(before, NotificationRecord.FindAll(NotificationRecord._.Action == "AI_LogSummary").Count);
    }

    [Fact(DisplayName = "AI日志摘要_读取近期日志调用AI并推送站内信")]
    public async Task LogSummary_Enabled_CallsAiAndNotifies()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");

        var marker = "AI摘要标记" + Guid.NewGuid().ToString("N")[..8];
        new XCode.Membership.Log { Action = "AiJobTest", Success = false, Remark = marker, CreateTime = DateTime.Now }.Insert();

        var ai = new FakeAI();
        var job = new AILogSummaryJob(ai, null);
        var rs = await WithSwitch(true, () => job.Execute(null));

        Assert.Equal(1, ai.Calls);
        Assert.Contains(marker, ai.Data);
        Assert.Contains("1.", ai.Prompt);
        Assert.False(rs.IsNullOrEmpty());

        var record = NotificationRecord.FindAll(NotificationRecord._.Action == "AI_LogSummary")
            .OrderByDescending(e => e.Id)
            .FirstOrDefault();
        Assert.NotNull(record);
        Assert.Equal("AI-REPORT", record.Content);
        Assert.True(record.Success);
        Assert.Matches(@"\d+", record.Result ?? "");
    }

    [Fact(DisplayName = "AI安全报告_开关关闭时跳过且不写站内信")]
    public async Task SecurityReport_Disabled_Skips()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        var before = NotificationRecord.FindAll(NotificationRecord._.Action == "AI_SecurityReport").Count;

        var ai = new FakeAI();
        var job = new AISecurityReportJob(ai, null);
        var rs = await WithSwitch(false, () => job.Execute(null));

        Assert.False(rs.IsNullOrEmpty());
        Assert.Equal(0, ai.Calls);
        Assert.Equal(before, NotificationRecord.FindAll(NotificationRecord._.Action == "AI_SecurityReport").Count);
    }

    [Fact(DisplayName = "AI安全报告_汇总近七天OAuth日志并推送站内信")]
    public async Task SecurityReport_Enabled_CallsAiAndNotifies()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");

        new OAuthLog { Provider = "AiJobTest", Success = false, CreateTime = DateTime.Now, Remark = "失败样例" }.Insert();

        var ai = new FakeAI();
        var job = new AISecurityReportJob(ai, null);
        var rs = await WithSwitch(true, () => job.Execute(null));

        Assert.Equal(1, ai.Calls);
        Assert.Contains("OAuthTotal", ai.Data);
        Assert.Contains("失败样例", ai.Data);
        Assert.False(rs.IsNullOrEmpty());

        var record = NotificationRecord.FindAll(NotificationRecord._.Action == "AI_SecurityReport")
            .OrderByDescending(e => e.Id)
            .FirstOrDefault();
        Assert.NotNull(record);
        Assert.Equal("AI-REPORT", record.Content);
        Assert.True(record.Success);
    }
}

