using NewLife.Cube.Services;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>节点互拉附件时，短时令牌只加在非公开分类上。</summary>
public class FileStoragePullTokenTests
{
    [Fact(DisplayName = "关闭附件校验时不签发同步令牌")]
    public void NeedSyncPullToken_ValidateOff_ReturnsFalse()
    {
        Assert.False(CubeFileStorage.NeedSyncPullToken(false, "", "WorkOrder"));
    }

    [Fact(DisplayName = "公开分类不签发同步令牌")]
    public void NeedSyncPullToken_PublicCategory_ReturnsFalse()
    {
        Assert.False(CubeFileStorage.NeedSyncPullToken(true, "markdown, WorkOrder", "workorder"));
    }

    [Fact(DisplayName = "非公开分类签发同步令牌")]
    public void NeedSyncPullToken_PrivateCategory_ReturnsTrue()
    {
        Assert.True(CubeFileStorage.NeedSyncPullToken(true, "markdown,avatar", "WorkOrder"));
        Assert.True(CubeFileStorage.NeedSyncPullToken(true, "", "WorkOrder"));
        Assert.True(CubeFileStorage.NeedSyncPullToken(true, "markdown", null));
    }
}
