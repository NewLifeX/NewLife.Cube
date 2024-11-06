using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using NewLife.Collections;
using NewLife.Cube.Common;
using NewLife.Cube.Extensions;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using XCode;

namespace NewLife.Cube;

/// <summary>实体模型绑定器</summary>
class EntityModelBinder : ComplexTypeModelBinder
{
    /// <summary>实例化实体模型绑定器</summary>
    /// <param name="propertyBinders"></param>
    /// <param name="loggerFactory"></param>
    public EntityModelBinder(IDictionary<ModelMetadata, IModelBinder> propertyBinders, ILoggerFactory loggerFactory)
        : base(propertyBinders, loggerFactory) { }

    /// <summary>创建模型。对于有Key的请求，使用FindByKeyForEdit方法先查出来数据，而不是直接反射实例化实体对象</summary>
    /// <param name="bindingContext"></param>
    /// <returns></returns>
    protected override Object CreateModel(ModelBindingContext bindingContext)
    {
        var modelType = bindingContext.ModelType;
        if (modelType.As<IEntity>() || modelType.As<IModel>())
        {
            // 如果提交数据里面刚好有名为model的字段，这是Add/Edit接口的入参，则需要清空modelName，否则无法绑定
            if (bindingContext.ModelName == "model") bindingContext.ModelName = String.Empty;
        }

        // 尝试从body读取json格式的参数
        var ctx = bindingContext.HttpContext;
        var request = ctx.Request;
        if (request.GetRequestBody<Object>() != null)
        {
            ctx.Items["EntityBody"] = ctx.Items["RequestBody"];
            var cubeBodyValueProvider = new CubeBodyValueProvider(bindingContext.ValueProvider,
                ctx.Items["EntityBody"] as NullableDictionary<String, Object>);

            // 添加body提供者，从body中取值，只取第一层，
            // 下面的BindProperty方法，以前从body中并没有处理值的格式，
            // 强行绑定会出错记录在ModelState，在api中返回400错误，mvc不会
            bindingContext.ValueProvider = cubeBodyValueProvider;
        }
        if (!modelType.As<IEntity>()) return base.CreateModel(bindingContext);

        var fact = EntityFactory.CreateFactory(modelType);
        if (fact == null) return base.CreateModel(bindingContext);

        var pks = fact.Table.PrimaryKeys;
        var uk = fact.Unique;

        // 填充接口入参中的实体对象，Add新增除外
        IEntity entity = null;
        var act = bindingContext.ActionContext?.ActionDescriptor as ControllerActionDescriptor;
        if (act == null || !act.ActionName.EqualIgnoreCase("Add"))
        {
            if (uk != null)
            {
                // 查询实体对象用于编辑
                var id = bindingContext.ValueProvider.GetValue(uk.Name);
                if (id != ValueProviderResult.None) entity = fact.FindByKeyForEdit(id.ToString());
            }
            else if (pks.Length > 0)
            {
                // 查询实体对象用于编辑
                var exp = new WhereExpression();
                foreach (var item in pks)
                {
                    var v = bindingContext.ValueProvider.GetValue(item.Name);
                    if (v == ValueProviderResult.None) continue;
                    exp &= item.Equal(v.ChangeType(item.Type));
                }

                entity = fact.Find(exp);
            }
        }

        return entity ?? fact.Create(true);
    }

    protected override Boolean CanBindProperty(ModelBindingContext bindingContext, ModelMetadata propertyMetadata)
    {
        // 不要绑定复杂类型，那是扩展属性
        if (propertyMetadata.ModelType.GetTypeCode() == TypeCode.Object) return false;

        return base.CanBindProperty(bindingContext, propertyMetadata);
    }

    /// <summary>
    /// 绑定属性，在这里赋值
    /// </summary>
    /// <param name="bindingContext"></param>
    /// <returns></returns>
    protected override Task BindProperty(ModelBindingContext bindingContext)
    {
        var metadata = bindingContext.ModelMetadata;

        switch (metadata.ModelType.GetTypeCode())
        {
            case TypeCode.DateTime:
                // 客户端可能提交空时间，不要绑定属性，以免出现空时间验证失败
                //if (result.Model is not DateTime) return Task.CompletedTask;
                var dt = bindingContext.ValueProvider.GetValue(metadata.Name).Values;
                if (dt.Count == 0 || dt.ToString().IsNullOrEmpty()) return Task.CompletedTask;

                break;
        }

        return base.BindProperty(bindingContext);
    }

    /// <summary>
    /// 设置属性，二次处理
    /// </summary>
    /// <param name="bindingContext"></param>
    /// <param name="modelName"></param>
    /// <param name="propertyMetadata"></param>
    /// <param name="result"></param>
    protected override void SetProperty(ModelBindingContext bindingContext, String modelName, ModelMetadata propertyMetadata, ModelBindingResult result)
    {
        switch (propertyMetadata.ModelType.GetTypeCode())
        {
            case TypeCode.String:
                // 如果有多个值，则修改结果，避免 3,2,5 变成只有3
                var vs = bindingContext.ValueProvider.GetValue(modelName).Values;
                if (vs.Count > 1) result = ModelBindingResult.Success($",{vs.Where(e => e != "-1").Join()},");
                break;
        }

        base.SetProperty(bindingContext, modelName, propertyMetadata, result);
    }
}

/// <summary>实体模型绑定器提供者，为所有XCode实体类提供实体模型绑定器</summary>
public class EntityModelBinderProvider : IModelBinderProvider
{
    /// <summary>获取绑定器</summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        if (!context.Metadata.ModelType.As<IEntity>() &&
            !context.Metadata.ModelType.As<IModel>()) return null;

        var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
        var propertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
        foreach (var property in context.Metadata.Properties)
        {
            propertyBinders.Add(property, context.CreateBinder(property));
        }

        return new EntityModelBinder(propertyBinders, loggerFactory);
    }

    /// <summary>实例化</summary>
    public EntityModelBinderProvider() => XTrace.WriteLine("注册实体模型绑定器：{0}", typeof(EntityModelBinderProvider).FullName);
}