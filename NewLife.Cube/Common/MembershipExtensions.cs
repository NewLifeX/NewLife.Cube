using System;
using XCode.Membership;
using System.Linq;
using System.Threading;
#if __CORE__
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Razor;
#else
using System.Web.Mvc;
#endif

namespace NewLife.Cube
{
    /// <summary>用户扩展</summary>
    public static class MembershipExtensions
    {
        /// <summary>用户只有拥有当前菜单的指定权限</summary>
        /// <param name="user">指定用户</param>
        /// <param name="flags">是否拥有多个权限中的任意一个，或的关系。如果需要表示与的关系，可以传入一个多权限位合并</param>
        /// <returns></returns>
        [Obsolete]
        public static Boolean Has(this IUser user, params PermissionFlags[] flags)
        {
            if (user == null || user.Role == null) return false;

            var menu = CurrentMenu;
            if (menu == null) throw new Exception("无法定位当前权限菜单！");

            // 如果没有指定权限子项，则指判断是否拥有资源
            if (flags == null || flags.Length == 0) return user.Role.Has(menu.ID);

            //return user.Role.Has(menu.ID, flag);
            foreach (var item in flags)
            {
                // 菜单必须拥有这些权限位才行
                if (menu.Permissions.ContainsKey((Int32)item))
                {
                    // 如果判断None，则直接返回
                    if (item == PermissionFlags.None) return true;

                    if (user.Role.Has(menu.ID, item)) return true;
                }
            }
            return false;
        }

        /// <summary>当前请求所在菜单。自动根据当前请求的文件路径定位</summary>
        static IMenu CurrentMenu
        {
#if !__CORE__
            get
            {
                var context = System.Web.HttpContext.Current;
                if (context == null) return null;

                var menu = context.Items["CurrentMenu"] as IMenu;
                if (menu == null && !context.Items.Contains("CurrentMenu"))
                {
                    var ss = context.Request.AppRelativeCurrentExecutionFilePath.Split("/");
                    // 默认路由包括区域、控制器、动作，Url有时候会省略动作，再往后的就是参数了，动作和参数不参与菜单匹配
                    var max = ss.Length - 1;
                    if (ss[0] == "~") max++;

                    var fact = ManageProvider.Menu;

                    // 寻找当前所属菜单，路径倒序，从最长Url路径查起
                    for (var i = max; i > 0 && menu == null; i--)
                    {
                        var url = ss.Take(i).Join("/");
                        menu = fact.FindByUrl(url);
                    }

                    context.Items["CurrentMenu"] = menu;
                }
                return menu;
            }
#else
            get { return null; }
#endif
        }

        /// <summary>用户只有拥有当前菜单的指定权限</summary>
        /// <param name="user">指定用户</param>
        /// <param name="respath"></param>
        /// <param name="flags">是否拥有多个权限中的任意一个，或的关系。如果需要表示与的关系，可以传入一个多权限位合并</param>
        /// <returns></returns>
        [Obsolete]
        public static Boolean Has(this IUser user, String respath, params PermissionFlags[] flags)
        {
            if (user == null || user.Role == null) return false;

            var menu = ManageProvider.Menu.Root.FindByPath(respath);
            if (menu == null) throw new XException("无法定位权限菜单{0}！", respath);

            // 如果没有指定权限子项，则指判断是否拥有资源
            if (flags == null || flags.Length == 0) return user.Role.Has(menu.ID);

            foreach (var item in flags)
            {
                // 菜单必须拥有这些权限位才行
                if (menu.Permissions.ContainsKey((Int32)item))
                {
                    // 如果判断None，则直接返回
                    if (item == PermissionFlags.None) return true;

                    if (user.Role.Has(menu.ID, item)) return true;
                }
            }
            return false;
        }

#if __CORE__
        /// <summary>用户只有拥有当前菜单的指定权限</summary>
        /// <param name="page">页面</param>
        /// <param name="flags">是否拥有多个权限中的任意一个，或的关系。如果需要表示与的关系，可以传入一个多权限位合并</param>
        /// <returns></returns>
        public static Boolean Has(this IRazorPage page, params PermissionFlags[] flags)
        {
            // 没有用户时无权
            var user = page.ViewContext.ViewBag.User as IUser ?? 
                page.ViewContext.HttpContext.User.Identity as IUser ?? 
                Thread.CurrentPrincipal?.Identity as IUser;
            if (user == null) return false;

            // 没有菜单时不做权限控制
            var menu = page.ViewContext.ViewBag.Menu as IMenu;
            if (menu == null) menu = page.ViewContext.HttpContext.Items["CurrentMenu"] as IMenu;
            if (menu == null) return true;

            return user.Has(menu, flags);
        }

        ///// <summary>添加管理提供者</summary>
        ///// <param name="service"></param>
        //public static void AddManageProvider(this IServiceCollection service)
        //{
        //    service.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        //    //service.AddSingleton<IManageProvider, ManageProvider>();
        //    service.AddSingleton(ManageProvider.Provider);
        //}

        ///// <summary>使用管理提供者</summary>
        ///// <param name="app"></param>
        //public static void UseManagerProvider(this IApplicationBuilder app)
        //{
        //    //DefaultManageProviderForCore.Context = app.ApplicationServices.GetService<IHttpContextAccessor>();
        //}
#else
        /// <summary>用户只有拥有当前菜单的指定权限</summary>
        /// <param name="page">页面</param>
        /// <param name="flags">是否拥有多个权限中的任意一个，或的关系。如果需要表示与的关系，可以传入一个多权限位合并</param>
        /// <returns></returns>
        public static Boolean Has(this WebViewPage page, params PermissionFlags[] flags)
        {
            // 没有用户时无权
            var user = page.ViewBag.User as IUser ?? page.User.Identity as IUser;
            if (user == null) return false;

            // 没有菜单时不做权限控制
            var menu = page.ViewBag.Menu as IMenu;
            if (menu == null) return true;

            return user.Has(menu, flags);
        }
#endif
    }
}