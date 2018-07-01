using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NewLife.Reflection;
using XCode;
using HttpContext = NewLife.Web.HttpContext;

namespace NewLife.CubeNC.Com
{
    public class EntityModelBinder:IModelBinder
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

            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, entity);
                ctx.Session.Set(ckey, ms.GetBuffer());
            }
        }

        private static IEntity GetEntity(Type type, params Object[] keys)
        {
            var ctx = HttpContext.Current;
            var ckey = GetCacheKey(type, keys);
            var bytes = ctx.Session.Get(ckey);
            using (var ms = new MemoryStream(bytes))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(ms) as IEntity;
            }
        }

    }



    public class EntityModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context) => 
            context.Metadata.ModelType.As<IEntity>() ? new EntityModelBinder() : null;
    }
}
