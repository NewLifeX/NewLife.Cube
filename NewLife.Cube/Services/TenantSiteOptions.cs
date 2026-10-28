using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>租户站点选项。按租户配置默认语言、允许语言，以及按语言覆盖的品牌名称与 Logo</summary>
/// <remarks>
/// 不新增表：复用系统级字典参数（<see cref="Parameter"/>，用户 0，类别 <c>TenantSite</c>，名称=租户编码，长值=JSON），
/// 管理员可在「参数管理」中维护。读取按租户编码命中唯一索引，不做进程缓存，修改后即时生效。
/// </remarks>
public class TenantSiteOptions
{
    /// <summary>租户默认语言。为空时使用站点默认语言</summary>
    public String DefaultLanguage { get; set; }

    /// <summary>允许的语言白名单。为空表示不限制，取全部受支持语言</summary>
    public IList<String> Languages { get; set; } = [];

    /// <summary>按语言的站点名称覆盖。键为语言名称，如 zh-Hans</summary>
    public IDictionary<String, String> Names { get; set; } = new Dictionary<String, String>();

    /// <summary>按语言的 Logo 覆盖。键为语言名称，如 zh-Hans</summary>
    public IDictionary<String, String> Logos { get; set; } = new Dictionary<String, String>();
}

/// <summary>租户站点选项存取。以字典参数形式持久化，按租户编码读写</summary>
public static class TenantSiteOptionsStore
{
    /// <summary>字典参数分类</summary>
    public const String Category = "TenantSite";

    /// <summary>读取租户站点选项</summary>
    /// <param name="tenantCode">租户编码。为空时返回 null</param>
    /// <returns>租户选项；未配置或解析失败时返回 null</returns>
    public static TenantSiteOptions Load(String tenantCode)
    {
        if (tenantCode.IsNullOrEmpty()) return null;

        var p = Parameter.Find(Parameter._.UserID == 0 & Parameter._.Category == Category & Parameter._.Name == tenantCode);
        if (p == null || p.LongValue.IsNullOrEmpty()) return null;

        try
        {
            return p.LongValue.ToJsonEntity<TenantSiteOptions>() ?? new TenantSiteOptions();
        }
        catch
        {
            // 配置损坏时按未配置处理，避免影响匿名站点接口
            return null;
        }
    }

    /// <summary>保存租户站点选项</summary>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="options">租户选项</param>
    public static void Save(String tenantCode, TenantSiteOptions options)
    {
        if (tenantCode.IsNullOrEmpty()) throw new ArgumentNullException(nameof(tenantCode));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var p = Parameter.GetOrAdd(0, Category, tenantCode);
        p.LongValue = options.ToJson();
        p.Update();
    }

    /// <summary>删除租户站点选项</summary>
    /// <param name="tenantCode">租户编码</param>
    public static void Remove(String tenantCode)
    {
        if (tenantCode.IsNullOrEmpty()) return;

        Parameter.Find(Parameter._.UserID == 0 & Parameter._.Category == Category & Parameter._.Name == tenantCode)?.Delete();
    }
}
