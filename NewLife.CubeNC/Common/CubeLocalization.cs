using System.Collections;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace NewLife.Cube;

/// <summary>魔方多语言支持</summary>
/// <remarks>
/// 复用 NewLife.Core 的本地化能力：英文为源文与兜底，<c>Locales/*.yml</c> 在编译期生成语言表。
/// 请求语言优先取 <c>?lang=</c>，其次 Cookie，最后 <c>Accept-Language</c>；
/// 未显式指定时跟随 <see cref="CultureInfo.CurrentUICulture"/>。
/// 基本设置指定了受支持的界面语言时全站锁定，忽略上述来源；留空表示自动跟随请求语言。
/// </remarks>
public static class CubeLocalization
{
    #region 属性
    /// <summary>支持的语言。直接取 Core 语言清单，与 Locales 词表集保持一致</summary>
    /// <remarks>魔方 Locales 覆盖 Core 内置语言；Core 新增语言时若魔方缺少词表，界面回退英文源文。</remarks>
    public static String[] SupportedCultures => Localization.SupportedCultures;
    #endregion

    #region 方法
    /// <summary>获取基本设置中的界面语言</summary>
    /// <returns>配置的语言名称；未配置时返回空</returns>
    /// <remarks>语言统一由 NewLife.Setting.Language 控制，魔方不再单独维护语言配置。留空表示自动跟随请求语言，指定受支持语言表示全站锁定。</remarks>
    public static String GetConfiguredLanguage() => NewLife.Setting.Current?.Language;

    /// <summary>获取锁定的界面语言。配置了受支持的界面语言时返回该语言，否则返回 null</summary>
    /// <returns>受支持的锁定语言；未配置或语言不受支持时返回 null</returns>
    /// <remarks>基本设置中的界面语言一经指定即全站生效，忽略 ?lang / Cookie / Accept-Language</remarks>
    public static String? GetForcedLanguage() => NormalizeCulture(GetConfiguredLanguage());

    /// <summary>翻译指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <returns>本地化文本；缺少翻译时返回原文</returns>
    public static String T(String template) => Localization.Translate(template);

    /// <summary>获取语言选项，供基本设置等页面的语言下拉框绑定</summary>
    /// <returns>语言键到显示名称的映射，空键表示自动跟随系统/浏览器</returns>
    /// <remarks>清单与显示名称来自 Core 的 <see cref="Localization.GetLanguageOptions"/>，含运行时注册的语言，名称随界面语言本地化。</remarks>
    public static IDictionary GetLanguageOptions()
    {
        var dic = new Dictionary<String, String>
        {
            [""] = T("Auto (follow system)"),
        };

        foreach (var item in Localization.GetLanguageOptions())
        {
            if (!item.Key.IsNullOrEmpty()) dic[item.Key] = item.Value;
        }

        return dic;
    }

    /// <summary>翻译并格式化指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <param name="args">格式化参数</param>
    /// <returns>本地化文本；缺少翻译或格式化失败时回退英文</returns>
    public static String T(String template, params Object?[]? args) => Localization.Translate(template, args);

    /// <summary>归一化语言名称。把 en、de-AT 等未列入支持列表的语言映射到最接近的受支持区域语言</summary>
    /// <param name="name">语言名称，可带 Accept-Language 权重（如 en-US,en;q=0.9）</param>
    /// <returns>受支持的语言名称；无法匹配时返回 null</returns>
    /// <remarks>
    /// 请求头与查询串常用裸语言码（en、ja）、非支持区域（de-AT）或旧版别名（in、iw、no、mo），
    /// 统一交给 Core 的 <see cref="Localization.NormalizeCulture"/> 归一化。
    /// </remarks>
    public static String? NormalizeCulture(String? name) => Localization.NormalizeCulture(name);

    /// <summary>注册请求级语言识别</summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCubeLocalization(this IServiceCollection services)
    {
        var cultures = new List<CultureInfo>();
        foreach (var item in SupportedCultures)
        {
            try
            {
                cultures.Add(new CultureInfo(item));
            }
            catch (CultureNotFoundException) { }
        }

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;

            // 锁定语言同时作为默认语言，请求无语言来源时也回退到该语言
            var forced = GetForcedLanguage();
            if (forced != null) options.SetDefaultCulture(forced);

            options.RequestCultureProviders =
            [
                new NormalizedQueryStringRequestCultureProvider { QueryStringKey = "lang", UIQueryStringKey = "lang" },
                new NormalizedCookieRequestCultureProvider { CookieName = ".AspNet.Culture" },
                new NormalizedAcceptLanguageHeaderRequestCultureProvider(),
            ];
        });

        return services;
    }

    /// <summary>启用请求级语言识别</summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器</returns>
    /// <remarks>
    /// 锁定语言经 <see cref="Localization.SetLanguage"/> 写入进程级 <see cref="Localization.Culture"/>；
    /// 未锁定语言时置空，由中间件写入的 <see cref="CultureInfo.CurrentUICulture"/>（AsyncLocal）按请求生效。
    /// </remarks>
    public static IApplicationBuilder UseCubeLocalization(this IApplicationBuilder app)
    {
        // 服务端日志与配置注释使用宿主语言，避免同一日志文件随请求语言混杂。
        // 配置受支持的界面语言时，界面与日志同时锁定为该语言。
        var autoLog = CultureInfo.CurrentUICulture;
        Localization.SetLanguage(GetForcedLanguage());
        Localization.LogCulture ??= autoLog;

        app.UseRequestLocalization();

        // 锁定语言在请求级再次生效，运行期修改设置后无需重启。
        // 未锁定语言时交还给请求语言来源（?lang → Cookie → Accept-Language）。
        return app.Use((context, next) =>
        {
            var forced = GetForcedLanguage();
            if (forced != null)
            {
                var culture = new CultureInfo(forced);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }

            // 运行期由强制切换为自动时清除进程级语言，恢复请求语言与启动时的日志语言
            if (forced != Localization.ForcedLanguage)
            {
                Localization.SetLanguage(forced);
                if (forced == null) Localization.LogCulture = autoLog;
            }

            return next();
        });
    }
    #endregion

    #region 语言来源
    /// <summary>把语言来源的结果归一化到受支持语言</summary>
    /// <param name="result">原始结果</param>
    /// <returns>归一化后的结果；候选都不受支持时返回 null</returns>
    internal static ProviderCultureResult? Normalize(ProviderCultureResult? result)
    {
        if (result == null) return null;

        var name = FindSupported(result.UICultures) ?? FindSupported(result.Cultures);
        return name == null ? null : new ProviderCultureResult(name);
    }

    private static String? FindSupported(System.Collections.IEnumerable? names)
    {
        if (names == null) return null;

        foreach (var item in names)
        {
            var name = NormalizeCulture(item?.ToString());
            if (name != null) return name;
        }

        return null;
    }
    #endregion
}

/// <summary>归一化查询串语言来源。支持 ?lang=en 这类裸语言码</summary>
internal class NormalizedQueryStringRequestCultureProvider : QueryStringRequestCultureProvider
{
    /// <summary>读取查询串语言</summary>
    /// <param name="httpContext">HTTP 上下文</param>
    /// <returns>语言结果</returns>
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
        => CubeLocalization.Normalize(await base.DetermineProviderCultureResult(httpContext));
}

/// <summary>归一化 Cookie 语言来源。支持区域语言写入的 Cookie 值</summary>
internal class NormalizedCookieRequestCultureProvider : CookieRequestCultureProvider
{
    /// <summary>读取 Cookie 语言</summary>
    /// <param name="httpContext">HTTP 上下文</param>
    /// <returns>语言结果</returns>
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
        => CubeLocalization.Normalize(await base.DetermineProviderCultureResult(httpContext));
}

/// <summary>归一化 Accept-Language 语言来源。支持 en、de-AT 等非支持列表中的语言</summary>
internal class NormalizedAcceptLanguageHeaderRequestCultureProvider : RequestCultureProvider
{
    /// <summary>读取请求头语言。按客户端偏好顺序取首个受支持语言</summary>
    /// <param name="httpContext">HTTP 上下文</param>
    /// <returns>语言结果</returns>
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        foreach (var header in httpContext.Request.Headers.AcceptLanguage)
        {
            if (header.IsNullOrEmpty()) continue;

            foreach (var item in header.Split(','))
            {
                var name = CubeLocalization.NormalizeCulture(item);
                if (name != null) return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(name));
            }
        }

        return Task.FromResult<ProviderCultureResult?>(null);
    }
}
