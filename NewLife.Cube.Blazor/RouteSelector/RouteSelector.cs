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
    public static class RouteSelector
    {
        public static async Task<Endpoint> MathEndpoint(this IServiceProvider service, string path)
        {
            var matcherBuilder = service.GetService<DfaMatcherBuilder>();
            var matcher = matcherBuilder.CreateMatcher();
            return await matcher.SelectorAsync(path);
        }

        [Obsolete("原生方法 参考使用")]
        public static async Task MathEndpoint(this IServiceProvider service, HttpContext context)
        {
            var matcherBuilder = service.GetService<DfaMatcherBuilder>();
            var matcher = matcherBuilder.CreateMatcher();
            await matcher.MatchAsync(context);
        }

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

    public class SelectorEndpointResult
    {
        private readonly BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly Type actionInvokerType;

        private readonly IActionInvoker actionInvoker;

        private Controller _controller;
        private object _result;

        public Controller Controller
        {
            get
            {
                if (_controller == null)
                {
                    _controller = actionInvokerType.GetField("_instance", bindingFlags)?.GetValue(actionInvoker) as Controller;
                }

                return _controller;
            }
        }

        public ViewDataDictionary ViewData => Controller.ViewData;

        public dynamic ViewBag => Controller.ViewBag;

        public object Result
        {
            get
            {
                if (_result == null)
                {
                    _result = actionInvokerType.GetField("_result", bindingFlags)?.GetValue(actionInvoker);
                }

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
