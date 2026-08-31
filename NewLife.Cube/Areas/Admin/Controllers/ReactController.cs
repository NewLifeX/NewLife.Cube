using System.ComponentModel;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>React皮肤设置控制器</summary>
[DisplayName("React皮肤设置")]
[Description("React前端皮肤专属配置，如表单风格等，数据存数据库参数字典表")]
[AdminArea]
[Menu(31, true, Icon = "Skin")]
public class ReactController : ConfigController<ReactSetting>
{
}
