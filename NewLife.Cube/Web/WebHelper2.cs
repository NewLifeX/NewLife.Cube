using Microsoft.Extensions.Primitives;
using NewLife.Collections;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.Services;
using NewLife.Log;
using NewLife.Serialization;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>Web助手</summary>
public static class WebHelper2
{
    #region Http请求
    /// <summary>获取请求值</summary>
    /// <param name="request"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static String Get(this HttpRequest request, String key) => request.GetRequestValue(key);

    /// <summary>获取Session值</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="session"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static T Get<T>(this ISession session, String key) where T : class
    {
        if (!session.TryGetValue(key, out var buf)) return default(T);

        return buf.ToStr().ToJsonEntity<T>();
    }

    /// <summary>获取Session值</summary>
    /// <param name="session"></param>
    /// <param name="key"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    public static Object Get(this ISession session, String key, Type targetType)
    {
        if (!session.TryGetValue(key, out var buf)) return null;

        var rs = buf.ToStr().ToJsonEntity(targetType, null);
        if (rs is IEntity entity && entity.HasDirty) entity.Dirtys.Clear();

        return rs;
    }

    /// <summary>设置Session值</summary>
    /// <param name="session"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public static void Set(this ISession session, String key, Object value) => session.Set(key, value?.ToJson().GetBytes());

    /// <summary>获取用户主机。优先从转发头解析；配置可信代理后按代理链解析并防伪造，最终返回单一IP地址</summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>客户端IP地址</returns>
    /// <remarks>
    /// 后续收敛：NewLife.Core 发版后统一改用其 IP/网段工具，Web 侧解析复用 NewLife.Remoting.Extensions 的公共实现，魔方仅保留自动学习等差异化部分
    /// </remarks>
    public static String GetUserHost(this HttpContext context)
    {
        var request = context.Request;

        // 直连地址
        var remote = context.Connection?.RemoteIpAddress;
        if (remote != null && remote.IsIPv4MappedToIPv6) remote = remote.MapToIPv4();
        var remoteIp = remote + "";

        var str = "";
        if (str.IsNullOrEmpty()) str = request.Headers["X-Remote-Ip"];
        if (str.IsNullOrEmpty()) str = request.Headers["HTTP_X_FORWARDED_FOR"];
        if (str.IsNullOrEmpty()) str = request.Headers["X-Real-IP"];
        if (str.IsNullOrEmpty()) str = request.Headers["X-Forwarded-For"];

        if (!str.IsNullOrEmpty())
        {
            // 自动学习：仅携带转发头的请求说明来源正在替他人转发，内网直连来源才可能是反向代理/负载均衡入口
            LearnTrustedProxy(remoteIp);

            var trusted = GetAllTrustedProxies();
            if (trusted.Length == 0)
            {
                // 未配置可信代理，兼容旧行为信任全部转发头；多层反代时取链首地址，并折叠为单一IP
                var first = str.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).FirstOrDefault(e => e.Length > 0);
                if (!first.IsNullOrEmpty()) str = first;
            }
            else if (!IsTrustedProxy(remoteIp, trusted))
            {
                // 配置了可信代理但直连来源不可信，转发头可被伪造，直接使用直连地址
                str = remoteIp;
            }
            else
            {
                // 从链尾向左跳过可信代理，取第一个不可信地址作为客户端地址
                var ips = str.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).Where(e => e.Length > 0).ToList();
                var found = (String)null;
                for (var i = ips.Count - 1; i >= 0; i--)
                {
                    if (!IsTrustedProxy(ips[i], trusted))
                    {
                        found = ips[i];
                        break;
                    }
                }
                str = found ?? (ips.Count > 0 ? ips[0] : remoteIp);
            }
        }

        if (str.IsNullOrEmpty()) str = request.Headers["REMOTE_ADDR"];
        //if (str.IsNullOrEmpty()) str = request.Headers["Host"];
        if (str.IsNullOrEmpty()) str = remoteIp;

        return str;
    }

    private static readonly Object _learnLock = new();

    /// <summary>获取全部可信代理。手工配置优先，未配置时使用自动学习结果；列表项支持精确IP、*通配和IPv4 CIDR网段</summary>
    /// <returns>可信代理数组，未配置且未学习时为空数组</returns>
    public static String[] GetAllTrustedProxies()
    {
        var set = CubeSetting.Current;

        // 手工配置优先：配置可信代理后即接管，不再使用学习结果
        var txt = set.TrustedProxies;
        if (txt.IsNullOrEmpty()) txt = set.LearnedProxies;
        if (txt.IsNullOrEmpty()) return [];

        var list = new List<String>();
        foreach (var item in txt.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            var t = item.Trim();
            if (t.Length > 0 && !list.Any(e => e.EqualIgnoreCase(t))) list.Add(t);
        }

        return [.. list];
    }

    /// <summary>判断地址是否属于可信代理（含自动学习）。自动封禁前调用，避免误封反向代理/网关导致整片用户不可用</summary>
    /// <param name="ip">待判断地址</param>
    /// <returns>是否可信代理</returns>
    public static Boolean IsTrustedProxyAddress(String ip)
    {
        if (ip.IsNullOrEmpty()) return false;

        foreach (var t in GetAllTrustedProxies())
        {
            // 通配全部只表示信任所有转发头，不代表所有地址都是代理，不参与封禁保护
            if (t == "*" || t == "0.0.0.0/0") continue;

            if (t.Contains('/'))
            {
                if (MatchCidr(ip, t)) return true;
            }
            else if (t.IsMatch(ip))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>获取代理链摘要，用于安全事件审计。记录直连地址与转发头原始值，仅在安全事件写入时调用</summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>链摘要文本</returns>
    public static String GetIpChain(this HttpContext context)
    {
        var request = context.Request;

        var remote = context.Connection?.RemoteIpAddress;
        if (remote != null && remote.IsIPv4MappedToIPv6) remote = remote.MapToIPv4();

        var sb = Pool.StringBuilder.Get();
        try
        {
            sb.Append("直连=").Append(remote + "");
            var xff = request.Headers["X-Forwarded-For"] + "";
            if (xff.IsNullOrEmpty()) xff = request.Headers["HTTP_X_FORWARDED_FOR"] + "";
            if (!xff.IsNullOrEmpty()) sb.Append("; XFF=").Append(xff);
            var real = request.Headers["X-Real-IP"] + "";
            if (!real.IsNullOrEmpty()) sb.Append("; X-Real-IP=").Append(real);
            var remoteHeader = request.Headers["X-Remote-Ip"] + "";
            if (!remoteHeader.IsNullOrEmpty()) sb.Append("; X-Remote-Ip=").Append(remoteHeader);

            return sb.ToString();
        }
        finally
        {
            Pool.StringBuilder.Return(sb);
        }
    }

    /// <summary>自动学习可信代理。携带转发头且可信代理未配置时，学习内网直连来源；先到先学，学习数量由配置控制</summary>
    /// <param name="ip">直连地址</param>
    private static void LearnTrustedProxy(String ip)
    {
        if (ip.IsNullOrEmpty()) return;

        var set = CubeSetting.Current;

        // 0=不学习。单机房主备通常2个入口，双机房通常4个
        var max = set.TrustedProxyLearning;
        if (max <= 0) return;

        // 手工配置优先，配置了可信代理就不再学习
        if (!set.TrustedProxies.IsNullOrEmpty()) return;

        // 只学内网来源，公网来源不学；环回地址（本机）语义模糊，交给手工配置
        if (!AuthHelper.IsInnerIp(ip)) return;
        if (System.Net.IPAddress.TryParse(ip, out var addr) && System.Net.IPAddress.IsLoopback(addr)) return;

        // 已学习过则跳过。分隔符包裹，避免10.0.0.1误判为已存在于10.0.0.10
        var learned = set.LearnedProxies;
        if (!learned.IsNullOrEmpty() && ($",{learned},").Contains($",{ip},", StringComparison.OrdinalIgnoreCase)) return;

        lock (_learnLock)
        {
            // 锁内重读，避免并发学习时相互覆盖
            var current = set.LearnedProxies;
            var list = new List<String>();
            if (!current.IsNullOrEmpty())
            {
                foreach (var item in current.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = item.Trim();
                    if (t.Length > 0 && !list.Any(e => e.EqualIgnoreCase(t))) list.Add(t);
                }
            }

            if (list.Count >= max) return;
            if (list.Any(e => e.EqualIgnoreCase(ip))) return;

            list.Add(ip);
            set.LearnedProxies = String.Join(",", list);

            try
            {
                set.Save();

                XTrace.WriteLine("安全防御自动学习可信代理 {0}，累计 {1}/{2} 个", ip, list.Count, max);
            }
            catch (Exception ex)
            {
                // 配置保存失败不影响请求处理，内存中已生效
                XTrace.WriteException(ex);
            }
        }
    }

    /// <summary>判断地址是否在可信代理列表内。列表项支持精确IP、*通配和IPv4 CIDR网段</summary>
    /// <param name="ip">待判断IP</param>
    /// <param name="trusted">可信代理列表</param>
    /// <returns>是否可信</returns>
    private static Boolean IsTrustedProxy(String ip, String[] trusted)
    {
        if (ip.IsNullOrEmpty()) return false;

        foreach (var item in trusted)
        {
            var t = item.Trim();
            if (t.Length == 0) continue;

            if (t == "*") return true;

            if (t.Contains('/'))
            {
                if (MatchCidr(ip, t)) return true;
            }
            else if (t.IsMatch(ip))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>IPv4 CIDR网段匹配</summary>
    /// <param name="ip">IPv4地址</param>
    /// <param name="cidr">网段，如 10.0.0.0/8</param>
    /// <returns>是否属于该网段</returns>
    private static Boolean MatchCidr(String ip, String cidr)
    {
        var ss = cidr.Split('/');
        if (ss.Length != 2) return false;

        var mask = ss[1].ToInt();
        if (mask is < 0 or > 32) return false;

        var ip1 = ParseIpv4(ip);
        var ip2 = ParseIpv4(ss[0]);
        if (ip1 < 0 || ip2 < 0) return false;

        if (mask == 0) return true;

        var bits = (~((1L << (32 - mask)) - 1)) & 0xFFFFFFFFL;
        return (ip1 & bits) == (ip2 & bits);
    }

    /// <summary>解析IPv4地址为整数，非法返回-1</summary>
    /// <param name="ip">IPv4地址</param>
    /// <returns>整型地址</returns>
    private static Int64 ParseIpv4(String ip)
    {
        if (ip.IsNullOrEmpty()) return -1;

        var ss = ip.Split('.');
        if (ss.Length != 4) return -1;

        var n = 0L;
        foreach (var item in ss)
        {
            var b = item.ToInt();
            if (b is < 0 or > 255) return -1;

            n = (n << 8) | (UInt32)b;
        }

        return n;
    }

    /// <summary>返回请求字符串和表单的名值字段，过滤空值和ViewState，同名时优先表单</summary>
    public static IDictionary<String, String> Params
    {
        get
        {
            var ctx = NewLife.Web.HttpContext.Current;
            if (ctx.Items["Params"] is IDictionary<String, String> dic) return dic;

            var req = ctx.Request;
            var nvss = new[]
            {
                req.Query,
                req.HasFormContentType ? (IEnumerable<KeyValuePair<String, StringValues>>) req.Form : new List<KeyValuePair<String, StringValues>>()
            };

            // 这里必须用可空字典，否则直接通过索引查不到数据时会抛出异常
            dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            foreach (var nvs in nvss)
            {
                foreach (var item in nvs)
                {
                    if (item.Key.IsNullOrWhiteSpace()) continue;
                    if (item.Key.StartsWithIgnoreCase("__VIEWSTATE")) continue;

                    // 空值不需要
                    var value = item.Value.ToString();
                    if (value.IsNullOrWhiteSpace())
                    {
                        // 如果请求字符串里面有值而后面表单为空，则抹去
                        if (dic.ContainsKey(item.Key)) dic.Remove(item.Key);
                        continue;
                    }

                    // 同名时优先表单
                    dic[item.Key] = value.Trim();
                }
            }
            ctx.Items["Params"] = dic;

            return dic;
        }
    }

    /// <summary>获取Linux发行版名称</summary>
    /// <returns></returns>
    public static String GetLinuxName()
    {
        var fr = "/etc/redhat-release";
        var dr = "/etc/debian-release";
        if (File.Exists(fr))
            return File.ReadAllText(fr).Trim();
        else if (File.Exists(dr))
            return File.ReadAllText(dr).Trim();
        else
        {
            var sr = "/etc/os-release";
            if (File.Exists(sr)) return File.ReadAllText(sr).SplitAsDictionary("=", "\n", true)["PRETTY_NAME"].Trim();
        }

        return null;
    }

    /// <summary>获取引用页</summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static String GetReferer(this HttpRequest request) => request.Headers["Referer"].FirstOrDefault();

    /// <summary>获取外部来源。空值、无法解析或属于本站的引用页视为站内跳转，返回空</summary>
    /// <param name="request">请求</param>
    /// <returns>外部来源URL，否则返回空</returns>
    public static String GetExternalRefer(this HttpRequest request) => GetExternalRefer(request.Headers["Referer"].FirstOrDefault(), request.Host.Host + "");

    /// <summary>获取外部来源。空值、无法解析或属于本站的引用页视为站内跳转，返回空</summary>
    /// <param name="refer">引用页URL</param>
    /// <param name="host">本站主机名</param>
    /// <returns>外部来源URL，否则返回空</returns>
    public static String GetExternalRefer(String refer, String host)
    {
        if (refer.IsNullOrEmpty() || host.IsNullOrEmpty()) return null;

        // 无法解析为绝对地址的，不视为有效外部来源
        if (!Uri.TryCreate(refer, UriKind.Absolute, out var uri)) return null;

        // 与本站同主机名的引用页视为站内跳转
        if (uri.Host.EqualIgnoreCase(host)) return null;

        return refer;
    }

    /// <summary>获取当前请求的外部来源。优先取在线会话采集的首个外部来源，其次取请求引用页</summary>
    /// <param name="context">上下文</param>
    /// <returns>外部来源URL，无则返回空</returns>
    public static String GetSourceUrl(this HttpContext context)
    {
        if (context.Items["Cube_Online"] is UserOnline online && !online.Referer.IsNullOrEmpty()) return online.Referer;

        return GetExternalRefer(context.Request);
    }

    /// <summary>从URL提取主机名，用于用户归属等短字段</summary>
    /// <param name="url">完整URL</param>
    /// <returns>主机名，解析失败返回空</returns>
    public static String GetHost(this String url)
    {
        if (url.IsNullOrEmpty()) return null;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
    }
    #endregion

    #region Http响应
    /// <summary>设置文件哈希相关的响应头</summary>
    /// <param name="response">Http响应</param>
    /// <param name="hash">文件哈希值，格式：[算法名$]哈希值，如MD5$abc123或abc123</param>
    public static void SetFileHashHeaders(this HttpResponse response, String hash)
    {
        if (hash.IsNullOrEmpty()) return;

        // 解析哈希算法名称和哈希值
        var algorithm = "MD5";
        var hashValue = hash;

        var dollarIndex = hash.IndexOf('$');
        if (dollarIndex > 0)
        {
            algorithm = hash[..dollarIndex];
            hashValue = hash[(dollarIndex + 1)..];
        }

        // 1. RFC 3230 标准 Digest 头
        response.Headers["Digest"] = $"{algorithm}={hashValue}";

        // 2. X-Content-MD5（兼容某些客户端，总是用MD5）
        if (algorithm.EqualIgnoreCase("MD5"))
            response.Headers["X-Content-MD5"] = hashValue;

        // 3. ETag（用于缓存验证）
        response.Headers["ETag"] = $"\"{hashValue}\"";

        // 4. 自定义头（易于识别）
        response.Headers["X-File-Hash"] = $"{algorithm}:{hashValue}";
    }
    #endregion

    /// <summary>修正多租户菜单</summary>
    public static void FixTenantMenu()
    {
        var root = Menu.FindByName("Admin");
        if (root != null)
        {
            var set = CubeSetting.Current;
            foreach (var item in root.Childs)
            {
                if (item.Name.Contains("Tenant"))
                {
                    item.Visible = set.EnableTenant;
                    item.Update();
                }
            }
        }
    }
}