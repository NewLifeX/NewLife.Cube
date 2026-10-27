using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Collections;
using NewLife.Cube.Models;
using NewLife.Cube.Tests.Membership;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// DATA-9 数据导入测试。覆盖 CSV/JSON 解析（表头映射、注释行跳过、空行跳过、未知列写入扩展数据、非法 JSON 拒绝）、
/// 导入动作按扩展名分发，以及批量写入策略（空表批量插入、按主键合并更新）。
/// </summary>
[Collection("TenantAuth")]
public class ImportControllerTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public ImportControllerTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 测试实体与控制器
    /// <summary>导入测试实体。手写实体，解析阶段不访问数据库</summary>
    [BindTable("ImportTestEntity", "导入测试实体", ConnName = "Test")]
    private class ImportTestEntity : Entity<ImportTestEntity>
    {
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String _Name = null!;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [DataObjectField(false, false, false, 50)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

        private Decimal _Amount;
        /// <summary>金额</summary>
        [DisplayName("金额")]
        [DataObjectField(false, false, false, 0)]
        public Decimal Amount { get => _Amount; set { if (OnPropertyChanging("Amount", value)) { _Amount = value; OnPropertyChanged("Amount"); } } }

        private Boolean _Enable;
        /// <summary>启用</summary>
        [DisplayName("启用")]
        [DataObjectField(false, false, false, 0)]
        public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

        /// <summary>索引器重写：按字段名读写私有字段</summary>
        public override Object? this[String name]
        {
            get => name switch
            {
                "Id" => _Id,
                "Name" => _Name,
                "Amount" => _Amount,
                "Enable" => _Enable,
                _ => base[name],
            };
            set
            {
                switch (name)
                {
                    case "Id": Id = value.ToInt(); break;
                    case "Name": Name = value + ""; break;
                    case "Amount": Amount = value.ToDecimal(); break;
                    case "Enable": Enable = value.ToBoolean(); break;
                    default: base[name] = value; break;
                }
            }
        }
    }

    /// <summary>导入测试控制器。OnImport 记录解析结果，不落库</summary>
    private class ImportTestController : EntityController<ImportTestEntity>
    {
        /// <summary>解析出来的实体</summary>
        public List<IEntity> Captured { get; } = [];

        /// <summary>最近一次导入上下文</summary>
        public ImportContext? Context { get; private set; }

        protected override Int32 OnImport(IEntityFactory factory, IList<IEntity> list, ImportContext context)
        {
            Captured.AddRange(list);
            Context = context;

            return list.Count;
        }

        /// <summary>测试环境无 HttpContext，重写分页获取避免空引用</summary>
        protected override Pager GetCachePager() => null!;

        /// <summary>测试环境无日志上下文，重写日志为空实现</summary>
        protected override void WriteLog(String action, Boolean success, String remark) { }

        public Int32 TestImportCsv(String name, Stream stream) => ImportCsv(name, stream, Factory, null!);

        public Int32 TestImportJson(String name, Stream stream) => ImportJson(name, stream, Factory, null!);
    }

    /// <summary>参数导入控制器。直接使用字典参数实体落库</summary>
    private class ParameterImportController : EntityController<Parameter>
    {
        protected override Pager GetCachePager() => null!;

        protected override void WriteLog(String action, Boolean success, String remark) { }

        public Int32 TestOnImport(IList<IEntity> list, ImportContext context) => OnImport(Factory, list, context);
    }
    #endregion

    #region 辅助
    private static ImportTestController CreateController()
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            var controller = new ImportTestController();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            return controller;
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    private static ParameterImportController CreateParameterController()
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            var controller = new ParameterImportController();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            return controller;
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    private static MemoryStream ToStream(String text) => new(Encoding.UTF8.GetBytes(text));

    private static FormFile ToFormFile(Stream stream, String fileName)
    {
        var file = new FormFile(stream, 0, stream.Length, "file", fileName) { Headers = new HeaderDictionary() };
        file.Headers["Content-Type"] = "application/octet-stream";

        return file;
    }

    private static String Unique(String prefix) => prefix + "_" + Guid.NewGuid().ToString("N")[..8];
    #endregion

    #region CSV 解析
    [Fact(DisplayName = "导入Csv_表头按字段映射且空行跳过")]
    public void ImportCsv_MapsHeadersAndSkipsBlankRows()
    {
        var ctrl = CreateController();
        var csv = "Name,Amount,Enable,Extra\r\n张三,12.5,1,x\r\n,,,\r\n李四,20,0,y\r\n";

        var rs = ctrl.TestImportCsv("data.csv", ToStream(csv));

        Assert.Equal(2, rs);
        Assert.Equal(2, ctrl.Captured.Count);

        var first = (ImportTestEntity)ctrl.Captured[0];
        Assert.Equal("张三", first.Name);
        Assert.Equal(12.5m, first.Amount);
        Assert.True(first.Enable);
        // 未匹配到字段的列写入扩展数据，不丢弃
        Assert.Equal("x", first["Extra"]);

        var second = (ImportTestEntity)ctrl.Captured[1];
        Assert.Equal("李四", second.Name);
        Assert.Equal(20m, second.Amount);
        Assert.False(second.Enable);

        // 表头映射信息进入上下文
        Assert.Contains("Name", ctrl.Context!.Headers);
        Assert.Equal(4, ctrl.Context.Fields.Length);
    }

    [Fact(DisplayName = "导入Csv_首行说明文字被跳过并继续识别表头")]
    public void ImportCsv_SkipsCommentRowBeforeHeader()
    {
        var ctrl = CreateController();
        var csv = "说明：本文件由系统导出\r\nName,Amount\r\n张三,1.5\r\n";

        var rs = ctrl.TestImportCsv("data.csv", ToStream(csv));

        Assert.Equal(1, rs);
        var entity = (ImportTestEntity)ctrl.Captured.Single();
        Assert.Equal("张三", entity.Name);
        Assert.Equal(1.5m, entity.Amount);
    }
    #endregion

    #region JSON 解析
    [Fact(DisplayName = "导入Json_数组逐项映射且非对象元素跳过")]
    public void ImportJson_MapsArrayItems()
    {
        var ctrl = CreateController();
        var json = """[{"Name":"张三","Amount":12.5},{"Name":"李四","Enable":true},"标量"]""";

        var rs = ctrl.TestImportJson("data.json", ToStream(json));

        Assert.Equal(2, rs);
        Assert.Equal(2, ctrl.Captured.Count);
        Assert.Equal("张三", ((ImportTestEntity)ctrl.Captured[0]).Name);
        Assert.Equal(12.5m, ((ImportTestEntity)ctrl.Captured[0]).Amount);
        Assert.True(((ImportTestEntity)ctrl.Captured[1]).Enable);
    }

    [Fact(DisplayName = "导入Json_根节点非数组时拒绝")]
    public void ImportJson_NonArrayRoot_Throws()
    {
        var ctrl = CreateController();

        Assert.Throws<XException>(() => ctrl.TestImportJson("data.json", ToStream("""{"Name":"张三"}""")));
    }
    #endregion

    #region 导入动作分发
    [Fact(DisplayName = "导入动作_Csv按扩展名分发成功")]
    public void Import_CsvFile_Dispatches()
    {
        var ctrl = CreateController();
        using var ms = ToStream("Name,Amount\r\n张三,3.5\r\n");
        var file = ToFormFile(ms, "data.csv");

        var rs = ctrl.Import(file);

        var result = Assert.IsType<ContentResult>(rs);
        var json = new JsonParser(result.Content!).Decode() as IDictionary<String, Object>;
        Assert.NotNull(json);
        Assert.Equal(0, json!["code"].ToInt());
        Assert.Single(ctrl.Captured);
    }

    [Fact(DisplayName = "导入动作_不支持的扩展名被拒绝")]
    public void Import_UnsupportedExtension_Throws()
    {
        var ctrl = CreateController();
        using var ms = ToStream("Name\r\n张三\r\n");
        var file = ToFormFile(ms, "data.txt");

        Assert.Throws<NotSupportedException>(() => ctrl.Import(file));
        Assert.Empty(ctrl.Captured);
    }

    [Fact(DisplayName = "导入动作_空文件被拒绝")]
    public void Import_EmptyFile_Throws()
    {
        var ctrl = CreateController();
        using var ms = new MemoryStream();
        var file = ToFormFile(ms, "data.csv");

        Assert.Throws<ArgumentNullException>(() => ctrl.Import(file));
    }
    #endregion

    #region 写入策略
    [Fact(DisplayName = "导入落库_空表批量插入")]
    public void OnImport_AutoMode_EmptyTable_BatchInserts()
    {
        var category = Unique("Imp");
        var ctrl = CreateParameterController();
        var list = new List<IEntity>
        {
            new Parameter { Category = category, Name = "A", Value = "1", Enable = true },
            new Parameter { Category = category, Name = "B", Value = "2", Enable = true },
        };
        var context = new ImportContext { Name = "data.csv", Mode = ImportMode.Auto, TotalCount = 0 };

        var rs = ctrl.TestOnImport(list, context);

        Assert.Equal(2, rs);
        Assert.Equal(2, Parameter.FindCount(Parameter._.Category == category));
    }

    [Fact(DisplayName = "导入落库_按主键合并更新已有数据")]
    public void OnImport_MergeMode_UpdatesByPrimaryKey()
    {
        var category = Unique("Imp");
        var origin = Parameter.GetOrAdd(_fx.LegacyUser.ID, category, "Key", "旧值");
        Assert.True(origin.ID > 0);

        var ctrl = CreateParameterController();
        var list = new List<IEntity>
        {
            new Parameter { ID = origin.ID, Category = category, Name = "Key", Value = "新值", Enable = true },
        };
        var context = new ImportContext { Name = "data.csv", Mode = ImportMode.Merge, TotalCount = 1 };

        var rs = ctrl.TestOnImport(list, context);

        Assert.Equal(1, rs);
        var found = Parameter.FindByKey(origin.ID);
        Assert.NotNull(found);
        Assert.Equal("新值", found!.Value);
        // 合并而不是新增
        Assert.Equal(1, Parameter.FindCount(Parameter._.Category == category));
    }
    #endregion
}
