using System;
using System.IO;
using System.Linq;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// CI 门禁测试。发布流水线（publish/publish-beta）只负责构建、打包、推送，
/// 测试门禁统一由 test.yml 承担（push/PR 触发，多平台矩阵 + 前端 i18n + E2E）。
/// 这里守护"测试工作流真的会跑后端测试"，防止调整流水线时把测试门禁整体删掉。
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

    [Fact(DisplayName = "CI_测试工作流覆盖两个后端测试工程")]
    public void TestWorkflow_CoversBackendTests()
    {
        var dir = FindWorkflowDir();
        if (dir == null) return;

        var yml = ReadWorkflow(dir, "test.yml");

        Assert.Contains("XUnitTest/XUnitTest.csproj", yml);
        Assert.Contains("NewLife.Cube.Tests/NewLife.Cube.Tests.csproj", yml);
        Assert.Contains("dotnet test", yml);
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
