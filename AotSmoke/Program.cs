using System.Globalization;
using System.Text.Json.Serialization;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Common;
using NewLife.Cube.Extensions;
using NewLife.Cube.Models;
using NewLife.Cube.Services;
using NewLife.Cube.Web;

namespace AotSmoke;

/// <summary>ApiResponse 源生成序列化上下文</summary>
[JsonSerializable(typeof(ApiResponse<String>))]
internal partial class SmokeJsonContext : JsonSerializerContext { }

/// <summary>AOT 冒烟入口。覆盖魔方非 MVC 基座：本地化、响应包装、校验、威胁检测、UA 解析、页面设置</summary>
internal static class Program
{
    private static Int32 _pass;
    private static Int32 _fail;

    private static void Check(String name, Boolean ok, String? detail = null)
    {
        if (ok)
        {
            _pass++;
            Console.WriteLine($"PASS {name}");
        }
        else
        {
            _fail++;
            Console.WriteLine($"FAIL {name} {detail}");
        }
    }

    private static Int32 Main()
    {
        // NativeAOT 判真：原生发布件入口 Assembly.Location 为空
#pragma warning disable IL3000 // 该调用正是 AOT 判别本身
        Console.WriteLine($"AOT={(typeof(Program).Assembly.Location.IsNullOrEmpty() ? "yes" : "no")}");
#pragma warning restore IL3000

        // 1. 本地化：英文源文、简体、繁体、未知语言回退
        var old = Localization.Culture;
        try
        {
            Localization.Culture = new CultureInfo("zh-CN");
            Check("locale-zh-hans", Localization.Translate("Operation succeeded") == "操作成功", Localization.Translate("Operation succeeded"));
            Localization.Culture = new CultureInfo("zh-TW");
            Check("locale-zh-hant", Localization.Translate("Delete") == "刪除", Localization.Translate("Delete"));
            Localization.Culture = new CultureInfo("en-US");
            Check("locale-en", Localization.Translate("Operation succeeded") == "Operation succeeded");
            Localization.Culture = new CultureInfo("sw-KE");
            Check("locale-fallback", Localization.Translate("Operation succeeded") == "Operation succeeded");
        }
        catch (CultureNotFoundException)
        {
            Check("locale-invariant", true);
        }
        finally
        {
            Localization.Culture = old;
        }

        // 2. ApiResponse 包装与 STJ 源生成序列化
        var ok = "data".ToOkApiResponse();
        Check("api-response-code", ok.Code == 0 && !ok.Message.IsNullOrEmpty());
        var json = System.Text.Json.JsonSerializer.Serialize(ok, SmokeJsonContext.Default.ApiResponseString);
        Check("api-response-json", json.Contains("\"Code\":0") && json.Contains("data"), json);

        var fail = "data".ToFailApiResponse(CubeCode.Failed.ToInt(), "boom");
        Check("api-response-fail", fail.Code != 0 && fail.Message == "boom");

        // 3. 格式校验
        Check("valid-email", ValidFormatHelper.IsEmail("test@example.com"));
        Check("valid-mobile", ValidFormatHelper.IsMobile("13800138000"));
        Check("invalid-mobile", !ValidFormatHelper.IsMobile("10086"));

        // 4. 威胁检测
        var threat = ThreatDetector.Detect("/?id=1%20union%20select%201", null, "Mozilla/5.0");
        Check("threat-detect", threat != null, "no threat detected");
        Check("threat-clean", ThreatDetector.Detect("/Admin/User", "name=test", "Mozilla/5.0") == null);

        // 5. 浏览器特性解析
        var ua = new UserAgentParser();
        Check("ua-parse", ua.Parse("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0 Safari/537.36"));

        // 6. 空富文本判断
        Check("empty-html", EmptyHtmlHelper.IsEmptyHtml("<p><br></p>"));
        Check("non-empty-html", !EmptyHtmlHelper.IsEmptyHtml("<p>hello</p>"));

        // 7. 页面设置
        var setting = PageSetting.Global.Clone();
        Check("page-setting", setting != null && !ReferenceEquals(setting, PageSetting.Global));

        Console.WriteLine($"TOTAL pass={_pass} fail={_fail}");

        return _fail > 0 ? 1 : 0;
    }
}
