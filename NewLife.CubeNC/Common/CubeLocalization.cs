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
/// </remarks>
public static class CubeLocalization
{
    #region 属性
    /// <summary>支持的语言。前三位固定为简体中文、繁体中文、英文，其余与 Locales 词表集对齐</summary>
    public static String[] SupportedCultures { get; } =
    [
        "zh-CN", "zh-TW", "en-US",
        "es-ES", "de-DE", "ja-JP", "fr-FR", "pt-BR", "ru-RU", "ko-KR", "ar-SA",
        "bn-BD", "cs-CZ", "da-DK", "el-GR", "fa-IR", "fi-FI", "he-IL", "hi-IN", "hu-HU",
        "id-ID", "it-IT", "ms-MY", "nb-NO", "nl-NL", "pl-PL", "ro-RO", "sv-SE", "th-TH",
        "tr-TR", "uk-UA", "vi-VN",
    ];
    #endregion

    #region 方法
    /// <summary>翻译指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <returns>本地化文本；缺少翻译时返回原文</returns>
    public static String T(String template) => Localization.Translate(template);

    /// <summary>翻译并格式化指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <param name="args">格式化参数</param>
    /// <returns>本地化文本；缺少翻译或格式化失败时回退英文</returns>
    public static String T(String template, params Object?[]? args) => Localization.Translate(template, args);

    /// <summary>归一化语言名称。把 en、de-AT 等未列入支持列表的语言映射到最接近的受支持区域语言</summary>
    /// <param name="name">语言名称，可带 Accept-Language 权重（如 en-US,en;q=0.9）</param>
    /// <returns>受支持的语言名称；无法匹配时返回 null</returns>
    /// <remarks>
    /// 请求头与查询串常用裸语言码（en、ja）或非支持区域（de-AT），
    /// 而支持列表是区域语言（en-US、ja-JP、de-DE），直接匹配会失败并回退宿主语言。
    /// </remarks>
    public static String? NormalizeCulture(String? name)
    {
        if (name.IsNullOrWhiteSpace()) return null;

        // 取首个候选并去掉权重
        var first = name.Split(',')[0].Trim();
        var p = first.IndexOf(';');
        if (p > 0) first = first[..p].Trim();
        if (first.IsNullOrEmpty()) return null;

        // 精确匹配
        foreach (var item in SupportedCultures)
        {
            if (item.EqualIgnoreCase(first)) return item;
        }

        // 语言前缀匹配：en → en-US，de-AT → de-DE，zh → zh-CN
        var lang = first;
        var dash = first.IndexOf('-');
        if (dash > 0) lang = first[..dash];
        foreach (var item in SupportedCultures)
        {
            if (item.StartsWith(lang + "-", StringComparison.OrdinalIgnoreCase)) return item;
        }

        return null;
    }

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

            var language = CubeSetting.Current?.Language;
            if (!language.IsNullOrWhiteSpace())
            {
                try
                {
                    options.SetDefaultCulture(language);
                }
                catch (CultureNotFoundException) { }
            }

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
    /// <see cref="Localization.Culture"/> 是进程级全局状态，置空后由中间件写入的
    /// <see cref="CultureInfo.CurrentUICulture"/>（AsyncLocal）按请求生效。
    /// </remarks>
    public static IApplicationBuilder UseCubeLocalization(this IApplicationBuilder app)
    {
        Localization.Culture = null;

        // 服务端日志与配置注释使用宿主语言，避免同一日志文件随请求语言混杂。
        // 显式配置 CubeSetting.Language 时优先使用，否则固定为启动时的系统语言。
        var language = CubeSetting.Current?.Language;
        if (!language.IsNullOrWhiteSpace())
        {
            try
            {
                Localization.LogCulture = new CultureInfo(language);
            }
            catch (CultureNotFoundException) { }
        }
        else
        {
            Localization.LogCulture = CultureInfo.CurrentUICulture;
        }

        return app.UseRequestLocalization();
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
