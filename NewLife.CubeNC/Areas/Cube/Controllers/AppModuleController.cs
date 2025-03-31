using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Modules;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>应用插件管理</summary>
[CubeArea]
[Menu(36)]
public class AppModuleController : EntityController<AppModule, AppModuleModel>
{
    static AppModuleController()
    {
        LogOnChange = true;

        ListFields.RemoveField("UpdateUserId", "UpdateIP", "Remark")
            .RemoveCreateField();

        {
            var df = ListFields.AddListField("Log");
            df.DisplayName = "日志";
            df.Url = "/Admin/Log?category=应用插件&linkId={Id}";
            df.Target = "_blank";
        }
    }

    private static Boolean _inited;
    /// <summary>
    /// 高级搜索
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<AppModule> Search(Pager p)
    {
        if (!_inited)
        {
            ScanAppModule();

            _inited = true;
        }

        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        //return AppModule.Search(start, end, p["Q"], p);
        return base.Search(p);
    }

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
        foreach (var item in ModuleManager.ScanAllAdapters())
        {
            ModuleManager.Merge(item.Key, item.Value, list, "Adapter");
        }
    }
}