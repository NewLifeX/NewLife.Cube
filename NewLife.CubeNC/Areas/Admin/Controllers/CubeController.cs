using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using NewLife.Serialization;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>系统设置控制器</summary>
[DisplayName("魔方设置")]
[AdminArea]
[Menu(30, true, Icon = "fa-wrench")]
public class CubeController : ConfigController<CubeSetting>
{
    private Boolean _has;
    private readonly UIService _uIService;

    /// <summary>AI 助手主题色方案。键为方案名，值为"主色,辅色"，首个为默认方案</summary>
    private static readonly Dictionary<String, String> _colorSchemes = new()
    {
        ["新生命绿"] = "#2ecc71,#1e8e3e",
        ["靛蓝紫"] = "#667eea,#764ba2",
        ["翠绿"] = "#10b981,#059669",
        ["天青蓝"] = "#0ea5e9,#0369a1",
        ["湖青"] = "#06b6d4,#0e7490",
        ["琥珀橙"] = "#f59e0b,#ea580c",
        ["玫瑰红"] = "#f43f5e,#be123c",
        ["藤萝紫"] = "#8b5cf6,#6d28d9",
        ["樱花粉"] = "#f472b6,#db2777",
        ["石墨黑"] = "#475569,#0f172a",
    };

    /// <summary>实例化</summary>
    /// <param name="uIService"></param>
    public CubeController(UIService uIService) => _uIService = uIService;

    /// <summary>执行前</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!_has)
        {
            var list = GetMembers(typeof(CubeSetting));
            var df = list.FirstOrDefault(e => e.Name == "Theme");
            if (df != null)
            {
                df.Description = $"可选主题 {_uIService.Themes.Join("/")}";
                df.DataSource = e => _uIService.Themes.ToDictionary(e => e, e => e);
            }

            df = list.FirstOrDefault(e => e.Name == "Skin");
            if (df != null)
            {
                df.Description = $"可选皮肤 {_uIService.Skins.Join("/")}";
                df.DataSource = e => _uIService.Skins.ToDictionary(e => e, e => e);
            }

            df = list.FirstOrDefault(e => e.Name == "EChartsTheme");
            if (df != null)
            {
                var themes = _uIService.GetEChartsThemes();
                df.Description = $"可选主题 {themes.Join("/")}";
                themes.Insert(0, "default");
                df.DataSource = e => themes.ToDictionary(e => e, e => e);
            }

            // AI 助手配色：主题色方案下拉联动主色/辅色，主色/辅色渲染为颜色选择器
            df = list.FirstOrDefault(e => e.Name == "AIColorScheme");
            if (df != null)
            {
                df.Description = "选择预设主题色方案后自动填充主色/辅色，可再手动微调";
                df.ItemType = "singleSelect";
                df.DataSource = e => _colorSchemes.Keys.ToDictionary(e => e, e => e);
                // 联动映射传给前端：方案名 → "主色,辅色"
                df.Properties["ColorMap"] = JsonHelper.ToJson(_colorSchemes);
                df.Properties["ColorTargets"] = "AIPrimaryColor,AISecondaryColor";
            }

            foreach (var name in new[] { "AIPrimaryColor", "AISecondaryColor" })
            {
                df = list.FirstOrDefault(e => e.Name == name);
                if (df != null) df.ItemType = "color";
            }

            _has = true;
        }

        base.OnActionExecuting(filterContext);

        PageSetting.NavView = "_Object_Nav";
    }

    /// <summary>
    /// 获取登录设置
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    public ActionResult GetLoginConfig() => Ok(data: new LoginConfigModel());

    /// <summary>更新时触发</summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override ActionResult Update(CubeSetting obj)
    {
        var rs = base.Update(obj);

        WebHelper2.FixTenantMenu();

        return rs;
    }
}