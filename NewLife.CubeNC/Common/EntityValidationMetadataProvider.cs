using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using NewLife.Reflection;
using XCode;

namespace NewLife.Cube;

/// <summary>实体验证元数据提供者</summary>
public class EntityValidationMetadataProvider : IValidationMetadataProvider
{
    /// <summary>创建验证元数据</summary>
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        // IEntity参数，不需要验证子级
        if (context.Key.ModelType.As<IEntity>())
        {
            context.ValidationMetadata.ValidateChildren = false;
        }
    }
}
