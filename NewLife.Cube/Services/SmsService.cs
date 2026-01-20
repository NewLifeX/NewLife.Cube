using System.Text;
using NewLife.Caching;
using NewLife.Cube.Entity;
using NewLife.Security;

namespace NewLife.Cube.Services;

/// <summary>短信配置服务。管理多租户短信渠道配置</summary>
/// <remarks>实例化短信配置服务</remarks>
/// <param name="cache">缓存</param>
public class SmsService(ICache cache)
{
    /// <summary>获取短信配置。根据租户和操作类型获取可用的短信配置</summary>
    /// <param name="tenantId">租户编号。0表示系统全局</param>
    /// <param name="action">操作类型。Login/Reset/Bind/Notify</param>
    /// <returns>短信配置，未找到时返回null</returns>
    public SmsConfigModel GetConfig(Int32 tenantId, String action)
    {
        // 优先从缓存获取
        var cacheKey = $"SmsConfig:{tenantId}:{action}";
        var config = cache.Get<SmsConfigModel>(cacheKey);
        if (config != null) return config;

        // 从数据库查询，待实体类生成后实现
        config = LoadFromDatabase(tenantId, action);
        if (config != null)
            cache.Set(cacheKey, config, 300);

        return config;
    }

    /// <summary>从数据库加载配置</summary>
    /// <param name="tenantId">租户编号</param>
    /// <param name="action">操作类型</param>
    /// <returns>短信配置</returns>
    protected virtual SmsConfigModel LoadFromDatabase(Int32 tenantId, String action)
    {
        // 待 SmsConfig 实体类生成后实现
        // 查询顺序：
        // 1. 先查指定租户的配置
        // 2. 未找到则查系统全局配置（TenantId=0）
        // 3. 按优先级排序，选择启用对应功能的第一个配置

        // 临时返回null，待实体类生成后补充实现
        return null;
    }

    /// <summary>清除配置缓存</summary>
    /// <param name="tenantId">租户编号。-1表示清除所有</param>
    public void ClearCache(Int32 tenantId = -1)
    {
        if (tenantId < 0)
        {
            // 清除所有短信配置缓存
            // 由于Cache没有按前缀删除的功能，这里只能留空
            // 实际使用时可以通过重启服务或等待缓存过期来刷新
        }
        else
        {
            foreach (var action in new[] { "Login", "Reset", "Bind", "Notify" })
            {
                cache.Remove($"SmsConfig:{tenantId}:{action}");
            }
        }
    }

    /// <summary>默认验证码长度</summary>
    public const Int32 DefaultCodeLength = 4;

    /// <summary>默认有效期（分钟）</summary>
    public const Int32 DefaultExpireMinutes = 5;

    /// <summary>验证手机号格式是否正确</summary>
    /// <param name="phone">手机号</param>
    /// <returns>格式正确返回true</returns>
    public static Boolean IsValidPhone(String phone)
        => !String.IsNullOrWhiteSpace(phone)
           && phone.Length >= 11
           && phone.All(c => c >= '0' && c <= '9');

    /// <summary>生成验证码</summary>
    /// <param name="codeLength">验证码长度。默认4位</param>
    /// <returns>验证码字符串</returns>
    public static String GenerateVerifyCode(Int32 codeLength = DefaultCodeLength)
    {
        if (codeLength <= 0) codeLength = DefaultCodeLength;

        var seed = $"{Rand.Next(Int32.MaxValue / 10, Int32.MaxValue)}";
        var sb = new StringBuilder();
        for (var i = 0; i < codeLength; i++)
        {
            var index = i % seed.Length;
            var c = seed[index]; // 避免超长，超过长度时会循环取
            sb.Append(c);
        }
        return sb.ToString();
    }
}
