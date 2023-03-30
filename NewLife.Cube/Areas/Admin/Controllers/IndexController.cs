using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>首页</summary>
[AdminArea]
[DisplayName("首页")]
public class IndexController : ControllerBaseX
{
    private readonly IManageProvider _provider;

    static IndexController() => MachineInfo.RegisterAsync();

    /// <summary>实例化</summary>
    public IndexController(IManageProvider manageProvider)
    {
        _provider = manageProvider;

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
    /// <param name="id"></param>
    /// <returns></returns>
    [DisplayName("服务器信息")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ActionResult Main(String id)
    {
        //ViewBag.Act = id;
        //ViewBag.Config = SysConfig.Current;

        //var name = Process.GetCurrentProcess().ProcessName;

        //ViewBag.WebServerName = name;
        //ViewBag.MyAsms = AssemblyX.GetMyAssemblies().OrderBy(e => e.Name).OrderByDescending(e => e.Compile).ToArray();

        //var Asms = AssemblyX.GetAssemblies(null).ToArray();
        //Asms = Asms.OrderBy(e => e.Name).OrderByDescending(e => e.Compile).ToArray();
        //ViewBag.Asms = Asms;

        ////return View();
        //switch ((id + "").ToLower())
        //{
        //    case "processmodules": return View("ProcessModules");
        //    case "assembly": return View("Assembly");
        //    case "session": return View("Session");
        //    case "cache": return View("Cache");
        //    case "servervar": return View("ServerVar");
        //    case "memoryfree": return View("MemoryFree");
        //    default: return View();
        //}
        return Json(0, "ok");
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
    public ActionResult GetMenuTree() => Json(0, null, GetMenu());

    private IList<MenuTree> GetMenu()
    {
        var user = _provider.Current as IUser;

        var fact = ManageProvider.Menu;
        var menus = fact.Root.Childs;
        if (user?.Role != null)
            menus = fact.GetMySubMenus(fact.Root.ID, user, true);

        // 如果顶级只有一层，并且至少有三级目录，则提升一级
        if (menus.Count == 1 && menus[0].Childs.All(m => m.Childs.Count > 0)) menus = menus[0].Childs; 
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