using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Reflection;

namespace BigCookieKit.AspCore.RouteSelector
{
    /// <summary>基于 DFA 的端点路由选择器，供 Blazor 内部转发调用 MVC 接口</summary>
    public static class RouteSelector
    {
        /// <summary>按路径匹配端点</summary>
        /// <param name="service">服务提供者</param>
        /// <param name="path">请求路径</param>
        /// <returns>匹配到的端点</returns>
        public static async Task<Endpoint> MathEndpoint(this IServiceProvider service, string path)
        {
            var matcherBuilder = service.GetService<DfaMatcherBuilder>();
            var matcher = matcherBuilder.CreateMatcher();
            return await matcher.SelectorAsync(path);
        }

        /// <summary>按 HttpContext 匹配端点（原生方法，仅作参考）</summary>
        /// <param name="service">服务提供者</param>
        /// <param name="context">HTTP 上下文</param>
        /// <returns>匹配任务</returns>
        [Obsolete("原生方法 参考使用")]
        public static async Task MathEndpoint(this IServiceProvider service, HttpContext context)
        {
            var matcherBuilder = service.GetService<DfaMatcherBuilder>();
            var matcher = matcherBuilder.CreateMatcher();
            await matcher.MatchAsync(context);
        }

        /// <summary>调用端点对应的 MVC 动作</summary>
        /// <param name="endpoint">端点</param>
        /// <param name="httpContext">HTTP 上下文</param>
        /// <returns>动作执行结果</returns>
        public static async Task<SelectorEndpointResult> InvokeAsync(this Endpoint endpoint, HttpContext httpContext)
        {
            var dataTokens = endpoint.Metadata.GetMetadata<IDataTokensMetadata>();
            var routeData = new RouteData();
            routeData.PushState(null, httpContext.Request.RouteValues, new RouteValueDictionary(dataTokens?.DataTokens));
            var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()!;
            var actionContext = new ActionContext(httpContext, routeData, action);
            IActionInvokerFactory invokerFactory = httpContext.RequestServices.GetRequiredService<IActionInvokerFactory>();
            var invoker = invokerFactory.CreateInvoker(actionContext);
            await invoker!.InvokeAsync();
            return await Task.FromResult(new SelectorEndpointResult(invoker));
        }

        /// <summary>注册端点路由选择所需的服务</summary>
        /// <param name="services">服务集合</param>
        public static void AddRouteSelector(this IServiceCollection services)
        {
            var dataSources = new ObservableCollection<EndpointDataSource>();

            services.TryAddSingleton<EndpointDataSource>(s =>
            {
                return new CompositeEndpointDataSource(dataSources);
            });

            //Microsoft.Extensions.DependencyInjection.RoutingServiceCollectionExtensions
            services.TryAddSingleton<ParameterPolicyFactory, DefaultParameterPolicyFactory>();
            services.TryAddTransient<DfaMatcherBuilder>();
            services.TryAddSingleton<EndpointSelector, DefaultEndpointSelector>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, HttpMethodMatcherPolicy>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, HostMatcherPolicy>());
        }
    }

    /// <summary>端点动作执行结果，提供控制器、视图数据与返回值的访问</summary>
    public class SelectorEndpointResult
    {
        private readonly BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly Type actionInvokerType;

        private readonly IActionInvoker actionInvoker;

        private Controller _controller;
        private object _result;

        /// <summary>控制器实例</summary>
        public Controller Controller
        {
            get
            {
                _controller ??= actionInvokerType.GetField("_instance", bindingFlags)?.GetValue(actionInvoker) as Controller;

                return _controller;
            }
        }

        /// <summary>视图数据</summary>
        public ViewDataDictionary ViewData => Controller.ViewData;

        /// <summary>动态视图包</summary>
        public dynamic ViewBag => Controller.ViewBag;

        /// <summary>动作返回值</summary>
        public object Result
        {
            get
            {
                _result ??= actionInvokerType.GetField("_result", bindingFlags)?.GetValue(actionInvoker);

                return _result;
            }
        }

        internal SelectorEndpointResult(IActionInvoker actionInvoker)
        {
            this.actionInvoker = actionInvoker;
            actionInvokerType = actionInvoker.GetType();
        }


    }

    internal readonly record struct DfaBuilderWorkerWorkItem(RouteEndpoint Endpoint, int PrecedenceDigit, List<DfaNode> Parents);
}
