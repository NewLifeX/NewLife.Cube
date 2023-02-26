using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;

namespace NewLife.Cube.Admin.Controllers;

/// <summary>系统设置控制器</summary>
[DisplayName("魔方设置")]
[Area("Admin")]
[Menu(30, true, Icon = "fa-wrench")]
public class CubeController : ConfigController<Setting>
{
    private Boolean _has;
    private readonly UIService _uIService;

    /// <summary>实例化</summary>
    /// <param name="uIService"></param>
    public CubeController(UIService uIService) => _uIService = uIService;

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

    /// <summary>
    /// 获取登录设置
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    public ActionResult GetLoginConfig() => Ok(data: new LoginConfigModel());
}