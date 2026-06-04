using System.ComponentModel;
using NewLife.Cube.Entity;
using NewLife.Cube.Jobs;
using NewLife.Log;
using NewLife.Serialization;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.AI.Jobs;

/// <summary>每日日志摘要作业。定时汇总过去 24h 日志并由 AI 生成摘要推送</summary>
[DisplayName("AI 每日日志摘要")]
[Description("每天定时汇总过去 24 小时的系统日志，由 AI 生成摘要后通过通知系统推送给管理员")]
[CronJob("AILogSummary", "0 0 8 * * ? *", Enable = false)]
public class AILogSummaryJob : CubeJobBase
{
    #region 属性
    private readonly IAIService _ai;
    private readonly ITracer _tracer;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="ai"></param>
    /// <param name="tracer"></param>
    public AILogSummaryJob(IAIService ai, ITracer tracer)
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
        using var span = _tracer?.NewSpan(nameof(AILogSummaryJob));

        var set = CubeSetting.Current;
        if (!set.AISwitch) return "AI 未启用，跳过";

        // 查询过去 24h 的日志（取最近 50 条）
        var now = DateTime.Now;
        var start = now.AddHours(-24);
        var logs = XCode.Membership.Log.FindAll(
            XCode.Membership.Log._.CreateTime >= start & XCode.Membership.Log._.CreateTime <= now,
            XCode.Membership.Log._.ID.Desc(), null, 0, 50);
        var count = logs.Count;
        if (count == 0) return "过去 24 小时无日志";

        // 构建日志摘要 JSON
        var logItems = logs.Select(e => new
        {
            e.Category,
            e.Action,
            Message = e.Remark,
            e.CreateTime,
        }).ToList();
        var logsJson = logItems.ToJson();

        // CronJob 保留深度推理，使用 ChatAsync 而非 AnalyzeLogsAsync
        var prompt = @"你是系统运维专家。请分析以下错误日志，用中文给出：
1. 错误类型归类（按频率排序）
2. 最可能的根因
3. 建议的排查步骤

直接输出分析报告，不要加无关解释。";
        var result = await _ai.ChatAsync(prompt, logsJson);

        // 推送站内信通知
        var record = new NotificationRecord
        {
            Action = "AI_LogSummary",
            Channel = "InApp",
            Title = $"日志日报 {now:yyyy-MM-dd}",
            Content = result,
            Success = true,
            Result = $"分析 {count} 条日志",
        };
        record.Insert();

        span?.AppendTag(result);

        return $"分析 {count} 条日志完成";
    }
    #endregion
}
