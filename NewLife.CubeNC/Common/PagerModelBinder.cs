using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewLife.Data;
using NewLife.Web;

namespace NewLife.Cube
{
    /// <summary>分页模型绑定器</summary>
    public class PagerModelBinder : IModelBinder
    {
        private readonly IDictionary<ModelMetadata, IModelBinder> _propertyBinders;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>实例化分页模型绑定器</summary>
        /// <param name="propertyBinders"></param>
        /// <param name="loggerFactory"></param>
        public PagerModelBinder(IDictionary<ModelMetadata, IModelBinder> propertyBinders, ILoggerFactory loggerFactory)
        {
            _propertyBinders = propertyBinders ?? throw new ArgumentNullException(nameof(propertyBinders));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>创建模型。对于有Key的请求，使用FindByKeyForEdit方法先查出来数据，而不是直接反射实例化实体对象</summary>
        /// <param name="bindingContext"></param>
        /// <returns></returns>
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var modelType = bindingContext.ModelType;
            //var controllerContext = bindingContext.ActionContext;
            if (modelType == typeof(Pager) || modelType == typeof(PageParameter))
            {
                var pager = new Pager
                {
                    Params = WebHelper.Params
                };

                var routes = bindingContext.ActionContext.RouteData.Values;
                //foreach (var item in routes)
                //{
                //    if (!pager.Params.ContainsKey(item.Key)) pager.Params[item.Key] = item.Value + "";
                //}
                if (!pager.Params.ContainsKey("id") && routes.TryGetValue("id", out var id)) pager.Params["id"] = id + "";

                var complexTypeModelBinder = new ComplexTypeModelBinder(_propertyBinders, _loggerFactory);

                bindingContext.Model = pager;

                await complexTypeModelBinder.BindModelAsync(bindingContext);

                bindingContext.Result = ModelBindingResult.Success(pager);
            }

            //return Task.CompletedTask;
        }
    }

    /// <summary>分页模型绑定器提供者</summary>
    public class PagerModelBinderProvider : IModelBinderProvider
    {
        /// <summary>获取绑定器</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            var modelType = context.Metadata.ModelType;
            if (modelType == typeof(Pager) || modelType == typeof(PageParameter))
            {
                var propertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
                foreach (var property in context.Metadata.Properties)
                {
                    propertyBinders.Add(property, context.CreateBinder(property));
                }

                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                return new PagerModelBinder(propertyBinders, loggerFactory);
            }

            return null;
        }
    }
}