namespace NewLife.Cube;

/// <summary>通知内置渠道名称。发送通知时优先使用这些常量，避免手写字符串</summary>
public static class NotificationChannels
{
    /// <summary>站内信</summary>
    public const String InApp = "InApp";

    /// <summary>邮件</summary>
    public const String Mail = "Mail";

    /// <summary>短信</summary>
    public const String Sms = "Sms";

    /// <summary>APP 推送</summary>
    public const String AppPush = "AppPush";

    /// <summary>通用 Webhook</summary>
    public const String Webhook = "Webhook";

    /// <summary>钉钉机器人</summary>
    public const String DingTalk = "DingTalk";

    /// <summary>企业微信机器人</summary>
    public const String WeCom = "WeCom";

    /// <summary>内置渠道列表</summary>
    public static String[] BuiltIn => [InApp, Mail, Sms, AppPush, Webhook, DingTalk, WeCom];
}
