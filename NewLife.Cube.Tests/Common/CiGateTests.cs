using System;
using System.IO;
using System.Linq;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// DEVOPS-5 发布前测试门禁测试。守护 CI 工作流中"发布前必须跑测试"的步骤，
/// 防止后续调整流水线时把测试门禁悄悄删掉。
/// </summary>
public class CiGateTests
{
    private static String? FindWorkflowDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var path = Path.Combine(dir.FullName, ".github", "workflows");
            if (Directory.Exists(path)) return path;

            dir = dir.Parent;
        }

        return null;
    }

    private static String ReadWorkflow(String dir, String name)
    {
        var file = Path.Combine(dir, name);
        Assert.True(File.Exists(file), $"缺少工作流 {name}");

        return File.ReadAllText(file);
    }

    [Fact(DisplayName = "发布门禁_发布流水线包含后端测试步骤")]
    public void PublishWorkflows_RunBackendTests()
    {
        var dir = FindWorkflowDir();
        if (dir == null) return;

        foreach (var name in new[] { "publish.yml", "publish-beta.yml" })
        {
            var yml = ReadWorkflow(dir, name);

            Assert.Contains("dotnet test", yml);
            Assert.Contains("NewLife.Cube.Tests", yml);
        }
    }

    [Fact(DisplayName = "发布门禁_共享包发布前运行前端包测试")]
    public void NpmPublishWorkflow_RunsPackageTests()
    {
        var dir = FindWorkflowDir();
        if (dir == null) return;

        var yml = ReadWorkflow(dir, "publish-npm.yml");

        Assert.Contains("pnpm", yml);
        Assert.Contains("test", yml);
    }

    [Fact(DisplayName = "CI_存在独立测试工作流")]
    public void TestWorkflow_Exists()
    {
        var dir = FindWorkflowDir();
        if (dir == null) return;

        var yml = ReadWorkflow(dir, "test.yml");

        Assert.True(yml.Contains("dotnet test") || yml.Contains("test:unit"), "测试工作流应包含后端或前端测试步骤");
    }
}
