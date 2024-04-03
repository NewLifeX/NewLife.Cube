using System.ComponentModel;
using IoTServer;
using NewLife.Cube;
using NewLife.Cube.Cube;

namespace CubeDemo.Areas.School.Controllers;

/// <summary></summary>
[CubeArea]
[DisplayName("测试配置")]
public class TestConfigController : ConfigController<IoTSetting>
{
}
