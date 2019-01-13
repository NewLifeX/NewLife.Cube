using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NewLife.Data;
using NewLife.Web;

namespace NewLife.Cube
{
    /// <summary>分页模型绑定器</summary>
    public class PagerModelBinder : IModelBinder
    {
        /// <summary>创建模型。对于有Key的请求，使用FindByKeyForEdit方法先查出来数据，而不是直接反射实例化实体对象</summary>
        /// <param name="bindingContext"></param>
        /// <returns></returns>
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var modelType = bindingContext.ModelType;
            //var controllerContext = bindingContext.ActionContext;
            if (modelType == typeof(Pager) || modelType == typeof(PageParameter))
            {
                var pager = new Pager
                {
                    Params = WebHelper.Params
                };
                bindingContext.Result = ModelBindingResult.Success(pager);
            }

            return Task.CompletedTask;
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
            if (modelType == typeof(Pager) || modelType == typeof(PageParameter)) return new PagerModelBinder();

            return null;
        }
    }
}