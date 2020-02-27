using System;
using System.Linq;
using System.Web.Mvc;
using NewLife.Reflection;
using XCode;

namespace NewLife.Cube
{
    /// <summary>实体模型绑定器。特殊处理XCode实体类</summary>
    public class EntityModelBinder : DefaultModelBinder
    {
        /// <summary>创建模型。对于有Key的请求，使用FindByKeyForEdit方法先查出来数据，而不是直接反射实例化实体对象</summary>
        /// <param name="controllerContext"></param>
        /// <param name="bindingContext"></param>
        /// <param name="modelType"></param>
        /// <returns></returns>
        protected override Object CreateModel(ControllerContext controllerContext, ModelBindingContext bindingContext, Type modelType)
        {
            if (modelType.As<IEntity>())
            {
                var fact = EntityFactory.CreateOperate(modelType);
                if (fact != null)
                {
                    var rvs = controllerContext.RouteData.Values;
                    var pks = fact.Table.PrimaryKeys;
                    var uk = fact.Unique;

                    IEntity entity = null;
                    if (uk != null)
                    {
                        // 查询实体对象用于编辑
                        var id = rvs[uk.Name];
                        //if (id != null) entity = GetEntity(fact.EntityType, id) ?? fact.FindByKeyForEdit(id);
                        if (id != null) fact.FindByKeyForEdit(id);
                        if (entity == null) entity = fact.Create();
                    }
                    else if (pks.Length > 0)
                    {
                        // 查询实体对象用于编辑
                        //var vs = pks.Select(e => rvs[e.Name]).ToArray();
                        //entity = GetEntity(fact.EntityType, vs);
                        //if (entity == null)
                        //{
                        var req = controllerContext.HttpContext.Request;
                        var exp = new WhereExpression();
                        foreach (var item in pks)
                        {
                            exp &= item.Equal(req[item.Name].ChangeType(item.Type));
                        }

                        entity = fact.Find(exp);
                        //}
                        if (entity == null) entity = fact.Create();
                    }

                    if (entity != null)
                    {
                        var fs = controllerContext.HttpContext.Request.Form;
                        // 提前填充动态字段的扩展属性
                        foreach (var item in fact.Fields)
                        {
                            if (item.IsDynamic && fs.AllKeys.Contains(item.Name)) entity.SetItem(item.Name, fs[item.Name]);
                        }

                        return entity;
                    }

                    return fact.Create();
                }
            }

            return base.CreateModel(controllerContext, bindingContext, modelType);
        }
    }

    /// <summary>实体模型绑定器提供者，为所有XCode实体类提供实体模型绑定器</summary>
    public class EntityModelBinderProvider : IModelBinderProvider
    {
        /// <summary>获取绑定器</summary>
        /// <param name="modelType"></param>
        /// <returns></returns>
        public virtual IModelBinder GetBinder(Type modelType)
        {
            if (modelType.As<IEntity>()) return new EntityModelBinder();

            return null;
        }
    }
}