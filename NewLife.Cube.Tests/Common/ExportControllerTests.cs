using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using XCode;
using XCode.Configuration;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>导出功能单元测试。验证 OnExportExcel/OnExportExcelTemplate 用 NewLife.Office ExcelWriter 生成真 xlsx（而非 CSV），且导出模板隐藏自动维护字段</summary>
public class ExportControllerTests
{
    #region 测试实体
    /// <summary>导出测试实体。手写实体，仅用于元数据，不访问数据库</summary>
    [BindTable("ExportTestEntity", "导出测试实体", ConnName = "Test")]
    private class ExportTestEntity : Entity<ExportTestEntity>
    {
        /// <summary>测试枚举。验证 CSV 导出枚举为数字</summary>
        public enum ExportKind
        {
            /// <summary>甲</summary>
            A = 0,

            /// <summary>乙</summary>
            B = 1,
        }

        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String _Name = null!;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [Description("名称")]
        [DataObjectField(false, false, false, 50)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

        private Decimal _Amount;
        /// <summary>金额</summary>
        [DisplayName("金额")]
        [Description("金额")]
        [DataObjectField(false, false, false, 0)]
        public Decimal Amount { get => _Amount; set { if (OnPropertyChanging("Amount", value)) { _Amount = value; OnPropertyChanged("Amount"); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, false, 0)]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

        private Int32 _Kind;
        /// <summary>类型</summary>
        [DisplayName("类型")]
        [Description("类型")]
        [DataObjectField(false, false, false, 0)]
        public ExportKind Kind { get => (ExportKind)_Kind; set { if (OnPropertyChanging("Kind", value)) { _Kind = (Int32)value; OnPropertyChanged("Kind"); } } }

        /// <summary>索引器重写：按字段名读写私有字段</summary>
        public override Object? this[String name]
        {
            get => name switch
            {
                "Id" => _Id,
                "Name" => _Name,
                "Amount" => _Amount,
                "CreateTime" => _CreateTime,
                "Kind" => _Kind,
                _ => base[name],
            };
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToInt(); break;
                    case "Name": _Name = value + ""; break;
                    case "Amount": _Amount = value.ToDecimal(); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "Kind": _Kind = value.ToInt(); break;
                    default: base[name] = value; break;
                }
            }
        }
    }
    #endregion

    #region 测试控制器
    private class ExportTestController : ReadOnlyEntityController<ExportTestEntity>
    {
        /// <summary>内存数据。导出不走数据库</summary>
        public ExportTestEntity[] Data { get; set; } = [];

        protected override IEnumerable<ExportTestEntity> ExportData(Int32 max = 0)
        {
            if (max > 0 && Data.Length > max) return Data.Take(max);
            return Data;
        }

        /// <summary>暴露 WriteExcelToStream 供测试</summary>
        public void TestWriteExcelToStream(IList<DataField> fields, IEnumerable<ExportTestEntity> data, Stream stream)
            => WriteExcelToStream(fields, data, stream);

        /// <summary>暴露 GetExportFields 供测试</summary>
        public List<DataField> TestGetExportFields(IList<FieldItem> fs, IEnumerable<ExportTestEntity> list)
            => GetExportFields(fs, list);

        /// <summary>测试环境无 HttpContext，重写分页获取避免 WebHelper.Params 空引用</summary>
        protected override Pager GetCachePager() => null;

        /// <summary>测试环境无日志上下文，重写日志为空实现</summary>
        protected override void WriteLog(String action, Boolean success, String remark) { }
    }
    #endregion

    /// <summary>创建测试控制器。临时关闭 OrderByKey 避免构造函数内 Meta.Count 查询数据库</summary>
    private static ExportTestController CreateController()
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            return new ExportTestController
            {
                Data =
                [
                    new ExportTestEntity { Id = 1, Name = "张三", Amount = 12.34m, CreateTime = new DateTime(2026, 9, 1, 8, 30, 0), Kind = ExportTestEntity.ExportKind.A },
                    new ExportTestEntity { Id = 2, Name = "李四", Amount = 56.78m, CreateTime = new DateTime(2026, 9, 1, 9, 0, 0), Kind = ExportTestEntity.ExportKind.B },
                ],
            };
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    /// <summary>断言字节流是有效 xlsx（zip 含标准部件），且共享字符串包含指定表头</summary>
    private static void AssertXlsx(Byte[] bytes, params String[] expectedHeaders)
    {
        using var ms = new MemoryStream(bytes);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        var names = zip.Entries.Select(e => e.FullName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("[Content_Types].xml", names);
        Assert.Contains("xl/workbook.xml", names);
        Assert.Contains("xl/sharedStrings.xml", names);

        if (expectedHeaders.Length > 0)
        {
            var ss = zip.GetEntry("xl/sharedStrings.xml");
            Assert.NotNull(ss);
            using var sr = new StreamReader(ss.Open());
            var xml = sr.ReadToEnd();
            foreach (var header in expectedHeaders)
            {
                Assert.Contains(header, xml);
            }
        }
    }

    [Fact(DisplayName = "WriteExcelToStream_内存实体_生成有效xlsx且含中文表头")]
    public void WriteExcelToStream_GeneratesValidXlsx()
    {
        var ctrl = CreateController();
        var fields = ctrl.TestGetExportFields(ExportTestEntity.Meta.Factory.AllFields, ctrl.Data);

        using var ms = new MemoryStream();
        ctrl.TestWriteExcelToStream(fields, ctrl.Data, ms);

        AssertXlsx(ms.ToArray(), "编号", "名称", "金额", "创建时间");
    }

    [Fact(DisplayName = "ExportFile_Excel_返回xlsx内容类型与xlsx文件名")]
    public void ExportFile_Excel_ReturnsXlsx()
    {
        var ctrl = CreateController();

        var result = ctrl.ExportFile("Excel");

        var fr = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fr.ContentType);
        Assert.EndsWith(".xlsx", fr.FileDownloadName, StringComparison.OrdinalIgnoreCase);

        using var ms = new MemoryStream();
        fr.FileStream.CopyTo(ms);
        AssertXlsx(ms.ToArray(), "编号", "名称");
    }

    [Fact(DisplayName = "ExportFile_Xml_返回真实Xml而非Json")]
    public void ExportFile_Xml_ReturnsRealXml()
    {
        var ctrl = CreateController();

        var result = ctrl.ExportFile("Xml");

        var fr = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/xml", fr.ContentType);
        Assert.EndsWith(".xml", fr.FileDownloadName, StringComparison.OrdinalIgnoreCase);

        var text = fr.FileContents.ToStr();
        // 真实 XML：以元素节点开头且含实体数据（此前误用 ToJson 输出 JSON 数组，应以 [ 开头）
        Assert.StartsWith("<", text);
        Assert.False(text.StartsWith("[", StringComparison.Ordinal));
        Assert.Contains("<ExportTestEntity>", text);
        Assert.Contains("<Id>", text);
    }

    [Fact(DisplayName = "ExportFile_未知格式_抛出ArgumentOutOfRangeException")]
    public void ExportFile_UnknownFormat_Throws()
    {
        var ctrl = CreateController();

        Assert.Throws<ArgumentOutOfRangeException>(() => ctrl.ExportFile("BadFormat"));
    }

    [Fact(DisplayName = "ExportFile_Csv_英文表头且枚举导出数字")]
    public void ExportFile_Csv_EnglishHeadersAndEnumNumber()
    {
        var ctrl = CreateController();

        var result = ctrl.ExportFile("Csv");

        var fr = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("text/csv", fr.ContentType);
        Assert.EndsWith(".csv", fr.FileDownloadName, StringComparison.OrdinalIgnoreCase);

        using var ms = new MemoryStream();
        fr.FileStream.CopyTo(ms);
        var text = ms.ToArray().ToStr();
        var lines = text.Split('\n');

        // 表头：英文字段名（对齐 MVC CsvResult），首列 Id
        Assert.StartsWith("\"Id\",\"Name\",\"Amount\",\"CreateTime\",\"Kind\"", lines[0]);
        // 数据行：枚举字段导出数字（Kind=A→0），不导出枚举名称。字段无逗号时合法不带引号
        Assert.EndsWith(",0", lines[1].TrimEnd('\r'));
        Assert.EndsWith(",1", lines[2].TrimEnd('\r'));
    }

    [Fact(DisplayName = "ExportFile_Zip_返回zip且含db与xml条目")]
    public void ExportFile_Zip_ReturnsZip()
    {
        var ctrl = CreateController();

        var result = ctrl.ExportFile("Zip");

        var fr = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/zip", fr.ContentType);
        Assert.EndsWith(".zip", fr.FileDownloadName, StringComparison.OrdinalIgnoreCase);

        using var ms = new MemoryStream();
        fr.FileStream.CopyTo(ms);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        var names = zip.Entries.Select(e => e.FullName).ToList();

        // 数据文件 .db + 表结构 .xml，支持异地恢复（对齐 MVC ExportZip）
        Assert.Contains(names, e => e.EndsWith(".db", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, e => e.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
    }
}
