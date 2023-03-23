using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;

namespace NewLife.Cube
{
    /// <summary>基于主题的视图路径搜索扩展</summary>
    public class ThemeViewLocationExpander : IViewLocationExpander
    {
        /// <summary>控制上下文，不同主题使用不同视图路径。ExpandViewLocations基于上下文不同值有相应缓存</summary>
        /// <param name="context"></param>
        public void PopulateValues(ViewLocationExpanderContext context)
        {
            // 选择视图使用的主题样式，首页的视图特殊处理
            var set = CubeSetting.Current;
            var theme = set.Theme;
            if (context.AreaName.EqualIgnoreCase("Admin") &&
                context.ControllerName.EqualIgnoreCase("Index") &&
                context.ActionContext.ActionDescriptor is ControllerActionDescriptor act &&
                act.ActionName.EqualIgnoreCase("Index"))
                theme = set.Skin;
            context.Values["theme"] = theme.IsNullOrEmpty() ? "ACE" : theme;
        }

        /// <summary>扩展搜索路径</summary>
        /// <param name="context"></param>
        /// <param name="viewLocations"></param>
        /// <returns></returns>
        public virtual IEnumerable<String> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<String> viewLocations)
        {
            var theme = context.Values["theme"];
            if (theme.IsNullOrEmpty()) return viewLocations;

            var vs = viewLocations.ToList();

            var p = vs.IndexOf("/Views/Shared/{0}.cshtml");
            if (p >= 0) vs.Insert(p, "/Views/" + theme + "/{0}.cshtml");

            p = vs.IndexOf("/Areas/{2}/Views/Shared/{0}.cshtml");
            if (p >= 0) vs.Insert(p, "/Areas/{2}/Views/" + theme + "/{0}.cshtml");
            p = vs.IndexOf("/Areas/{2}/Views/{1}/{0}.cshtml");
            if (p >= 0) vs.Insert(p, "/Areas/{2}/Views/{1}_" + theme + "/{0}.cshtml");

            return vs;
        }
    }
}