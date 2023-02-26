using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Common;
using NewLife.Log;
using NewLife.Reflection;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers;

/// <summary>首页</summary>
[DisplayName("首页")]
public class IndexController : ControllerBaseX
{
    /// <summary>菜单顺序。扫描是会反射读取</summary>
    protected static Int32 MenuOrder { get; set; } = 10;

    static IndexController() => MachineInfo.RegisterAsync();

    /// <summary>实例化</summary>
    public IndexController() => PageSetting.EnableNavbar = false;

    /// <summary>首页</summary>
    /// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    [AllowAnonymous]
    [HttpGet]
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
        return Ok();
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
        return Ok();
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

    /// <summary>菜单不可见</summary>
    /// <param name="menu"></param>
    /// <returns></returns>
    protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
    {
        if (menu.Visible)
        {
            menu.Visible = false;
            (menu as IEntity).Update();
        }

        return base.ScanActionMenu(menu);
    }
}