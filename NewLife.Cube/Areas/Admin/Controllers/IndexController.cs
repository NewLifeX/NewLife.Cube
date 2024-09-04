using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>首页</summary>
[DisplayName("首页")]
[AdminArea]
[Menu(0, false, Icon = "fa-home")]
public class IndexController : ControllerBaseX
{
    private readonly IManageProvider _provider;
    private readonly IWebHostEnvironment _env;

    static IndexController() => MachineInfo.RegisterAsync();

    /// <summary>实例化</summary>
    public IndexController(IManageProvider manageProvider, IWebHostEnvironment env)
    {
        _provider = manageProvider;
        _env=env;
        PageSetting.EnableNavbar = false;
    }

    /// <summary>首页</summary>
    /// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    [AllowAnonymous]
    [HttpGet("/[area]/[controller]")]
    public ActionResult Index()
    {
        var user = ManageProvider.Provider.TryLogin(HttpContext);
        if (user == null) return RedirectToAction("Login", "User", new { r = Request.Path + "" });

        //ViewBag.User = ManageProvider.User;
        //ViewBag.Config = SysConfig.Current;

        //// 工作台页面
        //var startPage = Request["page"];
        //if (startPage.IsNullOrEmpty()) startPage = Setting.Current.StartPage;

        //ViewBag.Main = startPage;

        //return View();
        return Json(0, "ok");
    }

    /// <summary>服务器信息</summary>
    /// <returns></returns>
    [DisplayName("服务器信息")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ActionResult Main()
    {
        var req = HttpContext.Request;
        var conn = HttpContext.Connection;
        var gc = $"IsServerGC={GCSettings.IsServerGC},LatencyMode={GCSettings.LatencyMode}";
        var mi = MachineInfo.Current ?? new MachineInfo();
        var process = Process.GetCurrentProcess();
        var asm = Assembly.GetExecutingAssembly();
        var att = asm.GetCustomAttribute<TargetFrameworkAttribute>();
        var ver = att?.FrameworkDisplayName ?? att?.FrameworkName;
        var addrLocal = conn.LocalIpAddress;
        var addrRemote = conn.RemoteIpAddress;
        if (addrLocal != null && addrLocal.IsIPv4MappedToIPv6) addrLocal = addrLocal.MapToIPv4();
        if (addrRemote != null && addrRemote.IsIPv4MappedToIPv6) addrRemote = addrRemote.MapToIPv4();
        var userHost = HttpContext.GetUserHost();
        var result = new
        {
            system = req.GetRawUrl().AbsoluteUri,
            path = _env.ContentRootPath,
            host = req.Headers["Host"],
            local = addrLocal + ":" + conn.LocalPort,
            remote = addrRemote + ":" + conn.RemotePort,
            application = process.ProcessName,
            applicationTitle = Environment.CommandLine,
            version = ver,
            os = mi.OSName,
            osVersion = mi.OSVersion,
            machineId = mi.UUID,
            machineProduct = mi.Product,
            cpu = mi.Processor + Environment.ProcessorCount + "核心 使用率" + mi.CpuRate.ToString("p0") + mi.Temperature + " ℃",
            openTime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss"),
            serverTime = DateTime.Now,
            memory = "物理：" + (mi.AvailableMemory / 1024 / 1024).ToString("n0") + "M/" + (mi.Memory / 1024 / 1024).ToString("n0") + "M    工作/提交: " + (process.WorkingSet64 / 1024 / 1024).ToString("n0") + "M/@" + (process.PrivateMemorySize64 / 1024 / 1024).ToString("n0") + "M   GC: " + (GC.GetTotalMemory(false) / 1024 / 1024).ToString("n0") + "M",
            processTime = process.TotalProcessorTime.TotalSeconds.ToString("N2") + "秒 启动于" + process.StartTime.ToLocalTime().ToFullString(),
            gc = gc,
            //startTime = ApplicationManager.Load().StartTime.ToLocalTime().ToFullString()
        };
        return Json(0,null,result);
    }

    /// <summary>服务器变量列表</summary>
    /// <returns></returns>
    [DisplayName("服务器变量列表")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ActionResult ServerVarList()
    {
        var req = HttpContext.Request;
        var list=new List<dynamic>();
        foreach(var kv in req.Headers)
        {
            var v = kv.Value.ToString();
            var key = kv.Key;
            list.Add(new { name=key, value=v });
        }
        var rqlist = new List<dynamic>();
        foreach (var pi in req.GetType().GetProperties())
        {
            var type = pi.PropertyType;
            if (pi.GetIndexParameters().Length > 0 || (type != typeof(String)
                                                  && type != typeof(Uri)
                                                  && type != typeof(PathString)
                                                  && type != typeof(HostString)
                                                  && !typeof(Boolean).IsAssignableFrom(type)
                                                  && !typeof(String).IsAssignableFrom(type)))
            {
                continue;
            }
            rqlist.Add(new { name = pi.Name, value = req.GetValue(pi) });
        }
            return Json(0,null,new {server=list,requestName= req.GetType().FullName, request=rqlist});
    }
    /// <summary>进程模块列表</summary>
    /// <param name="model">All全部,OnlyUser用户</param>
    /// <returns></returns>
    [DisplayName("进程模块列表")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ActionResult ProcessList(String model)
    {
        var isAll = String.Equals("All", model, StringComparison.OrdinalIgnoreCase);
        var process = Process.GetCurrentProcess();
        var result = new List<dynamic>();
        foreach (ProcessModule item in process.Modules)
        {
            try
            {
                if (isAll || item.FileVersionInfo.CompanyName != "Microsoft Corporation")
                {
                    result.Add(new
                    {
                        name = item.ModuleName,
                        companyName = item.FileVersionInfo.CompanyName,
                        productName = item.FileVersionInfo.ProductName,
                        description = item.FileVersionInfo.FileDescription,
                        version = item.FileVersionInfo.FileVersion,
                        size = item.ModuleMemorySize,
                        fileName = item.FileName
                    });
                }
            }
            catch { }
        }
        return Json(0, null, result);
    }

    /// <summary>程序集列表</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [DisplayName("程序集列表")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ActionResult AssemblyList(String model)
    {
        var isAll = String.Equals("All", model, StringComparison.OrdinalIgnoreCase);
        var result =new List<dynamic>();
        AssemblyX[] asms = null;
        if(isAll)
           asms=AssemblyX.GetAssemblies(null).OrderBy(e => e.Name).OrderByDescending(e => e.Compile).ToArray();
        else
            asms = AssemblyX.GetMyAssemblies().OrderBy(e => e.Name).OrderByDescending(e => e.Compile).ToArray();
        foreach (var assembly in asms)
        {
            result.Add(new
            {
                name = assembly.Name,
                title = assembly.Title,
                fileVersion = assembly.FileVersion,
                version = assembly.Version,
                compileTime = assembly.Compile.ToFullString(),
                location = assembly.Asm.Location
            });
        }
        return Json(0,null, result);
    }

    /// <summary>重启</summary>
    /// <returns></returns>
    [DisplayName("重启")]
    [EntityAuthorize((PermissionFlags)16)]
    [HttpPost]
    public ActionResult Restart()
    {
        //try
        {
            //AppDomain.Unload(AppDomain.CurrentDomain);
            //HttpContext.User = null;
            //HttpRuntime.UnloadAppDomain();
            //HostingEnvironment.InitiateShutdown();
            //ApplicationManager.GetApplicationManager().ShutdownAll();
            // 通过修改web.config时间来重启站点，稳定可靠
            var wc = "web.config".GetFullPath();
            System.IO.File.SetLastWriteTime(wc, DateTime.Now);
        }
        //catch { }

        return RedirectToAction(nameof(Main));
    }

    /// <summary>
    /// 释放内存，参考之前的Runtime方法
    /// </summary>
    /// <returns></returns>
    [DisplayName("释放内存")]
    [EntityAuthorize((PermissionFlags)16)]
    [HttpGet]
    public ActionResult MemoryFree()
    {
        try
        {
            GC.Collect();

            // 释放当前进程所占用的内存
            var p = Process.GetCurrentProcess();
            SetProcessWorkingSetSize(p.Handle, -1, -1);
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }

        return RedirectToAction(nameof(Main));
    }

    [DllImport("kernel32.dll")]
    static extern Boolean SetProcessWorkingSetSize(IntPtr proc, Int32 min, Int32 max);

    /// <summary>
    /// 获取菜单树
    /// </summary>
    /// <returns></returns>
    [EntityAuthorize]
    [HttpGet]
    public ActionResult GetMenuTree(String module) => Json(0, null, GetMenu(module));

    private IList<MenuTree> GetMenu(String module)
    {
        var user = _provider.Current as IUser;

        var fact = ManageProvider.Menu;
        var menus = fact.Root.Childs;
        if (user?.Role != null)
        {
            menus = fact.GetMySubMenus(fact.Root.ID, user, true);
        }

        // 根据模块过滤菜单
        if (module.EqualIgnoreCase("base"))
        {
            // 直接取base下级，以及所有仅有二级的菜单
            var ms = menus.FirstOrDefault(e => e.Name.EqualIgnoreCase("base"))?.Childs ?? [];
            foreach (var item in menus)
            {
                if (!item.Name.EqualIgnoreCase("base") && item.Childs.All(e => e.Childs.Count == 0))
                {
                    ms.Add(item);
                }
            }
            menus = ms;
        }
        else if (!module.IsNullOrEmpty())
        {
            menus = menus.FirstOrDefault(e => e.Name.EqualIgnoreCase(module))?.Childs ?? [];
        }
        else
        {
            // 去掉三级菜单，仅显示二级菜单。如果没有可用菜单，则取第一个有可访问子菜单的模块来显示
            var ms = menus.Where(e => e.Childs.All(x => x.Childs.Count == 0)).ToList() as IList<IMenu>;
            if (ms.Count == 0)
            {
                foreach (var item in menus)
                {
                    ms = fact.GetMySubMenus(item.ID, user, true);
                    if (ms.Count > 0) break;
                }
            }

            menus = ms;
        }

        // 如果顶级只有一层，并且至少有三级目录，则提升一级
        if (menus.Count == 1 && menus[0].Childs.All(m => m.Childs.Count > 0)) { menus = menus[0].Childs; }

        var menuTree = MenuTree.GetMenuTree(pMenuTree =>
        {
            var subMenus = fact.GetMySubMenus(pMenuTree.ID, user, true);
            return subMenus;
        }, list =>
        {

            var menuList = (from menu in list
                                // where m.Visible
                            select new MenuTree
                            {
                                ID = menu.ID,
                                Name = menu.Name,
                                DisplayName = menu.DisplayName ?? menu.Name,
                                FullName = menu.FullName,
                                Url = menu.Url,
                                Icon = menu.Icon,
                                Visible = menu.Visible,
                                NewWindow = menu.NewWindow,
                                ParentID = menu.ParentID,
                                Permissions = menu.Permissions
                            }).ToList();
            return menuList.Count > 0 ? menuList : null;
        }, menus);

        return menuTree;
    }
}