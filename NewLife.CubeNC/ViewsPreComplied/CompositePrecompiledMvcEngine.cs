using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NewLife.CubeNC.ViewsPreComplied
{
    public class CompositePrecompiledMvcEngine: RazorViewEngine, IRazorViewEngine
    {
        private readonly IRazorPageActivator _pageActivator;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly DiagnosticSource _diagnosticSource;

        //public CompositePrecompiledMvcEngine() { }
        /// <inheritdoc />
        public CompositePrecompiledMvcEngine(IRazorPageFactoryProvider pageFactory,
            IRazorPageActivator pageActivator,
            HtmlEncoder htmlEncoder,
            IOptions<RazorViewEngineOptions> optionsAccessor,
            RazorProjectFileSystem razorFileSystem,
            ILoggerFactory loggerFactory,
            DiagnosticSource diagnosticSource)
            : base(pageFactory, pageActivator, htmlEncoder, optionsAccessor, razorFileSystem, loggerFactory, diagnosticSource)
        {
            _pageActivator = pageActivator;
            _htmlEncoder = htmlEncoder;
            _diagnosticSource = diagnosticSource;
        }

    
        public new ViewEngineResult FindView(ActionContext context, String viewName, Boolean isMainPage)
        {
            //return ViewEngineResult.Found(viewName,new PrecompiledMvcView(this,_pageActivator,null,null, _htmlEncoder, _diagnosticSource));
            return base.FindView(context, viewName, isMainPage);
        }


        //public String GetAbsolutePath(String executingFilePath, String pagePath)
        //{
        //    throw new NotImplementedException();
        //}

        //public RazorPageResult GetPage(String executingFilePath, String pagePath)
        //{
        //    throw new NotImplementedException();
        //}

        //public new ViewEngineResult GetView(String executingFilePath, String viewPath, Boolean isMainPage)
        //{
        //    //throw new NotImplementedException();

        //    //return ViewEngineResult.Found(,);
        //    //return base.GetView(executingFilePath, viewPath, isMainPage);
        //}
    }

    /// <inheritdoc cref="RazorView" />
    public class PrecompiledMvcView : RazorView,IView
    {
        private readonly IRazorPageActivator _pageActivator;

        /// <inheritdoc />
        public PrecompiledMvcView(IRazorViewEngine viewEngine, IRazorPageActivator pageActivator, IReadOnlyList<IRazorPage> viewStartPages, IRazorPage razorPage, HtmlEncoder htmlEncoder, DiagnosticSource diagnosticSource) 
            : base(viewEngine, pageActivator, viewStartPages, razorPage, htmlEncoder, diagnosticSource)
        {
            _pageActivator = pageActivator;
        }

        /// <inheritdoc />
        public new Task RenderAsync(ViewContext context)
        {
            _pageActivator.Activate(RazorPage,context);
            if (RazorPage == null) throw new InvalidOperationException("无效视图类型");
           return RazorPage.ExecuteAsync();
        }
    }

    /// <inheritdoc />
    public class RazorViewOPtionsSetup : IConfigureOptions<MvcViewOptions>
    {
        private readonly IRazorViewEngine _compositePrecompiledMvcEngine;

        /// <inheritdoc />
        public RazorViewOPtionsSetup(IRazorViewEngine razorViewEngine)
        {
            _compositePrecompiledMvcEngine = razorViewEngine;
        }

        void IConfigureOptions<MvcViewOptions>.Configure(MvcViewOptions options)
        {
            //options.ViewEngines.Clear();
            options.ViewEngines.Add(_compositePrecompiledMvcEngine);
        }

    }

    public static class RazorViewOPtionsExtensions
    {
        //public static void AddRazorViewOPtions(this IMvcBuilder builder)
        //{
        //    builder.Services.Configure()
        //}
    } 
}
