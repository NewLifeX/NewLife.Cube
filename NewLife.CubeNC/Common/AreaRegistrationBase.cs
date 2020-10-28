using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XCode.Membership;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.Cube
{
    /// <summary>区域特性基类</summary>
    /// <remarks>
    /// 提供以下功能：
    /// 1，区域名称。从类名中截取。其中DisplayName特性作为菜单中文名。
    /// 2，静态构造注册一次视图引擎、绑定提供者、过滤器
    /// 3，注册区域默认路由
    /// </remarks>
    public class AreaBase : AreaAttribute
    {
        /// <summary>所有区域类型</summary>
        public static Type[] Areas { get; private set; }

        /// <summary>实例化区域注册</summary>
        public AreaBase(String areaName) : base(areaName)
        {
        }

        static AreaBase()
        {
            XTrace.WriteLine("{0} Start 初始化魔方 {0}", new String('=', 32));
            Assembly.GetExecutingAssembly().WriteVersion();

            // 遍历所有引用了AreaRegistrationBase的程序集
            //var list = new List<PrecompiledViewAssembly>();
            foreach (var asm in FindAllArea())
            {
                XTrace.WriteLine("注册区域视图程序集：{0}", asm.FullName);

                //var pva = new PrecompiledViewAssembly(asm);
                //list.Add(pva);
            }

            // 自动检查并下载魔方资源
            ThreadPoolX.QueueUserWorkItem(CheckContent);

            XTrace.WriteLine("{0} End   初始化魔方 {0}", new String('=', 32));
        }

        /// <summary>遍历所有引用了AreaRegistrationBase的程序集</summary>
        /// <returns></returns>
        static List<Assembly> FindAllArea()
        {
            var list = new List<Assembly>();
            Areas = typeof(AreaBase).GetAllSubclasses().ToArray();
            foreach (var item in Areas)
            {
                var asm = item.Assembly;
                if (!list.Contains(asm))
                {
                    list.Add(asm);
                    //yield return asm;
                }
            }

            // 为了能够实现模板覆盖，程序集相互引用需要排序，父程序集在前
            list.Sort((x, y) =>
            {
                if (x == y) return 0;
                if (x != null && y == null) return 1;
                if (x == null && y != null) return -1;

                //return x.GetReferencedAssemblies().Any(e => e.FullName == y.FullName) ? 1 : -1;
                // 对程序集引用进行排序时，不能使用全名，当魔方更新而APP没有重新编译时，版本的不同将会导致全名不同，无法准确进行排序
                var yname = y.GetName().Name;
                return x.GetReferencedAssemblies().Any(e => e.Name == yname) ? 1 : -1;
            });

            return list;
        }

        static void CheckContent()
        {
            var set = Setting.Current;

            // 释放ico图标
            var ico = "favicon.ico";
            var wwwroot = set.WebRootPath;
            var ico2 = wwwroot.CombinePath(ico).GetFullPath();
            if (!File.Exists(ico2))
            {
                var asm = Assembly.GetExecutingAssembly();
                var ns = asm.GetManifestResourceNames();
                var f = ns.FirstOrDefault(e => e.EndsWithIgnoreCase(ico));
                if (!f.IsNullOrEmpty())
                {
                    XTrace.WriteLine("释放图标{0}到{1}", f, ico2);

                    var ms = asm.GetManifestResourceStream(f);
                    File.WriteAllBytes(ico2.EnsureDirectory(true), ms.ReadBytes());
                }
            }

            // 检查魔方样式
            if (set.ResourceUrl.IsNullOrEmpty())
            {
                var content = set.WebRootPath.CombinePath("Content");
                var js = content.CombinePath("Cube.js").GetFullPath();
                var css = content.CombinePath("Cube.css").GetFullPath();
                if (File.Exists(js) && File.Exists(css))
                {
                    // 判断脚本时间
                    var dt = DateTime.MinValue;
                    var ss = File.ReadAllLines(js);
                    for (var i = 0; i < 5; i++)
                    {
                        if (DateTime.TryParse(ss[i].TrimStart("//").Trim(), out dt)) break;
                    }
                    // 要求脚本最小更新时间
                    if (dt >= "2020-02-04 00:00:00".ToDateTime()) return;
                }

                var url = Setting.Current.PluginServer;
                if (url.IsNullOrEmpty()) return;

                var wc = new WebClientX()
                {
                    Log = XTrace.Log
                };
                wc.DownloadLinkAndExtract(url, "Cube_Content", content.GetFullPath(), true);
            }
        }

        /// <summary>注册区域，每个继承此区域特性的类的静态构造函数都调用此方法，以进行相关注册</summary>
        public static void RegisterArea<T>() where T : AreaBase
        {
            var areaType = typeof(T);
            var ns = areaType.Namespace + ".Controllers";
            var areaName = areaType.Name.TrimEnd("Area");
            XTrace.WriteLine("开始注册权限管理区域[{0}]，控制器命名空间[{1}]", areaName, ns);

            // 自动检查并添加菜单
            TaskEx.Run(() =>
            {
                try
                {
                    ScanController<T>();
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            });
        }

        /// <summary>自动扫描控制器，并添加到菜单</summary>
        /// <remarks>默认操作当前注册区域的下一级Controllers命名空间</remarks>
        protected static void ScanController<T>() where T : AreaBase
        {
            var areaType = typeof(T);

#if DEBUG
            XTrace.WriteLine("{0}.ScanController", areaType.Name.TrimEnd("AreaRegistration"));
#endif
            var areaName = areaType.Name.TrimEnd("Area");
            var mf = ManageProvider.Menu;
            if (mf == null) return;

            using var tran = (mf as IEntityFactory).Session.CreateTrans();

            XTrace.WriteLine("初始化[{0}]的菜单体系", areaName);
            mf.ScanController(areaName, areaType.Assembly, areaType.Namespace + ".Controllers");

            // 更新区域名称为友好中文名
            var menu = mf.Root.FindByPath(areaName);
            if (menu != null && menu.DisplayName.IsNullOrEmpty())
            {
                var dis = areaType.GetDisplayName();
                var des = areaType.GetDescription();

                if (!dis.IsNullOrEmpty()) menu.DisplayName = dis;
                if (!des.IsNullOrEmpty()) menu.Remark = des;

                (menu as IEntity).Update();
            }

            tran.Commit();
        }

        private static ICollection<String> _areas;

        /// <summary>判断控制器是否归属于魔方管辖</summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        public static Boolean Contains(ControllerBase controller)
        {
            // 判断控制器是否在管辖范围之内，不拦截其它控制器的异常信息
            var ns = controller.GetType().Namespace;
            if (!ns.EndsWith(".Controllers")) return false;

            if (_areas == null) _areas = new HashSet<String>(Areas.Select(e => e.Namespace));

            // 该控制器父级命名空间必须有对应的区域注册类，才会拦截其异常
            ns = ns.TrimEnd(".Controllers");
            //return Areas.Any(e => e.Namespace == ns);
            return _areas.Contains(ns);
        }

        /// <summary>判断控制器是否归属于魔方管辖</summary>
        /// <param name="controllerActionDescriptor"></param>
        /// <returns></returns>
        public static Boolean Contains(ControllerActionDescriptor controllerActionDescriptor)
        {
            // 判断控制器是否在管辖范围之内，不拦截其它控制器的异常信息
            var controller = controllerActionDescriptor.ControllerTypeInfo;
            var ns = controller.Namespace;
            if (!ns.EndsWith(".Controllers")) return false;

            if (_areas == null) _areas = new HashSet<String>(Areas.Select(e => e.Namespace));

            // 该控制器父级命名空间必须有对应的区域注册类，才会拦截其异常
            ns = ns.TrimEnd(".Controllers");
            //return Areas.Any(e => e.Namespace == ns);
            return _areas.Contains(ns);
        }
    }
}