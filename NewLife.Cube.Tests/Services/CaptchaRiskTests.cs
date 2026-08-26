using System;
using NewLife.Cube;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// 风险自适应验证码测试：GetRiskLevel 风险评分、RequireCaptcha 场景强制/自适应/可信设备豁免、SetTrustedDevice。
/// 使用内存缓存（TestCacheProvider）构造 UserService，不依赖数据库。
/// </summary>
public class CaptchaRiskTests : IDisposable
{
    // 与 UserService 缓存前缀常量保持一致（内部实现耦合，注释说明；变更 UserService 常量时需同步）
    private const String PasswordLoginIpPrefix = "CubeLogin:IP:";
    private const String PasswordLoginUserPrefix = "CubeLogin:";
    private const String LoginIpSubnet24Prefix = "CubeLogin:subnet24:";
    private const String LoginIpSubnet16Prefix = "CubeLogin:subnet16:";

    private readonly TestCacheProvider _cacheProvider = new();
    private readonly UserService _userService;
    private readonly CubeSetting _set;
    private readonly Int32 _origScene;
    private readonly Boolean _origRisk;
    private readonly Int32 _origThreshold;
    private readonly Int32 _origTrustedDays;
    private readonly Int32 _origMaxError;

    public CaptchaRiskTests()
    {
        _userService = new UserService(null!, null!, new PasswordService(), _cacheProvider, null!, null!, null!, null!, new TenantContextService());
        _set = CubeSetting.Current;

        // 保存原值，Dispose 恢复（进程级静态状态）
        _origScene = _set.CaptchaScene;
        _origRisk = _set.CaptchaRisk;
        _origThreshold = _set.CaptchaRiskThreshold;
        _origTrustedDays = _set.TrustedDeviceDays;
        _origMaxError = _set.MaxLoginError;

        _set.CaptchaScene = 0;
        _set.CaptchaRisk = true;
        _set.CaptchaRiskThreshold = 2;
        _set.TrustedDeviceDays = 30;
        _set.MaxLoginError = 5;
    }

    public void Dispose()
    {
        _set.CaptchaScene = _origScene;
        _set.CaptchaRisk = _origRisk;
        _set.CaptchaRiskThreshold = _origThreshold;
        _set.TrustedDeviceDays = _origTrustedDays;
        _set.MaxLoginError = _origMaxError;
        _cacheProvider.Cache.Clear();
    }

    #region GetRiskLevel 风险评分

    [Theory(DisplayName = "GetRiskLevel 内网IP 返回0")]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("192.168.1.100")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("::1")]
    public void GetRiskLevel_InnerIp_ReturnsZero(String ip)
    {
        Assert.Equal(0, _userService.GetRiskLevel(ip, null));
    }

    [Theory(DisplayName = "GetRiskLevel 公网IP无失败记录 返回1")]
    [InlineData("8.8.8.8")]
    [InlineData("114.114.114.114")]
    public void GetRiskLevel_PublicIpNoFail_ReturnsOne(String ip)
    {
        Assert.Equal(1, _userService.GetRiskLevel(ip, null));
    }

    [Fact(DisplayName = "GetRiskLevel 公网IP有登录失败记录 返回2")]
    public void GetRiskLevel_PublicIpWithFail_ReturnsTwo()
    {
        const String ip = "8.8.8.8";
        _cacheProvider.Cache.Set($"{PasswordLoginIpPrefix}{ip}", 1);

        Assert.Equal(2, _userService.GetRiskLevel(ip, null));
    }

    [Fact(DisplayName = "GetRiskLevel 账号有失败记录 风险提升")]
    public void GetRiskLevel_UserWithFail_RiskUp()
    {
        const String user = "riskuser";
        _cacheProvider.Cache.Set($"{PasswordLoginUserPrefix}{user}", 1);

        Assert.Equal(2, _userService.GetRiskLevel("8.8.8.8", user));
    }

    [Fact(DisplayName = "GetRiskLevel 子网有失败记录 风险提升")]
    public void GetRiskLevel_SubnetWithFail_RiskUp()
    {
        const String ip = "8.8.8.8";
        _cacheProvider.Cache.Set($"{LoginIpSubnet24Prefix}8.8.8", 1);

        Assert.Equal(2, _userService.GetRiskLevel(ip, null));
    }

    [Fact(DisplayName = "GetRiskLevel 达到封禁阈值 返回3")]
    public void GetRiskLevel_Blocked_ReturnsThree()
    {
        const String ip = "8.8.8.8";
        _cacheProvider.Cache.Set($"{PasswordLoginIpPrefix}{ip}", 5);

        Assert.Equal(3, _userService.GetRiskLevel(ip, null));
    }

    #endregion

    #region RequireCaptcha 场景强制与自适应

    [Fact(DisplayName = "RequireCaptcha CaptchaScene强制 内网也要求")]
    public void RequireCaptcha_SceneForced_AlwaysTrue()
    {
        _set.CaptchaScene = 1;

        Assert.True(_userService.RequireCaptcha(1, "127.0.0.1"));
        Assert.True(_userService.RequireCaptcha(1, "8.8.8.8"));
    }

    [Fact(DisplayName = "RequireCaptcha 自适应关闭 不要求验证码")]
    public void RequireCaptcha_RiskOff_NoCaptcha()
    {
        _set.CaptchaRisk = false;
        _set.CaptchaScene = 0;

        Assert.False(_userService.RequireCaptcha(1, "8.8.8.8"));
    }

    [Fact(DisplayName = "RequireCaptcha 内网低风险 免验证码")]
    public void RequireCaptcha_InnerIp_NoCaptcha()
    {
        _set.CaptchaScene = 0;

        Assert.False(_userService.RequireCaptcha(1, "127.0.0.1"));
        Assert.False(_userService.RequireCaptcha(2, "192.168.1.5"));
    }

    [Fact(DisplayName = "RequireCaptcha 公网无异常 低于阈值免验证码")]
    public void RequireCaptcha_PublicIpBelowThreshold_NoCaptcha()
    {
        _set.CaptchaScene = 0;

        // 公网+无失败 = 风险1 < 阈值2 → 免
        Assert.False(_userService.RequireCaptcha(1, "8.8.8.8"));
    }

    [Fact(DisplayName = "RequireCaptcha 公网有失败记录 达到阈值要求验证码")]
    public void RequireCaptcha_PublicIpWithFail_RequireCaptcha()
    {
        _set.CaptchaScene = 0;
        const String ip = "8.8.8.8";
        _cacheProvider.Cache.Set($"{PasswordLoginIpPrefix}{ip}", 1);

        // 公网+失败 = 风险2 >= 阈值2 → 要求
        Assert.True(_userService.RequireCaptcha(1, ip));
    }

    [Fact(DisplayName = "RequireCaptcha 阈值调低 公网即要求")]
    public void RequireCaptcha_ThresholdLowered_PublicIpRequires()
    {
        _set.CaptchaScene = 0;
        _set.CaptchaRiskThreshold = 1;
        const String ip = "8.8.8.8";

        // 阈值=1：公网（风险1）即要求
        Assert.True(_userService.RequireCaptcha(1, ip));
    }

    [Fact(DisplayName = "RequireCaptcha 可信设备豁免自适应验证码")]
    public void RequireCaptcha_TrustedDevice_Exempt()
    {
        _set.CaptchaScene = 0;
        const String ip = "8.8.8.8";
        const String deviceId = "device-abc123";
        _cacheProvider.Cache.Set($"{PasswordLoginIpPrefix}{ip}", 1);

        // 未标记前要求
        Assert.True(_userService.RequireCaptcha(1, ip, null, deviceId));

        // 标记可信后豁免
        _userService.SetTrustedDevice(deviceId, ip);
        Assert.False(_userService.RequireCaptcha(1, ip, null, deviceId));
    }

    [Fact(DisplayName = "RequireCaptcha CaptchaScene强制 可信设备不豁免")]
    public void RequireCaptcha_SceneForced_TrustedDeviceNotExempt()
    {
        _set.CaptchaScene = 1;
        const String ip = "127.0.0.1";
        const String deviceId = "device-abc123";
        _userService.SetTrustedDevice(deviceId, ip);

        // 场景强制不受可信设备豁免
        Assert.True(_userService.RequireCaptcha(1, ip, null, deviceId));
    }

    #endregion

    #region 可信设备

    [Fact(DisplayName = "IsTrustedDevice 换IP 视为不可信")]
    public void IsTrustedDevice_DifferentIp_ReturnsFalse()
    {
        _userService.SetTrustedDevice("device-x", "10.0.0.1");

        Assert.True(_userService.IsTrustedDevice("device-x", "10.0.0.1"));
        Assert.False(_userService.IsTrustedDevice("device-x", "8.8.8.8"));
    }

    [Fact(DisplayName = "TrustedDeviceDays=0 不标记可信设备")]
    public void TrustedDeviceDaysZero_NoTrust()
    {
        _set.TrustedDeviceDays = 0;
        _userService.SetTrustedDevice("device-y", "10.0.0.1");

        Assert.False(_userService.IsTrustedDevice("device-y", "10.0.0.1"));
    }

    #endregion
}
