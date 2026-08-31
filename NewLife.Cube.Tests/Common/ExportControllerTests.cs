using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
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

        /// <summary>索引器重写：按字段名读写私有字段</summary>
        public override Object? this[String name]
        {
            get => name switch
            {
                "Id" => _Id,
                "Name" => _Name,
                "Amount" => _Amount,
                "CreateTime" => _CreateTime,
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
        public void TestWriteExcelToStream(IList<FieldItem> fields, IEnumerable<ExportTestEntity> data, Stream stream)
            => WriteExcelToStream(fields, data, stream);
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
                    new ExportTestEntity { Id = 1, Name = "张三", Amount = 12.34m, CreateTime = new DateTime(2026, 9, 1, 8, 30, 0) },
                    new ExportTestEntity { Id = 2, Name = "李四", Amount = 56.78m, CreateTime = new DateTime(2026, 9, 1, 9, 0, 0) },
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
        var fields = ExportTestEntity.Meta.Factory.AllFields;

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

    [Fact(DisplayName = "ExportFile_ExcelTemplate_返回xlsx且隐藏自动维护字段")]
    public void ExportFile_ExcelTemplate_HidesAutoFields()
    {
        var ctrl = CreateController();

        var result = ctrl.ExportFile("ExcelTemplate");

        var fr = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fr.ContentType);
        Assert.EndsWith(".xlsx", fr.FileDownloadName, StringComparison.OrdinalIgnoreCase);

        using var ms = new MemoryStream();
        fr.FileStream.CopyTo(ms);
        // 模板保留业务字段，隐藏自动维护字段（CreateTime）
        AssertXlsx(ms.ToArray(), "编号", "名称", "金额");
        using var zip = new ZipArchive(new MemoryStream(ms.ToArray()), ZipArchiveMode.Read);
        var ss = zip.GetEntry("xl/sharedStrings.xml");
        Assert.NotNull(ss);
        using var sr = new StreamReader(ss.Open());
        var xml = sr.ReadToEnd();
        Assert.DoesNotContain("创建时间", xml);
    }

    [Fact(DisplayName = "ExportFile_未知格式_抛出ArgumentOutOfRangeException")]
    public void ExportFile_UnknownFormat_Throws()
    {
        var ctrl = CreateController();

        Assert.Throws<ArgumentOutOfRangeException>(() => ctrl.ExportFile("BadFormat"));
    }
}
