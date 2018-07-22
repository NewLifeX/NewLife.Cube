using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NewLife.CubeNC.Extensions;
using NewLife.Log;
using NewLife.Reflection;
using XCode;
using HttpContext = NewLife.Web.HttpContext;

namespace NewLife.CubeNC.Com
{
    public class EntityModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var modelType = bindingContext.ModelType;
            var controllerContext = bindingContext.ActionContext;
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
                        if (rvs[uk.Name] != null) entity = GetEntity(fact.EntityType, rvs[uk.Name]) ?? fact.FindByKeyForEdit(rvs[uk.Name]);
                        if (entity == null) entity = fact.Create();
                    }
                    else if (pks.Length > 0)
                    {
                        // 查询实体对象用于编辑
                        var vs = pks.Select(e => rvs[e.Name]).ToArray();
                        entity = GetEntity(fact.EntityType, vs);
                        if (entity == null)
                        {
                            var req = controllerContext.HttpContext.Request;
                            var exp = new WhereExpression();
                            foreach (var item in pks)
                            {
                                exp &= item.Equal(
                                    (req.Form[item.Name].Count > 0 ? req.Form[item.Name] : req.Query[item.Name])
                                    .ChangeType(item.Type));
                            }

                            entity = fact.Find(exp);
                        }
                        if (entity == null) entity = fact.Create();
                    }

                    if (entity != null)
                    {
                        var fs = controllerContext.HttpContext.Request.Form;
                        // 提前填充动态字段的扩展属性
                        foreach (var item in fact.Fields)
                        {
                            if (item.IsDynamic && fs.ContainsKey(item.Name)) entity.SetItem(item.Name, fs[item.Name]);
                        }

                        bindingContext.Result = ModelBindingResult.Success(entity);
                    }

                    bindingContext.Result = ModelBindingResult.Success(fact.Create());
                }

            }

            return Task.CompletedTask;
        }

        private static String GetCacheKey(Type type, params Object[] keys)
        {
            return "CubeModel_{0}_{1}".F(type.FullName, keys.Join("_"));
        }

        /// <summary>呈现表单前，保存实体对象。提交时优先使用该对象而不是去数据库查找，避免脏写</summary>
        /// <param name="entity"></param>
        internal static void SetEntity(IEntity entity)
        {
            var ctx = HttpContext.Current;
            var fact = EntityFactory.CreateOperate(entity.GetType());

            var ckey = "";
            var pks = fact.Table.PrimaryKeys;
            var uk = fact.Unique;
            if (uk != null)
                ckey = GetCacheKey(entity.GetType(), entity[uk.Name]);
            else if (pks.Length > 0)
                ckey = GetCacheKey(entity.GetType(), pks.Select(e => entity[e.Name]).ToArray());

            ctx.Session.Set(ckey, entity.ToBytes());
        }

        private static IEntity GetEntity(Type type, params Object[] keys)
        {
            var ctx = HttpContext.Current;
            var ckey = GetCacheKey(type, keys);
            return ctx.Session.Get<IEntity>(ckey);
        }

    }


    /// <summary>实体模型绑定器提供者，为所有XCode实体类提供实体模型绑定器</summary>
    public class EntityModelBinderProvider : IModelBinderProvider
    {
        /// <summary>
        /// 获取绑定器
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IModelBinder GetBinder(ModelBinderProviderContext context) => 
            context.Metadata.ModelType.As<IEntity>() ? new EntityModelBinder() : null;

        static EntityModelBinderProvider()
        {
            XTrace.WriteLine("注册实体模型绑定器：{0}", typeof(EntityModelBinderProvider).FullName);
            //ModelBinderProviders.BinderProviders.Add(new EntityModelBinderProvider());
        }

        /// <summary>注册到全局模型绑定器提供者集合</summary>
        public static void Register()
        {
            // 引发静态构造，只执行一次
        }
    }
}
