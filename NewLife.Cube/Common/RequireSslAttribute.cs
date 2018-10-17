using System;
using System.Web.Mvc;
using NewLife.Log;
using NewLife.Web;

namespace NewLife.Cube
{
    /// <summary>SSL特性</summary>
    public class RequireSslAttribute : FilterAttribute, IAuthorizationFilter
    {
        #region 属性
        #endregion

        #region 方法
        /// <summary>验证时</summary>
        /// <param name="filterContext"></param>
        public virtual void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));

            // 强制SSL
            var cfg = Setting.Current;
            if (cfg.SslMode < SslModes.HomeOnly) return;

            var req = filterContext.HttpContext.Request;
            if (!req.IsSecureConnection && !req.IsLocal && !req.IsAjaxRequest() && req.HttpMethod.EqualIgnoreCase("GET"))
                HandleNonHttpsRequest(filterContext);
        }

        /// <summary>拦截非Http请求</summary>
        /// <param name="filterContext"></param>
        protected virtual void HandleNonHttpsRequest(AuthorizationContext filterContext)
        {
            var req = filterContext.HttpContext.Request;

            // 有可能前端访问的是https，经反向代理后变成http
            var uri = req.GetRawUrl();
            if (uri.Scheme.StartsWith("https")) return;

            var url = "https://" + uri.Host + req.RawUrl;
            filterContext.Result = new RedirectResult(url);
        }
        #endregion

        #region 注册全局过滤器
        private static Boolean _inited;
        /// <summary>注册全局过滤器</summary>
        public void Register()
        {
            if (_inited) return;
            _inited = true;

            // 注册过滤器
            XTrace.WriteLine("注册SSL过滤器：{0}", GetType().FullName);

            var filters = GlobalFilters.Filters;
            filters.Add(this);
        }
        #endregion
    }
}