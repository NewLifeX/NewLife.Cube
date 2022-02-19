using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NewLife.Cube.Web;

/// <summary>
/// 浏览器特性分析器
/// </summary>
public class UserAgentParser
{
    #region 属性
    /// <summary>
    /// 兼容性，一般是 Mozilla/5.0
    /// </summary>
    public String Compatible { get; set; }

    /// <summary>平台</summary>
    public String Platform { get; set; }

    /// <summary>加密特性</summary>
    public String Encryption { get; set; }

    /// <summary>系统或处理器</summary>
    public String OSorCPU { get; set; }

    /// <summary>设备</summary>
    public String Device { get; set; }

    /// <summary>设备编译版本</summary>
    public String DeviceBuild { get; set; }

    /// <summary>用户浏览器</summary>
    public String Brower { get; set; }

    /// <summary>移动版本</summary>
    public String Mobile { get; set; }
    #endregion

    #region 方法
    private static readonly Regex _regex = new(@"([^\s\(\)]+)\s*(\([^\(\)]+\))?");
    /// <summary>
    /// 分析浏览器UserAgent字符串
    /// </summary>
    /// <param name="userAgent"></param>
    /// <returns></returns>
    public Boolean Parse(String userAgent)
    {
        if (userAgent.IsNullOrEmpty()) return false;

        var ms = _regex.Matches(userAgent);

        var count = ms.Count;
        if (count == 0) return false;

        // 首先识别主流浏览器，不同浏览器格式不同
        var infos = ms.Select(e => e.Value?.Trim()).ToArray();
        ParseChrome(infos);

        // 其它浏览器
        if (Brower.IsNullOrEmpty()) ParseOtherBrowser(infos);

        // 移动
        var inf = infos.FirstOrDefault(e => e.StartsWithIgnoreCase("Mobile/"));
        if (inf != null) Mobile = inf.Trim();

        {
            // 识别操作系统平台
            var match = ms[0];
            Compatible = match.Groups[1].Value;

            // Mozilla/MozillaVersion (Platform; Encryption; OS-or-CPU; Language; PrereleaseVersi
            var ss = match.Groups[2].Value?.Trim('(', ')').Split(';');
            if (ss != null && ss.Length > 0)
            {
                if (ss.Length >= 4)
                {
                    Platform = ss[0]?.Trim();
                    Encryption = ss[1]?.Trim();
                    OSorCPU = ss[2]?.Trim().TrimStart("CPU ");
                    Device = ss[3]?.Trim();
                }
                else if (ss.Length >= 2)
                {
                    Platform = ss[0]?.Trim();
                    OSorCPU = ss[1]?.Trim().TrimStart("CPU ");
                }

                if (!OSorCPU.IsNullOrEmpty())
                {
                    var p = OSorCPU.IndexOf("like");
                    if (p >= 0) OSorCPU = OSorCPU[..p].Trim();
                }

                if (!Device.IsNullOrEmpty())
                {
                    var p = Device.IndexOf("Build/");
                    if (p >= 0)
                    {
                        DeviceBuild = Device[p..].Trim();
                        Device = Device[..p].Trim();
                    }
                }
            }
        }

        return true;
    }

    private void ParseChrome(String[] infos)
    {
        var inf = infos.FirstOrDefault(e => e.StartsWith("Chrome/"));
        if (inf == null) return;

        Brower = inf;
    }

    private void ParseOtherBrowser(String[] infos)
    {
        var list = infos.Where(e => !e.Contains("(") && !e.StartsWithIgnoreCase("AppleWebKit/", "Chrome/", "Safari/", "Mobile/", "Version/")).ToList();
        if (list.Count == 0)
        {
            // 最后识别Safari，别人都仿它
            var inf = infos.FirstOrDefault(e => e.StartsWithIgnoreCase("Safari/"));
            if (inf != null)
            {
                Brower = inf.Trim();

                // 合并版本
                inf = infos.FirstOrDefault(e => e.StartsWithIgnoreCase("Version/"));
                if (inf != null)
                    Brower = Brower.Split('/')[0] + "/" + inf.Split('/')[^1];
            }

            return;
        }

        Brower = list[0];
    }
    #endregion
}
