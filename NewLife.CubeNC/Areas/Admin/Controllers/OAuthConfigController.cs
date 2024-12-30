using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>OAuth配置</summary>
[AdminArea]
[Menu(0, false)]
public class OAuthConfigController : EntityController<OAuthConfig>
{
    static OAuthConfigController()
    {
        LogOnChange = true;

        ListFields.RemoveField("Secret", "Logo", "Server", "Scope", "AccessServer", "AuthUrl", "AccessUrl", "UserUrl", "AppUrl", "SecurityKey", "FieldMap", "AutoRole", "Remark");
        ListFields.RemoveCreateField();
        SearchFields.RemoveField("Debug", "AutoRegister", "FetchAvatar");
    }

    /// <summary>首页</summary>
    public override ActionResult Index(Pager p = null)
    {
        PageSetting.NavView = "_Object_Nav";

        return base.Index(p);
    }
}