using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Filters;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>设置控制器</summary>
[DisplayName("Basic settings")]
[AdminArea]
[Menu(0, false, Icon = "fa-bomb")]
public class CoreController : ConfigController<NewLife.Setting>
{
    /// <summary>已重载。语言字段使用全量语言下拉框</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        foreach (var name in new[] { nameof(NewLife.Setting.Language), nameof(NewLife.Setting.DefaultLanguage) })
        {
            var df = GetMembers(typeof(NewLife.Setting)).FirstOrDefault(e => e.Name == name);
            if (df != null)
            {
                df.ItemType = "singleSelect";
                df.DataSource = e => CubeLocalization.GetLanguageOptions();
            }
        }

        base.OnActionExecuting(filterContext);

        PageSetting.NavView = "_Object_Nav";
    }
}
