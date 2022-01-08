using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>字典参数</summary>
    [DisplayName("字典参数")]
    [Area("Admin")]
    [Menu(30, false, Icon = "fa-wrench")]
    public class ParameterController : EntityController<Parameter>
    {
        static ParameterController()
        {
            LogOnChange = true;

            ListFields.RemoveField("ID", "Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6");
            ListFields.RemoveCreateField();
        }
    }
}