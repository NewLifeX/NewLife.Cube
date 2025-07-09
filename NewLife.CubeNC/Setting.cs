﻿using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NewLife.Configuration;
using NewLife.Security;
using XCode.Configuration;

namespace NewLife.Cube;

/// <summary>魔方设置</summary>
[Obsolete("=>CubeSetting")]
public class Setting : CubeSetting { }

/// <summary>魔方设置</summary>
[DisplayName("魔方设置")]
[Config("Cube")]
public class CubeSetting : Config<CubeSetting>
{
    #region 静态
    /// <summary>指向数据库参数字典表</summary>
    static CubeSetting() => Provider = new DbConfigProvider { UserId = 0, Category = "Cube" };
    #endregion

    #region 通用
    /// <summary>是否启用调试。默认true</summary>
    [Description("调试")]
    [Category("通用")]
    public Boolean Debug { get; set; } = true;

    /// <summary>显示运行时间</summary>
    [Description("显示运行时间")]
    [Category("通用")]
    public Boolean ShowRunTime { get; set; } = true;

    /// <summary>头像目录。设定后下载远程头像到本地，默认Avatars子目录，web上一级Avatars。清空表示不抓取</summary>
    [Description("头像目录。设定后下载远程头像到本地，默认Avatars子目录，web上一级Avatars。清空表示不抓取")]
    [Category("通用")]
    public String AvatarPath { get; set; } = "Avatars";

    /// <summary>上传目录。默认Uploads</summary>
    [Description("上传目录。默认Uploads")]
    [Category("通用")]
    public String UploadPath { get; set; } = "Uploads";

    /// <summary>静态资源目录。默认wwwroot</summary>
    [Description("静态资源目录。默认wwwroot")]
    [Category("通用")]
    public String WebRootPath { get; set; } = "wwwroot";

    /// <summary>资源地址。指向CDN，如 https://sso.newlifex.com/content/，留空表示使用本地</summary>
    [Description("资源地址。指向CDN，如 https://sso.newlifex.com/content/，留空表示使用本地")]
    [Category("通用")]
    public String ResourceUrl { get; set; }

    /// <summary>跨域来源。允许其它源访问当前域，指定其它源http地址，*表示任意域</summary>
    [Description("跨域来源。允许其它源访问当前域，指定其它源http地址，*表示任意域")]
    [Category("通用")]
    public String CorsOrigins { get; set; }

    /// <summary>在iframe中展示。SAMEORIGIN-允许相同域名，ALLOWALL-允许任何域名</summary>
    [Description("在iframe中展示。默认为空-只允许相同域名，SAMEORIGIN-允许相同域名和端口，ALLOWALL-允许任何域名")]
    [Category("通用")]
    public String XFrameOptions { get; set; }

    ///// <summary>Cookie模式。token的cookies默认模式（ -1 Unspecified，0 None，1 Lax，2 Strict）</summary>
    //[Description("Cookie模式。token的cookies默认模式（ -1 Unspecified，0 None，1 Lax，2 Strict）")]
    //[Category("通用")]
    //public Int32 SameSiteMode { get; set; } = -1;

    ///// <summary>Cookie域名。可用于把Cookie写到顶级域名，默认为空写当前域。写顶级域要求https，同时会导致普通http无法在本地域写同名键值</summary>
    //[Description("Cookie域名。可用于把Cookie写到顶级域名，默认为空写当前域。写顶级域要求https，同时会导致普通http无法在本地域写同名键值")]
    //[Category("通用")]
    //public String CookieDomain { get; set; }

    /// <summary>分享有效期。分享令牌的有效期，默认7200秒</summary>
    [Description("分享有效期。分享令牌的有效期，默认7200秒")]
    [Category("通用")]
    public Int32 ShareExpire { get; set; } = 7200;

    /// <summary>机器人错误码。设置后拦截各种爬虫并返回相应错误，如404/500，默认0不拦截</summary>
    [Description("机器人错误码。设置后拦截各种爬虫并返回相应错误，如404/500，默认0不拦截")]
    [Category("通用")]
    public Int32 RobotError { get; set; }

    ///// <summary>强制使用SSL。强制访问https，使用http访问时跳转</summary>
    //[Description("强制使用SSL。强制访问https，使用http访问时跳转")]
    //[Category("通用")]
    //public Boolean ForceSSL { get; set; }

    /// <summary>强制跳转。指定目标schema和host，在GET访问发现不一致时强制跳转，host支持*。常用于强制跳转https，如https://*:8081</summary>
    [Description("强制跳转。指定目标schema和host，在GET访问发现不一致时强制跳转，host支持*。常用于强制跳转https，如https://*:8081")]
    [Category("通用")]
    public String ForceRedirect { get; set; }
    #endregion

    #region 用户登录
    /// <summary>默认角色。默认普通用户</summary>
    [Description("默认角色。默认普通用户")]
    [Category("用户登录")]
    public String DefaultRole { get; set; } = "普通用户";

    /// <summary>允许密码登录。允许输入用户名密码进行登录</summary>
    [Description("允许密码登录。允许输入用户名密码进行登录")]
    [Category("用户登录")]
    public Boolean AllowLogin { get; set; } = true;

    /// <summary>允许注册。允许输入用户名密码进行注册</summary>
    [Description("允许注册。允许输入用户名密码进行注册")]
    [Category("用户登录")]
    public Boolean AllowRegister { get; set; } = true;

    /// <summary>自动注册。默认true，SSO登录后，如果本地未登录，自动注册新用户。全局设置和OAuth应用设置只要有一个启用则表示使用</summary>
    [Description("自动注册。默认true，SSO登录后，如果本地未登录，自动注册新用户。全局设置和OAuth应用设置只要有一个启用则表示使用")]
    [Category("用户登录")]
    public Boolean AutoRegister { get; set; } = true;

    ///// <summary>密码强度。最小密码长度，默认6</summary>
    //[Description("密码强度。最小密码长度，默认6")]
    //[Category("用户登录")]
    //public Int32 MinPasswordLength { get; set; } = 6;

    /// <summary>密码强度。*表示无限制，默认8位起，数字大小写字母和符号。简易版^(?=.*\\d.*)(?=.*[a-zA-Z].*).{6,32}$</summary>
    [Description("密码强度。*表示无限制，默认8位起，数字大小写字母和符号。简易版^(?=.*\\d.*)(?=.*[a-zA-Z].*).{6,32}$")]
    [Category("用户登录")]
    public String PaswordStrength { get; set; } = @"^(?=.*\d.*)(?=.*[a-z].*)(?=.*[A-Z].*)(?=.*[^(0-9a-zA-Z)].*).{8,32}$";

    /// <summary>登录失败次数。短时间内，相同用户或IP地址连续登录错误次数达到该值后禁止登录，默认5</summary>
    [Description("登录失败次数。短时间内，相同用户或IP地址连续登录错误次数达到该值后禁止登录，默认5")]
    [Category("用户登录")]
    public Int32 MaxLoginError { get; set; } = 5;

    /// <summary>登录失败次数。短时间内，相同用户或IP地址连续登录错误次数达到该值后禁止登录，默认5</summary>
    [Description("登录封禁时间。触发风控禁止登录后的禁止时间，默认300秒")]
    [Category("用户登录")]
    public Int32 LoginForbiddenTime { get; set; } = 300;

    /// <summary>强行绑定用户名。根据SSO用户名强制绑定本地同名用户，而不需要增加提供者前缀，一般用于用户中心</summary>
    [Description("强行绑定用户名。根据SSO用户名强制绑定本地同名用户，而不需要增加提供者前缀，一般用于用户中心")]
    [Category("用户登录")]
    public Boolean ForceBindUser { get; set; } = true;

    /// <summary>绑定用户代码。根据SSO用户代码强制绑定本地用户</summary>
    [Description("绑定用户代码。根据SSO用户代码强制绑定本地用户")]
    [Category("用户登录")]
    public Boolean ForceBindUserCode { get; set; } = true;

    /// <summary>绑定用户手机。根据SSO用户手机强制绑定本地用户</summary>
    [Description("绑定用户手机。根据SSO用户手机强制绑定本地用户")]
    [Category("用户登录")]
    public Boolean ForceBindUserMobile { get; set; } = true;

    /// <summary>绑定用户邮箱。根据SSO用户邮箱强制绑定本地用户</summary>
    [Description("绑定用户邮箱。根据SSO用户邮箱强制绑定本地用户")]
    [Category("用户登录")]
    public Boolean ForceBindUserMail { get; set; } = true;

    /// <summary>绑定用户昵称。根据SSO用户昵称强制绑定本地用户，内部SSO可用，不建议用于社交网络，重名太多</summary>
    [Description("绑定用户昵称。根据SSO用户昵称强制绑定本地用户，内部SSO可用，不建议用于社交网络，重名太多")]
    [Category("用户登录")]
    public Boolean ForceBindNickName { get; set; } = true;

    /// <summary>使用Sso角色。SSO登录后继续使用SSO角色，默认true；否则使用DefaultRole</summary>
    [Description("使用Sso角色。SSO登录后继续使用SSO角色，默认true；否则使用DefaultRole")]
    [Category("用户登录")]
    public Boolean UseSsoRole { get; set; } = true;

    ///// <summary>保留本地角色。本地角色与SSO角色合并，默认false</summary>
    //[Description("保留本地角色。本地角色与SSO角色合并，默认false")]
    //[Category("用户登录")]
    //public Boolean KeepLocalRole { get; set; }

    /// <summary>使用Sso部门。SSO登录后继续使用SSO部门，默认true</summary>
    [Description("使用Sso部门。SSO登录后继续使用SSO部门，默认true")]
    [Category("用户登录")]
    public Boolean UseSsoDepartment { get; set; } = true;

    /// <summary>注销所有系统。false仅注销本系统，默认true时注销SsoServer</summary>
    [Description("注销所有系统。false仅注销本系统，默认true时注销SsoServer")]
    [Category("用户登录")]
    public Boolean LogoutAll { get; set; } = true;

    /// <summary>会话超时。单点登录后会话超时时间，该时间内可借助Cookie登录，默认0s</summary>
    [Description("会话超时。单点登录后会话超时时间，该时间内可借助Cookie登录，默认0s")]
    [Category("用户登录")]
    public Int32 SessionTimeout { get; set; } = 0;

    /// <summary>刷新用户周期。该周期内多次SSO登录只拉取一次用户信息，默认600秒</summary>
    [Description("刷新用户周期。该周期内多次SSO登录只拉取一次用户信息，默认600秒")]
    [Category("用户登录")]
    public Int32 RefreshUserPeriod { get; set; } = 600;

    /// <summary>JWT密钥。用于生成JWT令牌的算法和密钥，如HS256:ABCD1234</summary>
    [Description("JWT密钥。用于生成JWT令牌的算法和密钥，如HS256:ABCD1234")]
    [Category("用户登录")]
    public String JwtSecret { get; set; }

    /// <summary>令牌有效期。访问令牌AccessToken的有效期，默认7200秒</summary>
    [Description("令牌有效期。访问令牌AccessToken的有效期，默认7200秒")]
    [Category("用户登录")]
    public Int32 TokenExpire { get; set; } = 7200;

    /// <summary>在Cookie中存储令牌。读取令牌时，如果请求头没有携带令牌，则从Cookie读取，默认true</summary>
    [Description("在Cookie中存储令牌。读取令牌时，如果请求头没有携带令牌，则从Cookie读取，默认true")]
    [Category("用户登录")]
    public Boolean TokenCookie { get; set; } = true;
    #endregion

    #region 界面配置
    /// <summary>工作台页面。进入后台的第一个内容页</summary>
    [Description("工作台页面。进入后台的第一个内容页")]
    [Category("界面配置")]
    public String StartPage { get; set; }

    ///// <summary>布局页。</summary>
    //[Description("布局页。")]
    //[Category("界面配置")]
    //public String Layout { get; set; } = "~/Views/Shared/_Ace_Layout.cshtml";

    /// <summary>主题样式。每一个内容页，ACE/layui</summary>
    [Description("主题样式。每一个内容页，ACE/layui")]
    [Category("界面配置")]
    public String Theme { get; set; } = "";

    /// <summary>首页皮肤。最外层框架页，ACE/layui</summary>
    [Description("首页皮肤。最外层框架页，ACE/layui")]
    [Category("界面配置")]
    public String Skin { get; set; } = "";

    /// <summary>登录提示。留空表示不显示登录提示信息</summary>
    [Description("登录提示。留空表示不显示登录提示信息")]
    [Category("界面配置")]
    public String LoginTip { get; set; }

    /// <summary>表单组样式。大中小屏幕分别3/2/1列</summary>
    [Description("表单组样式。大中小屏幕分别3/2/1列，form-group col-xs-12 col-sm-6 col-lg-4")]
    [Category("界面配置")]
    public String FormGroupClass { get; set; } = "form-group col-xs-12 col-sm-6";

    /// <summary>下拉选择框。使用Bootstrap，美观，但有呈现方面的性能损耗</summary>
    [Description("下拉选择框。使用Bootstrap，美观，但有呈现方面的性能损耗")]
    [Category("界面配置")]
    public Boolean BootstrapSelect { get; set; } = true;

    /// <summary>最大下拉个数。表单页关联下拉列表最大允许个数，默认50，超过时显示文本数字框</summary>
    [Description("最大下拉个数。表单页关联下拉列表最大允许个数，默认50，超过时显示文本数字框")]
    [Category("界面配置")]
    public Int32 MaxDropDownList { get; set; } = 50;

    /// <summary>版权。留空表示不显示版权信息，支持变量now/runtime/framework，其中now支持格式化字符串如{now:yyyy}</summary>
    [Description("版权。留空表示不显示版权信息，支持变量now/runtime/framework，其中now支持格式化字符串如{now:yyyy}")]
    [Category("界面配置")]
    public String Copyright { get; set; }

    /// <summary>备案号。留空表示不显示备案信息</summary>
    [Description("备案号。留空表示不显示备案信息")]
    [Category("界面配置")]
    public String Registration { get; set; } = "沪ICP备10000000号";

    /// <summary>启用新UI</summary>
    [Description("启用新UI")]
    [Category("界面配置")]
    public Boolean EnableNewUI { get; set; }

    /// <summary>ECharts主题。图表样式主题</summary>
    [Description("ECharts主题。图表样式主题")]
    [Category("界面配置")]
    public String EChartsTheme { get; set; }

    /// <summary>标题后缀。在页面标题后面加上系统名称，默认true</summary>
    [Description("标题后缀。在页面标题后面加上系统名称，默认true")]
    [Category("界面配置")]
    public Boolean TitlePrefix { get; set; } = true;

    /// <summary>表格双击事件。列表页表格行双击事件禁用，默认true(启用)</summary>
    [Description("表格双击事件。列表页表格行双击事件禁用，默认true(启用)")]
    [Category("界面配置")]
    public Boolean EnableTableDoubleClick { get; set; } = true;

    /// <summary>星尘Web。星尘控制台地址，支持直达调用链 /trace?id={traceId} 或 /graph?id={traceId}</summary>
    [Description("星尘Web。星尘控制台地址，支持直达调用链 /trace?id={traceId} 或 /graph?id={traceId}")]
    [Category("界面配置")]
    public String StarWeb { get; set; }
    #endregion

    #region 系统功能
    /// <summary>OAuth服务。是否启用OAuth2.0服务，为其它应用提供单点登录服务</summary>
    [Description("OAuth服务。是否启用OAuth2.0服务，为其它应用提供单点登录服务")]
    [Category("系统功能")]
    public Boolean EnableOAuthServer { get; set; } = true;

    /// <summary>多租户。是否支持多租户，租户模式禁止访问系统管理，平台管理模式禁止访问租户页面</summary>
    [Description("多租户。是否支持多租户，租户模式禁止访问系统管理，平台管理模式禁止访问租户页面")]
    [Category("系统功能")]
    public Boolean EnableTenant { get; set; }

    /// <summary>用户在线。是否记录用户在线信息，0表示不记录，1表示仅记录已登录用户，2表示记录所有访客。默认2</summary>
    [Description("用户在线。是否记录用户在线信息，0表示不记录，1表示仅记录已登录用户，2表示记录所有访客。默认2")]
    [Category("系统功能")]
    public Int32 EnableUserOnline { get; set; } = 2;

    /// <summary>用户统计。是否统计用户访问，默认true</summary>
    [Description("用户统计。是否统计用户访问，默认true")]
    [Category("系统功能")]
    public Boolean EnableUserStat { get; set; } = true;

    /// <summary>数据保留时间。审计日志与OAuth日志，默认30天</summary>
    [Description("数据保留时间。审计日志与OAuth日志，默认30天")]
    [Category("系统功能")]
    public Int32 DataRetention { get; set; } = 30;

    /// <summary>文件保留时间。备份文件保留时间，默认15天</summary>
    [Description("文件保留时间。备份文件保留时间，默认15天")]
    [Category("系统功能")]
    public Int32 FileRetention { get; set; } = 15;

    /// <summary>保留文件大小。小于该大小的文件将不会被删除，即使超过保留时间，单位K字节，默认1024K</summary>
    [Description("保留文件大小。小于该大小的文件将不会被删除，即使超过保留时间，单位K字节，默认1024K")]
    [Category("系统功能")]
    public Int32 FileRetentionSize { get; set; } = 1024;

    /// <summary>最大导出行数。页面允许导出的最大行数，默认10_000_000</summary>
    [Description("最大导出行数。页面允许导出的最大行数，默认10_000_000")]
    [Category("系统功能")]
    public Int32 MaxExport { get; set; } = 10_000_000;

    /// <summary>最大备份行数。页面允许备份的最大行数，默认10_000_000</summary>
    [Description("最大备份行数。页面允许备份的最大行数，默认10_000_000")]
    [Category("系统功能")]
    public Int32 MaxBackup { get; set; } = 10_000_000;
    #endregion

    #region 方法
    /// <summary>实例化</summary>
    public CubeSetting() { }

    /// <summary>加载时触发</summary>
    protected override void OnLoaded()
    {
        if (StartPage.IsNullOrEmpty()) StartPage = "/Admin/User/Info";

        var web = Runtime.IsWeb;

        //if (AvatarPath.IsNullOrEmpty()) AvatarPath = web ? "..\\Avatars" : "Avatars";
        if (DefaultRole.IsNullOrEmpty() || DefaultRole == "3") DefaultRole = "普通用户";

        if (JwtSecret.IsNullOrEmpty() || JwtSecret.Split(':').Length != 2) JwtSecret = $"HS256:{Rand.NextString(16)}";

        // 取版权信息
        if (Copyright.IsNullOrEmpty())
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            if (asm != null)
            {
                var att = asm.GetCustomAttribute<AssemblyCopyrightAttribute>();
                if (att != null)
                {
                    Copyright = att.Copyright + " {runtime}";
                }
            }
        }

        // 版权信息中的年份替换为当前年份
        if (!Copyright.IsNullOrEmpty() && Copyright.Contains('-'))
        {
            var reg = new Regex(@"(\d{4})-(\d{4})");
            Copyright = reg.Replace(Copyright, math => $"{math.Groups[1]}-{{now:yyyy}}");
            //for (var i = 2000; i <= DateTime.Today.Year; i++)
            //{
            //    Copyright = Copyright.Replace(i + "", "{now:yyyy}");
            //}
        }

        if (PaswordStrength.IsNullOrEmpty()) PaswordStrength = @"^(?=.*\d.*)(?=.*[a-z].*)(?=.*[A-Z].*)(?=.*[^(0-9a-zA-Z)].*).{8,32}$";
        if (MaxLoginError <= 0) MaxLoginError = 5;
        if (LoginForbiddenTime <= 0) LoginForbiddenTime = 300;

        base.OnLoaded();
    }

    /// <summary>获取版权信息。动态替换年份</summary>
    /// <returns></returns>
    public String GetCopyright()
    {
        var cr = Copyright;
        if (cr.IsNullOrEmpty()) return null;

        // 处理运行时
        if (cr.Contains("{runtime}"))
        {
            cr = cr.Replace("{runtime}", $"<a href=\"https://get.dot.net\" target=\"_blank\">.NET {Environment.Version}</a>");
        }
        if (cr.Contains("{framework}"))
        {
            cr = cr.Replace("{framework}", $"<a href=\"https://get.dot.net\" target=\"_blank\">{RuntimeInformation.FrameworkDescription}</a>");
        }

        // 处理今年时间
        var p1 = cr.IndexOf("{now");
        if (p1 < 0) return cr;

        var format = "";
        var p2 = cr.IndexOf('}', p1);
        if (p2 > 0) format = cr.Substring(p1 + 1, p2 - p1 - 1).TrimStart("now").TrimStart(":");

        var now = DateTime.Now;
        if (format.IsNullOrEmpty())
            return cr[..p1] + now.Year + cr[(p2 + 1)..];
        else
            return cr[..p1] + now.ToString(format) + cr[(p2 + 1)..];
    }
    #endregion
}