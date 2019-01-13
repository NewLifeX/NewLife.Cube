using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
#if __CORE__
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Extensions;
#else
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
#endif

using NewLife.Common;
using NewLife.Log;
using NewLife.Reflection;
using XCode;
using XCode.Membership;
using NewLife.Cube.ViewModels;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>首页</summary>
    [DisplayName("首页")]
    [Area("Admin")]
    public class IndexController : ControllerBaseX
    {
        /// <summary>菜单顺序。扫描是会反射读取</summary>
        protected static Int32 MenuOrder { get; set; } = 10;

#if __CORE__
        private IManageProvider _Provider;

        private IndexController() { }

        /// <summary>实例化</summary>
        /// <param name="manageProvider"></param>
        public IndexController(IManageProvider manageProvider) => _Provider = manageProvider;
#endif

        /// <summary>首页</summary>
        /// <returns></returns>
        //[EntityAuthorize(PermissionFlags.Detail)]
        [AllowAnonymous]
#if !__CORE__
        [RequireSsl]
#endif
        public ActionResult Index()
        {
#if __CORE__
            var user = ManagerProviderHelper.TryLogin(_Provider, HttpContext.RequestServices);
#else
            var user = ManageProvider.Provider.TryLogin();
#endif
            if (user == null) return RedirectToAction("Login", "User", new
            {
#if __CORE__
                r = Request.GetEncodedPathAndQuery()
#else
                r = Request.Url.PathAndQuery
#endif
            });

#if __CORE__
            ViewBag.User = _Provider.Current;
#else
            ViewBag.User = ManageProvider.User;
#endif
            ViewBag.Config = SysConfig.Current;

            // 工作台页面
#if __CORE__
            var startPage = Request.GetRequestValue("page");
#else
            var startPage = Request["page"];
#endif
            if (startPage.IsNullOrEmpty()) startPage = Setting.Current.StartPage;

            ViewBag.Main = startPage;
            ViewBag.Menus = GetMenu();

            return View();
        }

        /// <summary>服务器信息</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName("服务器信息")]
        [EntityAuthorize(PermissionFlags.Detail)]
        public ActionResult Main(String id)
        {
            //if (id == "Restart")
            //{
            //    HttpRuntime.UnloadAppDomain();
            //    id = null;
            //}

            ViewBag.Act = id;
            //ViewBag.User = ManageProvider.User;
            ViewBag.Config = SysConfig.Current;
#if __CORE__
            var name = Request.Headers["Server_SoftWare"];
#else
            var name = Request.ServerVariables["Server_SoftWare"];
#endif
            if (String.IsNullOrEmpty(name)) name = Process.GetCurrentProcess().ProcessName;

#if !__CORE__
            // 检测集成管道，低版本.Net不支持，请使用者根据情况自行注释
            try
            {
                if (HttpRuntime.UsingIntegratedPipeline) name += " [集成管道]";
            }
            catch { }
#endif

            ViewBag.WebServerName = name;
            ViewBag.MyAsms = AssemblyX.GetMyAssemblies().OrderBy(e => e.Name).OrderByDescending(e => e.Compile).ToArray();

            var Asms = AssemblyX.GetAssemblies(null).ToArray();
            Asms = Asms.OrderBy(e => e.Name).OrderByDescending(e => e.Compile).ToArray();
            ViewBag.Asms = Asms;

            //return View();
            switch ((id + "").ToLower())
            {
                case "processmodules": return View("ProcessModules");
                case "assembly": return View("Assembly");
                case "session": return View("Session");
                case "cache": return View("Cache");
                case "servervar": return View("ServerVar");
                default: return View();
            }
        }

        /// <summary>重启</summary>
        /// <returns></returns>
        [DisplayName("重启")]
        [EntityAuthorize((PermissionFlags)16)]
        public ActionResult Restart()
        {
            //System.Web.HttpContext.Current.User = null;
            //try
            //{
            //    Process.GetCurrentProcess().Kill();
            //}
            //catch { }
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
        // [EntityAuthorize(PermissionFlags.Detail)]
        public List<MenuTree> GetMenu()
        {
            var user = _Provider.Current as IUser ?? XCode.Membership.UserX.FindAll().FirstOrDefault();

            //var fact = ObjectContainer.Current.Resolve<IMenuFactory>();
            var fact = ManageProvider.Menu;
            //fact.FindByID(1);
            //(fact as IEntityOperate)?.FindByKey(1);

            var menus = fact.Root.Childs;
            if (user?.Role != null)
            {
                menus = fact.GetMySubMenus(fact.Root.ID, user);
            }

            // 如果顶级只有一层，并且至少有三级目录，则提升一级
            if (menus.Count == 1 && menus[0].Childs.All(m => m.Childs.Count > 0)) { menus = menus[0].Childs; }

            menus = menus.Where(m => m.Visible).ToList();

            var menuTree = MenuTree.GetMenuTree(pMenuTree =>
            {
                return fact.GetMySubMenus(pMenuTree.ID, user).Where(m => m.Visible).ToList();
            }, list =>
            {

                var menuList = (from menu in list
                                    // where m.Visible
                                select new MenuTree
                                {
                                    ID = menu.ID,
                                    Name = menu.DisplayName,
                                    Url = Url.Content(menu.Url),
                                    Icon = menu.Icon,
                                    Class = ""
                                }).ToList();
                return menuList.Count > 0 ? menuList : null;
            }, menus.ToList());

            //var childs = menuTree[0].Children;
            return menuTree;
        }

        /// <summary>菜单不可见</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            if (menu.Visible)
            {
                menu.Visible = false;
                (menu as IEntity).Save();
            }

            return base.ScanActionMenu(menu);
        }
    }
}