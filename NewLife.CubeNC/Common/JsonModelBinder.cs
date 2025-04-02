using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using NewLife.Configuration;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Extensions;
using NewLife.Reflection;
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

            var entityBody = req.GetRequestBody(modelType);

            if (entityBody != null)
            {
                bindingContext.Result = ModelBindingResult.Success(entityBody);
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
            var isGenericType = false;
            if (modelType.BaseType?.FullName != null && modelType.BaseType.FullName.StartsWith("NewLife.Configuration.Config`1["))
            {
                var genericType = typeof(Config<>).MakeGenericType(modelType);
                isGenericType = genericType.FullName != null && modelType.As(genericType);
            }

            if (modelType.As<ICubeModel>() || isGenericType)
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