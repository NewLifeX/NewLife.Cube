using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NewLife.Common;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Threading;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>首页</summary>
    [DisplayName("首页")]
    [Area("Admin")]
    [Menu(0, false, Icon = "fa-home")]
    public class IndexController : ControllerBaseX
    {
        private readonly IManageProvider _provider;
        private readonly IHostApplicationLifetime _applicationLifetime;

        static IndexController() => MachineInfo.RegisterAsync();

        private IndexController() => PageSetting.EnableNavbar = false;

        /// <summary>实例化</summary>
        /// <param name="manageProvider"></param>
        /// <param name="appLifetime"></param>
        public IndexController(IManageProvider manageProvider, IHostApplicationLifetime appLifetime) : this()
        {
            _provider = manageProvider;
            _applicationLifetime = appLifetime;
        }

        /// <summary>首页</summary>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult Index()
        {
            var user = _provider.TryLogin(HttpContext);
            if (user == null) return RedirectToAction("Login", "User", new
            {
                r = Request.GetEncodedPathAndQuery()
            });

            ViewBag.User = _provider.Current;
            ViewBag.Config = SysConfig.Current;

            // 工作台页面
            var startPage = Request.GetRequestValue("page");
            if (startPage.IsNullOrEmpty()) startPage = Setting.Current.StartPage;

            ViewBag.Main = startPage;
            ViewBag.Menus = GetMenu();

            return View("CubeIndex");
        }

        /// <summary>服务器信息</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DisplayName("服务器信息")]
        [EntityAuthorize(PermissionFlags.Detail)]
        [Menu(10, true, Icon = "fa-home")]
        public ActionResult Main(String id)
        {
            ViewBag.Act = id;
            ViewBag.Config = SysConfig.Current;
            ViewBag.MyAsms = GetMyAssemblies().OrderBy(e => e.Name).OrderByDescending(e => e.Compile).ToArray();

            var Asms = AssemblyX.GetAssemblies(null).ToArray();
            Asms = Asms.OrderBy(e => e.Name).OrderByDescending(e => e.Compile).ToArray();
            ViewBag.Asms = Asms;

            return ((id + "").ToLower()) switch
            {
                "processmodules" => View("ProcessModules"),
                "assembly" => View("Assembly"),
                "session" => View("Session"),
                "cache" => View("Cache"),
                "servervar" => View("ServerVar"),
                _ => View(),
            };
        }

        /// <summary>获取当前应用程序的所有程序集，不包括系统程序集，仅限本目录</summary>
        /// <returns></returns>
        public static List<AssemblyX> GetMyAssemblies()
        {
            var list = new List<AssemblyX>();
            var hs = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            var cur = AppDomain.CurrentDomain.BaseDirectory.CombinePath("../").GetFullPath();
            foreach (var asmx in AssemblyX.GetAssemblies())
            {
                // 加载程序集列表很容易抛出异常，全部屏蔽
                try
                {
                    if (asmx.FileVersion.IsNullOrEmpty()) continue;

                    var file = asmx.Asm.Location;
                    if (file.IsNullOrEmpty()) continue;

                    if (file.StartsWith("file:///"))
                    {
                        file = file.TrimStart("file:///");
                        if (Path.DirectorySeparatorChar == '\\')
                            file = file.Replace('/', '\\');
                        else
                            file = file.Replace('\\', '/').EnsureStart("/");
                    }
                    if (!file.StartsWithIgnoreCase(cur)) continue;

                    if (!hs.Contains(file))
                    {
                        hs.Add(file);
                        list.Add(asmx);
                    }
                }
                catch { }
            }
            return list;
        }

        /// <summary>重启</summary>
        /// <returns></returns>
        [DisplayName("重启")]
        [EntityAuthorize((PermissionFlags)16)]
        public ActionResult Restart()
        {
            var manager = ApplicationManager.Load();

            try
            {
                var p = Process.GetCurrentProcess();
                var fileName = p.MainModule.FileName;
                var args = Environment.CommandLine.TrimStart(Path.ChangeExtension(fileName, ".dll")).Trim();
                args += " -delay";

                WriteLog("Restart", true, $"fileName={fileName} args={args}");

                Process.Start(fileName, args);

                ThreadPool.QueueUserWorkItem(s =>
                {
                    Thread.Sleep(100);

                    // 本进程退出
                    manager.Stop();
                    Thread.Sleep(200);
                    //p.Kill();
                    Environment.Exit(0);
                });
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);

                manager.Restart();
            }

            return JsonRefresh("重启成功", 2);
        }

        /// <summary>
        /// 释放内存，参考之前的Runtime方法
        /// </summary>
        /// <returns></returns>
        [DisplayName("释放内存")]
        [EntityAuthorize((PermissionFlags)32)]
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
        private static extern Boolean SetProcessWorkingSetSize(IntPtr proc, Int32 min, Int32 max);

        /// <summary>
        /// 获取菜单树
        /// </summary>
        /// <returns></returns>
        [EntityAuthorize()]
        public ActionResult GetMenuTree() => Ok(data: GetMenu());

        private IList<MenuTree> GetMenu()
        {
            var user = _provider.Current as IUser ?? XCode.Membership.User.FindAll().FirstOrDefault();

            var fact = ManageProvider.Menu;
            var menus = fact.Root.Childs;
            if (user?.Role != null)
            {
                menus = fact.GetMySubMenus(fact.Root.ID, user, true);
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
}