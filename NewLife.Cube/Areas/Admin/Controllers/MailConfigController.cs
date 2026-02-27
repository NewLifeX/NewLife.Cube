using NewLife.Cube.Entity;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>邮件配置</summary>
[AdminArea]
[Menu(0, false)]
public class MailConfigController : EntityController<MailConfig, MailConfigModel>
{
    static MailConfigController()
    {
        LogOnChange = true;

        ListFields.RemoveField("UserName", "Password", "FromMail", "FromName", "Remark");
    }
}
