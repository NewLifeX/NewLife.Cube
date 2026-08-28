using System.ComponentModel;
using NewLife.Cube.Entity;
using NewLife.Log;
using XCode;

namespace NewLife.Cube.Jobs;

/// <summary>附件清理作业参数</summary>
public class AttachmentCleanArgument
{
    /// <summary>无下载清理天数。超过该天数没有下载的附件将被清理，0 表示不启用该条件</summary>
    [DisplayName("无下载清理天数")]
    [Description("超过该天数没有下载的附件将被清理，0 表示不启用该条件")]
    public Int32 MaxDays { get; set; } = 180;

    /// <summary>下载较少清理天数。超过该天数且下载次数少于阈值的附件将被清理，0 表示不启用该条件</summary>
    [DisplayName("下载较少清理天数")]
    [Description("超过该天数且下载次数少于阈值的附件将被清理，0 表示不启用该条件")]
    public Int32 LongDays { get; set; } = 365;

    /// <summary>下载次数阈值。LongDays 内下载次数少于该值的附件将被清理</summary>
    [DisplayName("下载次数阈值")]
    [Description("LongDays 内下载次数少于该值的附件将被清理")]
    public Int32 MinDownloads { get; set; } = 10;

    /// <summary>单批最大删除数。避免一次性删除过多导致数据库IO跟不上，0 表示不限制</summary>
    [DisplayName("单批最大删除数")]
    [Description("避免一次性删除过多导致数据库IO跟不上，0 表示不限制")]
    public Int32 MaximumRows { get; set; } = 10000;
}

/// <summary>定期清理附件</summary>
/// <remarks>定时清理长期没有下载的附件，长时间无下载、或更长时间下载较少的附件都在清理范围之内</remarks>
[DisplayName("定期清理附件")]
[Description("定时清理长期没有下载的附件，长时间无下载、或更长时间下载较少都在清理范围之内")]
[CronJob("AttachmentClean", "0 0 3 * * ? *", Enable = false)]
public class AttachmentCleanJob : CubeJobBase<AttachmentCleanArgument>
{
    private readonly ITracer _tracer;

    /// <summary>实例化附件清理作业，用于定期清理长期没有下载的附件</summary>
    /// <param name="tracer"></param>
    public AttachmentCleanJob(ITracer tracer) => _tracer = tracer;

    /// <summary>执行作业</summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected override Task<String> OnExecute(AttachmentCleanArgument argument)
    {
        using var span = _tracer?.NewSpan("AttachmentClean", argument);

        // 构建清理条件，未配置任何条件时为空表达式
        var exp = BuildWhere(argument.MaxDays, argument.LongDays, argument.MinDownloads, DateTime.Now);
        if (exp.IsEmpty) return Task.FromResult("未配置有效的清理参数，跳过");

        // 分批删除，避免一次性删除过多导致数据库IO跟不上
        var rs = 0;
        var max = argument.MaximumRows > 0 ? argument.MaximumRows : 10000;
        for (var i = 0; i < 100; i++)
        {
            var list = Attachment.FindAll(exp, null, null, 0, max);
            if (list.Count == 0) break;

            rs += list.Delete();
        }

        span?.AppendTag($"删除附件 {rs} 个");
        XTrace.WriteLine("清理附件 {0} 个", rs);

        return Task.FromResult($"清理附件 {rs} 个");
    }

    /// <summary>构建清理条件。长时间无下载、或更长时间下载较少的附件都在清理范围之内</summary>
    /// <param name="maxDays">无下载清理天数，0 表示不启用</param>
    /// <param name="longDays">下载较少清理天数，0 表示不启用</param>
    /// <param name="minDownloads">下载次数阈值</param>
    /// <param name="now">当前时间</param>
    /// <returns>清理条件，未配置任何条件时返回空表达式</returns>
    public static WhereExpression BuildWhere(Int32 maxDays, Int32 longDays, Int32 minDownloads, DateTime now)
    {
        // 两个条件都未配置时返回空表达式，避免查询全表
        if (maxDays <= 0 && (longDays <= 0 || minDownloads <= 0)) return new WhereExpression();

        var exp = new WhereExpression();
        var minTime = new DateTime(2000, 1, 1); // 最后下载时间有效边界，早于该时间视为从未下载

        // 条件A：长时间无下载。曾下载过但很久没有下载，或从未下载且上传时间超期
        if (maxDays > 0)
        {
            var timeA = now.AddDays(-maxDays);
            var noDownload = Attachment._.LastDownload == null | Attachment._.LastDownload < minTime;
            exp |= (Attachment._.LastDownload > minTime & Attachment._.LastDownload < timeA) |
                   (noDownload & Attachment._.UploadTime < timeA);
        }

        // 条件B：更长时间下载较少。上传时间超期且下载次数少于阈值
        if (longDays > 0 && minDownloads > 0)
        {
            var timeB = now.AddDays(-longDays);
            exp |= Attachment._.UploadTime < timeB & Attachment._.Downloads < minDownloads;
        }

        return exp;
    }
}
