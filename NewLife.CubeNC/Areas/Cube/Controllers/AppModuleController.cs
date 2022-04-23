using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Modules;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Cube.Controllers
{
    /// <summary>应用插件管理</summary>
    [Area("Cube")]
    [Menu(20)]
    public class AppModuleController : EntityController<AppModule>
    {
        static AppModuleController()
        {
            LogOnChange = true;

            {
                var df = ListFields.AddListField("Log");
                df.DisplayName = "日志";
                df.Url = "../Admin/Log?category=应用插件&linkId={Id}";
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

            return AppModule.Search(start, end, p["Q"], p);
        }

        private static void ScanAppModule()
        {
            // 扫描本地目录所有协议插件
            var map = ModuleManager.ScanAll();
            if (map != null)
            {
                var list = AppModule.FindAll();
                foreach (var item in map)
                {
                    var drv = list.FirstOrDefault(e => e.Name.EqualIgnoreCase(item.Key));
                    if (drv == null)
                        drv = new AppModule { Name = item.Key, Enable = true };

                    if (drv.DisplayName.IsNullOrEmpty() || drv.DisplayName == drv.Name)
                    {
                        var dname = item.Value.GetDisplayName();
                        if (!dname.IsNullOrEmpty()) drv.DisplayName = dname;
                    }

                    if (drv.Type.IsNullOrEmpty()) drv.Type = ".NET";
                    if (drv.ClassName.IsNullOrEmpty()) drv.ClassName = item.Value.FullName;

                    if (drv.FilePath.IsNullOrEmpty()) drv.FilePath = item.Value.Assembly?.Location;
                    if (!drv.FilePath.IsNullOrEmpty())
                    {
                        var root = ".".GetFullPath();
                        if (drv.FilePath.StartsWithIgnoreCase(root))
                            drv.FilePath = drv.FilePath[root.Length..].TrimStart('/', '\\');
                    }

                    drv.Save();

                    list.Add(drv);
                }
            }
        }
    }
}