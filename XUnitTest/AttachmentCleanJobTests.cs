using System;
using System.ComponentModel;
using NewLife.Cube.Jobs;
using Xunit;

namespace XUnitTest;

/// <summary>附件清理作业单元测试 — 清理条件构建（长时间无下载 / 更长时间下载较少）</summary>
public class AttachmentCleanJobTests
{
    [Fact]
    [DisplayName("BuildWhere_未配置条件_返回空表达式")]
    public void BuildWhere_NoCondition_ReturnsEmpty()
    {
        var now = new DateTime(2026, 8, 28);

        var exp = AttachmentCleanJob.BuildWhere(0, 0, 10, now);

        Assert.True(exp.IsEmpty);
    }

    [Fact]
    [DisplayName("BuildWhere_仅无下载条件_包含最后下载与上传时间")]
    public void BuildWhere_MaxDaysOnly_ContainsLastDownloadAndUploadTime()
    {
        var now = new DateTime(2026, 8, 28);

        var exp = AttachmentCleanJob.BuildWhere(180, 0, 0, now);

        Assert.False(exp.IsEmpty);
        var sql = exp.ToString();
        Assert.Contains("LastDownload", sql);
        Assert.Contains("UploadTime", sql);
    }

    [Fact]
    [DisplayName("BuildWhere_仅下载较少条件_包含上传时间与下载次数")]
    public void BuildWhere_LongDaysOnly_ContainsUploadTimeAndDownloads()
    {
        var now = new DateTime(2026, 8, 28);

        var exp = AttachmentCleanJob.BuildWhere(0, 365, 10, now);

        Assert.False(exp.IsEmpty);
        var sql = exp.ToString();
        Assert.Contains("UploadTime", sql);
        Assert.Contains("Downloads", sql);
        Assert.DoesNotContain("LastDownload", sql);
    }

    [Fact]
    [DisplayName("BuildWhere_全条件启用_包含两类条件")]
    public void BuildWhere_AllConditions_ContainsBoth()
    {
        var now = new DateTime(2026, 8, 28);

        var exp = AttachmentCleanJob.BuildWhere(180, 365, 10, now);

        Assert.False(exp.IsEmpty);
        var sql = exp.ToString();
        Assert.Contains("LastDownload", sql);
        Assert.Contains("UploadTime", sql);
        Assert.Contains("Downloads", sql);
    }

    [Fact]
    [DisplayName("BuildWhere_下载阈值无效_仅无下载条件")]
    public void BuildWhere_InvalidMinDownloads_OnlyMaxDays()
    {
        var now = new DateTime(2026, 8, 28);

        var exp = AttachmentCleanJob.BuildWhere(180, 365, 0, now);

        Assert.False(exp.IsEmpty);
        var sql = exp.ToString();
        Assert.Contains("LastDownload", sql);
        Assert.DoesNotContain("Downloads", sql);
    }

    [Fact]
    [DisplayName("BuildWhere_无下载天数为零_仅下载较少条件")]
    public void BuildWhere_ZeroMaxDays_OnlyLongDays()
    {
        var now = new DateTime(2026, 8, 28);

        var exp = AttachmentCleanJob.BuildWhere(0, 365, 10, now);

        Assert.False(exp.IsEmpty);
        var sql = exp.ToString();
        Assert.Contains("UploadTime", sql);
        Assert.DoesNotContain("LastDownload", sql);
    }
}
