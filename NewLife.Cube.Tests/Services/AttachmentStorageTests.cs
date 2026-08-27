using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Cube.Services;
using NewLife.Cube.Web;
using NewLife.Data;
using NewLife.IO;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>测试用内存对象存储，实现 <see cref="IObjectStorage"/></summary>
public class MemoryObjectStorage : IObjectStorage
{
    public Dictionary<String, Byte[]> Items { get; } = new Dictionary<String, Byte[]>();

    public String? Server { get; set; }
    public String? AppId { get; set; }
    public String? Secret { get; set; }
    public Boolean CanGetUrl => true;
    public Boolean CanDelete => true;
    public Boolean CanSearch => false;
    public Boolean CanCopy => false;

    public Task<IObjectInfo?> GetAsync(String id)
        => Task.FromResult<IObjectInfo?>(Items.TryGetValue(id, out var v) ? new ObjectInfo { Name = id, Data = new ArrayPacket(v), Length = v.Length } : null);

    public Task<String?> GetUrlAsync(String id) => Task.FromResult<String?>("https://cdn.example.com/" + id);

    public Task<Boolean> ExistsAsync(String id) => Task.FromResult(Items.ContainsKey(id));

    public async Task<IObjectInfo?> PutAsync(String id, IPacket data)
    {
        using var ms = new MemoryStream();
        data.GetStream()!.CopyTo(ms);
        Items[id] = ms.ToArray();
        return new ObjectInfo { Name = id, Data = data, Length = data.Length };
    }

    public Task<Int32> DeleteAsync(String id) => Task.FromResult(Items.Remove(id) ? 1 : 0);

    public async Task<Int32> DeleteAsync(String[] ids)
    {
        var count = 0;
        foreach (var id in ids)
            count += await DeleteAsync(id);
        return count;
    }

    public Task<IObjectInfo?> CopyAsync(String sourceId, String destId) => throw new NotSupportedException();

    public Task<IList<IObjectInfo>?> SearchAsync(String? pattern = null, Int32 start = 0, Int32 count = 100) => throw new NotSupportedException();

    // 兼容旧版
    public Task<IObjectInfo?> Get(String id) => GetAsync(id);
    public Task<String?> GetUrl(String id) => GetUrlAsync(id);
    public Task<IObjectInfo?> Put(String id, IPacket data) => PutAsync(id, data);
    public Task<Int32> Delete(String id) => DeleteAsync(id);
    public Task<IList<IObjectInfo>?> Search(String? pattern = null, Int32 start = 0, Int32 count = 100) => throw new NotSupportedException();
}

/// <summary>
/// 覆盖 <see cref="LocalAttachmentStorage"/> 与 <see cref="ObjectAttachmentStorage"/> 的读写删查，
/// 以及 <see cref="AttachmentProvider"/> 门面的默认与切换行为。
/// </summary>
public class AttachmentStorageTests : IDisposable
{
    private readonly String _dir = Path.Combine(Path.GetTempPath(), $"CubeAtt_{Guid.NewGuid():N}");

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }
        catch
        {
            // 忽略清理失败
        }
    }

    [Fact]
    [System.ComponentModel.DisplayName("LocalAttachmentStorage_写读存在删除")]
    public async Task Local_WriteReadExistsDelete()
    {
        var storage = new LocalAttachmentStorage { UploadPath = _dir };
        var filePath = "Test/20260101/1.txt";

        await storage.WriteAsync(new MemoryStream("hello"u8.ToArray()), filePath);

        Assert.True(await storage.ExistsAsync(filePath));
        Assert.Null(storage.GetUrl(filePath));

        await using (var fs = await storage.ReadAsync(filePath))
        {
            Assert.NotNull(fs);
            using var reader = new StreamReader(fs!);
            Assert.Equal("hello", await reader.ReadToEndAsync());
        }

        // 物理文件存在
        var full = Path.Combine(_dir, filePath);
        Assert.True(File.Exists(full));

        await storage.DeleteAsync(filePath);
        Assert.False(File.Exists(full));
        Assert.False(await storage.ExistsAsync(filePath));
    }

    [Fact]
    [System.ComponentModel.DisplayName("LocalAttachmentStorage_缺失文件读取返回null")]
    public async Task Local_MissingFile_ReturnsNull()
    {
        var storage = new LocalAttachmentStorage { UploadPath = _dir };

        var fs = await storage.ReadAsync("Nope/1.txt");

        Assert.Null(fs);
    }

    [Fact]
    [System.ComponentModel.DisplayName("ObjectAttachmentStorage_委托对象存储读写")]
    public async Task Object_DelegatesToStorage()
    {
        var inner = new MemoryObjectStorage();
        var storage = new ObjectAttachmentStorage { Storage = inner, Name = "Oss" };

        Assert.False(storage.Local);
        Assert.Equal("Oss", storage.Name);

        var filePath = "Order/20260101/1.txt";
        await storage.WriteAsync(new MemoryStream("data"u8.ToArray()), filePath);

        Assert.True(await storage.ExistsAsync(filePath));
        Assert.Equal("https://cdn.example.com/" + filePath, storage.GetUrl(filePath));

        await using var fs = await storage.ReadAsync(filePath);
        Assert.NotNull(fs);
        Assert.Equal("data", new StreamReader(fs!).ReadToEnd());

        await storage.DeleteAsync(filePath);
        Assert.False(await storage.ExistsAsync(filePath));
    }

    [Fact]
    [System.ComponentModel.DisplayName("AttachmentProvider_默认本地存储")]
    public void Provider_DefaultIsLocal()
    {
        var provider = new AttachmentProvider();

        Assert.True(provider.Storage.Local);
        Assert.Equal("Local", provider.Name);
        Assert.Null(provider.GetUrl("x/1.txt"));
    }

    [Fact]
    [System.ComponentModel.DisplayName("AttachmentProvider_切换为对象存储")]
    public void Provider_SwitchToObjectStorage()
    {
        var provider = new AttachmentProvider
        {
            Storage = new ObjectAttachmentStorage { Storage = new MemoryObjectStorage(), Name = "Qiniu" },
        };

        Assert.False(provider.Storage.Local);
        Assert.Equal("Qiniu", provider.Name);
    }
}
