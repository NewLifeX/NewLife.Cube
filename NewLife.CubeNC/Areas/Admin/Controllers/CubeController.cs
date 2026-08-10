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
                df.DataSource = e => CubeSetting.ColorSchemes.Keys.ToDictionary(e => e, e => e);
                // 联动映射传给前端：方案名 → "主色,辅色"
                df.Properties["ColorMap"] = JsonHelper.ToJson(CubeSetting.ColorSchemes);
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
        // AI 配色联动兜底：主题方案与原来不同时，主色/辅色自动采用新方案颜色（前端联动失效时保底）
        var old = CubeSetting.Current;
        if (old != null && old.AIColorScheme != obj.AIColorScheme)
            obj.ApplyColorScheme(old.AIColorScheme);

        var rs = base.Update(obj);

        WebHelper2.FixTenantMenu();

        return rs;
    }
}