using NewLife.Cube.Entity;
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
    }
}
