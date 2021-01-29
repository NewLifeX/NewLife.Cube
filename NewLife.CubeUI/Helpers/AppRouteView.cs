using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NewLife.CubeUI.Services;

namespace NewLife.Cube.Helpers
{
    public class AppRouteView : RouteView
    {
        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public IAccountService AccountService { get; set; }

        protected override void Render(RenderTreeBuilder builder)
        {
            var authorize = Attribute.GetCustomAttribute(RouteData.PageType, typeof(AuthorizeAttribute)) != null;
            // 按理说登录页设置了匿名访问不该来这里的
            var uri = new Uri(NavigationManager.Uri);

            if (authorize && AccountService.User == null 
                          && !uri.LocalPath.StartsWith("/user/login", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"当前访问地址：{NavigationManager.Uri}");
                var returnUrl = WebUtility.UrlEncode(uri.PathAndQuery);
                Console.WriteLine($"returnUrl：{returnUrl}");
                NavigationManager.NavigateTo($"/User/Login?returnUrl={returnUrl}");
                base.Render(builder);
            }
            else
            {
                base.Render(builder);
            }
        }
    }
}
