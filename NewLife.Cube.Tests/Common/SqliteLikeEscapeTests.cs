using System;
using XCode.DataAccessLayer;
using XCode.Membership;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// 需要 XCode 指定版本及以上的用例。测试宿主引用的 XCode 版本过低时自动跳过并说明原因。
/// </summary>
public sealed class XCodeVersionFactAttribute : FactAttribute
{
    /// <summary>实例化。检查当前 XCode 版本是否满足要求</summary>
    /// <param name="major">主版本</param>
    /// <param name="minor">次版本</param>
    /// <param name="year">版本年份</param>
    /// <param name="day">版本日期</param>
    /// <param name="reason">跳过说明</param>
    public XCodeVersionFactAttribute(Int32 major, Int32 minor, Int32 year, Int32 day, String reason)
    {
        var version = typeof(DAL).Assembly.GetName().Version ?? new Version();
        var required = new Version(major, minor, year, day);
        if (version < required) Skip = $"{reason}（需要 XCode {required} 及以上，当前 {version}）";
    }
}

/// <summary>
/// SQLite 模糊搜索转义回归测试。
///
/// 根因在 NewLife.XCode 的 <c>DbBase.FormatLike</c>：它把 <c>%</c>/<c>_</c> 转义为 <c>\%</c>/<c>\_</c>，
/// 但只有 SQL Server/MySQL 把反斜杠当作 LIKE 的默认转义字符；SQLite 未声明 <c>ESCAPE</c> 时反斜杠是普通字符，
/// 导致含下划线/百分号的关键字一条也查不到。修复已提交到 XCode（SQLite 方言追加 <c>ESCAPE '\'</c>），
/// 随 2026.10.27 及以后版本发布；升级依赖后本用例自动生效。
/// </summary>
[Collection("TenantAuth")]
public class SqliteLikeEscapeTests
{
    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public SqliteLikeEscapeTests(TenantAuthFixture fixture) { }

    [XCodeVersionFact(12, 3, 2026, 1027, "等待 XCode 的 SQLite LIKE 转义修复发版", DisplayName = "SQLite模糊搜索_下划线与百分号按字面量匹配")]
    public void Contains_SpecialChars_MatchLiteral()
    {
        var category = "LikeEsc_" + Guid.NewGuid().ToString("N")[..8];
        Parameter.GetOrAdd(0, category, "K1", "sess_xxxx");
        Parameter.GetOrAdd(0, category, "K2", "sessxxxx");
        Parameter.GetOrAdd(0, category, "K3", "rate%5");
        Parameter.GetOrAdd(0, category, "K4", "rateX5");
        try
        {
            // 下划线必须按字面量匹配，不能当作“任意单字符”通配符
            Assert.Equal(1, Parameter.FindCount(Parameter._.Value.Contains("sess_xxxx") & Parameter._.Category == category));

            // 百分号同理
            Assert.Equal(1, Parameter.FindCount(Parameter._.Value.Contains("rate%5") & Parameter._.Category == category));
        }
        finally
        {
            Parameter.Delete(Parameter._.Category == category);
        }
    }
}
