using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>短信配置</summary>
[AdminArea]
[Menu(0, false)]
public class SmsConfigController : EntityController<SmsConfig, SmsConfigModel>
{
    static SmsConfigController()
    {
        LogOnChange = true;

        ListFields.RemoveField("AppKey", "AppSecret", "SchemaName", "Remark");
        ListFields.RemoveCreateField();
        SearchFields.RemoveField("EnableLogin", "EnableReset", "EnableBind", "EnableNotify");
    }

    /// <summary>首页</summary>
    public override ActionResult Index(Pager p = null)
    {
        PageSetting.NavView = "_Object_Nav";

        return base.Index(p);
    }
}
