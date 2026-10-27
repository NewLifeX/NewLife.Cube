using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace NewLife.Cube.Services;

/// <summary>Redis数据保护扩展</summary>
public static class DbDataProtectionBuilderExtensions
{
    private const String DataProtectionKeysName = "DataProtection-Keys";

    /// <summary>存储数据保护Key到Redis，自动识别已注入到容器的FullRedis或Redis单例</summary>
    /// <param name="builder">数据保护构建器</param>
    /// <param name="key">密钥在参数表中的存储键，空则使用 DataProtection-Keys</param>
    /// <returns>数据保护构建器，便于链式调用</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IDataProtectionBuilder PersistKeysToDb(this IDataProtectionBuilder builder, String key = null)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        if (key.IsNullOrEmpty()) key = DataProtectionKeysName;

        return PersistKeysToDbInternal(builder, key);
    }

    private static IDataProtectionBuilder PersistKeysToDbInternal(IDataProtectionBuilder builder, String key)
    {
        builder.Services.Configure(delegate (KeyManagementOptions options)
        {
            options.XmlRepository = new DbXmlRepository(key);
        });
        return builder;
    }
}
