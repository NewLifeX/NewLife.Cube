using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.IO;

namespace NewLife.Cube.Results;

/// <summary>Csv动作结果</summary>
public class CsvActionResult : IActionResult
{
    /// <summary>字段列表</summary>
    public IList<DataField> Fields { get; set; }

    /// <summary>数据集</summary>
    public IEnumerable<IModel> Data { get; set; }

    /// <summary>内容类型</summary>
    public String ContentType { get; set; } = "application/vnd.ms-excel";

    /// <summary>执行并输出结果</summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var rs = context.HttpContext.Response;
        rs.Headers[HeaderNames.ContentEncoding] = "UTF8";

        if (!ContentType.IsNullOrEmpty())
            rs.Headers[HeaderNames.ContentType] = ContentType;

        await using var csv = new CsvFile(rs.Body, true);

        // 列头
        var headers = Fields.Select(e => e.Name).ToArray();
        if (headers[0] == "ID") headers[0] = "Id";
        await csv.WriteLineAsync(headers);

        // 内容
        foreach (var entity in Data)
        {
            await csv.WriteLineAsync(Fields.Select(e => entity[e.Name]));
        }
    }
}
