using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NewLife.Common;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.Model;
using NewLife.Reflection;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>首页</summary>
    [DisplayName("首页")]
    [Area("Admin")]
    public class IndexController : ControllerBaseX
    {
        /// <summary>菜单顺序。扫描是会反射读取</summary>
        protected static Int32 MenuOrder { get; set; }

        private IManageProvider _Provider;

        static IndexController()
        {
            MenuOrder = 10;
        }

        private IndexController() { }

        /// <summary>实例化</summary>
        /// <param name="manageProvider"></param>
        public IndexController(IManageProvider manageProvider) => _Provider = manageProvider;

        /// <summary>首页</summary>
        /// <returns></returns>
        //[EntityAuthorize(PermissionFlags.Detail)]
        [AllowAnonymous]
        public ActionResult Index()
        {
            var user = ManagerProviderHelper.TryLogin(_Provider, HttpContext.RequestServices);
            if (user == null) return RedirectToAction("Login", "User", new
            {
                r = Request.GetEncodedPathAndQuery()
                //.Url.PathAndQuery
            });

            ViewBag.User = _Provider.Current;
            ViewBag.Config = SysConfig.Current;

            // 工作台页面
            var startPage = Request.GetRequestValue("page");
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
            ViewBag.User = _Provider.Current;
            ViewBag.Config = SysConfig.Current;

            var name = Request.Headers["Server_SoftWare"];
            if (String.IsNullOrEmpty(name)) name = Process.GetCurrentProcess().ProcessName;

            // 检测集成管道，低版本.Net不支持，请使用者根据情况自行注释
            try
            {
                //if (HttpRuntime.UsingIntegratedPipeline) name += " [集成管道]";
            }
            catch { }

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
    }
}