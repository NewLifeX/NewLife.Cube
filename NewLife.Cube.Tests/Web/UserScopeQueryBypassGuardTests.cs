using System;
using System.IO;
using Xunit;

namespace NewLife.Cube.Tests.Web;

/// <summary>列表 `?id=` 直查的行权守护（OSC-2608273d95 · 实现审计 N1）</summary>
/// <remarks>
/// 修复前 `Search(Pager)` 的 `id>0` 分支直接 `FindByID(id)` 返回，绕过 <c>SearchData</c> 的行权管道，
/// 非系统用户可借 `?id=&lt;他人ID&gt;` 读到他人行。此处按源码结构钉死双栈均走 `FindData(id)`（含 CanAccess），
/// 防止回退；行为级断言见 <c>DataScopeRowPermissionTests.FindData_OtherUser_Rejected</c>，
/// 真实行集（含租户）由 T10 冒烟覆盖。
/// </remarks>
public class UserScopeQueryBypassGuardTests
{
    [Theory(DisplayName = "列表_?id=分支必须经 FindData（不得绕过行权）")]
    [InlineData("NewLife.Cube/Areas/Admin/Controllers/UserController.cs")]
    [InlineData("NewLife.CubeNC/Areas/Admin/Controllers/UserController.cs")]
    public void SearchById_RoutesThroughFindData(String relativePath)
    {
        var path = ResolveRepoFile(relativePath);
        var text = File.ReadAllText(path);

        var idx = text.IndexOf("p[\"id\"].ToInt(-1)", StringComparison.Ordinal);
        Assert.True(idx > 0, $"{relativePath} 未找到 Search 的 id 分支");

        var branch = text.Substring(idx, Math.Min(400, text.Length - idx));

        Assert.Contains("FindData(id)", branch, StringComparison.Ordinal);
        Assert.DoesNotContain("FindByID(id)", branch, StringComparison.Ordinal);
    }

    /// <summary>从测试输出目录向上定位仓库根，再解析相对路径。</summary>
    /// <param name="relativePath">仓库内相对路径（正斜杠分隔）</param>
    /// <returns>绝对路径</returns>
    private static String ResolveRepoFile(String relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "魔方.sln"))) break;
            dir = dir.Parent;
        }

        Assert.True(dir != null, "未从测试输出目录向上定位到仓库根（魔方.sln）");

        var file = Path.Combine(dir!.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(file), $"未找到源码文件：{file}");

        return file;
    }
}
