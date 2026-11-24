using NewLife.Cube;
using NewLife.Cube.Services;
using NewLife.Messaging;
using Stardust.Storages;
using Xunit;

namespace XUnitTest;

/// <summary>分布式文件存储就绪判定。未初始化时控制器跳过远端拉取，避免事件总线尚未创建导致异常。</summary>
public class FileStorageReadinessTests
{
    [Fact(DisplayName = "文件存储：未初始化时不可请求远端文件")]
    public void CanRequestFile_False_BeforeInitialized()
    {
        using var storage = new CubeFileStorage(CubeSetting.Current, null!, null!, null!);

        Assert.False(storage.IsReady);
        Assert.False(FileStorageExtensions.CanRequestFile(storage));
        Assert.False(FileStorageExtensions.CanRequestFile(null));
    }

    [Fact(DisplayName = "文件存储：事件总线就绪后可以请求远端文件")]
    public void CanRequestFile_True_AfterBusAssigned()
    {
        using var storage = new CubeFileStorage(CubeSetting.Current, null!, null!, null!)
        {
            FileRequestBus = new EventBus<FileRequest>(),
        };

        Assert.True(storage.IsReady);
        Assert.True(FileStorageExtensions.CanRequestFile(storage));
    }
}
