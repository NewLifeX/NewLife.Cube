using System.Globalization;
using NewLife.Common;
using NewLife.Cube.Services;
using NewLife.Reflection;
using XCode.Membership;

namespace NewLife.Cube.Models;

/// <summary>站点语言信息</summary>
public class SiteLanguageModel
{
    /// <summary>当前请求解析出的语言</summary>
    public String Current { get; set; }

    /// <summary>站点默认语言。浏览器语言不受支持时回退到该语言</summary>
    public String Default { get; set; }

    /// <summary>是否锁定语言。true 时前端隐藏语言切换入口</summary>
    public Boolean Locked { get; set; }

    /// <summary>支持的语言列表。复用 NewLife.Core 的语言清单，含运行时注册语言</summary>
    public IList<LanguageInfo> Options { get; set; } = [];
}

/// <summary>站点 AI 能力</summary>
public class SiteAiFeatureModel
{
    /// <summary>是否启用 AI</summary>
    public Boolean Enabled { get; set; }

    /// <summary>AI 主色</summary>
    public String PrimaryColor { get; set; }

    /// <summary>AI 辅色</summary>
    public String SecondaryColor { get; set; }

    /// <summary>Mermaid 脚本地址</summary>
    public String MermaidUrl { get; set; }
}

/// <summary>站点功能开关</summary>
public class SiteFeaturesModel
{
    /// <summary>AI 能力</summary>
    public SiteAiFeatureModel Ai { get; set; }

    /// <summary>实例化站点功能开关</summary>
    /// <param name="set">魔方设置</param>
    public SiteFeaturesModel(CubeSetting set)
    {
        Ai = new SiteAiFeatureModel
        {
            Enabled = set.AISwitch,
            PrimaryColor = set.AIPrimaryColor,
            SecondaryColor = set.AISecondaryColor,
            MermaidUrl = set.GetMermaidUrl(),
        };
    }
}

/// <summary>站点配置。品牌字段为当前租户合并后的有效值，供登录页与前台页面匿名读取</summary>
public class SiteConfigModel
{
    private readonly CubeSetting _set;
    private readonly SysConfig _sys;
    private readonly ITenant _tenant;
    private readonly TenantSiteOptions _options;
    private readonly String _language;

    /// <summary>实例化站点配置</summary>
    /// <param name="tenant">当前租户，为 null 时返回站点级配置</param>
    public SiteConfigModel(ITenant tenant)
    {
        _set = CubeSetting.Current;
        _sys = SysConfig.Current;
        _tenant = tenant;
        _options = TenantSiteOptionsStore.Load(tenant?.Code);

        Language = BuildLanguage();
        _language = Language.Current;
        Features = new(_set);
    }

    /// <summary>站点名称。按语言的租户覆盖优先，其次租户名称，最后回退全局显示名</summary>
    public String Name => Pick(_options?.Names) ?? (!_tenant?.Name.IsNullOrEmpty() == true ? _tenant.Name : _sys.DisplayName);

    /// <summary>站点 Logo。按语言的租户覆盖优先，其次租户 Logo，最后回退登录页 Logo 配置；留空时由前端使用内置默认</summary>
    public String Logo => Pick(_options?.Logos) ?? (!_tenant?.Logo.IsNullOrEmpty() == true ? _tenant.Logo : _set.LoginLogo);

    /// <summary>登录页背景图。留空时由前端使用内置默认</summary>
    public String LoginBackground => _set.LoginBackground;

    /// <summary>登录提示。留空不显示</summary>
    public String LoginTip => _set.LoginTip;

    /// <summary>版权信息。服务端已替换变量，前端可直接渲染</summary>
    public String Copyright => _set.GetCopyright();

    /// <summary>备案号。留空不显示</summary>
    public String Registration => _set.Registration;

    /// <summary>站点程序版本</summary>
    public String Version => AssemblyX.Entry?.FileVersion;

    /// <summary>站点构建时间</summary>
    public String BuildTime
    {
        get
        {
            var time = AssemblyX.Entry?.Compile ?? DateTime.MinValue;
            return time.Year > 2000 ? time.ToString("yyyy-MM-dd HH:mm:ss") : null;
        }
    }

    /// <summary>当前租户编码。非空表示处于租户上下文，空表示非租户上下文</summary>
    public String TenantCode => _tenant?.Code;

    /// <summary>语言信息</summary>
    public SiteLanguageModel Language { get; }

    /// <summary>功能开关</summary>
    public SiteFeaturesModel Features { get; }

    /// <summary>按当前语言取租户配置的品牌覆盖值</summary>
    /// <param name="map">语言到取值的映射。键按受支持语言归一化后比对，如 zh-Hans 与 zh-CN 视为同一语言</param>
    /// <returns>命中的值；未配置返回 null</returns>
    private String Pick(IDictionary<String, String> map)
    {
        if (map == null || map.Count == 0 || _language.IsNullOrEmpty()) return null;

        foreach (var item in map)
        {
            if (item.Value.IsNullOrEmpty()) continue;
            if (CubeLocalization.NormalizeCulture(item.Key).EqualIgnoreCase(_language)) return item.Value;
        }

        return null;
    }

    private SiteLanguageModel BuildLanguage()
    {
        // 合并优先级：租户默认语言 → 站点默认语言 → 英文
        var siteDefault = CubeLocalization.NormalizeCulture(_options?.DefaultLanguage)
            ?? CubeLocalization.NormalizeCulture(NewLife.Setting.Current?.Language)
            ?? "en-US";
        var current = CubeLocalization.NormalizeCulture(CultureInfo.CurrentUICulture.Name) ?? siteDefault;

        // 租户允许语言白名单：为空不限制；当前语言不在白名单内时回退租户默认语言
        var allowed = _options?.Languages
            ?.Select(e => CubeLocalization.NormalizeCulture(e))
            .Where(e => !e.IsNullOrEmpty())
            .ToArray() ?? [];
        if (allowed.Length > 0 && !allowed.Contains(current, StringComparer.OrdinalIgnoreCase)) current = siteDefault;

        // 站点配置了受支持的界面语言即全站锁定，前端据此隐藏语言切换入口
        var locked = CubeLocalization.GetForcedLanguage() != null;

        var options = Localization.GetLanguages();
        if (allowed.Length > 0) options = options.Where(e => allowed.Contains(e.Name, StringComparer.OrdinalIgnoreCase)).ToList();

        return new SiteLanguageModel
        {
            Current = current,
            Default = siteDefault,
            Locked = locked,
            Options = options,
        };
    }
}
