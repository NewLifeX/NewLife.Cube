using System.ComponentModel;
using IoTServer;
using NewLife.Cube;

namespace CubeDemo.Areas.School.Controllers
{
    /// <summary></summary>
    [SchoolArea]
    [DisplayName("测试配置")]
    public class TestConfigController : ConfigController<IoTSetting>
    {
    }
}
