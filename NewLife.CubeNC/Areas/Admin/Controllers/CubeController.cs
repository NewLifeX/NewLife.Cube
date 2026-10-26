using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife;
using NewLife.Cube.AI;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>系统设置控制器</summary>
[DisplayName("Cube settings")]
[AdminArea]
[Menu(30, true, Icon = "fa-wrench")]
public class CubeController : ConfigController<CubeSetting>, IPageDataContext
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
            if (df != null) df.DataSource = e => _uIService.Themes.ToDictionary(e => e, e => e);

            df = list.FirstOrDefault(e => e.Name == "Skin");
            if (df != null) df.DataSource = e => _uIService.Skins.ToDictionary(e => e, e => e);

            df = list.FirstOrDefault(e => e.Name == "EChartsTheme");
            if (df != null)
            {
                var themes = _uIService.GetEChartsThemes();
                themes.Insert(0, "default");
                df.DataSource = e => themes.ToDictionary(e => e, e => e);
            }

            // 表格分隔样式与行高密度：下拉选择，ACE 主题经 _Layout 输出 body class 供 ace-ui.css 消费
            df = list.FirstOrDefault(e => e.Name == "TableStyle");
            if (df != null)
            {
                df.DataSource = e => new Dictionary<String, String>
                {
                    ["Light"] = L.T("Light - minimal thin lines"),
                    ["Standard"] = L.T("Standard - clear row lines"),
                    ["Grid"] = L.T("Grid - full cell borders"),
                };
            }

            df = list.FirstOrDefault(e => e.Name == "TableDensity");
            if (df != null)
            {
                df.DataSource = e => new Dictionary<String, String>
                {
                    ["Compact"] = L.T("Compact - dense"),
                    ["Normal"] = L.T("Normal - relaxed"),
                };
            }

            // AI 助手配色：主题色方案下拉联动主色/辅色，主色/辅色渲染为颜色选择器
            df = list.FirstOrDefault(e => e.Name == "AIColorScheme");
            if (df != null)
            {
                df.ItemType = "singleSelect";
                // 方案名同时是落库值与联动映射键，只在显示上按语言翻译
                df.DataSource = e => CubeSetting.ColorSchemes.Keys.ToDictionary(e => e, e => L.T(e));
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

    /// <summary>收集当前页面数据上下文（魔方设置配置摘要），供 AI 分析当前页面。实现 <see cref="IPageDataContext"/>，get_page_context 优先调用服务端实现</summary>
    /// <returns>配置摘要 JSON。敏感配置（ApiKey/Secret/Token 等）不向 AI 暴露</returns>
    public Task<String> GetPageDataContextAsync()
    {
        var set = CubeSetting.Current;
        var fields = new List<Object>();
        foreach (var fi in GetMembers(typeof(CubeSetting)))
        {
            // 敏感配置不向 AI 暴露（ApiKey/Secret/Token/连接串等）
            if (AiFormHelper.IsSensitiveField(fi.Name)) continue;

            var value = set.GetValue(fi.Name);
            fields.Add(new
            {
                name = fi.Name,
                displayName = fi.DisplayName,
                description = fi.Description,
                category = fi.Category,
                value = value?.ToString() ?? "",
            });
        }

        var data = new { page = "魔方设置", description = "魔方后台系统配置，可按分类查看各项设置项", fields }.ToJson();
        return Task.FromResult(data);
    }
}