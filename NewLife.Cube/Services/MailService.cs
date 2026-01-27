using System.Text;
using NewLife.Caching;
using NewLife.Cube.Entity;
using NewLife.Security;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>邮件配置服务。管理多租户邮件渠道配置</summary>
/// <remarks>实例化邮件配置服务</remarks>
/// <param name="cacheProvider">缓存提供者</param>
public class MailService(ICacheProvider cacheProvider)
{
    /// <summary>获取邮件配置。根据租户和操作类型获取可用的邮件配置</summary>
    /// <param name="tenantId">租户编号。0表示系统全局</param>
    /// <param name="action">操作类型。Login/Reset/Bind/Notify</param>
    /// <returns>邮件配置，未找到时返回null</returns>
    public MailConfigModel GetConfig(Int32 tenantId, String action)
    {
        // 优先从缓存获取
        var cache = cacheProvider.InnerCache;
        var cacheKey = $"MailConfig:{tenantId}:{action}";
        var config = cache.Get<MailConfigModel>(cacheKey);
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
    /// <returns>邮件配置</returns>
    protected virtual MailConfigModel LoadFromDatabase(Int32 tenantId, String action)
    {
        // 构建查询条件，先查指定租户，未找到再查全局配置
        var tenantIds = tenantId > 0 ? [tenantId, 0] : new[] { 0 };

        // 根据操作类型确定需要启用的功能字段
        var action2 = action.ToLower();

        // 查询满足条件的配置
        foreach (var tid in tenantIds)
        {
            var list = MailConfig.FindAllByTenantId(tid);
            if (list == null || list.Count == 0) continue;

            // 筛选启用的配置
            var configs = list.Where(e => e.Enable);

            // 按优先级降序排序，选择第一个
            var config = configs.OrderByDescending(e => e.Priority).FirstOrDefault();
            if (config != null)
            {
                // 转换为模型对象
                return config.ToModel();
            }
        }

        return null;
    }

    /// <summary>清除配置缓存</summary>
    /// <param name="tenantId">租户编号。-1表示清除所有</param>
    public void ClearCache(Int32 tenantId = -1)
    {
        var cache = cacheProvider.InnerCache;
        if (tenantId < 0)
        {
            // 清除所有邮件配置缓存
            // 由于Cache没有按前缀删除的功能，这里只能留空
            // 实际使用时可以通过重启服务或等待缓存过期来刷新
        }
        else
        {
            foreach (var action in new[] { "Login", "Reset", "Bind", "Notify" })
            {
                cache.Remove($"MailConfig:{tenantId}:{action}");
            }
        }
    }
    /// <summary>发送邮件验证码</summary>
    /// <param name="action">操作类型：login/bind/reset</param>
    /// <param name="mail">邮箱地址</param>
    /// <param name="code">验证码</param>
    /// <param name="config">邮件配置</param>
    /// <returns>验证码记录</returns>
    public async Task<VerifyCodeRecord> SendVerifyCode(String action, String mail, String code, MailConfigModel config)
    {
        var key = $"MailVerify:{config.Provider}:{config.Id}";
        var provider = cacheProvider.InnerCache.Get<IMailVerifyCode>(key);
        if (provider == null)
        {
            // 根据配置创建邮件验证码提供者
            if (config.Provider == "Smtp" || config.Provider.IsNullOrEmpty())
            {
                provider = new SmtpMailVerifyCode
                {
                    Server = config.Server,
                    Port = config.Port > 0 ? config.Port : 25,
                    EnableSsl = config.EnableSsl,
                    From = config.FromMail,
                    DisplayName = config.FromName,
                    Username = config.UserName,
                    Password = config.Password,
                };

                cacheProvider.InnerCache.Set(key, provider, 600);
            }
            provider = cacheProvider.InnerCache.Get<IMailVerifyCode>(key);
        }

        var record = new VerifyCodeRecord
        {
            TenantId = config.TenantId,
            Action = action,
            Channel = "Mail",
            ConfigId = config.Id,
            ConfigName = config + "",
            Provider = config.Provider,
            UserId = ManageProvider.User?.ID ?? 0,
            Target = mail,
            ExpireTime = DateTime.Now.AddSeconds(config.Expire > 0 ? config.Expire : 300),
        };

        try
        {
            switch (action.ToLower())
            {
                case "login":
                    record.Result = await provider.SendLogin(mail, code, config.Expire);
                    break;
                case "bind":
                    record.Result = await provider.SendBind(mail, code, config.Expire);
                    break;
                case "reset":
                    record.Result = await provider.SendReset(mail, code, config.Expire);
                    break;
                case "notify":
                default://TODO 通知
                    //record.Result = await provider.SendNotify(mail, code, config.Expire);
                    break;
            }

            record.Success = true;
        }
        catch (Exception ex)
        {
            record.Success = false;
            record.Result = ex.Message;
        }
        finally
        {
            record.Insert();
        }

        return record;
    }

    /// <summary>默认验证码长度</summary>
    public const Int32 DefaultCodeLength = 4;

    /// <summary>默认有效期（分钟）</summary>
    public const Int32 DefaultExpireMinutes = 5;

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
