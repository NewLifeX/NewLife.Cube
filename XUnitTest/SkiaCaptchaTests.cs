using System;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Caching;
using NewLife.Cube.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace XUnitTest;

/// <summary>SkiaCaptchaService 图片验证码服务单元测试</summary>
public class SkiaCaptchaTests
{
    #region 辅助

    /// <summary>创建使用内存缓存的 SkiaCaptchaService 实例</summary>
    private static DrawingCaptchaService CreateService() => new DrawingCaptchaService(new MemoryCacheProvider());

    /// <summary>创建服务与缓存提供者（用于需要直接读取答案的确定性测试）</summary>
    private static (DrawingCaptchaService Service, MemoryCacheProvider Provider) CreateServiceWithProvider()
    {
        var provider = new MemoryCacheProvider();
        return (new DrawingCaptchaService(provider), provider);
    }

    /// <summary>简单内存缓存提供者，用于单元测试隔离</summary>
    private class MemoryCacheProvider : ICacheProvider
    {
        public ICache Cache { get; set; }
        public ICache InnerCache { get; set; }

        public MemoryCacheProvider()
        {
            var cache = new MemoryCache();
            Cache = cache;
            InnerCache = cache;
        }

        public IProducerConsumer<T> GetQueue<T>(String name, String? topic = null)
            => throw new NotImplementedException();

        public IProducerConsumer<T> GetInnerQueue<T>(String name)
            => throw new NotImplementedException();

        public IDisposable AcquireLock(String name, Int32 msTimeout)
            => throw new NotImplementedException();
    }

    #endregion

    #region Generate 测试

    [Fact(DisplayName = "Generate 返回非空 CaptchaId 和 Image")]
    public void Generate_Returns_ValidResult()
    {
        var svc = CreateService();
        var result = svc.Generate();

        Assert.NotNull(result);
        Assert.False(result.CaptchaId.IsNullOrEmpty(), "CaptchaId 不应为空");
        Assert.False(result.Image.IsNullOrEmpty(), "Image 不应为空");
    }

    [Fact(DisplayName = "Generate 返回的 Image 是合法的 PNG DataURI")]
    public void Generate_Image_IsPngDataUri()
    {
        var svc = CreateService();
        var result = svc.Generate();

        Assert.StartsWith("data:image/png;base64,", result.Image);

        // Base64 部分可正常解码为非空字节
        var base64 = result.Image["data:image/png;base64,".Length..];
        var bytes = Convert.FromBase64String(base64);
        Assert.True(bytes.Length > 0, "PNG 图片数据不应为空");
    }

    [Fact(DisplayName = "Generate 返回的 Image 具有 PNG 文件头 (89 50 4E 47)")]
    public void Generate_Image_HasPngSignature()
    {
        var svc = CreateService();
        var result = svc.Generate();

        var base64 = result.Image["data:image/png;base64,".Length..];
        var bytes = Convert.FromBase64String(base64);

        // PNG magic bytes: 0x89 0x50 0x4E 0x47
        Assert.True(bytes.Length >= 4);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x4E, bytes[2]); // 'N'
        Assert.Equal(0x47, bytes[3]); // 'G'
    }

    [Fact(DisplayName = "Generate 每次调用返回不同的 CaptchaId")]
    public void Generate_Each_Call_Returns_DifferentId()
    {
        var svc = CreateService();
        var id1 = svc.Generate().CaptchaId;
        var id2 = svc.Generate().CaptchaId;

        Assert.NotEqual(id1, id2);
    }

    [Fact(DisplayName = "Generate 生成的 Image 大小合理（>1KB，<50KB）")]
    public void Generate_Image_SizeIsReasonable()
    {
        var svc = CreateService();
        var result = svc.Generate();

        var base64 = result.Image["data:image/png;base64,".Length..];
        var bytes = Convert.FromBase64String(base64);

        Assert.True(bytes.Length > 1024, $"图片太小：{bytes.Length} 字节");
        Assert.True(bytes.Length < 50 * 1024, $"图片太大：{bytes.Length} 字节");
    }

    [Fact(DisplayName = "Generate 保存 10 张样本图片到 ./captcha/ 目录供人工确认")]
    public void Generate_SaveSamplesToLocal()
    {
        var (svc, provider) = CreateServiceWithProvider();
        var dir = "./captcha/".GetFullPath();
        Directory.CreateDirectory(dir);

        for (var i = 1; i <= 10; i++)
        {
            var result = svc.Generate();
            var answer = provider.Cache.Get<String>($"Captcha:{result.CaptchaId}");
            var base64 = result.Image["data:image/png;base64,".Length..];
            var bytes = Convert.FromBase64String(base64);

            // 文件名含序号与答案，方便人工核对（例: 01_answer=7.png）
            var fileName = Path.Combine(dir, $"{i:D2}_answer={answer}.png");
            File.WriteAllBytes(fileName, bytes);

            Assert.True(File.Exists(fileName), $"文件未生成: {fileName}");
        }

        // 验证目录下至少有 10 个文件
        var count = Directory.GetFiles(dir, "*.png").Length;
        Assert.True(count >= 10, $"样本图片数量不足：{count}");
    }

    #endregion

    #region Validate 测试

    [Fact(DisplayName = "Validate 使用正确答案返回 true")]
    public void Validate_CorrectAnswer_ReturnsTrue()
    {
        var (svc, provider) = CreateServiceWithProvider();
        var result = svc.Generate();

        // 直接从缓存读取答案（确定性测试，不依赖概率）
        var answer = provider.Cache.Get<String>($"Captcha:{result.CaptchaId}");
        Assert.NotNull(answer);

        var ok = svc.Validate(result.CaptchaId, answer);
        Assert.True(ok, "正确答案应验证成功");
    }

    [Fact(DisplayName = "Validate 使用错误答案返回 false")]
    public void Validate_WrongAnswer_ReturnsFalse()
    {
        var svc = CreateService();
        var result = svc.Generate();

        // 答案范围 1-24，用 99 必然错误
        var ok = svc.Validate(result.CaptchaId, "99");
        Assert.False(ok);
    }

    [Fact(DisplayName = "Validate 使用空 captchaId 返回 false")]
    public void Validate_EmptyCaptchaId_ReturnsFalse()
    {
        var svc = CreateService();
        Assert.False(svc.Validate("", "5"));
        Assert.False(svc.Validate(null!, "5"));
    }

    [Fact(DisplayName = "Validate 使用空 code 返回 false")]
    public void Validate_EmptyCode_ReturnsFalse()
    {
        var svc = CreateService();
        var result = svc.Generate();
        Assert.False(svc.Validate(result.CaptchaId, ""));
        Assert.False(svc.Validate(result.CaptchaId, null!));
    }

    [Fact(DisplayName = "Validate 校验后再次使用同一 captchaId 返回 false（防重放）")]
    public void Validate_AfterUse_CannotReuseId()
    {
        var svc = CreateService();
        var result = svc.Generate();

        // 第一次用错误答案（会触发缓存删除）
        svc.Validate(result.CaptchaId, "99");

        // 第二次用任意答案，captchaId 已失效
        var ok = svc.Validate(result.CaptchaId, "5");
        Assert.False(ok, "captchaId 已消费，不应再次通过");
    }

    [Fact(DisplayName = "Validate 不存在的 captchaId 返回 false")]
    public void Validate_NonExistentId_ReturnsFalse()
    {
        var svc = CreateService();
        var ok = svc.Validate("nonexistent_id_that_does_not_exist", "5");
        Assert.False(ok);
    }

    [Fact(DisplayName = "Validate 忽略两端空白字符")]
    public void Validate_TrimsWhitespace()
    {
        var (svc, provider) = CreateServiceWithProvider();
        var result = svc.Generate();

        // 直接从缓存读取答案（确定性测试），再用带空白的答案验证
        var answer = provider.Cache.Get<String>($"Captcha:{result.CaptchaId}");
        Assert.NotNull(answer);

        var ok = svc.Validate(result.CaptchaId, $" {answer} ");
        Assert.True(ok, "应忽略两端空白");
    }

    #endregion

    #region DrawingCaptchaService 默认注册测试

    [Fact(DisplayName = "DrawingCaptchaService 可作为 ICaptchaService 默认注册")]
    public void DrawingCaptchaService_CanBeRegistered_AsICaptchaService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICacheProvider>(new MemoryCacheProvider());
        services.AddSingleton<ICaptchaService, DrawingCaptchaService>();

        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(ICaptchaService));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(DrawingCaptchaService), descriptor.ImplementationType);
    }

    #endregion
}
