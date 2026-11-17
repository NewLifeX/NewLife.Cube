using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.Web;
using XCode.DataAccessLayer;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// 覆盖 <see cref="Attachment.SaveFile"/> 在本地磁盘与对象存储两种模式下的落盘与元数据记录，
/// 验证存储类型写入 <see cref="Attachment.Storage"/> 字段。
/// 注意：Attachment.Meta 为进程级静态缓存，绑定首个访问的Cube连接，因此本类测试共用同一个SQLite库与上传目录。
/// </summary>
public class AttachmentSaveFileTests : IDisposable
{
    /// <summary>本地缺失附件先从分布式存储请求</summary>
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task AttachmentDownload_MissingLocalFile_RequestsPeer(Boolean image, Boolean fetch)
    {
        AttachmentProvider.Provider = new AttachmentProvider();
        var att = new Attachment { Category = "WorkOrder", Title = "peer", UploadTime = DateTime.Now };
        var data = Encoding.UTF8.GetBytes("peer file content");
        using var stream = new MemoryStream(data);
        Assert.True(await att.SaveFile(stream, null, "peer.png"));
        var path = att.GetFilePath();
        File.Delete(path);
        var storage = System.Reflection.DispatchProxy.Create<Stardust.Storages.IFileStorage, PeerStorageProxy>();
        var proxy = (PeerStorageProxy)(Object)storage;
        proxy.Receive = () => File.WriteAllBytes(path, data);
        var set = CubeSetting.Current;
        var old = (set.ValidateAttachment, set.FileStorageFetch, set.FileStorageFetchTimeout);
        set.ValidateAttachment = false;
        set.FileStorageFetch = fetch;
        set.FileStorageFetchTimeout = 0;
        try
        {
            var controller = new NewLife.Cube.Controllers.CubeController(null!, null!, Array.Empty<Microsoft.AspNetCore.Routing.EndpointDataSource>(), storage)
            {
                ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() },
            };
            var result = image ? await controller.Image(att.Id + ".png") : await controller.File(att.Id + ".png");
            Assert.Equal(fetch ? 1 : 0, proxy.Requests);
            if (fetch)
            {
                var file = Assert.IsType<Microsoft.AspNetCore.Mvc.PhysicalFileResult>(result);
                Assert.Equal(data, File.ReadAllBytes(file.FileName));
                Assert.Equal(att.Id, proxy.AttachmentId);
                Assert.Equal(att.FilePath, proxy.RequestPath);
            }
            else Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundObjectResult>(result);
        }
        finally
        {
            (set.ValidateAttachment, set.FileStorageFetch, set.FileStorageFetchTimeout) = old;
            att.Delete();
        }
    }

    /// <summary>模拟其他节点按请求补齐文件</summary>
    public class PeerStorageProxy : System.Reflection.DispatchProxy
    {
        internal Action Receive = null!;
        internal Int32 Requests;
        internal Int64 AttachmentId;
        internal String? RequestPath;

        /// <summary>处理存储调用</summary>
        /// <param name="targetMethod">目标方法</param>
        /// <param name="args">调用参数</param>
        /// <returns>异步调用结果</returns>
        protected override Object? Invoke(System.Reflection.MethodInfo? targetMethod, Object?[]? args)
        {
            if (targetMethod?.Name != "RequestFileAsync") throw new InvalidOperationException(targetMethod?.Name);
            Requests++;
            AttachmentId = (Int64)args![0]!;
            RequestPath = (String?)args[1];
            Receive();
            return Task.CompletedTask;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AttachmentDownload_ReturnsHashHeaders(Boolean image)
    {
        AttachmentProvider.Provider = new AttachmentProvider();
        var att = new Attachment { Category = "WorkOrder", Title = "hash", UploadTime = DateTime.Now };
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("file race hash regression"));
        Assert.True(await att.SaveFile(stream, null, "race.png"));
        var oldValidate = CubeSetting.Current.ValidateAttachment;
        CubeSetting.Current.ValidateAttachment = false;
        try
        {
            var http = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            var controller = new NewLife.Cube.Controllers.CubeController(null!, null!, Array.Empty<Microsoft.AspNetCore.Routing.EndpointDataSource>())
            {
                ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = http },
            };
            var result = image ? await controller.Image(att.Id + ".png") : await controller.File(att.Id + ".png");
            Assert.IsType<Microsoft.AspNetCore.Mvc.PhysicalFileResult>(result);
            Assert.Equal("MD5:" + att.Hash, http.Response.Headers["X-File-Hash"].ToString());
            Assert.Equal(att.Hash, http.Response.Headers["X-Content-MD5"].ToString());
        }
        finally
        {
            CubeSetting.Current.ValidateAttachment = oldValidate;
            att.Delete();
        }
    }

    // 静态共享：Attachment.Meta 静态缓存绑定首次访问的连接，所有测试方法必须共用同一SQLite库
    private static readonly String _dbFile = Path.Combine(Path.GetTempPath(), $"CubeAttDb_{Guid.NewGuid():N}.db");
    private static readonly String _uploadDir = Path.Combine(Path.GetTempPath(), $"CubeAttUp_{Guid.NewGuid():N}");

    private readonly String _oldUploadPath;
    private readonly AttachmentProvider _oldProvider;
    private String? _oldCube;

    public AttachmentSaveFileTests()
    {
        _oldUploadPath = CubeSetting.Current.UploadPath;
        _oldProvider = AttachmentProvider.Provider;

        // 上传目录指向临时目录
        CubeSetting.Current.UploadPath = _uploadDir;

        // 首次初始化时配置SQLite并建表
        if (_oldCube == null)
        {
            String? oldCube = null;
            var hadCube = DAL.ConnStrs != null && DAL.ConnStrs.TryGetValue("Cube", out oldCube);
            if (hadCube) _oldCube = oldCube!;
            DAL.AddConnStr("Cube", $"Data Source={_dbFile}", null, "SQLite");
            DAL.CreateTable();
        }
    }

    public void Dispose()
    {
        // 恢复附件存储提供者与上传目录
        AttachmentProvider.Provider = _oldProvider;
        CubeSetting.Current.UploadPath = _oldUploadPath;
    }

    [Fact]
    [System.ComponentModel.DisplayName("SaveFile_本地与对象存储_写文件并记录存储类型")]
    public async Task SaveFile_LocalAndObject_RecordsStorage()
    {
        // ===== 本地磁盘 =====
        AttachmentProvider.Provider = new AttachmentProvider();

        var att = new Attachment { Category = "Order", Title = "测试", UploadTime = DateTime.Now };
        var data = Encoding.UTF8.GetBytes("cube local file");
        using (var stream = new MemoryStream(data))
        {
            Assert.True(await att.SaveFile(stream, null, "order.txt"));
        }

        Assert.Equal("Local", att.Storage);
        Assert.True(att.IsLocalStorage());
        Assert.Equal("本地", att.StorageName);

        // 文件已落盘
        var filePath = att.GetFilePath();
        Assert.NotNull(filePath);
        Assert.True(File.Exists(filePath!));
        Assert.Equal(data.Length, att.Size);
        Assert.Equal(32, att.Hash!.Length); // MD5 32位十六进制

        // 数据库有记录，Storage已持久化
        var dbAtt = Attachment.FindById(att.Id);
        Assert.NotNull(dbAtt);
        Assert.Equal("Local", dbAtt!.Storage);

        // 删除记录时同步删除文件
        dbAtt!.Delete();
        Assert.False(File.Exists(filePath!));

        // ===== 对象存储 =====
        var inner = new MemoryObjectStorage();
        AttachmentProvider.Provider = new AttachmentProvider
        {
            Storage = new ObjectAttachmentStorage { Storage = inner, Name = "Oss" },
        };

        var att2 = new Attachment { Category = "Order", Title = "测试", UploadTime = DateTime.Now };
        var data2 = Encoding.UTF8.GetBytes("cube cloud file");
        using (var stream = new MemoryStream(data2))
        {
            Assert.True(await att2.SaveFile(stream, null, "order.txt"));
        }

        Assert.Equal("Oss", att2.Storage);
        Assert.False(att2.IsLocalStorage());
        Assert.Equal("阿里云OSS", att2.StorageName);

        // 对象存储中有文件
        Assert.True(await inner.ExistsAsync(att2.FilePath));
        Assert.Equal(data2.Length, att2.Size);
        Assert.Equal(32, att2.Hash!.Length);

        // 云Url可获取
        Assert.StartsWith("https://cdn.example.com/", att2.GetUrl());

        // 删除记录时同步删除云对象
        var dbAtt2 = Attachment.FindById(att2.Id);
        Assert.NotNull(dbAtt2);
        Assert.Equal("Oss", dbAtt2!.Storage);
        dbAtt2!.Delete();
        Assert.False(await inner.ExistsAsync(att2.FilePath));
    }
}

