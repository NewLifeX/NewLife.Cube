using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Web;
using System.Web.Mvc;
using NewLife.Common;
using NewLife.Reflection;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>首页</summary>
    [DisplayName("首页")]
    public class IndexController : ControllerBaseX
    {
        /// <summary>菜单顺序。扫描是会反射读取</summary>
        protected static Int32 MenuOrder { get; set; }

        static IndexController()
        {
            MenuOrder = 10;
        }

        /// <summary>首页</summary>
        /// <returns></returns>
        //[EntityAuthorize(PermissionFlags.Detail)]
        [AllowAnonymous]
        [RequireSsl]
        public ActionResult Index()
        {
            var user = ManageProvider.Provider.TryLogin();
            if (user == null) return RedirectToAction("Login", "User", new { r = Request.Url.PathAndQuery });

            ViewBag.User = ManageProvider.User;
            ViewBag.Config = SysConfig.Current;

            // 工作台页面
            var startPage = Request["page"];
            if (startPage.IsNullOrEmpty()) startPage = Setting.Current.StartPage;

            ViewBag.Main = startPage;

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

            var name = Request.ServerVariables["Server_SoftWare"];
            if (String.IsNullOrEmpty(name)) name = Process.GetCurrentProcess().ProcessName;

            // 检测集成管道，低版本.Net不支持，请使用者根据情况自行注释
            try
            {
                if (HttpRuntime.UsingIntegratedPipeline) name += " [集成管道]";
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
                ReleaseMemory();
            }
            catch (Exception)
            {

                throw;
            }
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

        /// <summary>设置进程的程序集大小，将部分物理内存占用转移到虚拟内存</summary>
        /// <param name="pid">要设置的进程ID</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns></returns>
        public static Boolean SetProcessWorkingSetSize(Int32 pid, Int32 min, Int32 max)
        {
            var p = pid <= 0 ? Process.GetCurrentProcess() : Process.GetProcessById(pid);
            return Win32Native.SetProcessWorkingSetSize(p.Handle, min, max);
        }

        /// <summary>释放当前进程所占用的内存</summary>
        /// <returns></returns>
        public static Boolean ReleaseMemory()
        {
            GC.Collect();

            return SetProcessWorkingSetSize(0, -1, -1);
        }
    }
    class Win32Native
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(Int32 nStdHandle);

        [SecurityCritical]
        internal static Boolean DoesWin32MethodExist(String moduleName, String methodName)
        {
            var moduleHandle = GetModuleHandle(moduleName);
            if (moduleHandle == IntPtr.Zero) return false;
            return GetProcAddress(moduleHandle, methodName) != IntPtr.Zero;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail), DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(String moduleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, String methodName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetCurrentProcess();

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean IsWow64Process([In] IntPtr hSourceProcessHandle, [MarshalAs(UnmanagedType.Bool)] out Boolean isWow64);

        [DllImport("kernel32.dll")]
        internal static extern Boolean SetProcessWorkingSetSize(IntPtr proc, Int32 min, Int32 max);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern Boolean GetVersionEx([In, Out] OSVersionInfoEx ver);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class OSVersionInfoEx
        {
            public Int32 OSVersionInfoSize;
            public Int32 MajorVersion;        // 系统主版本号
            public Int32 MinorVersion;        // 系统次版本号
            public Int32 BuildNumber;         // 系统构建号
            public Int32 PlatformId;          // 系统支持的平台
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public String CSDVersion;       // 系统补丁包的名称
            public UInt16 ServicePackMajor; // 系统补丁包的主版本
            public UInt16 ServicePackMinor; // 系统补丁包的次版本
            //public OSSuites SuiteMask;         // 标识系统上的程序组
            //public OSProductType ProductType;        // 标识系统类型
            public Byte Reserved;           // 保留
            public OSVersionInfoEx()
            {
                OSVersionInfoSize = Marshal.SizeOf(this);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern Int32 GetSystemMetrics(Int32 nIndex);

        public struct MEMORYSTATUSEX
        {
            internal UInt32 dwLength;
            internal UInt32 dwMemoryLoad;
            internal UInt64 ullTotalPhys;
            internal UInt64 ullAvailPhys;
            internal UInt64 ullTotalPageFile;
            internal UInt64 ullAvailPageFile;
            internal UInt64 ullTotalVirtual;
            internal UInt64 ullAvailVirtual;
            internal UInt64 ullAvailExtendedVirtual;
            internal void Init()
            {
                dwLength = checked((UInt32)Marshal.SizeOf(typeof(MEMORYSTATUSEX)));
            }
        }

        [SecurityCritical]
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
    }
}