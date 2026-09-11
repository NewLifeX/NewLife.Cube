using System.Text.RegularExpressions;

namespace NewLife.Cube.Services;

/// <summary>威胁检测结果</summary>
public class ThreatResult
{
    /// <summary>威胁类别。如SQL注入/XSS/路径遍历/命令注入/文件包含/扫描器/蜜罐探测/协议异常</summary>
    public String? Category { get; set; }

    /// <summary>命中特征。具体特征名称或规则说明</summary>
    public String? Pattern { get; set; }

    /// <summary>命中位置。Url/Body/UserAgent</summary>
    public String? Target { get; set; }

    /// <summary>威胁等级。1=可疑，仅记录；2=攻击，拦截级</summary>
    public Int32 Level { get; set; }

    /// <summary>命中片段。截断后的原始内容，供审计回溯</summary>
    public String? Snippet { get; set; }

    /// <summary>已重写字符串</summary>
    /// <returns></returns>
    public override String ToString() => $"[{Category}]{Pattern}@{Target}";
}

/// <summary>威胁检测器。内置WAF特征库，识别扫描器指纹、攻击载荷、蜜罐路径与协议异常</summary>
/// <remarks>
/// 检测顺序按开销从低到高：协议异常、蜜罐路径、扫描器UA、攻击载荷（Url/Body）。
/// 检出即返回首个命中，未命中返回 null。
/// 误报控制：特征以组合式为主，避免宽泛关键词；富文本等业务场景可通过访问规则的放行规则豁免。
/// </remarks>
public static class ThreatDetector
{
    #region 特征库
    private static readonly RegexOptions _options = RegexOptions.IgnoreCase | RegexOptions.Compiled;
    private static readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(100);

    /// <summary>扫描器用户代理指纹。均为安全扫描/攻击工具，不含curl等通用HTTP客户端，避免误伤API调用</summary>
    private static readonly String[] _scannerUas =
    [
        "sqlmap", "nikto", "nmap", "masscan", "zmap", "zgrab", "fscan", "goby",
        "nuclei", "xray", "wpscan", "dirsearch", "dirbuster", "gobuster", "ffuf",
        "feroxbuster", "hydra", "medusa", "patator", "acunetix", "nessus", "openvas",
        "w3af", "netsparker", "qualys", "havij", "jbrofuzz", "whatweb", "wafw00f",
    ];

    /// <summary>蜜罐路径。正常业务不会访问的敏感路径，命中即扫描探测行为</summary>
    private static readonly String[] _honeypotPaths =
    [
        "/.env", "/.git/", "/.svn/", "/.hg/", "/.ds_store", "/.bash_history",
        "wp-login.php", "/wp-admin/", "xmlrpc.php", "/wp-content/", "/wp-includes/",
        "/phpmyadmin", "/pma/", "/myadmin/",
        "/manager/html", "/host-manager/", "/jmx-console/", "/web-console/", "/invoker/",
        "/actuator", "/druid/", "/h2-console/", "/telescope",
        "/solr/", "/jenkins/", "/cgi-bin/", "/hnap1", "/boaform/",
        "/backup.zip", "/backup.sql", "/db.sql", "/dump.sql", "/www.zip", "/web.rar",
    ];

    /// <summary>危险文件后缀。魔方为纯.NET体系，出现这些脚本路径即为入侵探测</summary>
    private static readonly Regex _dangerousExtension = R(@"\.(php\d?|jspx?|asp|aspx|ashx)([?/]|$)");

    /// <summary>攻击载荷特征。组合式正则，尽量降低误报；等级2=拦截级，等级1=记录级</summary>
    private static readonly (String Category, String Pattern, Regex Regex, Int32 Level)[] _attackPatterns =
    [
        // SQL注入
        ("SQL注入", "union select", R(@"\bunion\s+(all\s+)?select\b"), 2),
        ("SQL注入", "or 1=1", R(@"\bor\s+1\s*=\s*1\b"), 2),
        ("SQL注入", "and 1=1", R(@"\band\s+1\s*=\s*1\b"), 2),
        ("SQL注入", "引号or数字", R(@"['""]\s*or\s*['""]\d"), 2),
        ("SQL注入", "sleep函数", R(@"\b(sleep|pg_sleep|benchmark)\s*\("), 2),
        ("SQL注入", "information_schema", R(@"\binformation_schema\b"), 2),
        ("SQL注入", "xp_cmdshell", R(@"\bxp_cmdshell\b"), 2),
        ("SQL注入", "into outfile", R(@"\binto\s+(out|dump)file\b"), 2),
        ("SQL注入", "报错注入", R(@"\b(updatexml|extractvalue|load_file)\s*\("), 2),
        ("SQL注入", "waitfor delay", R(@"\bwaitfor\s+delay\b"), 2),

        // XSS
        ("XSS", "script标签", R(@"<\s*script\b"), 2),
        ("XSS", "javascript协议", R(@"javascript\s*:"), 2),
        ("XSS", "事件属性", R(@"\bon(error|load|click|focus|mouseover|toggle)\s*="), 2),
        ("XSS", "document.cookie", R(@"document\.cookie"), 2),

        // 路径遍历。多级在前，优先命中更危险的多级穿越
        ("路径遍历", "多级../", R(@"(\.\./){2,}"), 2),
        ("路径遍历", "单级../", R(@"\.\./"), 1),
        ("路径遍历", "编码../", R(@"%2e%2e"), 2),
        ("路径遍历", "敏感文件", R(@"(/etc/(passwd|shadow|hosts)|win\.ini|boot\.ini|/proc/self/environ)"), 2),

        // 命令注入
        ("命令注入", "分隔符命令", R(@"[;|&]\s*(cat|whoami|wget|curl|nc|bash|sh|uname)(\s|$|/)"), 2),
        ("命令注入", "子shell", R(@"\$\([a-z_/]"), 1),
        ("命令注入", "shell路径", R(@"(/bin/(sh|bash)|bash\s+-i)\b"), 2),

        // 文件包含
        ("文件包含", "协议封装", R(@"\b(php|file|data|expect|zip|phar)://"), 2),

        // Log4j
        ("Log4j", "jndi注入", R(@"\$\{jndi:"), 2),
        ("Log4j", "大小写绕过", R(@"\$\{(lower|upper):j"), 2),

        // XXE
        ("XXE", "ENTITY声明", R(@"<!entity\b"), 2),

        // SSRF
        ("SSRF", "云元数据", R(@"(169\.254\.169\.254|100\.100\.100\.200|/latest/meta-data/|metadata\.google)"), 2),

        // 反序列化
        ("反序列化", "Java序列化头", R(@"rO0AB"), 2),
        ("反序列化", "PHP序列化", R(@"\bO:\d+:"), 1),
    ];
    #endregion

    #region 检测入口
    /// <summary>检测请求威胁。检出返回首个命中，未检出返回 null</summary>
    /// <param name="url">请求地址，含查询字符串</param>
    /// <param name="body">请求体。仅文本类型内容，调用方限制大小</param>
    /// <param name="userAgent">用户代理</param>
    /// <returns>威胁检测结果</returns>
    public static ThreatResult? Detect(String? url, String? body = null, String? userAgent = null)
    {
        // 1. 协议异常（最廉价）
        var rs = DetectProtocolAnomaly(url);
        if (rs != null) return rs;

        var decoded = UrlDecode(url);

        // 2. 蜜罐路径
        rs = DetectHoneypot(decoded);
        if (rs != null) return rs;

        // 3. 扫描器指纹
        rs = DetectScanner(userAgent);
        if (rs != null) return rs;

        // 4. 攻击载荷：Url 解码文本，必要时附加双重解码副本
        rs = MatchPatterns(decoded, "Url");
        if (rs != null) return rs;

        var decoded2 = decoded != url ? UrlDecode(decoded) : null;
        if (decoded2 != null && decoded2 != decoded)
        {
            rs = MatchPatterns(decoded2, "Url");
            if (rs != null) return rs;
        }

        // 5. 攻击载荷：请求体（原样与解码两种形态）
        if (!body.IsNullOrEmpty())
        {
            rs = MatchPatterns(body, "Body");
            if (rs == null)
            {
                var bodyDecoded = UrlDecode(body);
                if (bodyDecoded != body) rs = MatchPatterns(bodyDecoded, "Body");
            }
            if (rs != null) return rs;
        }

        return null;
    }
    #endregion

    #region 内部检测
    /// <summary>协议异常检测。超长地址、空字节、双重编码、控制字符</summary>
    /// <param name="url">原始请求地址</param>
    /// <returns></returns>
    private static ThreatResult? DetectProtocolAnomaly(String? url)
    {
        if (url.IsNullOrEmpty()) return null;

        if (url.Length > 2048) return Create("协议异常", "超长URL", "Url", 1, url);

        if (url.Contains("%00")) return Create("协议异常", "空字节", "Url", 2, url);
        if (url.Contains("%25") && Regex.IsMatch(url, @"%25[0-9a-fA-F]{2}"))
            return Create("协议异常", "双重编码", "Url", 1, url);

        var decoded = UrlDecode(url);
        if (decoded != null)
        {
            if (decoded.Contains('\0')) return Create("协议异常", "空字节", "Url", 2, url);
            foreach (var ch in decoded)
            {
                // 允许制表/换行/回车，其余控制字符视为异常
                if (ch < 0x20 && ch != '\t' && ch != '\r' && ch != '\n') return Create("协议异常", "控制字符", "Url", 1, url);
            }
        }

        return null;
    }

    /// <summary>蜜罐路径检测。对地址路径部分匹配敏感路径与危险脚本后缀</summary>
    /// <param name="decodedUrl">解码后的请求地址</param>
    /// <returns></returns>
    private static ThreatResult? DetectHoneypot(String? decodedUrl)
    {
        if (decodedUrl.IsNullOrEmpty()) return null;

        // 仅取路径部分，避免查询字符串误伤
        var path = decodedUrl;
        var p = path.IndexOf('?');
        if (p >= 0) path = path[..p];
        if (path.IsNullOrEmpty()) return null;

        var lower = path.ToLowerInvariant();
        foreach (var item in _honeypotPaths)
        {
            if (lower.Contains(item)) return Create("蜜罐探测", item, "Url", 2, path);
        }

        var m = _dangerousExtension.Match(lower);
        if (m.Success) return Create("危险后缀", m.Value, "Url", 2, path);

        return null;
    }

    /// <summary>扫描器指纹检测</summary>
    /// <param name="userAgent">用户代理</param>
    /// <returns></returns>
    private static ThreatResult? DetectScanner(String? userAgent)
    {
        if (userAgent.IsNullOrEmpty()) return null;

        var lower = userAgent.ToLowerInvariant();
        foreach (var item in _scannerUas)
        {
            if (lower.Contains(item)) return Create("扫描器", item, "UserAgent", 2, userAgent);
        }

        return null;
    }

    /// <summary>攻击载荷正则匹配</summary>
    /// <param name="text">待检测文本</param>
    /// <param name="target">文本位置</param>
    /// <returns></returns>
    private static ThreatResult? MatchPatterns(String? text, String target)
    {
        if (text.IsNullOrEmpty()) return null;

        // 超长文本截断，防御检测成本放大
        if (text.Length > 65536) text = text[..65536];

        foreach (var (category, pattern, regex, level) in _attackPatterns)
        {
            Match m;
            try
            {
                m = regex.Match(text);
            }
            catch (RegexMatchTimeoutException)
            {
                continue;
            }

            if (m.Success) return Create(category, pattern, target, level, m.Value);
        }

        return null;
    }

    /// <summary>创建威胁检测结果</summary>
    /// <param name="category">威胁类别</param>
    /// <param name="pattern">命中特征</param>
    /// <param name="target">命中位置</param>
    /// <param name="level">威胁等级</param>
    /// <param name="snippet">原始片段</param>
    /// <returns></returns>
    private static ThreatResult Create(String category, String pattern, String target, Int32 level, String? snippet) => new()
    {
        Category = category,
        Pattern = pattern,
        Target = target,
        Level = level,
        Snippet = Cut(snippet, 100),
    };

    /// <summary>URL解码。畸形输入不抛异常，返回原样</summary>
    /// <param name="text">待解码文本</param>
    /// <returns></returns>
    private static String? UrlDecode(String? text)
    {
        if (text.IsNullOrEmpty()) return text;
        if (!text.Contains('%') && !text.Contains('+')) return text;

        try
        {
            return Uri.UnescapeDataString(text.Replace('+', ' '));
        }
        catch
        {
            return text;
        }
    }

    /// <summary>截断字符串</summary>
    /// <param name="text">文本</param>
    /// <param name="max">最大长度</param>
    /// <returns></returns>
    private static String? Cut(String? text, Int32 max) => text == null || text.Length <= max ? text : text[..max];

    /// <summary>创建忽略大小写的预编译正则</summary>
    /// <param name="pattern">正则表达式</param>
    /// <returns></returns>
    private static Regex R(String pattern) => new(pattern, _options, _timeout);
    #endregion
}
