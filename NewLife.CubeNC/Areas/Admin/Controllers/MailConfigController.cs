using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Web;
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
        ListFields.RemoveCreateField();
    }

    /// <summary>首页</summary>
    public override ActionResult Index(Pager p = null)
    {
        if (p["nav"].ToInt() > 0)
        {
            PageSetting.NavView = "_Object_Nav";
            PageSetting.EnableNavbar = false;
        }

        return base.Index(p);
    }
}
