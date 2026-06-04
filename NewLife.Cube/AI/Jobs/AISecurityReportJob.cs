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
    /// <param name="ai"></param>
    /// <param name="tracer"></param>
    public AISecurityReportJob(IAIService ai, ITracer tracer)
    {
        _ai = ai;
        _tracer = tracer;
    }
    #endregion

    #region 方法
    /// <summary>执行</summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    public override async Task<String> Execute(String argument)
    {
        using var span = _tracer?.NewSpan(nameof(AISecurityReportJob));

        var set = CubeSetting.Current;
        if (!set.AISwitch) return "AI 未启用，跳过";

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

        var result = await _ai.ChatAsync(@"你是信息安全专家。根据以下系统安全数据，生成安全态势分析周报（中文）：
1. 整体安全态势评估
2. 需要关注的风险点
3. 建议采取的安全措施

直接输出周报，不要加无关解释。", securityData);

        // 推送站内信通知
        var record = new NotificationRecord
        {
            Action = "AI_SecurityReport",
            Channel = "InApp",
            Title = $"安全分析周报 {start:yyyy-MM-dd} ~ {now:yyyy-MM-dd}",
            Content = result,
            Success = true,
            Result = $"分析 {oauthLogs.Count} 条 OAuth 日志",
        };
        record.Insert();

        span?.AppendTag(result);

        return $"安全分析周报完成，OAuth {oauthLogs.Count}/{failCount}";
    }
    #endregion
}
