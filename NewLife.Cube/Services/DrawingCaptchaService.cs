using System.IO.Compression;
using NewLife.Caching;

namespace NewLife.Cube.Services;

/// <summary>纯托管 PNG 图片验证码服务，基于 BCL 实现，无第三方依赖，支持全平台</summary>
/// <remarks>
/// 使用内嵌 8×8 位图字体和手写 PNG/zlib 编码器生成带噪点与干扰线的算术题验证码图片。
/// 无需引用 SkiaSharp 等图形库，适用于所有 .NET 6+ 平台（Windows/Linux/macOS/Docker）。
/// 已作为默认验证码实现注册到 DI 容器，替代原 SVG 实现。
/// </remarks>
/// <param name="cacheProvider">缓存提供者（用于存储验证码答案）</param>
public class DrawingCaptchaService(ICacheProvider cacheProvider) : ICaptchaService
{
    #region 常量

    private const String CachePrefix = "Captcha:";
    private const Int32 TtlSeconds = 300;
    private const Int32 Width = 160;
    private const Int32 Height = 60;
    private const Int32 FontW = 8;  // 字符宽（像素）
    private const Int32 FontH = 8;  // 字符高（像素）
    private const Int32 Scale = 2;  // 字体放大倍数

    #endregion

    #region 内嵌位图字体

    // 8×8 像素字体：每字节代表一行，最高位 (bit7) 为最左像素。
    // 索引: 0-9 = '0'-'9'，10 = '+'，11 = '-'，12 = '='，13 = ' '
    private static readonly Byte[][] _font =
    [
        [0x3C, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x00], // 0
        [0x18, 0x38, 0x18, 0x18, 0x18, 0x18, 0x7E, 0x00], // 1
        [0x3C, 0x66, 0x06, 0x0C, 0x18, 0x30, 0x7E, 0x00], // 2
        [0x3C, 0x66, 0x06, 0x1E, 0x06, 0x66, 0x3C, 0x00], // 3
        [0x30, 0x38, 0x3C, 0x36, 0x7E, 0x30, 0x30, 0x00], // 4
        [0x7E, 0x60, 0x7C, 0x06, 0x06, 0x66, 0x3C, 0x00], // 5
        [0x3C, 0x60, 0x7C, 0x66, 0x66, 0x66, 0x3C, 0x00], // 6
        [0x7E, 0x06, 0x0C, 0x18, 0x30, 0x30, 0x30, 0x00], // 7
        [0x3C, 0x66, 0x66, 0x3C, 0x66, 0x66, 0x3C, 0x00], // 8
        [0x3C, 0x66, 0x66, 0x3E, 0x06, 0x0C, 0x38, 0x00], // 9
        [0x00, 0x18, 0x18, 0x7E, 0x18, 0x18, 0x00, 0x00], // +
        [0x00, 0x00, 0x00, 0x7E, 0x00, 0x00, 0x00, 0x00], // -
        [0x00, 0x00, 0x7E, 0x00, 0x7E, 0x00, 0x00, 0x00], // =
        [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00], // (space)
    ];

    #endregion

    #region 字段

    private readonly ICache _cache = cacheProvider.Cache;

    #endregion

    #region 方法

    /// <inheritdoc/>
    public CaptchaResult Generate()
    {
        var (expr, answer) = GenerateMathExpr();

        var captchaId = Guid.NewGuid().ToString("N");
        _cache.Set($"{CachePrefix}{captchaId}", answer.ToString(), TtlSeconds);

        return new CaptchaResult
        {
            CaptchaId = captchaId,
            Image = BuildPng(expr + " ="),
        };
    }

    /// <inheritdoc/>
    public Boolean Validate(String captchaId, String code)
    {
        if (captchaId.IsNullOrEmpty() || code.IsNullOrEmpty()) return false;

        var key = $"{CachePrefix}{captchaId}";
        var stored = _cache.Get<String>(key);
        if (stored.IsNullOrEmpty()) return false;

        // 校验后立即删除，防止重放攻击
        _cache.Remove(key);
        return String.Equals(stored.Trim(), code.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region 辅助

    private static (String Expr, Int32 Answer) GenerateMathExpr()
    {
        var a = Random.Shared.Next(1, 15);
        var b = Random.Shared.Next(1, 10);
        if (a >= b && Random.Shared.Next(2) == 0)
            return ($"{a} - {b}", a - b);
        return ($"{a} + {b}", a + b);
    }

    private static String BuildPng(String text)
    {
        var pixels = new Byte[Width * Height * 3]; // RGB 像素缓冲区
        FillBackground(pixels);
        DrawNoiseDots(pixels);
        DrawInterferenceLines(pixels);
        DrawText(pixels, text);

        using var ms = new MemoryStream(8192);
        WritePng(ms, pixels);
        return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
    }

    // ── 绘制函数 ─────────────────────────────────────────────────────────────────

    private static void FillBackground(Byte[] p)
    {
        var rnd = Random.Shared;
        for (var i = 0; i < p.Length; i += 3)
        {
            var v = (Byte)(228 + rnd.Next(18));
            p[i] = p[i + 1] = p[i + 2] = v;
        }
    }

    private static void DrawNoiseDots(Byte[] p)
    {
        var rnd = Random.Shared;
        for (var n = 70 + rnd.Next(50); n > 0; n--)
        {
            var off = (rnd.Next(Height) * Width + rnd.Next(Width)) * 3;
            var c = (Byte)(90 + rnd.Next(100));
            p[off] = p[off + 1] = p[off + 2] = c;
        }
    }

    private static void DrawInterferenceLines(Byte[] p)
    {
        var rnd = Random.Shared;
        for (var i = 0; i < 4 + rnd.Next(3); i++)
            DrawLine(p, rnd.Next(Width), rnd.Next(Height),
                        rnd.Next(Width), rnd.Next(Height),
                        (Byte)(80 + rnd.Next(120)),
                        (Byte)(80 + rnd.Next(120)),
                        (Byte)(80 + rnd.Next(120)));
    }

    // Bresenham 直线算法
    private static void DrawLine(Byte[] p, Int32 x0, Int32 y0, Int32 x1, Int32 y1, Byte r, Byte g, Byte b)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        while (true)
        {
            if ((UInt32)x0 < Width && (UInt32)y0 < Height)
            {
                var off = (y0 * Width + x0) * 3;
                p[off] = r; p[off + 1] = g; p[off + 2] = b;
            }
            if (x0 == x1 && y0 == y1) break;
            var e2 = err * 2;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private static void DrawText(Byte[] p, String text)
    {
        var rnd = Random.Shared;
        var charW = FontW * Scale;
        var charH = FontH * Scale;
        var startX = (Width - text.Length * charW) / 2;
        var baseY = (Height - charH) / 2;

        for (var ci = 0; ci < text.Length; ci++)
        {
            var glyph = GetGlyph(text[ci]);
            if (glyph == null) continue;

            var cx = startX + ci * charW;
            var cy = baseY + rnd.Next(-4, 5); // 垂直抖动增强防OCR
            var fr = (Byte)rnd.Next(20, 120);
            var fg = (Byte)rnd.Next(20, 120);
            var fb = (Byte)rnd.Next(20, 120);

            for (var row = 0; row < FontH; row++)
            {
                var mask = glyph[row];
                if (mask == 0) continue;
                for (var col = 0; col < FontW; col++)
                {
                    if ((mask & (0x80 >> col)) == 0) continue;
                    for (var sy = 0; sy < Scale; sy++)
                    {
                        for (var sx = 0; sx < Scale; sx++)
                        {
                            var px = cx + col * Scale + sx;
                            var py = cy + row * Scale + sy;
                            if ((UInt32)px < Width && (UInt32)py < Height)
                            {
                                var off = (py * Width + px) * 3;
                                p[off] = fr; p[off + 1] = fg; p[off + 2] = fb;
                            }
                        }
                    }
                }
            }
        }
    }

    private static Byte[]? GetGlyph(Char ch) => ch switch
    {
        >= '0' and <= '9' => _font[ch - '0'],
        '+' => _font[10],
        '-' => _font[11],
        '=' => _font[12],
        ' ' => _font[13],
        _ => null,
    };

    #endregion

    #region PNG 编码（纯 BCL，无第三方依赖）

    // CRC32 查找表（IEEE 多项式 0xEDB88320）
    private static readonly UInt32[] _crcTable = BuildCrcTable();

    private static UInt32[] BuildCrcTable()
    {
        var t = new UInt32[256];
        for (UInt32 n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
                c = (c & 1u) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }

    private static UInt32 UpdateCrc(UInt32 crc, ReadOnlySpan<Byte> data)
    {
        foreach (var b in data)
            crc = _crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static void WritePng(Stream s, Byte[] pixels)
    {
        // PNG 签名
        s.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR：宽×高×位深=8×颜色=2(RGB)
        Span<Byte> ihdr = stackalloc Byte[13];
        WriteInt32BE(ihdr, 0, Width);
        WriteInt32BE(ihdr, 4, Height);
        ihdr[8] = 8; ihdr[9] = 2;
        WriteChunk(s, "IHDR"u8, ihdr);

        // 构建扫描行：每行前置 1 字节滤波器 0（None）
        var rowStride = 1 + Width * 3;
        var scanlines = new Byte[Height * rowStride];
        for (var y = 0; y < Height; y++)
            pixels.AsSpan(y * Width * 3, Width * 3)
                  .CopyTo(scanlines.AsSpan(y * rowStride + 1));

        // IDAT：ZLibStream (.NET 6+ BCL 内置) 生成符合 PNG 标准的 zlib 压缩数据
        using var idatMs = new MemoryStream();
        using (var zlib = new ZLibStream(idatMs, CompressionLevel.Fastest, leaveOpen: true))
            zlib.Write(scanlines);
        WriteChunk(s, "IDAT"u8, idatMs.ToArray().AsSpan());

        // IEND
        WriteChunk(s, "IEND"u8, ReadOnlySpan<Byte>.Empty);
    }

    private static void WriteChunk(Stream s, ReadOnlySpan<Byte> type, ReadOnlySpan<Byte> data)
    {
        Span<Byte> len = stackalloc Byte[4];
        WriteInt32BE(len, 0, data.Length);
        s.Write(len);
        s.Write(type);
        s.Write(data);

        var crc = UpdateCrc(0xFFFFFFFF, type);
        crc = UpdateCrc(crc, data) ^ 0xFFFFFFFF;
        Span<Byte> crcBuf = stackalloc Byte[4];
        WriteInt32BE(crcBuf, 0, (Int32)crc);
        s.Write(crcBuf);
    }

    private static void WriteInt32BE(Span<Byte> buf, Int32 offset, Int32 value)
    {
        buf[offset] = (Byte)(value >> 24);
        buf[offset + 1] = (Byte)(value >> 16);
        buf[offset + 2] = (Byte)(value >> 8);
        buf[offset + 3] = (Byte)value;
    }

    #endregion
}
