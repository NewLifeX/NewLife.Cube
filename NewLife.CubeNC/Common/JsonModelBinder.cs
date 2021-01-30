using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Web;
using XCode;

namespace NewLife.Cube
{
    /// <summary>Json模型绑定器</summary>
    public class JsonModelBinder : IModelBinder
    {
        private readonly IDictionary<ModelMetadata, IModelBinder> _propertyBinders;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>实例化分页模型绑定器</summary>
        /// <param name="propertyBinders"></param>
        /// <param name="loggerFactory"></param>
        public JsonModelBinder(IDictionary<ModelMetadata, IModelBinder> propertyBinders, ILoggerFactory loggerFactory)
        {
            _propertyBinders = propertyBinders ?? throw new ArgumentNullException(nameof(propertyBinders));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>对于Json请求，从body中读取参数</summary>
        /// <param name="bindingContext"></param>
        /// <returns></returns>
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var req = bindingContext.HttpContext.Request;

            var modelType = bindingContext.ModelType;

            if (req.ContentType !=null && req.ContentType.Contains("json") && req.ContentLength > 0)
            {
                // 允许同步IO，便于CsvFile刷数据Flush
                var ft = bindingContext.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature>();
                if (ft != null) ft.AllowSynchronousIO = true;

                var body = req.Body.ToStr();
                var m = body.ToJsonEntity(modelType);

                bindingContext.Result = ModelBindingResult.Success(m);
            }
            else
            {
                var modelBinder = new ComplexTypeModelBinder(_propertyBinders, _loggerFactory);
                await modelBinder.BindModelAsync(bindingContext);
            }
        }
    }

    /// <summary>Json模型绑定器提供者</summary>
    public class JsonModelBinderProvider : IModelBinderProvider
    {
        /// <summary>获取绑定器</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            var modelType = context.Metadata.ModelType;
            if (modelType.As<ICubeModel>())
            {
                var propertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
                foreach (var property in context.Metadata.Properties)
                {
                    propertyBinders.Add(property, context.CreateBinder(property));
                }

                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                return new JsonModelBinder(propertyBinders, loggerFactory);
            }

            return null;
        }
    }
}