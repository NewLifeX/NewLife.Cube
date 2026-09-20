using NewLife.Cube;
using Xunit;

namespace NewLife.Cube.Tests;

public class ShareRequestAllowTests
{
    [Fact(DisplayName = "工作台分享页路径识别 /home 与 /Workbench/{slug}")]
    public void IsWorkbenchSharePage()
    {
        Assert.True(ManagerProviderHelper.IsWorkbenchSharePage("/home"));
        Assert.True(ManagerProviderHelper.IsWorkbenchSharePage("/Workbench/ops"));
        Assert.False(ManagerProviderHelper.IsWorkbenchSharePage("/Admin/User"));
        Assert.False(ManagerProviderHelper.IsWorkbenchSharePage(""));
    }

    [Fact(DisplayName = "工作台分享令牌可访问 /Cube/Workbench，列表分享令牌不可")]
    public void WorkbenchShare_AllowsWorkbenchApi()
    {
        Assert.True(ManagerProviderHelper.IsShareRequestAllowed("/Cube/Workbench", "/home"));
        Assert.True(ManagerProviderHelper.IsShareRequestAllowed("/Cube/Workbench/Named/ops", "/Workbench/ops"));
        Assert.True(ManagerProviderHelper.IsShareRequestAllowed("/Cube/Widget/Query", "/home"));
        Assert.False(ManagerProviderHelper.IsShareRequestAllowed("/Cube/Workbench", "/Admin/User"));
        Assert.True(ManagerProviderHelper.IsShareRequestAllowed("/Admin/User/GetPage", "/Admin/User"));
    }
}
