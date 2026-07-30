using System.ComponentModel;
using NewLife.Cube.AI;
using Xunit;

namespace XUnitTest;

/// <summary>AI 数据助手单元测试 — 字段安全过滤</summary>
public class AiDataHelperTests
{
    [Fact]
    [DisplayName("IsSafeFieldName - 普通业务字段应安全")]
    public void IsSafeFieldName_NormalField_ShouldBeSafe()
    {
        Assert.True(AiDataHelper.IsSafeFieldName("UserName"));
        Assert.True(AiDataHelper.IsSafeFieldName("DisplayName"));
        Assert.True(AiDataHelper.IsSafeFieldName("CreateTime"));
        Assert.True(AiDataHelper.IsSafeFieldName("Status"));
        Assert.True(AiDataHelper.IsSafeFieldName("Amount"));
        Assert.True(AiDataHelper.IsSafeFieldName("Remark"));
    }

    [Fact]
    [DisplayName("IsSafeFieldName - 密码相关字段应被过滤")]
    public void IsSafeFieldName_Password_ShouldBeUnsafe()
    {
        Assert.False(AiDataHelper.IsSafeFieldName("Password"));
        Assert.False(AiDataHelper.IsSafeFieldName("UserPassword"));
        Assert.False(AiDataHelper.IsSafeFieldName("Pwd"));
    }

    [Fact]
    [DisplayName("IsSafeFieldName - 手机号字段应被过滤")]
    public void IsSafeFieldName_Mobile_ShouldBeUnsafe()
    {
        Assert.False(AiDataHelper.IsSafeFieldName("Mobile"));
        Assert.False(AiDataHelper.IsSafeFieldName("Phone"));
        Assert.False(AiDataHelper.IsSafeFieldName("CellPhone"));
    }

    [Fact]
    [DisplayName("IsSafeFieldName - Token/密钥字段应被过滤")]
    public void IsSafeFieldName_Token_ShouldBeUnsafe()
    {
        Assert.False(AiDataHelper.IsSafeFieldName("Token"));
        Assert.False(AiDataHelper.IsSafeFieldName("AccessToken"));
        Assert.False(AiDataHelper.IsSafeFieldName("ApiKey"));
        Assert.False(AiDataHelper.IsSafeFieldName("Secret"));
        Assert.False(AiDataHelper.IsSafeFieldName("AppSecret"));
    }

    [Fact]
    [DisplayName("IsSafeFieldName - 邮箱字段应被过滤")]
    public void IsSafeFieldName_Email_ShouldBeUnsafe()
    {
        Assert.False(AiDataHelper.IsSafeFieldName("Email"));
        Assert.False(AiDataHelper.IsSafeFieldName("Mail"));
    }

    [Fact]
    [DisplayName("IsSafeFieldName - 空字符串应不安全")]
    public void IsSafeFieldName_NullOrEmpty_ShouldBeUnsafe()
    {
        Assert.False(AiDataHelper.IsSafeFieldName(null!));
        Assert.False(AiDataHelper.IsSafeFieldName(""));
    }

    [Fact]
    [DisplayName("IsSafeFieldName - 大小写不敏感匹配")]
    public void IsSafeFieldName_CaseInsensitive()
    {
        Assert.False(AiDataHelper.IsSafeFieldName("password"));
        Assert.False(AiDataHelper.IsSafeFieldName("PASSWORD"));
        Assert.False(AiDataHelper.IsSafeFieldName("mObIlE"));
        Assert.False(AiDataHelper.IsSafeFieldName("EMAIL"));
    }

    [Fact]
    [DisplayName("IsSafeFieldName - IP/MAC/地址等隐私字段应过滤")]
    public void IsSafeFieldName_PrivacyFields_ShouldBeUnsafe()
    {
        Assert.False(AiDataHelper.IsSafeFieldName("IP"));
        Assert.False(AiDataHelper.IsSafeFieldName("ClientIP"));
        Assert.False(AiDataHelper.IsSafeFieldName("MacAddress"));
        Assert.False(AiDataHelper.IsSafeFieldName("Avatar"));
        Assert.False(AiDataHelper.IsSafeFieldName("Fingerprint"));
        Assert.False(AiDataHelper.IsSafeFieldName("DeviceId"));
    }
}
