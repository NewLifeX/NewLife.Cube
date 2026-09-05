using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>值集存取（<see cref="LovStore"/>）测试集合。夹具一次性构建 SQLite 库（Membership 连接），测试共享同一数据源。</summary>
[CollectionDefinition("LovStore", DisableParallelization = true)]
public class LovStoreCollection : ICollectionFixture<LovStoreFixture>
{
}

/// <summary>
/// LovStore 测试夹具：独立 SQLite 连接（Parameter 实体重映射），自动迁移建表。
/// 使用独立连接名绕开 Membership 连接的 DAL 全局缓存（跨测试集合污染根因）。
/// </summary>
public class LovStoreFixture : IDisposable
{
    /// <summary>独立连接名。避免与其它测试集合共享的 Membership 连接缓存冲突</summary>
    public const String ConnName = "LovStore";

    /// <summary>数据库文件</summary>
    public String DbFile { get; }

    public LovStoreFixture()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        DbFile = Path.Combine(dir, "LovStoreTests.db");

        // 清理历史库文件（含 WAL/SHM 残留），保证每次运行干净，避免上一轮数据导致冲突
        foreach (var f in Directory.GetFiles(dir, "LovStoreTests.db*"))
        {
            try { File.Delete(f); } catch { }
        }

        // 注册独立连接；实体重映射在测试类构造函数按测试线程执行（Meta.ConnName 线程级）
        DAL.AddConnStr(ConnName, $"Data Source={DbFile}", null, "SQLite");
    }

    public void Dispose()
    {
        // 释放连接、移除独立连接，避免影响其它测试集合
        try { DAL.Create(ConnName).Reset(); } catch { }
        DAL.ConnStrs?.TryRemove(ConnName, out _);

        // 清理库文件（含 WAL/SHM 残留），避免下次运行脏数据
        try
        {
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(DbFile)!, "LovStoreTests.db*"))
            {
                File.Delete(f);
            }
        }
        catch
        {
            // 忽略清理失败
        }
    }
}

/// <summary>值集存取（<see cref="LovStore"/>）集成测试（Parameter 表，SQLite）。覆盖四类明细数据的保存读回与整表覆盖</summary>
[Collection("LovStore")]
public class LovStoreTests : IDisposable
{
    private readonly String _oldConn;

    /// <summary>每个测试实例在自己的执行线程上重映射 Parameter 到独立连接。
    /// Meta.ConnName 是线程级配置，fixture 线程设置不作用于测试线程，故须在测试构造函数中设置</summary>
    public LovStoreTests()
    {
        _oldConn = Parameter.Meta.ConnName;
        Parameter.Meta.ConnName = LovStoreFixture.ConnName;
    }

    public void Dispose() => Parameter.Meta.ConnName = _oldConn;

    [Fact]
    [DisplayName("枚举值：保存后整表覆盖读回")]
    public void EnumItems_SaveAndFind_Overwrite()
    {
        var lovDefId = 11;
        LovStore.SaveEnumItems(lovDefId, new List<LovEnumItemModel>
        {
            new LovEnumItemModel { LovDefId = lovDefId, Value = "1", Label = "启用", Sort = 0, Enabled = true },
            new LovEnumItemModel { LovDefId = lovDefId, Value = "2", Label = "停用", Sort = 1, Enabled = true },
        });

        // 整表覆盖：旧值应全部被替换
        LovStore.SaveEnumItems(lovDefId, new List<LovEnumItemModel>
        {
            new LovEnumItemModel { LovDefId = lovDefId, Value = "3", Label = "删除", Sort = 0, Enabled = true },
        });

        var items = LovStore.FindEnumItems(lovDefId);
        Assert.Single(items);
        Assert.Equal("3", items[0].Value);
        Assert.Equal("删除", items[0].Label);
        Assert.Equal(lovDefId, items[0].LovDefId);
    }

    [Fact]
    [DisplayName("枚举值：无记录时读回空列表")]
    public void EnumItems_Find_Empty()
    {
        var items = LovStore.FindEnumItems(999);
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    [DisplayName("列表配置：保存读回，未保存时返回 null")]
    public void ListConfig_SaveAndFind_NullWhenAbsent()
    {
        var lovDefId = 22;
        LovStore.SaveListConfig(lovDefId, new LovListConfigModel
        {
            LovDefId = lovDefId,
            RequestUrl = "http://external/api/roles",
            Method = "GET",
            Pageable = true,
            PageNumField = "pageIndex",
            PageSizeField = "pageSize",
            DataPath = "data",
            TotalPath = "total",
            ProxyRequest = true,
        });

        var config = LovStore.FindListConfig(lovDefId);
        Assert.NotNull(config);
        Assert.Equal(lovDefId, config.LovDefId);
        Assert.Equal("http://external/api/roles", config.RequestUrl);
        Assert.True(config.Pageable);
        Assert.True(config.ProxyRequest);

        // 未保存过的值集定义返回 null
        Assert.Null(LovStore.FindListConfig(888));
    }

    [Fact]
    [DisplayName("搜索字段：保存后读回")]
    public void SearchFields_SaveAndFind()
    {
        var lovDefId = 33;
        LovStore.SaveSearchFields(lovDefId, new List<LovSearchFieldModel>
        {
            new LovSearchFieldModel { LovDefId = lovDefId, Field = "name", Title = "名称", ComponentType = "input", ParamType = "QUERY", Sort = 0 },
        });

        var fields = LovStore.FindSearchFields(lovDefId);
        Assert.Single(fields);
        Assert.Equal("name", fields[0].Field);
        Assert.Equal("名称", fields[0].Title);
        Assert.Equal("QUERY", fields[0].ParamType);
    }

    [Fact]
    [DisplayName("表格列：保存后读回")]
    public void TableColumns_SaveAndFind()
    {
        var lovDefId = 44;
        LovStore.SaveTableColumns(lovDefId, new List<LovTableColumnModel>
        {
            new LovTableColumnModel { LovDefId = lovDefId, Field = "id", Title = "编号", Width = 80, Sortable = true, Sort = 0 },
            new LovTableColumnModel { LovDefId = lovDefId, Field = "name", Title = "名称", Width = 200, Sort = 1 },
        });

        var cols = LovStore.FindTableColumns(lovDefId);
        Assert.Equal(2, cols.Count);
        Assert.Equal("id", cols[0].Field);
        Assert.Equal(80, cols[0].Width);
        Assert.True(cols[0].Sortable);
        Assert.Equal("名称", cols[1].Title);
    }
}
