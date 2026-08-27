using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.Services;

/// <summary>认证辅助工具。子网/设备ID/IP判定等内部静态方法，供基础与增强认证服务共享</summary>
internal static class AuthHelper
{
    /// <summary>从完整IPv4地址提取三段前缀（/24 子网，如103.125.146）。非IPv4地址返回空字符串</summary>
    /// <param name="ip">完整IP地址</param>
    /// <returns>三段IP前缀；非IPv4或格式不合法时返回空字符串</returns>
    public static String GetSubnet24(String ip)
    {
        if (ip.IsNullOrEmpty()) return "";
        if (!System.Net.IPAddress.TryParse(ip, out var addr) || addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return "";
        var parts = ip.Split('.');
        return parts.Length >= 3 ? $"{parts[0]}.{parts[1]}.{parts[2]}" : "";
    }

    /// <summary>从完整IPv4地址提取两段前缀（/16 子网，如103.125）。非IPv4地址返回空字符串</summary>
    /// <param name="ip">完整IP地址</param>
    /// <returns>两段IP前缀；非IPv4或格式不合法时返回空字符串</returns>
    public static String GetSubnet16(String ip)
    {
        if (ip.IsNullOrEmpty()) return "";
        if (!System.Net.IPAddress.TryParse(ip, out var addr) || addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return "";
        var parts = ip.Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : "";
    }

    /// <summary>获取设备ID。优先从 Cookie 读取，未携带时返回空</summary>
    /// <param name="httpContext">HTTP上下文</param>
    /// <returns>设备ID，无 Cookie 时返回空</returns>
    public static String GetDeviceId(HttpContext httpContext)
    {
        if (httpContext == null || httpContext.Request == null) return null;

        var id = httpContext.Request.Cookies["CubeDeviceId"];
        if (id.IsNullOrEmpty()) id = httpContext.Request.Cookies["CubeDeviceId0"];
        return id;
    }

    /// <summary>判断IP是否为内网/本机地址</summary>
    /// <param name="ip">客户端IP</param>
    /// <returns>内网地址返回 true</returns>
    public static Boolean IsInnerIp(String ip)
    {
        if (ip.IsNullOrEmpty()) return false;

        // 去除IPv6端口与IPv4映射前缀
        var p = ip.Trim();
        var idx = p.IndexOf(':');
        if (idx > 0 && p.IndexOf('.') < 0) p = p.Substring(0, idx);
        if (p.StartsWith("::ffff:")) p = p.Substring(7);

        if (p == "::1" || p == "127.0.0.1") return true;

        // IPv4 私有网段：10.*、172.16-31.*、192.168.*、127.*
        var ss = p.Split('.');
        if (ss.Length != 4) return false;
        if (!Int32.TryParse(ss[0], out var a) || !Int32.TryParse(ss[1], out var b)) return false;
        if (a == 10) return true;
        if (a == 172 && b >= 16 && b <= 31) return true;
        if (a == 192 && b == 168) return true;
        if (a == 127) return true;

        return false;
    }
}
