using System;
using System.Web.Mvc;
using NewLife.Data;
using NewLife.Web;

namespace NewLife.Cube
{
    /// <summary>分页模型绑定器</summary>
    public class PagerModelBinder : DefaultModelBinder
    {
        /// <summary>创建模型。对于有Key的请求，使用FindByKeyForEdit方法先查出来数据，而不是直接反射实例化实体对象</summary>
        /// <param name="controllerContext"></param>
        /// <param name="bindingContext"></param>
        /// <param name="modelType"></param>
        /// <returns></returns>
        protected override Object CreateModel(ControllerContext controllerContext, ModelBindingContext bindingContext, Type modelType)
        {
            if (modelType == typeof(Pager) || modelType == typeof(PageParameter))
            {
                var pager = new Pager
                {
                    Params = WebHelper.Params
                };

                return pager;
            }

            return base.CreateModel(controllerContext, bindingContext, modelType);
        }
    }

    /// <summary>分页模型绑定器提供者</summary>
    public class PagerModelBinderProvider : IModelBinderProvider
    {
        /// <summary>获取绑定器</summary>
        /// <param name="modelType"></param>
        /// <returns></returns>
        public IModelBinder GetBinder(Type modelType)
        {
            if (modelType == typeof(Pager) || modelType == typeof(PageParameter)) return new PagerModelBinder();

            return null;
        }
    }
}