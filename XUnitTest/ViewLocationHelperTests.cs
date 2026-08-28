using System;
using System.ComponentModel;
using NewLife.Cube;
using Xunit;

namespace XUnitTest;

/// <summary>ViewLocationHelper 视图定位辅助单元测试</summary>
public class ViewLocationHelperTests
{
    [Fact]
    [DisplayName("主题插入规则：默认主题为 ACE")]
    public void GetThemeInsertRules_DefaultTheme()
    {
        var rules = ViewLocationHelper.GetThemeInsertRules(null);

        Assert.Equal(3, rules.Count);
        Assert.Equal("/Views/ACE/{0}.cshtml", rules["/Views/Shared/{0}.cshtml"]);
        Assert.Equal("/Areas/{2}/Views/ACE/{0}.cshtml", rules["/Areas/{2}/Views/Shared/{0}.cshtml"]);
        Assert.Equal("/Areas/{2}/Views/{1}_ACE/{0}.cshtml", rules["/Areas/{2}/Views/{1}/{0}.cshtml"]);
    }

    [Fact]
    [DisplayName("主题插入规则：自定义主题")]
    public void GetThemeInsertRules_CustomTheme()
    {
        var rules = ViewLocationHelper.GetThemeInsertRules("Tabler");

        Assert.Equal("/Views/Tabler/{0}.cshtml", rules["/Views/Shared/{0}.cshtml"]);
        Assert.Equal("/Areas/{2}/Views/Tabler/{0}.cshtml", rules["/Areas/{2}/Views/Shared/{0}.cshtml"]);
        Assert.Equal("/Areas/{2}/Views/{1}_Tabler/{0}.cshtml", rules["/Areas/{2}/Views/{1}/{0}.cshtml"]);
    }

    [Fact]
    [DisplayName("查找模式格式化为具体路径")]
    public void Format_ReplacePlaceholders()
    {
        Assert.Equal("/Areas/Admin/Views/User_ACE/_List_Search.cshtml", ViewLocationHelper.Format("/Areas/{2}/Views/{1}_ACE/{0}.cshtml", "Admin", "User", "_List_Search"));
        Assert.Equal("/Views/ACE/_List_Search.cshtml", ViewLocationHelper.Format("/Views/ACE/{0}.cshtml", null, null, "_List_Search"));
        Assert.Null(ViewLocationHelper.Format(null, "Admin", "User", "_List_Search"));
    }

    [Fact]
    [DisplayName("带区域的候选路径按优先级排序")]
    public void GetCandidates_WithArea_OrderedByPriority()
    {
        var list = ViewLocationHelper.GetCandidates("Admin", "User", "_List_Search", "ACE");

        Assert.Equal(6, list.Length);
        Assert.Equal("/Areas/Admin/Views/User_ACE/_List_Search.cshtml", list[0]);
        Assert.Equal("/Areas/Admin/Views/User/_List_Search.cshtml", list[1]);
        Assert.Equal("/Areas/Admin/Views/ACE/_List_Search.cshtml", list[2]);
        Assert.Equal("/Areas/Admin/Views/Shared/_List_Search.cshtml", list[3]);
        Assert.Equal("/Views/ACE/_List_Search.cshtml", list[4]);
        Assert.Equal("/Views/Shared/_List_Search.cshtml", list[5]);
    }

    [Fact]
    [DisplayName("无区域无控制器时只生成应用级路径")]
    public void GetCandidates_NoAreaNoController_AppLevelOnly()
    {
        var list = ViewLocationHelper.GetCandidates(null, null, "Error", "ACE");

        Assert.Equal(2, list.Length);
        Assert.Equal("/Views/ACE/Error.cshtml", list[0]);
        Assert.Equal("/Views/Shared/Error.cshtml", list[1]);
    }

    [Fact]
    [DisplayName("无区域有控制器时生成控制器级与应用级路径")]
    public void GetCandidates_NoAreaWithController_ControllerLevel()
    {
        var list = ViewLocationHelper.GetCandidates(null, "Home", "Index", "Tabler");

        Assert.Equal(4, list.Length);
        Assert.Equal("/Views/Home_Tabler/Index.cshtml", list[0]);
        Assert.Equal("/Views/Home/Index.cshtml", list[1]);
        Assert.Equal("/Views/Tabler/Index.cshtml", list[2]);
        Assert.Equal("/Views/Shared/Index.cshtml", list[3]);
    }

    [Fact]
    [DisplayName("主题为空时兜底默认主题")]
    public void GetCandidates_EmptyTheme_UseDefault()
    {
        var list = ViewLocationHelper.GetCandidates("Admin", "User", "Index", null);

        Assert.Equal("/Areas/Admin/Views/User_ACE/Index.cshtml", list[0]);
        Assert.Equal("/Views/ACE/Index.cshtml", list[4]);
        Assert.Equal("/Views/Shared/Index.cshtml", list[5]);
    }

    [Fact]
    [DisplayName("视图名为空时抛出参数异常")]
    public void GetCandidates_NullView_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ViewLocationHelper.GetCandidates("Admin", "User", null, "ACE"));
        Assert.Throws<ArgumentNullException>(() => ViewLocationHelper.GetCandidates("Admin", "User", "", "ACE"));
    }

    [Fact]
    [DisplayName("物理文件探测：相对内容根判断存在性")]
    public void IsPhysicalFile_ExistsAndNot()
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CubeViewHelper_" + Guid.NewGuid().ToString("N"));
        var dir = System.IO.Path.Combine(root, "Views", "Shared");
        System.IO.Directory.CreateDirectory(dir);
        var file = System.IO.Path.Combine(dir, "_List_Search.cshtml");
        System.IO.File.WriteAllText(file, "");

        try
        {
            Assert.True(ViewLocationHelper.IsPhysicalFile(root, "/Views/Shared/_List_Search.cshtml"));
            Assert.False(ViewLocationHelper.IsPhysicalFile(root, "/Views/Shared/_NotExists.cshtml"));
            Assert.False(ViewLocationHelper.IsPhysicalFile(null, "/Views/Shared/_List_Search.cshtml"));
            Assert.False(ViewLocationHelper.IsPhysicalFile(root, null));
        }
        finally
        {
            System.IO.Directory.Delete(root, true);
        }
    }
}
