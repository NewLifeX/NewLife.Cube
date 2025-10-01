using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.IO;

namespace NewLife.Cube.Results;

/// <summary>Excel动作结果</summary>
public class ExcelResult : IActionResult
{
    /// <summary>字段列表</summary>
    public IList<DataField> Fields { get; set; }

    /// <summary>数据集</summary>
    public IEnumerable<IModel> Data { get; set; }

    /// <summary>内容类型</summary>
    public String ContentType { get; set; } = "application/vnd.ms-excel";

    /// <summary>附件文件名。若指定，则作为文件下载输出</summary>
    public String AttachmentName { get; set; }

    /// <summary>Http上下文</summary>
    public HttpContext HttpContext { get; set; }

    /// <summary>执行并输出结果</summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task ExecuteResultAsync(ActionContext context)
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

        //await using var csv = new CsvFile(rs.Body, true);
        using var excel = new ExcelWriter(rs.Body);

        // 列头
        var headers = new List<String>();
        foreach (var fi in Fields)
        {
            var name = fi.DisplayName;
            if (name.IsNullOrEmpty()) name = fi.Description;
            if (name.IsNullOrEmpty()) name = fi.Name;

            // 第一行以ID开头的csv文件，容易被识别为SYLK文件
            if (name == "ID" && fi == Fields[0]) name = "Id";
            headers.Add(name);
        }
        //await csv.WriteLineAsync(headers);
        excel.WriteHeader(null, headers);

        // 内容
        excel.WriteRows(null, Data.Select(e => Fields.Select(f => e[f.Name]).ToArray()));

        excel.Save();

        return Task.CompletedTask;
    }
}
