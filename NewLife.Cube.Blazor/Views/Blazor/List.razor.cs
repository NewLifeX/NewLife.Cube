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
    public partial class List
    {
        [Parameter] public string Area { get; set; }
        [Parameter] public string Module { get; set; }
        [Parameter] public string Action { get; set; }

        [Inject] HttpContextAccessor accessor { get; set; }

        [Inject] private IManageProvider _provider { get; set; }

        private IManageUser User { get => _provider.Current; }
        private SysConfig Config { get => SysConfig.Current; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override async Task OnInitializedAsync()
        {
            var json = await GetActionJson();
            await base.OnInitializedAsync();
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task<object> GetActionJson()
        {
            var tempRequestContentType = accessor.HttpContext.Request.ContentType;
            var tempResponseContentType = accessor.HttpContext.Response.ContentType;

            Stream stream = new MemoryStream();
            accessor.HttpContext.Request.ContentType = "application/json";
            accessor.HttpContext.Response.ContentType = "application/json";
            accessor.HttpContext.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(stream));

            var result = await ActionInvoke();

            accessor.HttpContext.Request.ContentType = tempRequestContentType;
            accessor.HttpContext.Response.ContentType = tempResponseContentType;

            stream.Seek(0, SeekOrigin.Begin);
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return new JsonParser(Encoding.UTF8.GetString(bytes)).Decode();
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
