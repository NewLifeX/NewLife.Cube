using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.AI;
using NewLife.Cube.ViewModels;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>系统设置控制器</summary>
[DisplayName("Settings center")]
[AdminArea]
[Menu(30, true, Icon = "Tools")]
public class CubeController : ConfigController<CubeSetting>, IPageDataContext
{
    private readonly CubeSkinManager _skinManager;

    /// <summary>实例化</summary>
    /// <param name="skinManager">前端皮肤管理器</param>
    public CubeController(CubeSkinManager skinManager) => _skinManager = skinManager;

    /// <summary>获取配置字段。SPA皮肤字段渲染为下拉，列出当前项目已引用的皮肤包</summary>
    /// <param name="kind">视图类型</param>
    /// <returns></returns>
    protected override IList<DataField> OnGetFields(ViewKinds kind)
    {
        var list = base.OnGetFields(kind);

        var df = list.FirstOrDefault(e => e.Name == nameof(CubeSetting.SpaSkin));
        if (df != null)
        {
            df.ItemType = "singleSelect";
            df.DataSource = _ => _skinManager.Skins.Values
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(e => e.Name, e => e.DisplayName);
        }

        return list;
    }

    //private Boolean _has;
    //private readonly UIService _uIService;

    ///// <summary>实例化</summary>
    ///// <param name="uIService"></param>
    //public CubeController(UIService uIService) => _uIService = uIService;

    ///// <summary>执行前</summary>
    ///// <param name="filterContext"></param>
    //public override void OnActionExecuting(ActionExecutingContext filterContext)
    //{
    //    if (!_has)
    //    {
    //        var list = GetMembers(typeof(Setting));
    //        var df = list.FirstOrDefault(e => e.Name == "Theme");
    //        if (df != null)
    //        {
    //            df.Description = $"可选主题 {_uIService.Themes.Join("/")}";
    //            df.DataSource = e => _uIService.Themes.ToDictionary(e => e, e => e);
    //        }

    //        df = list.FirstOrDefault(e => e.Name == "Skin");
    //        if (df != null)
    //        {
    //            df.Description = $"可选皮肤 {_uIService.Skins.Join("/")}";
    //            df.DataSource = e => _uIService.Skins.ToDictionary(e => e, e => e);
    //        }

    //        df = list.FirstOrDefault(e => e.Name == "EChartsTheme");
    //        if (df != null)
    //        {
    //            var themes = _uIService.GetEChartsThemes();
    //            df.Description = $"可选主题 {themes.Join("/")}";
    //            themes.Insert(0, "default");
    //            df.DataSource = e => themes.ToDictionary(e => e, e => e);
    //        }

    //        _has = true;
    //    }

    //    base.OnActionExecuting(filterContext);
    //}

    /// <summary>获取登录设置</summary>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    public ActionResult GetLoginConfig() => Json(0, null, new LoginConfigModel());

    /// <summary>收集当前页面数据上下文（魔方设置配置摘要），供 AI 分析当前页面。实现 <see cref="IPageDataContext"/>，get_page_context 优先调用服务端实现</summary>
    /// <returns>配置摘要 JSON。敏感配置（ApiKey/Secret/Token 等）不向 AI 暴露</returns>
    [HttpGet]
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

        var data = new { page = L.T("Cube settings"), description = L.T("Cube backend settings, grouped by category"), fields }.ToJson();
        return Task.FromResult(data);
    }
}