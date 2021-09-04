using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using NewLife.Common;
using NewLife.Model;
using NewLife.Serialization;
using Newtonsoft.Json;
using XCode.Membership;

namespace NewLife.Cube.Blazor.Views.Blazor
{
    public partial class List
    {
        [Parameter] public string Area { get; set; }
        [Parameter] public string Module { get; set; }
        [Parameter] public string Action { get; set; }

        [Inject] HttpClient http { get; set; }
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
            Stream stream = new MemoryStream();
            accessor.HttpContext.Request.ContentType = "application/json";
            accessor.HttpContext.Response.ContentType = "application/json";
            accessor.HttpContext.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(stream));
            var endpoints = ManageProvider2.EndpointRoute.DataSources.FirstOrDefault().Endpoints;
            var endpoint = endpoints.FirstOrDefault(x => x.DisplayName.Contains($".{Module}Controller.{Action}"));
            var dataTokens = endpoint.Metadata.GetMetadata<IDataTokensMetadata>();
            var routeData = new Microsoft.AspNetCore.Routing.RouteData();
            routeData.PushState(router: null, accessor.HttpContext.Request.RouteValues, new RouteValueDictionary(dataTokens?.DataTokens));
            var action = endpoint.Metadata.GetMetadata<ActionDescriptor>()!;
            var actionContext = new ActionContext(accessor.HttpContext, routeData, action);
            var invokerFactory = accessor.HttpContext.RequestServices.GetRequiredService<IActionInvokerFactory>();
            var invoker = invokerFactory.CreateInvoker(actionContext);
            await invoker!.InvokeAsync();
            stream.Seek(0, SeekOrigin.Begin);
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return new JsonParser(Encoding.UTF8.GetString(bytes)).Decode();
        }

        private string RouteSegment(string segment)
        {

            return segment.IsNullOrEmpty() || segment == "~" ? "" : $"/{segment}";
        }
    }


    public class MyHttpResponseBodyFeature : IHttpResponseBodyFeature
    {
        private PipeWriter? _pipeWriter;
        private bool _started;
        private bool _completed;
        private bool _disposed;

        public MyHttpResponseBodyFeature(Stream stream)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public MyHttpResponseBodyFeature(Stream stream, IHttpResponseBodyFeature priorFeature)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            PriorFeature = priorFeature;
        }

        public Stream Stream { get; }

        public IHttpResponseBodyFeature PriorFeature { get; }

        public PipeWriter Writer
        {
            get
            {
                if (_pipeWriter == null)
                {
                    _pipeWriter = PipeWriter.Create(Stream, new StreamPipeWriterOptions(leaveOpen: true));
                    if (_completed)
                    {
                        _pipeWriter.Complete();
                    }
                }

                return _pipeWriter;
            }
        }

        public virtual void DisableBuffering()
        {
            PriorFeature?.DisableBuffering();
        }

        public virtual async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken)
        {
            if (!_started)
            {
                await StartAsync(cancellationToken);
            }
            await SendFileFallback.SendFileAsync(Stream, path, offset, count, cancellationToken);
        }

        public virtual Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (!_started)
            {
                _started = true;
                return Stream.FlushAsync(cancellationToken);
            }
            return Task.CompletedTask;
        }

        public virtual async Task CompleteAsync()
        {
            if (_disposed)
            {
                return;
            }
            if (_completed)
            {
                return;
            }

            if (!_started)
            {
                await StartAsync();
            }

            _completed = true;

            if (_pipeWriter != null)
            {
                await _pipeWriter.CompleteAsync();
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
