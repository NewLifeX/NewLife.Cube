using System.Collections;
using NewLife.Cube.ViewModels;

namespace NewLife.Cube;

/// <summary>魔方设置字段助手。为枚举型配置项统一提供下拉数据源，MVC 与 SPA 共用</summary>
public static class CubeSettingFields
{
    /// <summary>应用下拉数据源</summary>
    /// <param name="fields">配置字段集合</param>
    public static void Apply(IList<DataField> fields)
    {
        if (fields == null) return;

        // 安全防御模式
        Set(fields, nameof(CubeSetting.SecurityMode), () => new Dictionary<String, String>
        {
            ["0"] = T("Off"),
            ["1"] = T("Observe only"),
            ["2"] = T("Block"),
            ["3"] = T("Automatic"),
        });

        // Cookie 的 SameSite 模式（仅 API 版提供该配置项）
        Set(fields, "SameSiteMode", () => new Dictionary<String, String>
        {
            ["-1"] = T("Unspecified"),
            ["0"] = T("None"),
            ["1"] = T("Lax"),
            ["2"] = T("Strict"),
        });

        // iframe 嵌入策略
        Set(fields, nameof(CubeSetting.XFrameOptions), () => new Dictionary<String, String>
        {
            [""] = T("Same domain only"),
            ["SAMEORIGIN"] = T("Same domain and port"),
            ["ALLOWALL"] = T("Any domain"),
        });

        // 爬虫拦截返回的错误码
        Set(fields, nameof(CubeSetting.RobotError), () => new Dictionary<String, String>
        {
            ["0"] = T("Off"),
            ["404"] = T("404 Not Found"),
            ["500"] = T("500 Internal Server Error"),
        });

        // 用户在线记录范围
        Set(fields, nameof(CubeSetting.EnableUserOnline), () => new Dictionary<String, String>
        {
            ["0"] = T("Do not record"),
            ["1"] = T("Signed-in users only"),
            ["2"] = T("All visitors"),
        });

        // 验证码场景位掩码
        Set(fields, nameof(CubeSetting.CaptchaScene), () => new Dictionary<String, String>
        {
            ["0"] = T("Off"),
            ["1"] = T("Sign-in"),
            ["2"] = T("Registration"),
            ["3"] = $"{T("Sign-in")} + {T("Registration")}",
            ["4"] = T("Verification code requests"),
            ["5"] = $"{T("Sign-in")} + {T("Verification code requests")}",
            ["6"] = $"{T("Registration")} + {T("Verification code requests")}",
            ["7"] = T("All scenarios"),
        });

        // 多租户查询策略
        Set(fields, nameof(CubeSetting.TenantQueryPolicy), () => new Dictionary<String, String>
        {
            [nameof(TenantQueryPolicies.DenyWithEmpty)] = T("Deny with empty result"),
            [nameof(TenantQueryPolicies.ThrowOnMissingTenant)] = T("Throw an error"),
        });

        // 验证码风险阈值
        Set(fields, nameof(CubeSetting.CaptchaRiskThreshold), () => new Dictionary<String, String>
        {
            ["0"] = T("Intranet"),
            ["1"] = T("Internet"),
            ["2"] = T("Internet with recent failures"),
            ["3"] = T("Banned"),
        });

        // 附件存储类型
        Set(fields, nameof(CubeSetting.AttachmentStorage), () => new Dictionary<String, String>
        {
            ["Local"] = T("Local"),
            ["Oss"] = T("Alibaba Cloud OSS"),
            ["Cos"] = T("Tencent Cloud COS"),
            ["Qiniu"] = T("Qiniu"),
            ["EasyIO"] = T("EasyIO"),
        });

        // 列表表格分隔样式与行高密度（仅 MVC 版提供该配置项）
        Set(fields, "TableStyle", () => new Dictionary<String, String>
        {
            ["Light"] = T("Light - minimal thin lines"),
            ["Standard"] = T("Standard - clear row lines"),
            ["Grid"] = T("Grid - full cell borders"),
        });
        Set(fields, "TableDensity", () => new Dictionary<String, String>
        {
            ["Compact"] = T("Compact - dense"),
            ["Normal"] = T("Normal - relaxed"),
        });
    }

    private static void Set(IList<DataField> fields, String name, Func<IDictionary> factory)
    {
        var field = fields.FirstOrDefault(e => String.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        if (field == null) return;

        field.ItemType = "singleSelect";
        field.DataSource = _ => factory();
    }

    private static String T(String text) => CubeLocalization.T(text);
}
