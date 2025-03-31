using NewLife.Cube.Entity;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>OAuth配置</summary>
[AdminArea]
[Menu(0, false)]
public class OAuthConfigController : EntityController<OAuthConfig, OAuthConfigModel>
{
    static OAuthConfigController()
    {
        LogOnChange = true;

        ListFields.RemoveField("Secret", "Logo", "AuthUrl", "AccessUrl", "UserUrl", "Remark");
    }
}