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
    /// <summary>支持的语言。前三位固定为简体中文、繁体中文、英文，其余按 README 语种顺序</summary>
    public static String[] SupportedCultures { get; } =
    [
        "zh-CN", "zh-TW", "en-US",
        "es-ES", "de-DE", "ja-JP", "fr-FR", "pt-BR", "ru-RU", "ko-KR", "ar-SA",
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
                new QueryStringRequestCultureProvider { QueryStringKey = "lang", UIQueryStringKey = "lang" },
                new CookieRequestCultureProvider { CookieName = ".AspNet.Culture" },
                new AcceptLanguageHeaderRequestCultureProvider(),
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

        return app.UseRequestLocalization();
    }
    #endregion
}
