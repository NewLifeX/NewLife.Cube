using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.IO;

namespace NewLife.Cube.Results;

/// <summary>Csv动作结果</summary>
public class CsvResult : IActionResult
{
    /// <summary>字段列表</summary>
    public IList<DataField> Fields { get; set; }

    /// <summary>数据集</summary>
    public IEnumerable<IModel> Data { get; set; }

    /// <summary>内容类型</summary>
    public String ContentType { get; set; } = "application/vnd.ms-excel";

    /// <summary>附件文件名。若指定，则作为文件下载输出</summary>
    public String AttachmentName { get; set; }

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

        await using var csv = new CsvFile(rs.Body, true);

        // 列头
        var headers = Fields.Select(e => e.Name).ToArray();
        if (headers[0] == "ID") headers[0] = "Id";
        await csv.WriteLineAsync(headers);

        // 内容
        foreach (var entity in Data)
        {
            // 导出枚举类型时，使用数字而不是字符串
            await csv.WriteLineAsync(Fields.Select(e => e.Type.IsEnum ? (Int32)entity[e.Name] : entity[e.Name]));
        }
    }
}
