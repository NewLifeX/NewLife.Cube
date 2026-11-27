using System.IO;
using System.Text;
using System.Threading.Tasks;

using BigCookieKit.AspCore.RouteSelector;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;

using NewLife.Common;
using NewLife.Model;
using NewLife.Serialization;

using XCode.Membership;

namespace NewLife.Cube.Blazor.Views.Blazor
{
    /// <summary>Blazor 列表组件，按区域/模块/动作调用后端接口并渲染数据</summary>
    public partial class List
    {
        /// <summary>区域名称</summary>
        [Parameter] public string Area { get; set; }
        /// <summary>模块名称</summary>
        [Parameter] public string Module { get; set; }
        /// <summary>动作名称</summary>
        [Parameter] public string Action { get; set; }

        [Inject] HttpContextAccessor accessor { get; set; }

        [Inject] private IManageProvider _provider { get; set; }

        private IManageUser User { get => _provider.Current; }
        private SysConfig Config { get => SysConfig.Current; }

        /// <summary>组件初始化</summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        /// <summary>组件异步初始化，加载动作数据</summary>
        protected override async Task OnInitializedAsync()
        {
            var json = await GetActionJson();
            await base.OnInitializedAsync();
        }

        /// <summary>设置组件参数</summary>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);
        }

        /// <summary>参数设置完成</summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
        }

        /// <summary>参数设置完成（异步）</summary>
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
        }

        /// <summary>组件渲染完成</summary>
        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
        }

        /// <summary>组件渲染完成（异步）</summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task<object> GetActionJson()
        {
            var tempRequestContentType = accessor.HttpContext.Request.ContentType;
            var tempResponseContentType = accessor.HttpContext.Response.ContentType;

            var stream = new MemoryStream();
            accessor.HttpContext.Request.ContentType = "application/json";
            accessor.HttpContext.Response.ContentType = "application/json";
            accessor.HttpContext.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(stream));

            var result = await ActionInvoke();

            accessor.HttpContext.Request.ContentType = tempRequestContentType;
            accessor.HttpContext.Response.ContentType = tempResponseContentType;

            return new JsonParser(Encoding.UTF8.GetString(stream.ToArray())).Decode();
        }

        private async Task<SelectorEndpointResult> ActionInvoke()
        {
            var path = "";
            if (Area == null)
            {
                path = $"/{Module}/{Action}/Index";
            }
            else
            {
                path = $"/{Area}/{Module}/{Action}";
            }
            var endpoint = await accessor.HttpContext.RequestServices.MathEndpoint(path);
            return await endpoint.InvokeAsync(accessor.HttpContext);
        }
    }

}
