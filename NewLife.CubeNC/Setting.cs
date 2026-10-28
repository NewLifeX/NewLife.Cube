using System.ComponentModel;
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
[DisplayName("Cube settings")]
[Config("Cube")]
public class CubeSetting : Config<CubeSetting>
{
    #region 静态
    /// <summary>指向数据库参数字典表</summary>
    static CubeSetting() => Provider = new DbConfigProvider { UserId = 0, Category = "Cube" };

    /// <summary>AI 助手主题色方案。键为方案名，值为"主色,辅色"，首个为默认方案</summary>
    public static IDictionary<String, String> ColorSchemes { get; } = new Dictionary<String, String>
    {
        ["新生命绿"] = "#2ecc71,#1e8e3e",
        ["靛蓝紫"] = "#667eea,#764ba2",
        ["翠绿"] = "#10b981,#059669",
        ["天青蓝"] = "#0ea5e9,#0369a1",
        ["湖青"] = "#06b6d4,#0e7490",
        ["琥珀橙"] = "#f59e0b,#ea580c",
        ["玫瑰红"] = "#f43f5e,#be123c",
        ["藤萝紫"] = "#8b5cf6,#6d28d9",
        ["樱花粉"] = "#f472b6,#db2777",
        ["石墨黑"] = "#475569,#0f172a",
    };
    #endregion

    #region 通用
    /// <summary>是否启用调试。默认true</summary>
    [Description("Debug")]
    [Category("General")]
    public Boolean Debug { get; set; } = true;

    /// <summary>显示运行时间</summary>
    [Description("Show run time")]
    [Category("General")]
    public Boolean ShowRunTime { get; set; } = true;

    /// <summary>头像目录。设定后下载远程头像到本地，默认Avatars子目录，web上一级Avatars。清空表示不抓取</summary>
    [Description("Avatar directory. Downloads remote avatars locally; defaults to the Avatars subdirectory, or Avatars next to the web root. Empty disables downloading")]
    [Category("General")]
    public String AvatarPath { get; set; } = "Avatars";

    /// <summary>文字头像字符数。头像不存在时自动生成SVG文字头像，可选1~3个字符。中文取前N字以保留姓氏，英文取各单词首字母。默认2</summary>
    [Description("Text avatar characters. Generates an SVG text avatar when no image exists; 1 to 3 characters. Uses the first N Chinese characters to keep the surname, or the initials of English words. Default 2")]
    [Category("General")]
    public Int32 AvatarChars { get; set; } = 2;

    /// <summary>上传目录。默认Uploads</summary>
    [Description("Upload directory. Defaults to Uploads")]
    [Category("General")]
    public String UploadPath { get; set; } = "Uploads";

    /// <summary>静态资源目录。默认wwwroot</summary>
    [Description("Web root directory. Defaults to wwwroot")]
    [Category("General")]
    public String WebRootPath { get; set; } = "wwwroot";

    /// <summary>资源地址。指向CDN，如 https://sso.newlifex.com/content/，留空表示使用本地</summary>
    [Description("Resource URL. CDN base such as https://sso.newlifex.com/content/; empty uses local resources")]
    [Category("General")]
    public String ResourceUrl { get; set; }

    /// <summary>获取 Mermaid 图表库地址。配置 ResourceUrl 时使用自建 CDN（与 echarts 同约定 /mermaid/mermaid.min.js），否则使用公共 CDN（npmmirror，国内访问稳定）</summary>
    /// <returns>Mermaid 脚本完整地址</returns>
    public String GetMermaidUrl()
    {
        var res = ResourceUrl;
        if (!String.IsNullOrEmpty(res)) return res.TrimEnd('/') + "/mermaid/mermaid.min.js";

        return "https://registry.npmmirror.com/mermaid/11.12.3/files/dist/mermaid.min.js";
    }

    /// <summary>跨域来源。允许其它源访问当前域，指定其它源http地址，*表示任意域</summary>
    [Description("CORS origins. Other origins allowed to access this site; * allows any origin")]
    [Category("General")]
    public String CorsOrigins { get; set; }
#if DEBUG
        = "*";
#endif

    /// <summary>在iframe中展示。SAMEORIGIN-允许相同域名，ALLOWALL-允许任何域名</summary>
    [Description("Iframe embedding. Empty allows the same domain only; SAMEORIGIN allows the same domain and port; ALLOWALL allows any domain")]
    [Category("General")]
    public String XFrameOptions { get; set; }

    ///// <summary>Cookie模式。token的cookies默认模式（ -1 Unspecified，0 None，1 Lax，2 Strict）</summary>
    //[Description("Cookie模式。token的cookies默认模式（ -1 Unspecified，0 None，1 Lax，2 Strict）")]
    //[Category("General")]
    //public Int32 SameSiteMode { get; set; } = -1;

    ///// <summary>Cookie域名。可用于把Cookie写到顶级域名，默认为空写当前域。写顶级域要求https，同时会导致普通http无法在本地域写同名键值</summary>
    //[Description("Cookie域名。可用于把Cookie写到顶级域名，默认为空写当前域。写顶级域要求https，同时会导致普通http无法在本地域写同名键值")]
    //[Category("General")]
    //public String CookieDomain { get; set; }

    /// <summary>分享有效期。分享令牌的有效期，默认7200秒</summary>
    [Description("Share expiration. Lifetime of share tokens in seconds, default 7200")]
    [Category("General")]
    public Int32 ShareExpire { get; set; } = 7200;

    /// <summary>机器人错误码。设置后拦截各种爬虫并返回相应错误，如404/500，默认0不拦截</summary>
    [Description("Bot error code. Returns this error for crawlers, such as 404/500; 0 disables blocking")]
    [Category("General")]
    public Int32 RobotError { get; set; }

    ///// <summary>强制使用SSL。强制访问https，使用http访问时跳转</summary>
    //[Description("强制使用SSL。强制访问https，使用http访问时跳转")]
    //[Category("General")]
    //public Boolean ForceSSL { get; set; }

    /// <summary>强制跳转。指定目标schema和host，在GET访问发现不一致时强制跳转，host支持*。常用于强制跳转https，如https://*:8081</summary>
    [Description("Force redirect. Target scheme and host for GET requests; * is supported in the host. Commonly used to force HTTPS, such as https://*:8081")]
    [Category("General")]
    public String ForceRedirect { get; set; }
    #endregion

    #region 安全防御
    /// <summary>安全防御模式。0=关闭，1=观察模式仅记录，2=拦截模式，3=自动模式（默认；观察为主，连续攻击自动封禁，大范围攻击临时拦截并自动回落）</summary>
    [Description("Security mode. 0: off; 1: observe and log only; 2: block; 3: automatic (default; observes, bans repeated attackers, and temporarily blocks broad attacks with automatic rollback)")]
    [Category("Security")]
    public Int32 SecurityMode { get; set; } = 3;

    /// <summary>自动封禁时长档位。逗号分隔秒数，按触发次数递增；空值使用默认档位 60,300,1800,7200,86400</summary>
    [Description("Auto-ban durations. Comma-separated seconds that increase with each trigger; empty uses 60,300,1800,7200,86400")]
    [Category("Security")]
    public String BlockDurations { get; set; }

    /// <summary>可信代理。来自这些代理的转发头才被信任，逗号分隔IP或CIDR网段；空值兼容旧行为信任全部转发头</summary>
    [Description("Trusted proxies. Forwarded headers are only trusted from these comma-separated IPs or CIDR ranges; empty keeps the legacy behavior of trusting all forwarded headers")]
    [Category("Security")]
    public String TrustedProxies { get; set; }

    /// <summary>可信代理自动学习数量。可信代理未配置时，自动学习携带转发头的内网直连来源（反向代理/负载均衡入口），最多学习该数量，0=不学习</summary>
    [Description("Trusted proxy learning limit. When no trusted proxies are configured, private direct sources that send forwarded headers are learned up to this count; 0 disables learning (typically 2 for one data center, 4 for two)")]
    [Category("Security")]
    public Int32 TrustedProxyLearning { get; set; } = 4;

    /// <summary>已学习代理。由系统自动维护，可信代理为空时参与链解析；清空可重置学习结果</summary>
    [Description("Learned proxies. Maintained automatically; clear it to reset the learned list")]
    [Category("Security")]
    public String LearnedProxies { get; set; }
    #endregion

    #region 用户登录
    /// <summary>默认角色。默认普通用户</summary>
    [Description("Default role. Defaults to the Normal User role")]
    [Category("User login")]
    public String DefaultRole { get; set; } = "普通用户";

    /// <summary>允许密码登录。允许输入用户名密码进行登录</summary>
    [Description("Allow password sign-in. Allows signing in with a username and password")]
    [Category("User login")]
    public Boolean AllowLogin { get; set; } = true;

    /// <summary>允许明文密码。默认true，兼容旧版客户端；设为false后强制要求前端通过 Challenge-Response 加密密码传输</summary>
    [Description("Allow plain-text passwords. Default true for legacy clients; false forces Challenge-Response encrypted passwords")]
    [Category("User login")]
    public Boolean AllowPlainPassword { get; set; } = true;

    /// <summary>允许注册。允许输入用户名密码进行注册</summary>
    [Description("Allow registration. Allows registering with a username and password")]
    [Category("User login")]
    public Boolean AllowRegister { get; set; } = true;

    /// <summary>自动注册。默认true，SSO登录后，如果本地未登录，自动注册新用户。全局设置和OAuth应用设置只要有一个启用则表示使用</summary>
    [Description("Auto registration. Default true; after SSO sign-in an unknown local user is registered automatically. Enabled when either the global or the OAuth app setting is on")]
    [Category("User login")]
    public Boolean AutoRegister { get; set; } = true;

    ///// <summary>密码强度。最小密码长度，默认6</summary>
    //[Description("密码强度。最小密码长度，默认6")]
    //[Category("User login")]
    //public Int32 MinPasswordLength { get; set; } = 6;

    /// <summary>密码强度。*表示无限制，默认8位起，数字大小写字母和符号。简易版^(?=.*\\d.*)(?=.*[a-zA-Z].*).{6,32}$</summary>
    [Description("Password strength. * means unrestricted; by default at least 8 characters with digits, upper/lower case letters and symbols. Simple form: ^(?=.*\\d.*)(?=.*[a-zA-Z].*).{6,32}$")]
    [Category("User login")]
    public String PaswordStrength { get; set; } = @"^(?=.*\d.*)(?=.*[a-z].*)(?=.*[A-Z].*)(?=.*[^(0-9a-zA-Z)].*).{8,32}$";

    /// <summary>复杂密码校验。启用后登录页校验密码复杂度（按密码强度），禁用后登录页仅要求密码非空，默认true</summary>
    [Description("Password complexity check. When enabled the sign-in page validates password strength; when disabled it only requires a non-empty password. Default true")]
    [Category("User login")]
    public Boolean EnablePasswordComplexity { get; set; } = true;

    /// <summary>登录失败次数。短时间内，相同用户或IP地址连续登录错误次数达到该值后禁止登录，默认5</summary>
    [Description("Login failure limit. Bans sign-in after this many consecutive failures for the same user or IP, default 5")]
    [Category("User login")]
    public Int32 MaxLoginError { get; set; } = 5;

    /// <summary>登录失败次数。短时间内，相同用户或IP地址连续登录错误次数达到该值后禁止登录，默认5</summary>
    [Description("Login ban duration. Seconds a risk-control ban lasts, default 300")]
    [Category("User login")]
    public Int32 LoginForbiddenTime { get; set; } = 300;

    /// <summary>三段IP封禁阈值。三段IP（如103.125.146.*）连续登录失败次数达到该值后封禁该IP段，默认10，0表示不启用</summary>
    [Description("Class C subnet ban threshold. Bans a /24 subnet such as 103.125.146.* after this many failures, default 10; 0 disables")]
    [Category("User login")]
    public Int32 MaxLoginErrorBySubnet24 { get; set; } = 10;

    /// <summary>两段IP封禁阈值。两段IP（如103.125.*.*）连续登录失败次数达到该值后封禁该IP段，默认20，0表示不启用</summary>
    [Description("Class B subnet ban threshold. Bans a /16 subnet such as 103.125.*.* after this many failures, default 20; 0 disables")]
    [Category("User login")]
    public Int32 MaxLoginErrorBySubnet16 { get; set; } = 20;

    /// <summary>强行绑定用户名。根据SSO用户名强制绑定本地同名用户，而不需要增加提供者前缀，一般用于用户中心</summary>
    [Description("Force bind by user name. Binds SSO users to local users with the same name without a provider prefix; intended for user centers")]
    [Category("User login")]
    public Boolean ForceBindUser { get; set; } = true;

    /// <summary>绑定用户代码。根据SSO用户代码强制绑定本地用户</summary>
    [Description("Bind by user code. Binds SSO users to local users by user code")]
    [Category("User login")]
    public Boolean ForceBindUserCode { get; set; } = true;

    /// <summary>绑定用户手机。根据SSO用户手机强制绑定本地用户</summary>
    [Description("Bind by mobile. Binds SSO users to local users by mobile number")]
    [Category("User login")]
    public Boolean ForceBindUserMobile { get; set; } = true;

    /// <summary>绑定用户邮箱。根据SSO用户邮箱强制绑定本地用户</summary>
    [Description("Bind by email. Binds SSO users to local users by email")]
    [Category("User login")]
    public Boolean ForceBindUserMail { get; set; } = true;

    /// <summary>绑定用户昵称。根据SSO用户昵称强制绑定本地用户，内部SSO可用，不建议用于社交网络，重名太多</summary>
    [Description("Bind by nickname. Binds SSO users to local users by nickname; suitable for internal SSO, not for social networks with many duplicate names")]
    [Category("User login")]
    public Boolean ForceBindNickName { get; set; } = true;

    /// <summary>使用Sso角色。SSO登录后继续使用SSO角色，默认true；否则使用DefaultRole</summary>
    [Description("Use SSO roles. Keeps SSO roles after sign-in, default true; otherwise DefaultRole is used")]
    [Category("User login")]
    public Boolean UseSsoRole { get; set; } = true;

    ///// <summary>保留本地角色。本地角色与SSO角色合并，默认false</summary>
    //[Description("保留本地角色。本地角色与SSO角色合并，默认false")]
    //[Category("User login")]
    //public Boolean KeepLocalRole { get; set; }

    /// <summary>使用Sso部门。SSO登录后继续使用SSO部门，默认true</summary>
    [Description("Use SSO departments. Keeps SSO departments after sign-in, default true")]
    [Category("User login")]
    public Boolean UseSsoDepartment { get; set; } = true;

    /// <summary>SSO角色规则。SSO登录后按规则自动升级用户主角色，多条规则用逗号（半角或全角）分隔。格式：原角色+部门通配符=目标角色，部门通配符支持前缀(路由*)、后缀(*专员)、精确匹配。例如：游客+路由*=路由专员,普通用户+*车队=调度员</summary>
    [Description("SSO role rules. Automatically upgrades the primary role after SSO sign-in; separate rules with commas. Format: source role+department wildcard=target role, for example Guest+Route*=Route Specialist")]
    [Category("User login")]
    public String RoleRules { get; set; }

    /// <summary>注销所有系统。false仅注销本系统，默认true时注销SsoServer</summary>
    [Description("Sign out everywhere. false signs out of this site only; true (default) also signs out of the SSO server")]
    [Category("User login")]
    public Boolean LogoutAll { get; set; } = true;

    /// <summary>允许多设备登录。false时同一账号新登录会踢掉旧设备，注销时吊销该账号所有令牌；true时仅吊销当前会话令牌，默认true</summary>
    [Description("Allow multi-device sign-in. false kicks old devices on a new sign-in and revokes all tokens on sign-out; true (default) revokes only the current session")]
    [Category("User login")]
    public Boolean EnableMultiDeviceLogin { get; set; } = true;

    /// <summary>会话超时。单点登录后会话超时时间，该时间内可借助Cookie登录，默认0s</summary>
    [Description("Session timeout. Session lifetime after SSO sign-in during which the cookie can sign in again; default 0s")]
    [Category("User login")]
    public Int32 SessionTimeout { get; set; } = 0;

    /// <summary>令牌滑动刷新阈值。JWT剩余有效期低于该秒数时在TryLogin中自动刷新令牌并写入Cookie，0表示禁用滑动过期</summary>
    [Description("Token sliding refresh threshold. Refreshes the JWT and rewrites the cookie when its remaining lifetime falls below this many seconds; 0 disables sliding expiration")]
    [Category("User login")]
    public Int32 TokenRefreshThreshold { get; set; } = 900;

    /// <summary>刷新用户周期。该周期内多次SSO登录只拉取一次用户信息，默认600秒</summary>
    [Description("User refresh period. Fetches user information once per period across SSO sign-ins, default 600 seconds")]
    [Category("User login")]
    public Int32 RefreshUserPeriod { get; set; } = 600;

    /// <summary>JWT密钥。用于生成JWT令牌的算法和密钥，如HS256:ABCD1234</summary>
    [Description("JWT secret. Algorithm and key used to sign JWTs, such as HS256:ABCD1234")]
    [Category("User login")]
    public String JwtSecret { get; set; }

    /// <summary>令牌有效期。访问令牌AccessToken的有效期，默认7200秒</summary>
    [Description("Token lifetime. Lifetime of the access token in seconds, default 7200")]
    [Category("User login")]
    public Int32 TokenExpire { get; set; } = 7200;

    /// <summary>在Cookie中存储令牌。读取令牌时，如果请求头没有携带令牌，则从Cookie读取，默认true</summary>
    [Description("Store token in cookie. Reads the token from the cookie when the request header does not carry one, default true")]
    [Category("User login")]
    public Boolean TokenCookie { get; set; } = true;

    /// <summary>验证附件访问。访问附件时，是否验证登录状态，默认true</summary>
    [Description("Validate attachment access. Requires sign-in to access attachments, default true")]
    [Category("User login")]
    public Boolean ValidateAttachment { get; set; } = true;

    /// <summary>公开附件分类。无需登录即可访问的附件分类，多个用逗号分隔，如 markdown,avatar。ValidateAttachment=true时生效</summary>
    [Description("Public attachment categories. Categories accessible without sign-in, comma-separated, such as markdown,avatar. Applies when ValidateAttachment is true")]
    [Category("User login")]
    public String PublicAttachmentCategories { get; set; }

    /// <summary>私有附件分类。仅上传人本人可访问的附件分类，多个用逗号分隔。ValidateAttachment=true时生效</summary>
    [Description("Private attachment categories. Categories only the uploader can access, comma-separated. Applies when ValidateAttachment is true")]
    [Category("User login")]
    public String OwnerOnlyAttachmentCategories { get; set; }

    /// <summary>接口浏览器。开启后系统管理员可访问 /Cube/Apis 查看路由清单，默认false，避免生产环境暴露攻击面</summary>
    [Description("API explorer. Allow system administrators to list routes; off by default")]
    [Category("Security")]
    public Boolean EnableApiExplorer { get; set; } = false;

    /// <summary>上传与导入文件体积上限（MB）。附件上传与数据导入共用同一阈值，0 表示不限制，默认20</summary>
    [Description("Upload size limit in MB; 0 means unlimited")]
    [Category("Security")]
    public Int32 UploadMaxSize { get; set; } = 20;

    /// <summary>SSO跨域重定向白名单。登录后允许携带JWT Token重定向的目标域名，逗号分隔，支持通配符前缀*.company.com。留空=仅允许同站重定向，拒绝所有跨域跳转以防止凭据泄露</summary>
    [Description("SSO redirect allowlist. Domains allowed to receive the JWT after sign-in, comma-separated with wildcard prefixes such as *.company.com. Empty allows the same site only; other origins are rejected and sent back to the home page")]
    [Category("User login")]
    public String SsoSafeDomains { get; set; }

    /// <summary>外部验证地址。配置后，本地验证失败时调用外部接口验证用户名密码，成功则自动创建/更新本地用户并登录。POST格式：{"username":"...","password":"..."}，响应：{"code":0,"data":{"username":"...","displayName":"...","mail":"...","mobile":"...","roleName":"...","avatar":"..."}}</summary>
    [Description("External authentication URL. When local authentication fails, credentials are verified by this endpoint; on success the local user is created or updated and signed in")]
    [Category("User login")]
    public String ExternalAuthUrl { get; set; }
    #endregion

    #region 界面配置
    /// <summary>工作台页面。进入后台的第一个内容页</summary>
    [Description("Start page. First content page opened after entering the admin console")]
    [Category("UI")]
    public String StartPage { get; set; }

    ///// <summary>布局页。</summary>
    //[Description("布局页。")]
    //[Category("UI")]
    //public String Layout { get; set; } = "~/Views/Shared/_Ace_Layout.cshtml";

    /// <summary>主题样式。每一个内容页，ACE/layui</summary>
    [Description("Theme. Content page theme, ACE/layui")]
    [Category("UI")]
    public String Theme { get; set; } = "";

    /// <summary>首页皮肤。最外层框架页，ACE/layui</summary>
    [Description("Skin. Outer frame layout, ACE/layui")]
    [Category("UI")]
    public String Skin { get; set; } = "";

    /// <summary>登录提示。留空表示不显示登录提示信息</summary>
    [Description("Login tip. Empty hides the tip on the sign-in page")]
    [Category("UI")]
    public String LoginTip { get; set; }

    /// <summary>登录页Logo。留空时使用默认Logo</summary>
    [Description("Login logo. Empty uses the default logo")]
    [Category("UI")]
    public String LoginLogo { get; set; }

    /// <summary>登录页背景图。留空时使用默认背景，可填写图片URL</summary>
    [Description("Login background. Image URL; empty uses the default background")]
    [Category("UI")]
    public String LoginBackground { get; set; }

    /// <summary>表单组样式。大中小屏幕分别3/2/1列</summary>
    [Description("Form group layout. 3/2/1 columns on large/medium/small screens, form-group col-xs-12 col-sm-6 col-lg-4")]
    [Category("UI")]
    public String FormGroupClass { get; set; } = "form-group col-xs-12 col-sm-6";

    /// <summary>下拉选择框。使用Bootstrap，美观，但有呈现方面的性能损耗</summary>
    [Description("Bootstrap select. Nicer drop-downs with a rendering cost")]
    [Category("UI")]
    public Boolean BootstrapSelect { get; set; } = true;

    /// <summary>最大下拉个数。表单页关联下拉列表最大允许个数，默认50，超过时显示文本数字框</summary>
    [Description("Maximum drop-down items. Maximum options in a related drop-down on forms, default 50; larger lists fall back to a text box")]
    [Category("UI")]
    public Int32 MaxDropDownList { get; set; } = 50;

    /// <summary>版权。留空表示不显示版权信息，支持变量now/runtime/framework，其中now支持格式化字符串如{now:yyyy}</summary>
    [Description("Copyright. Empty hides the copyright; variables now/runtime/framework are supported, and now accepts formats such as {now:yyyy}")]
    [Category("UI")]
    public String Copyright { get; set; }

    /// <summary>备案号。留空表示不显示备案信息</summary>
    [Description("Filing number. Empty hides the filing information")]
    [Category("UI")]
    public String Registration { get; set; } = "沪ICP备10000000号";

    /// <summary>启用新UI</summary>
    [Description("Enable new UI")]
    [Category("UI")]
    public Boolean EnableNewUI { get; set; }

    /// <summary>ECharts主题。图表样式主题</summary>
    [Description("ECharts theme. Chart theme")]
    [Category("UI")]
    public String EChartsTheme { get; set; }

    /// <summary>标题后缀。在页面标题后面加上系统名称，默认true</summary>
    [Description("Title suffix. Appends the system name to page titles, default true")]
    [Category("UI")]
    public Boolean TitlePrefix { get; set; } = true;

    /// <summary>表格双击事件。列表页表格行双击事件禁用，默认true(启用)</summary>
    [Description("Table double-click. Enables row double-click on list pages, default true")]
    [Category("UI")]
    public Boolean EnableTableDoubleClick { get; set; } = true;

    /// <summary>表格分隔样式。列表页数据表格的分隔风格：Light-轻量、Standard-标准（默认）、Grid-网格</summary>
    [Description("Table style. Light: minimal thin lines; Standard: clear row lines (default); Grid: full cell borders")]
    [Category("UI")]
    public String TableStyle { get; set; } = "Standard";

    /// <summary>表格行高密度。列表页数据表格的行高：Compact-紧凑（默认）、Normal-适中</summary>
    [Description("Table density. Compact: dense and information-rich (default); Normal: relaxed")]
    [Category("UI")]
    public String TableDensity { get; set; } = "Compact";

    /// <summary>星尘Web。星尘控制台地址，支持直达调用链 /trace?id={traceId} 或 /graph?id={traceId}</summary>
    [Description("Stardust web. Stardust console URL; supports direct links to /trace?id={traceId} or /graph?id={traceId}")]
    [Category("UI")]
    public String StarWeb { get; set; }
    #endregion

    #region AI
    /// <summary>AI 总开关。启用后可使用日志分析、通知润色等 AI 辅助功能，默认false</summary>
    [Description("AI master switch. Enables AI features such as log analysis and notification polishing, default false")]
    [Category("AI")]
    public Boolean AISwitch { get; set; }

    /// <summary>AI 服务商。支持 NewLife / Ollama / DeepSeek / DashScope / OpenAI 等，默认NewLife</summary>
    [Description("AI provider. Supports NewLife, Ollama, DeepSeek, DashScope, OpenAI and others; default NewLife")]
    [Category("AI")]
    public String AIProvider { get; set; } = "NewLifeAI";

    /// <summary>AI 服务地址。NewLife 默认 https://ai.newlifex.com</summary>
    [Description("AI endpoint. Defaults to https://ai.newlifex.com for NewLife")]
    [Category("AI")]
    public String AIEndpoint { get; set; } = "https://ai.newlifex.com";

    /// <summary>AI ApiKey。NewLife 默认 sk-CubeAI2026</summary>
    [Description("AI ApiKey. Defaults to sk-CubeAI2026 for NewLife")]
    [Category("AI")]
    public String AIApiKey { get; set; } = "sk-CubeAI2026";

    /// <summary>AI 默认模型。NewLife 默认 newlife-flash</summary>
    [Description("AI default model. Defaults to newlife-flash for NewLife")]
    [Category("AI")]
    public String AIModel { get; set; } = "newlife-flash";

    /// <summary>AI 默认深度推理。AI 分析与对话默认是否启用深度推理（think），默认false即快速</summary>
    [Description("AI default deep reasoning. Enables think mode for analysis and chat by default; false means fast responses")]
    [Category("AI")]
    public Boolean AIDefaultThink { get; set; }

    /// <summary>AI 助手主题色方案。预设方案联动填充主色/辅色，可再手动微调，默认新生命绿</summary>
    [Description("AI color scheme. Fills the primary and secondary colors from a preset")]
    [Category("AI")]
    public String AIColorScheme { get; set; } = "新生命绿";

    /// <summary>AI 助手主色。悬浮球、面板头、用户气泡等主色调，默认新生命绿#2ecc71</summary>
    [Description("AI primary color. Main color of the launcher, panel header and user bubbles; default NewLife green #2ecc71")]
    [Category("AI")]
    public String AIPrimaryColor { get; set; } = "#2ecc71";

    /// <summary>AI 助手辅色。主色渐变终点色，默认深绿#1e8e3e</summary>
    [Description("AI secondary color. Gradient end color, default dark green #1e8e3e")]
    [Category("AI")]
    public String AISecondaryColor { get; set; } = "#1e8e3e";

    /// <summary>主题方案联动填充主色/辅色。方案与旧值不同时采用新方案颜色，否则保留手动微调</summary>
    /// <param name="oldScheme">原主题色方案</param>
    public void ApplyColorScheme(String oldScheme)
    {
        if (String.IsNullOrEmpty(AIColorScheme) || AIColorScheme == oldScheme) return;
        if (!ColorSchemes.TryGetValue(AIColorScheme, out var colors)) return;

        var parts = colors.Split(',');
        if (parts.Length >= 2)
        {
            AIPrimaryColor = parts[0].Trim();
            AISecondaryColor = parts[1].Trim();
        }
    }
    #endregion

    #region 系统功能
    /// <summary>OAuth服务。是否启用OAuth2.0服务，为其它应用提供单点登录服务</summary>
    [Description("OAuth server. Enables the OAuth 2.0 server that provides SSO for other applications")]
    [Category("System")]
    public Boolean EnableOAuthServer { get; set; } = true;

    /// <summary>文件管理。是否启用后台文件管理，可浏览/上传/下载/删除站点内文件，存在安全风险，默认false</summary>
    [Description("File manager. Enables backend file management (browse, upload, download, delete); this carries security risks")]
    [Category("System")]
    public Boolean EnableFileManager { get; set; }

    /// <summary>多租户。是否支持多租户，租户模式禁止访问系统管理，平台管理模式禁止访问租户页面</summary>
    [Description("Multi-tenancy. Enables tenants; tenant mode cannot access system administration and platform mode cannot access tenant pages")]
    [Category("System")]
    public Boolean EnableTenant { get; set; }

    /// <summary>多租户查询策略。DenyWithEmpty=无租户上下文返回空集（fail-closed），ThrowOnMissingTenant=显式抛错。默认DenyWithEmpty</summary>
    [Description("Tenant query policy. DenyWithEmpty: return an empty set without tenant context; ThrowOnMissingTenant: throw an explicit error")]
    [Category("System")]
    public TenantQueryPolicies TenantQueryPolicy { get; set; } = TenantQueryPolicies.DenyWithEmpty;

    /// <summary>用户在线。是否记录用户在线信息，0表示不记录，1表示仅记录已登录用户，2表示记录所有访客。默认2</summary>
    [Description("User online. 0: do not record; 1: signed-in users only; 2: all visitors (default)")]
    [Category("System")]
    public Int32 EnableUserOnline { get; set; } = 2;

    /// <summary>用户统计。是否统计用户访问，默认true</summary>
    [Description("User statistics. Records user visits, default true")]
    [Category("System")]
    public Boolean EnableUserStat { get; set; } = true;

    /// <summary>启用短信。包括短信验证码和系统通知，默认false</summary>
    [Description("Enable SMS. SMS verification codes and system notifications, default false")]
    [Category("System")]
    public Boolean EnableSms { get; set; }

    /// <summary>启用邮件。包括邮件验证码和系统通知，默认false</summary>
    [Description("Enable email. Email verification codes and system notifications, default false")]
    [Category("System")]
    public Boolean EnableMail { get; set; }

    /// <summary>需要邮箱验证。注册后必须激活邮箱才能登录，注册时必须提供邮箱，默认false</summary>
    [Description("Require email verification. Users must activate their email before signing in and must provide one when registering, default false")]
    [Category("System")]
    public Boolean RequireMailVerify { get; set; }

    /// <summary>需要手机验证。注册后必须激活手机才能登录，注册时必须提供手机，默认false</summary>
    [Description("Require mobile verification. Users must activate their mobile before signing in and must provide one when registering, default false")]
    [Category("System")]
    public Boolean RequireMobileVerify { get; set; }

    /// <summary>验证码场景。位掩码强制指定需要图片验证码的场景：0=不启用，1=登录，2=注册，4=发验证码（防短信轰炸），可组合，如3=登录+注册均需验证码。该开关为强制要求，不受风险自适应豁免，默认0</summary>
    [Description("Captcha scenarios. Bit mask: 0: off; 1: sign-in; 2: registration; 4: verification code requests (anti-SMS-bombing); combine with 3 for sign-in and registration. Mandatory scenarios are never exempted, default 0")]
    [Category("System")]
    public Int32 CaptchaScene { get; set; }

    /// <summary>风险自适应验证码。CaptchaScene 非 0 时生效：自动感知当前请求环境安全度（内网/可信设备/登录失败历史），不安全时对未强制场景也要求图片验证码，默认true</summary>
    [Description("Risk-adaptive captcha. Disabled when CaptchaScene is 0; when enabled it detects unsafe environments and requires a captcha, default true")]
    [Category("System")]
    public Boolean CaptchaRisk { get; set; } = true;

    /// <summary>验证码风险阈值。风险评分达到该值要求验证码：0=内网，1=公网，2=公网+近期登录失败，3=封禁中。默认2</summary>
    [Description("Captcha risk threshold. Requires a captcha when the risk score reaches this value: 0: intranet; 1: internet; 2: internet with recent failures; 3: banned. Default 2")]
    [Category("System")]
    public Int32 CaptchaRiskThreshold { get; set; } = 2;

    /// <summary>可信设备有效期。曾成功登录的设备在有效期内免自适应验证码（天），默认30</summary>
    [Description("Trusted device lifetime. Days during which previously successful devices skip adaptive captchas, default 30")]
    [Category("System")]
    public Int32 TrustedDeviceDays { get; set; } = 30;

    /// <summary>启用MFA。是否允许用户开启多因素认证（TOTP），增强版功能，默认false</summary>
    [Description("Enable MFA. Allows users to enable TOTP multi-factor authentication; an enhanced feature, default false")]
    [Category("System")]
    public Boolean EnableMfa { get; set; }

    /// <summary>文件存储提供服务。是否响应其他节点的文件下载请求，默认true</summary>
    [Description("File storage serving. Responds to file download requests from other nodes, default true")]
    [Category("System")]
    public Boolean FileStorageProvide { get; set; } = true;

    /// <summary>文件存储拉取文件。是否主动拉取其他节点发布的新文件，默认true</summary>
    [Description("File storage fetch. Pulls new files published by other nodes, default true")]
    [Category("System")]
    public Boolean FileStorageFetch { get; set; } = true;

    /// <summary>文件存储拉取超时。下载本地缺失文件时等待其他节点同步的最大时间（毫秒），默认5000</summary>
    [Description("File fetch timeout. Maximum milliseconds to wait for other nodes when a file is missing locally, default 5000")]
    [Category("System")]
    public Int32 FileStorageFetchTimeout { get; set; } = 5_000;

    /// <summary>附件存储类型。Local本地磁盘，Oss阿里云，Cos腾讯云，Qiniu七牛，EasyIO易对象，默认Local</summary>
    [Description("Attachment storage. Local disk, Oss (Alibaba Cloud), Cos (Tencent Cloud), Qiniu or EasyIO; default Local")]
    [Category("System")]
    public String AttachmentStorage { get; set; } = "Local";

    /// <summary>对象存储服务器。OSS/COS/七牛S3兼容端点，如 oss-cn-beijing.aliyuncs.com</summary>
    [Description("Object storage server. S3-compatible endpoint for OSS/COS/Qiniu, such as oss-cn-beijing.aliyuncs.com")]
    [Category("System")]
    public String ObjectStorageServer { get; set; }

    /// <summary>对象存储桶。OSS/COS/七牛存储桶名称</summary>
    [Description("Object storage bucket. Bucket name for OSS/COS/Qiniu")]
    [Category("System")]
    public String ObjectStorageBucket { get; set; }

    /// <summary>对象存储区域。用于S3签名作用域，如 cn-beijing / ap-beijing / cn-east-1</summary>
    [Description("Object storage region. S3 signing scope, such as cn-beijing / ap-beijing / cn-east-1")]
    [Category("System")]
    public String ObjectStorageRegion { get; set; } = "cn-north-1";

    /// <summary>对象存储应用标识。AccessKeyId</summary>
    [Description("Object storage app ID. AccessKeyId")]
    [Category("System")]
    public String ObjectStorageAppId { get; set; }

    /// <summary>对象存储应用密钥。AccessKeySecret</summary>
    [Description("Object storage app secret. AccessKeySecret")]
    [Category("System")]
    public String ObjectStorageSecret { get; set; }

    /// <summary>数据保留时间。审计日志与OAuth日志，默认30天</summary>
    [Description("Data retention. Days to keep audit and OAuth logs, default 30")]
    [Category("System")]
    public Int32 DataRetention { get; set; } = 30;

    /// <summary>文件保留时间。备份文件保留时间，默认15天</summary>
    [Description("File retention. Days to keep backup files, default 15")]
    [Category("System")]
    public Int32 FileRetention { get; set; } = 15;

    /// <summary>保留文件大小。小于该大小的文件将不会被删除，即使超过保留时间，单位K字节，默认1024K</summary>
    [Description("Retention size floor. Files smaller than this (KB) are never deleted even after the retention period, default 1024K")]
    [Category("System")]
    public Int32 FileRetentionSize { get; set; } = 1024;

    /// <summary>最大导出行数。页面允许导出的最大行数，默认10_000_000</summary>
    [Description("Maximum export rows. Maximum rows a page may export, default 10_000_000")]
    [Category("System")]
    public Int32 MaxExport { get; set; } = 10_000_000;

    /// <summary>最大备份行数。页面允许备份的最大行数，默认10_000_000</summary>
    [Description("Maximum backup rows. Maximum rows a page may back up, default 10_000_000")]
    [Category("System")]
    public Int32 MaxBackup { get; set; } = 10_000_000;
    #endregion

    #region API前缀
    /// <summary>API前缀。多个前缀用逗号或分号分隔，如 /api,/api/v1。请求路径命中前缀时自动去掉前缀并转发到真实路由</summary>
    [Description("API prefixes. Comma- or semicolon-separated prefixes such as /api,/api/v1; matching requests are forwarded to the real route without the prefix")]
    [Category("API prefix")]
    public String ApiPrefixes { get; set; }
    #endregion

    #region 方法
    /// <summary>实例化</summary>
    public CubeSetting() { }

    /// <summary>加载时触发</summary>
    protected override void OnLoaded()
    {
        if (StartPage.IsNullOrEmpty()) StartPage = "/Admin/Index/Dashboard";

        var web = Runtime.IsWeb;

        //if (AvatarPath.IsNullOrEmpty()) AvatarPath = web ? "..\\Avatars" : "Avatars";
        if (DefaultRole.IsNullOrEmpty() || DefaultRole == "3") DefaultRole = "普通用户";

        if (JwtSecret.IsNullOrEmpty() || JwtSecret.Split(':').Length != 2)
        {
            JwtSecret = $"HS256:{Rand.NextString(16)}";
            // 首次生成后持久化，避免进程重启密钥变化导致已签发令牌全部失效；多实例部署应在配置中显式固定
            try { Save(); } catch { }
        }

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
        if (p2 > 0) format = cr.Substring(p1 + 1, p2 - p1 - 1).TrimPrefix("now").TrimPrefix(":");

        var now = DateTime.Now;
        if (format.IsNullOrEmpty())
            return cr[..p1] + now.Year + cr[(p2 + 1)..];
        else
            return cr[..p1] + now.ToString(format) + cr[(p2 + 1)..];
    }
    #endregion
}
