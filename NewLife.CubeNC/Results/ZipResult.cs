using System.IO.Compression;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using XCode;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Results;

/// <summary>Zip动作结果</summary>
public class ZipResult : IActionResult
{
    /// <summary>数据集</summary>
    public IDictionary<Type, IEnumerable<IEntity>> Data { get; set; }

    /// <summary>内容类型</summary>
    public String ContentType { get; set; } = "application/zip";

    /// <summary>附件文件名。若指定，则作为文件下载输出</summary>
    public String AttachmentName { get; set; }

    /// <summary>Http上下文</summary>
    public HttpContext HttpContext { get; set; }

    /// <summary>执行并输出结果</summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var rs = context.HttpContext.Response;
        rs.Headers[HeaderNames.ContentEncoding] = "UTF8";

        if (!AttachmentName.IsNullOrEmpty())
        {
            if (!ContentType.IsNullOrEmpty())
                rs.Headers[HeaderNames.ContentType] = ContentType;
            rs.Headers[HeaderNames.ContentDisposition] = "attachment; filename=" + HttpUtility.UrlEncode(AttachmentName);
        }

        // 允许同步IO，便于刷数据Flush
        var ft = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature>();
        if (ft != null) ft.AllowSynchronousIO = true;

        using var zipArchive = new ZipArchive(rs.Body, ZipArchiveMode.Create, leaveOpen: true);

        foreach (var item in Data)
        {
            var type = item.Key;
            // 导出数据
            {
                var entry = zipArchive.CreateEntry(type.FullName + ".db");
                using var stream = entry.Open();

                item.Value.Write(stream);
            }
            // 导出结构
            {
                var factory = type.AsFactory();
                if (factory != null)
                {
                    var xml = DAL.Export([factory.Table.DataTable]);
                    var buf = xml.GetBytes();

                    var entry = zipArchive.CreateEntry(type.FullName + ".xml");
                    using var stream = entry.Open();
                    stream.Write(buf, 0, buf.Length);
                }
            }
        }

        // 注意：ZipArchive 在 Dispose 时才会写入中央目录（Central Directory）
        // 但因为我们设置了 leaveOpen: true，所以不会关闭 response.Body
        // 所以我们需要手动 Flush，确保数据写入
        await rs.Body.FlushAsync();
    }
}
