using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Tests.Membership;
using NewLife.Serialization;
using NewLife.Web;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// BASE-8 备份还原测试。覆盖全表备份生成 gz 文件、备份后删除数据可由还原恢复、备份条数上限拦截、无备份文件时还原报错。
/// </summary>
[Collection("TenantAuth")]
public class BackupRestoreTests : IDisposable
{
    private readonly TenantAuthFixture _fx;
    private readonly String _backupPath;
    private readonly String _originBackupPath;

    /// <summary>实例化。把备份目录指向临时目录，避免污染仓库</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public BackupRestoreTests(TenantAuthFixture fixture)
    {
        _fx = fixture;
        _originBackupPath = NewLife.Setting.Current.BackupPath;
        _backupPath = Path.Combine(Path.GetTempPath(), "cube_bak_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_backupPath);
        NewLife.Setting.Current.BackupPath = _backupPath;
    }

    /// <summary>还原备份目录设置并删除临时目录</summary>
    public void Dispose()
    {
        NewLife.Setting.Current.BackupPath = _originBackupPath;
        try
        {
            if (Directory.Exists(_backupPath)) Directory.Delete(_backupPath, true);
        }
        catch { }
    }

    #region 测试控制器
    /// <summary>参数备份控制器。备份文件名取自类名，测试用独立名称避免相互干扰</summary>
    private class ParameterBackupController : EntityController<Parameter>
    {
        protected override Pager GetCachePager() => null!;

        protected override void WriteLog(String action, Boolean success, String remark) { }
    }

    /// <summary>无备份文件的控制器</summary>
    private class NoBackupController : EntityController<Parameter>
    {
        protected override Pager GetCachePager() => null!;

        protected override void WriteLog(String action, Boolean success, String remark) { }
    }

    /// <summary>仅用于还原用例的控制器，避免与其它用例的备份文件同名</summary>
    private class RestoreCaseController : EntityController<Parameter>
    {
        protected override Pager GetCachePager() => null!;

        protected override void WriteLog(String action, Boolean success, String remark) { }
    }
    #endregion

    #region 辅助
    private static T Create<T>() where T : ControllerBase, new()
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            var controller = new T();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            return controller;
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    private static String Unique(String prefix) => prefix + "_" + Guid.NewGuid().ToString("N")[..8];

    private static Dictionary<String, Object> ReadJson(ActionResult rs)
    {
        var result = Assert.IsType<ContentResult>(rs);
        var json = new JsonParser(result.Content!).Decode() as IDictionary<String, Object>;

        return json == null ? [] : new Dictionary<String, Object>(json);
    }

    private String[] BackupFiles(String prefix) => Directory.Exists(_backupPath)
        ? Directory.GetFiles(_backupPath, prefix + "_*.gz")
        : [];
    #endregion

    [Fact(DisplayName = "备份_生成gz文件并返回成功")]
    public void Backup_CreatesGzFile()
    {
        var category = Unique("Bak");
        Parameter.GetOrAdd(_fx.LegacyUser.ID, category, "Key", "值");

        var ctrl = Create<ParameterBackupController>();

        var rs = ctrl.Backup();

        Assert.Equal(0, ReadJson(rs)["code"].ToInt());
        var files = BackupFiles("ParameterBackup");
        Assert.NotEmpty(files);
        Assert.True(new FileInfo(files[0]).Length > 0);
    }

    [Fact(DisplayName = "备份还原_删除的数据可由最新备份恢复")]
    public void Restore_BringsDeletedRowsBack()
    {
        // 备份/还原是整表操作，先清空参数表，避免已有主键与还原插入冲突
        Parameter.Delete(Parameter._.ID > 0);
        Assert.Equal(0, Parameter.FindCount());

        var category = Unique("Rst");
        var first = Parameter.GetOrAdd(_fx.LegacyUser.ID, category, "A", "1");
        var second = Parameter.GetOrAdd(_fx.LegacyUser.ID, category, "B", "2");
        Assert.True(first.ID > 0);
        Assert.True(second.ID > 0);

        var ctrl = Create<RestoreCaseController>();
        Assert.Equal(0, ReadJson(ctrl.Backup())["code"].ToInt());

        // 备份完成后删除数据，模拟误删
        Assert.Equal(2, Parameter.Delete(Parameter._.Category == category));
        Assert.Equal(0, Parameter.FindCount());

        var rs = ctrl.Restore();

        Assert.Equal(0, ReadJson(rs)["code"].ToInt());
        Assert.Equal(2, Parameter.FindCount());
        Assert.Equal("1", Parameter.FindByKey(first.ID)?.Value);
        Assert.Equal("2", Parameter.FindByKey(second.ID)?.Value);
    }

    [Fact(DisplayName = "备份_超过条数上限时拒绝")]
    public void Backup_ExceedsMaxBackup_Rejected()
    {
        var category = Unique("Lim");
        Parameter.GetOrAdd(_fx.LegacyUser.ID, category, "Key", "值");

        var set = CubeSetting.Current;
        var old = set.MaxBackup;
        set.MaxBackup = 0;
        try
        {
            var ctrl = Create<ParameterBackupController>();

            var rs = ctrl.Backup();

            Assert.Equal(500, ReadJson(rs)["code"].ToInt());
            Assert.Empty(BackupFiles("ParameterBackup"));
        }
        finally
        {
            set.MaxBackup = old;
        }
    }

    [Fact(DisplayName = "还原_无备份文件时报错")]
    public void Restore_NoBackupFile_ReturnsError()
    {
        var ctrl = Create<NoBackupController>();

        var rs = ctrl.Restore();

        Assert.Equal(500, ReadJson(rs)["code"].ToInt());
    }
}
