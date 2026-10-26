using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using NewLife.Cube;
using Xunit;

namespace XUnitTest;

/// <summary>主题视图路径搜索扩展单元测试</summary>
/// <remarks>
/// 覆盖视图覆写机制在真实视图引擎中的接入点：主题值来源（Theme/Skin 特殊处理）与主题路径在默认查找模式中的插入位置。
/// 与 ViewLocationHelperTests（候选路径优先级）互补，共同保证“主题级覆写优先于共享兜底”。
/// </remarks>
public class ThemeViewLocationExpanderTests
{
    #region 辅助

    /// <summary>构造视图定位扩展上下文</summary>
    /// <param name="area">区域名</param>
    /// <param name="controller">控制器名</param>
    /// <param name="action">动作名，同时作为视图名</param>
    /// <returns>视图定位扩展上下文</returns>
    private static ViewLocationExpanderContext CreateContext(String area, String controller, String action)
    {
        var descriptor = new ControllerActionDescriptor
        {
            ControllerName = controller,
            ActionName = action,
        };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor);

        // 真实管线由 MVC 在调用前初始化 Values，手工构造时需自行补上
        return new ViewLocationExpanderContext(actionContext, action, controller, area, null, true)
        {
            Values = new Dictionary<String, String?>(),
        };
    }

    /// <summary>临时修改魔方主题与首页皮肤配置，执行断言后恢复</summary>
    /// <param name="theme">内容页主题</param>
    /// <param name="skin">首页皮肤</param>
    /// <param name="action">断言逻辑</param>
    private static void WithTheme(String theme, String skin, Action action)
    {
        var set = CubeSetting.Current;
        var oldTheme = set.Theme;
        var oldSkin = set.Skin;
        try
        {
            set.Theme = theme;
            set.Skin = skin;
            action();
        }
        finally
        {
            set.Theme = oldTheme;
            set.Skin = oldSkin;
        }
    }

    /// <summary>默认视图查找模式（与 Razor 视图引擎内置顺序一致）</summary>
    private static String[] DefaultViewLocations() =>
    [
        "/Areas/{2}/Views/{1}/{0}.cshtml",
        "/Areas/{2}/Views/Shared/{0}.cshtml",
        "/Views/Shared/{0}.cshtml",
    ];

    #endregion

    #region 主题来源

    [Fact]
    [DisplayName("主题为空时回退默认主题 ACE")]
    public void PopulateValues_EmptyTheme_FallsBackToAce()
    {
        WithTheme("", "", () =>
        {
            var context = CreateContext("Admin", "User", "Index");

            new ThemeViewLocationExpander().PopulateValues(context);

            Assert.Equal("ACE", context.Values["theme"]);
        });
    }

    [Fact]
    [DisplayName("主题取值来自魔方设置")]
    public void PopulateValues_CustomTheme_FromSetting()
    {
        WithTheme("Tabler", "", () =>
        {
            var context = CreateContext("Admin", "User", "Index");

            new ThemeViewLocationExpander().PopulateValues(context);

            Assert.Equal("Tabler", context.Values["theme"]);
        });
    }

    [Fact]
    [DisplayName("后台首页取首页皮肤而非内容页主题")]
    public void PopulateValues_AdminIndex_UsesSkin()
    {
        WithTheme("Tabler", "layui", () =>
        {
            var context = CreateContext("Admin", "Index", "Index");

            new ThemeViewLocationExpander().PopulateValues(context);

            Assert.Equal("layui", context.Values["theme"]);
        });
    }

    [Fact]
    [DisplayName("后台首页之外的动作仍取内容页主题")]
    public void PopulateValues_AdminIndexOtherAction_UsesTheme()
    {
        WithTheme("Tabler", "layui", () =>
        {
            var context = CreateContext("Admin", "Index", "Main");

            new ThemeViewLocationExpander().PopulateValues(context);

            Assert.Equal("Tabler", context.Values["theme"]);
        });
    }

    #endregion

    #region 路径插入

    [Fact]
    [DisplayName("主题路径插入到各默认路径之前，保持优先级")]
    public void ExpandViewLocations_InsertsThemePathsBeforeAnchors()
    {
        var context = CreateContext("Admin", "User", "Index");
        context.Values["theme"] = "ACE";

        var result = new ThemeViewLocationExpander().ExpandViewLocations(context, DefaultViewLocations()).ToList();

        Assert.Equal(6, result.Count);
        Assert.Equal("/Areas/{2}/Views/{1}_ACE/{0}.cshtml", result[0]);
        Assert.Equal("/Areas/{2}/Views/{1}/{0}.cshtml", result[1]);
        Assert.Equal("/Areas/{2}/Views/ACE/{0}.cshtml", result[2]);
        Assert.Equal("/Areas/{2}/Views/Shared/{0}.cshtml", result[3]);
        Assert.Equal("/Views/ACE/{0}.cshtml", result[4]);
        Assert.Equal("/Views/Shared/{0}.cshtml", result[5]);
    }

    [Fact]
    [DisplayName("主题为空时保持默认查找模式不变")]
    public void ExpandViewLocations_EmptyTheme_ReturnsOriginal()
    {
        var context = CreateContext("Admin", "User", "Index");
        context.Values["theme"] = "";

        var result = new ThemeViewLocationExpander().ExpandViewLocations(context, DefaultViewLocations()).ToList();

        Assert.Equal(DefaultViewLocations(), result);
    }

    [Fact]
    [DisplayName("默认模式缺少锚点时原样返回")]
    public void ExpandViewLocations_UnmatchedAnchors_LeftUntouched()
    {
        var context = CreateContext("Admin", "User", "Index");
        context.Values["theme"] = "ACE";
        String[] locations = ["/Views/Home/{0}.cshtml"];

        var result = new ThemeViewLocationExpander().ExpandViewLocations(context, locations).ToList();

        Assert.Equal(locations, result);
    }

    #endregion
}
