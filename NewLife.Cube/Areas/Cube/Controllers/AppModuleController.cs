using NewLife.Cube.Entity;
using NewLife.Cube.Modules;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>应用插件管理</summary>
/// <remarks>
/// 与 MVC 版保持一致：列表页由 <see cref="EntityController{TEntity, TModel}"/> 元数据驱动，
/// 首次查询时扫描本地插件目录，把模块/适配器插件合并进 <see cref="AppModule"/> 表。
/// </remarks>
[CubeArea]
[Menu(36, true, Icon = "Box")]
public class AppModuleController : EntityController<AppModule, AppModuleModel>
{
    static AppModuleController()
    {
        LogOnChange = true;

        ListFields.RemoveField("UpdateUserId", "UpdateIP", "Remark").RemoveCreateField();

        {
            var df = ListFields.AddListField("Log", null, "Enable");
            df.DisplayName = "Log";
            df.Url = "/Admin/Log?category=App plugin&linkId={Id}";
            df.Target = "_blank";
        }
    }

    private static Boolean _inited;

    /// <summary>高级搜索</summary>
    /// <param name="p">分页参数</param>
    /// <returns>插件列表</returns>
    protected override IEnumerable<AppModule> Search(Pager p)
    {
        if (!_inited)
        {
            ScanAppModule();

            _inited = true;
        }

        return base.Search(p);
    }

    /// <summary>扫描本地目录的协议插件与适配器插件</summary>
    private static void ScanAppModule()
    {
        var list = AppModule.FindAll();

        // 扫描本地目录所有协议插件
        var map = ModuleManager.ScanAllModules();
        foreach (var item in map)
        {
            ModuleManager.Merge(item.Key, item.Value, list, "Module");
        }

        // 扫描适配器插件
        var map2 = ModuleManager.ScanAllAdapters();
        foreach (var item in map2)
        {
            ModuleManager.Merge(item.Key, item.Value, list, "Adapter");
        }
    }
}
