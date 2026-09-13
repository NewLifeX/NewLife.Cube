using System;
using System.IO;
using Xunit;

namespace NewLife.Cube.Tests.Web;

/// <summary>GetPage 列表分区补 Map 外键候选的接线守护。</summary>
/// <remarks>
/// list/allList 此前只走 <c>PrepareFieldsForApi</c>，不调用 Map 候选填充，导致列表外键编号列显示原始数字。
/// 此处按源码结构钉死双栈 GetPage 的列表分区均接入填充，防止回退；行为级断言见
/// <c>XUnitTest.MapCandidateFillerListTests</c> 与 <c>XUnitTest.DataFieldDataSourceMapTests</c>。
/// </remarks>
public class GetPageListMapCandidateGuardTests
{
    [Fact(DisplayName = "WebAPI GetPage：list/allList 均补 Map 候选，GetFields 含 List 分支")]
    public void Api_GetPage_FillsListCandidates()
    {
        var text = File.ReadAllText(ResolveRepoFile("NewLife.Cube/Common/ReadOnlyEntityController.cs"));

        Assert.Contains("FixSearchMapCandidates(listFields)", text, StringComparison.Ordinal);
        Assert.Contains("FixSearchMapCandidates(allList)", text, StringComparison.Ordinal);
        Assert.Contains("kind is ViewKinds.List or ViewKinds.Search", text, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "MVC GetPage：list 补 Map 候选，GetFields 含 List 分支")]
    public void Mvc_GetPage_FillsListCandidates()
    {
        var text = File.ReadAllText(ResolveRepoFile("NewLife.CubeNC/Common/ReadOnlyEntityController.cs"));

        Assert.Contains("MapCandidateFiller.Apply(listFields, Factory)", text, StringComparison.Ordinal);
        Assert.Contains("kind is ViewKinds.List or ViewKinds.Search", text, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "填充器：列表分流以 ListField 判定（String 展示列与地区列跳过）")]
    public void Filler_BranchesByListField()
    {
        var text = File.ReadAllText(ResolveRepoFile("NewLife.CubeNC/ViewModels/MapCandidateFiller.cs"));

        Assert.Contains("var forList = df is ListField;", text, StringComparison.Ordinal);
        Assert.Contains("if (forList && df.Type == typeof(String)) continue;", text, StringComparison.Ordinal);
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
