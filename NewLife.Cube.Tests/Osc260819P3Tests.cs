using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NewLife.Cube;
using NewLife.Cube.Entity;
using NewLife.Cube.Models;
using NewLife.Remoting;
using XCode;
using XCode.DataAccessLayer;
using XCode.Model;
using Xunit;
using Xunit.Abstractions;

namespace NewLife.Cube.Tests;

/// <summary>P3 测试控制器：mock Find/OnUpdate，用 XCode 完整生成的 NotificationRecord（SetItem/Dirtys 可用）</summary>
public class Osc260819P3Controller : EntityController<NotificationRecord>
{
    /// <summary>内存数据存储（主键 → 实体）</summary>
    public Dictionary<String, NotificationRecord> Store { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>OnUpdate 调用次数</summary>
    public Int32 UpdateCount { get; private set; }

    /// <summary>数据权限默认放行</summary>
    protected override Boolean ValidPermission(NotificationRecord entity, DataObjectMethodType type, Boolean post) => true;

    /// <summary>绕过基类 Valid 的 HttpContext/租户/日志逻辑，聚焦 PATCH 流程</summary>
    protected override Boolean Valid(NotificationRecord entity, DataObjectMethodType type, Boolean post) => true;

    /// <summary>无数据权限（测试无 HttpContext，基类 CreateWhere 会访问 HttpContext.Items）</summary>
    protected override WhereBuilder CreateWhere() => null;

    /// <summary>从内存存储查找</summary>
    /// <param name="key">主键</param>
    /// <returns></returns>
    protected override NotificationRecord Find(Object key) => Store.TryGetValue(key + "", out var e) ? e : null;

    /// <summary>记录更新（不落库）</summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    protected override Int32 OnUpdate(NotificationRecord entity)
    {
        UpdateCount++;
        return 1;
    }
}

/// <summary>OSC-260819e483 P3 PATCH 与批量改字段</summary>
[Collection("Osc260819")]
public class Osc260819P3Tests
{
    private readonly ITestOutputHelper _output;

    public Osc260819P3Tests(ITestOutputHelper output)
    {
        _output = output;
        DAL.AddConnStr("Cube", "Data Source=Osc260819P3;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Log", "Data Source=Osc260819P3Log;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Membership", "Data Source=Osc260819P3Membership;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    static NotificationRecord New(Int64 id) => new() { Id = id, Action = "Notify", Channel = "InApp", UserId = 1, Title = "t" + id, Content = "c", Success = true };

    static Osc260819P3Controller Ctrl() => new();

    [Fact(DisplayName = "P3 探针：NotificationRecord EditForm 白名单含 Title（供 Patch 用）")]
    public void Probe_EditFormFields()
    {
        var c = Ctrl();
        var fields = c.PatchFields == null ? null : typeof(Osc260819P3Controller)
            .GetMethod("BuildPatchWhitelist", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        _output.WriteLine($"HasBuildPatchWhitelist={fields != null}");
        // 直接通过一次 PATCH 探针确认 Title 在白名单：未知字段会 400，成功则 Ok=1
        c.Store["1"] = New(1);
        var res = c.PatchFields(new Osc260819P3Controller.PatchFieldsRequest { Id = "1", Values = new() { ["Title"] = "新标题" } });
        _output.WriteLine($"Probe Title PATCH: Code={res.Code} Msg={res.Message} Ok={res.Data.Ok} Fail={res.Data.Fail} Errors={string.Join(",", res.Data.Errors.Select(e => e.Id + ":" + e.Message))}");
        Assert.Equal(1, res.Data.Ok);
    }

    [Fact(DisplayName = "P3 PatchFields：白名单字段更新成功，Ok=1，OnUpdate 被调")]
    public void PatchFields_Ok()
    {
        var c = Ctrl();
        c.Store["1"] = New(1);

        var res = c.PatchFields(new Osc260819P3Controller.PatchFieldsRequest
        {
            Id = "1",
            Values = new() { ["Title"] = "新" },
        });

        Assert.Equal(0, res.Code);
        Assert.Equal(1, res.Data.Ok);
        Assert.Equal(0, res.Data.Fail);
        Assert.Empty(res.Data.Errors);
        Assert.Equal(1, c.UpdateCount);
    }

    [Fact(DisplayName = "P3 PatchFields：未知字段整单 400")]
    public void PatchFields_UnknownField_400()
    {
        var c = Ctrl();
        c.Store["1"] = New(1);

        var ex = Assert.Throws<ApiException>(() => c.PatchFields(new Osc260819P3Controller.PatchFieldsRequest
        {
            Id = "1",
            Values = new() { ["Ghost"] = 1 },
        }));
        Assert.Equal(CubeCode.ParamError.ToInt(), ex.Code);
        Assert.Equal(0, c.UpdateCount);
    }

    [Fact(DisplayName = "P3 PatchFields：缺 id/values → 400")]
    public void PatchFields_Empty_400()
    {
        var c = Ctrl();
        Assert.Throws<ApiException>(() => c.PatchFields(null));
        Assert.Throws<ApiException>(() => c.PatchFields(new Osc260819P3Controller.PatchFieldsRequest { Id = "", Values = new() { ["Title"] = "x" } }));
        Assert.Throws<ApiException>(() => c.PatchFields(new Osc260819P3Controller.PatchFieldsRequest { Id = "1", Values = new() { } }));
        Assert.Equal(0, c.UpdateCount);
    }

    [Fact(DisplayName = "P3 PatchFields：行不存在 → Fail=1 且带错误明细，不 500")]
    public void PatchFields_NotFound_Fail()
    {
        var c = Ctrl();
        var res = c.PatchFields(new Osc260819P3Controller.PatchFieldsRequest
        {
            Id = "999999",
            Values = new() { ["Title"] = "x" },
        });

        Assert.Equal(0, res.Code);
        Assert.Equal(0, res.Data.Ok);
        Assert.Equal(1, res.Data.Fail);
        Assert.Single(res.Data.Errors);
        Assert.Equal(0, c.UpdateCount);
    }

    [Fact(DisplayName = "P3 BatchUpdateFields：多行批量更新成功，Ok=2")]
    public void BatchUpdateFields_Ok()
    {
        var c = Ctrl();
        c.Store["1"] = New(1);
        c.Store["2"] = New(2);

        var res = c.BatchUpdateFields(new Osc260819P3Controller.BatchUpdateFieldsRequest
        {
            Keys = "1,2",
            Field = "Title",
            Value = "批量",
        });

        Assert.Equal(0, res.Code);
        Assert.Equal(2, res.Data.Ok);
        Assert.Equal(0, res.Data.Fail);
        Assert.Equal(2, c.UpdateCount);
    }

    [Fact(DisplayName = "P3 BatchUpdateFields：空 keys / 超 500 / 未知字段 → 400")]
    public void BatchUpdateFields_BadRequest_400()
    {
        var c = Ctrl();
        Assert.Throws<ApiException>(() => c.BatchUpdateFields(new Osc260819P3Controller.BatchUpdateFieldsRequest { Keys = "", Field = "Title", Value = "x" }));
        Assert.Throws<ApiException>(() => c.BatchUpdateFields(new Osc260819P3Controller.BatchUpdateFieldsRequest { Keys = ",,", Field = "Title", Value = "x" }));

        var many = string.Join(",", Enumerable.Range(1, 501));
        Assert.Throws<ApiException>(() => c.BatchUpdateFields(new Osc260819P3Controller.BatchUpdateFieldsRequest { Keys = many, Field = "Title", Value = "x" }));

        Assert.Throws<ApiException>(() => c.BatchUpdateFields(new Osc260819P3Controller.BatchUpdateFieldsRequest { Keys = "1", Field = "Ghost", Value = "x" }));
        Assert.Equal(0, c.UpdateCount);
    }

    [Fact(DisplayName = "P3 BatchUpdateFields：部分失败继续后续行，Ok/Fail 汇总")]
    public void BatchUpdateFields_PartialFail()
    {
        var c = Ctrl();
        c.Store["1"] = New(1);

        // 第一条存在、第二条不存在 → Ok=1 Fail=1
        var res = c.BatchUpdateFields(new Osc260819P3Controller.BatchUpdateFieldsRequest
        {
            Keys = "1,999999",
            Field = "Title",
            Value = "批量",
        });

        Assert.Equal(0, res.Code);
        Assert.Equal(1, res.Data.Ok);
        Assert.Equal(1, res.Data.Fail);
        Assert.Single(res.Data.Errors);
        Assert.Equal("999999", res.Data.Errors[0].Id);
        Assert.Equal(1, c.UpdateCount);
    }
}

/// <summary>P3 修复回归控制器：EntityAutomation（含必填 String Name），可模拟校验头开启（EnableFieldValidation override）</summary>
public class Osc260819P3AutoController : EntityController<EntityAutomation>
{
    /// <summary>内存数据存储（主键 → 实体）</summary>
    public Dictionary<String, EntityAutomation> Store { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>OnUpdate 调用次数</summary>
    public Int32 UpdateCount { get; private set; }

    /// <summary>测试开关：模拟写请求携带 X-Cube-Field-Validation 头</summary>
    public static Boolean ForceFieldValidation { get; set; }

    /// <summary>按模拟头开关启用字段级校验</summary>
    protected override Boolean EnableFieldValidation => ForceFieldValidation;

    /// <summary>数据权限默认放行</summary>
    protected override Boolean ValidPermission(EntityAutomation entity, DataObjectMethodType type, Boolean post) => true;

    /// <summary>绕过基类 Valid 的 HttpContext/租户/日志逻辑</summary>
    protected override Boolean Valid(EntityAutomation entity, DataObjectMethodType type, Boolean post) => true;

    /// <summary>无数据权限（测试无 HttpContext）</summary>
    protected override WhereBuilder CreateWhere() => null;

    /// <summary>从内存存储查找</summary>
    /// <param name="key">主键</param>
    /// <returns></returns>
    protected override EntityAutomation Find(Object key) => Store.TryGetValue(key + "", out var e) ? e : null;

    /// <summary>记录更新（不落库）</summary>
    /// <param name="entity">实体</param>
    /// <returns></returns>
    protected override Int32 OnUpdate(EntityAutomation entity)
    {
        UpdateCount++;
        return 1;
    }
}

/// <summary>OSC-260819e483 P3 修复：局部更新只校验被改字段（避免整实体其它空必填字段误伤）</summary>
[Collection("Osc260819")]
public class Osc260819P3FixTests
{
    public Osc260819P3FixTests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc260819P3Fix;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    static Osc260819P3AutoController Ctrl() => new();

    [Fact(DisplayName = "P3 修复：校验头开启时批量改字段，其它必填空字段（Name=null）不误伤 → Ok=1")]
    public void BatchUpdateFields_OnlyValidates_TouchedField()
    {
        Osc260819P3AutoController.ForceFieldValidation = true;
        try
        {
            var c = Ctrl();
            // Name 为必填 String（NOT NULL）但值为 null；改 Priority 应成功（只校验被改字段）
            var e = new EntityAutomation
            {
                Id = 100,
                Name = null,
                Enable = true,
                Priority = 100,
                TypePath = "Cube/Test",
            };
            c.Store["100"] = e;

            var res = c.BatchUpdateFields(new Osc260819P3AutoController.BatchUpdateFieldsRequest
            {
                Keys = "100",
                Field = "Priority",
                Value = "200",
            });

            Assert.Equal(0, res.Code);
            Assert.Equal(1, res.Data.Ok);
            Assert.Equal(0, res.Data.Fail);
            Assert.Empty(res.Data.Errors);
            Assert.Equal(200, e.Priority);
            Assert.Equal(1, c.UpdateCount);
        }
        finally
        {
            Osc260819P3AutoController.ForceFieldValidation = false;
        }
    }

    [Fact(DisplayName = "P3 修复：校验头开启时把必填字段本身置空 → 仍报错 Fail=1（校验被改字段语义保留）")]
    public void BatchUpdateFields_EmptyRequiredField_StillFails()
    {
        Osc260819P3AutoController.ForceFieldValidation = true;
        try
        {
            var c = Ctrl();
            var e = new EntityAutomation { Id = 101, Name = "ok", Enable = true, Priority = 100, TypePath = "Cube/Test" };
            c.Store["101"] = e;

            var res = c.BatchUpdateFields(new Osc260819P3AutoController.BatchUpdateFieldsRequest
            {
                Keys = "101",
                Field = "Name",
                Value = "",
            });

            Assert.Equal(0, res.Code);
            Assert.Equal(0, res.Data.Ok);
            Assert.Equal(1, res.Data.Fail);
            Assert.Contains("不可以为空", res.Data.Errors[0].Message);
            Assert.Equal(0, c.UpdateCount);
        }
        finally
        {
            Osc260819P3AutoController.ForceFieldValidation = false;
        }
    }

    [Fact(DisplayName = "P3 修复：无校验头时行为与今日一致（整表单校验不启用），批量改成功")]
    public void BatchUpdateFields_NoHeader_Ok()
    {
        Osc260819P3AutoController.ForceFieldValidation = false;
        var c = Ctrl();
        var e = new EntityAutomation { Id = 102, Name = "ok", Enable = true, Priority = 100, TypePath = "Cube/Test" };
        c.Store["102"] = e;

        var res = c.BatchUpdateFields(new Osc260819P3AutoController.BatchUpdateFieldsRequest
        {
            Keys = "102",
            Field = "Priority",
            Value = "300",
        });

        Assert.Equal(0, res.Code);
        Assert.Equal(1, res.Data.Ok);
        Assert.Equal(300, e.Priority);
    }
}
