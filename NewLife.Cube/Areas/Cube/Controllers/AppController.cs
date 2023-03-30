using System.ComponentModel;
using NewLife.Cube.Entity;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>应用系统</summary>
[DisplayName("应用系统")]
[CubeArea]
[Menu(38, true, Icon = "fa-star")]
public class AppController : EntityController<App>
{
    static AppController()
    {
        LogOnChange = true;

        ListFields.RemoveField("ID", "Secret", "HomePage", "Logo", "White", "Black", "Urls", "Remark");
        ListFields.RemoveCreateField();
        ListFields.RemoveUpdateField();

        {
            var df = ListFields.AddListField("AppLog", "Enable");
            //df.Header = "日志";
            df.DisplayName = "日志";
            df.Url = "/Cube/AppLog?appId={Id}";
        }

        {
            var df = AddFormFields.GetField("RoleIds");
            df.DataSource = e => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
        }

        {
            var df = EditFormFields.GetField("RoleIds");
            df.DataSource = e => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
        }

        {
            var df = ListFields.AddListField("Log", "UpdateUserId");
            //df.Header = "修改日志";
            df.DisplayName = "修改日志";
            df.Url = "/Admin/Log?category=应用系统&linkId={ID}";
        }
    }
}