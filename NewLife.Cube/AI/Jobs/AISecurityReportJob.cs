using System.ComponentModel;
using NewLife.Cube.Entity;
using NewLife.Cube.Jobs;
using NewLife.Log;
using NewLife.Serialization;
using XCode;

namespace NewLife.Cube.AI.Jobs;

/// <summary>安全分析周报作业。每周分析 OAuth 异常登录，由 AI 生成安全态势报告</summary>
[DisplayName("AI 安全分析周报")]
[Description("每周一分析 OAuth 登录记录，由 AI 生成安全态势报告并推送")]
[CronJob("AISecurityReport", "0 0 9 ? * MON", Enable = false)]
public class AISecurityReportJob : CubeJobBase
{
    #region 属性
    private readonly IAIService _ai;
    private readonly ITracer _tracer;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="ai">AI 服务，用于生成安全巡检报告</param>
    /// <param name="tracer">链路追踪器</param>
    public AISecurityReportJob(IAIService ai, ITracer tracer)
    {
        _ai = ai;
        _tracer = tracer;
    }
    #endregion

    #region 方法
    /// <summary>执行</summary>
    /// <param name="argument">作业参数，当前未使用</param>
    /// <returns>执行结果描述，写入作业日志</returns>
    public override async Task<String> Execute(String argument)
    {
        using var span = _tracer?.NewSpan(nameof(AISecurityReportJob));

        var set = CubeSetting.Current;
        if (!set.AISwitch) return L.T("AI is not enabled; skipped");

        var now = DateTime.Now;
        var start = now.AddDays(-7);

        // 查询本周 OAuth 日志
        var oauthLogs = OAuthLog.FindAll(
            OAuthLog._.CreateTime >= start & OAuthLog._.CreateTime <= now,
            OAuthLog._.Id.Desc(), null, 0, 100);
        var failCount = oauthLogs.Count(e => !e.Success);

        // 构建安全态势数据
        var securityData = new
        {
            Period = $"{start:yyyy-MM-dd} ~ {now:yyyy-MM-dd}",
            OAuthTotal = oauthLogs.Count,
            OAuthFailed = failCount,
            OAuthSuccessRate = oauthLogs.Count > 0
                ? $"{(Double)(oauthLogs.Count - failCount) / oauthLogs.Count * 100:F1}%"
                : "N/A",
            RecentLogs = oauthLogs.Take(20).Select(e => new
            {
                e.Provider,
                e.Action,
                e.Success,
                e.Remark,
                e.CreateTime,
            }),
        }.ToJson();

        var result = await _ai.ChatAsync(L.T(@"You are an information security expert. Based on the following security data, write a weekly security report covering:
1. Overall security posture
2. Risks that need attention
3. Recommended security measures

Output the report directly without extra explanation."), securityData);

        // 推送站内信通知
        var record = new NotificationRecord
        {
            Action = "AI_SecurityReport",
            Channel = "InApp",
            Title = L.T("Security weekly report {0} ~ {1}", start.ToString("yyyy-MM-dd"), now.ToString("yyyy-MM-dd")),
            Content = result,
            Success = true,
            Result = L.T("Analyzed {0} OAuth log entries", oauthLogs.Count),
        };
        record.Insert();

        span?.AppendTag(result);

        return L.T("Security weekly report completed, OAuth {0}/{1}", oauthLogs.Count, failCount);
    }
    #endregion
}
