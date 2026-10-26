using System;
using System.IO;
using NewLife.Cube.Entity;
using NewLife.Cube.Tests.Common;
using XCode.DataAccessLayer;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// 覆盖 <see cref="OAuthLog"/> 的保留清理逻辑，验证旧日志会按批次删除且保留最近日志。
/// </summary>
public class OAuthLogRetentionTests
{
    [Fact]
    public void DeleteBefore_DeletesOldRowsInBatches()
    {
        var dbFile = Path.Combine(Path.GetTempPath(), $"OAuthLogRetentionTests_{Guid.NewGuid():N}.db");
        // 用连接作用域临时把 Cube 指向独立库：切换时会清掉 DAL 与实体会话缓存，
        // 结束后还原连接串并再次清理，避免与其它用例的 Cube 库互相污染
        using var conn = TestConnections.Use("Cube", $"Data Source={dbFile}");
        try
        {
            DAL.Create("Cube").SetTables((IDataTable)OAuthLog.Meta.Table.DataTable.Clone());

            var now = DateTime.Now;
            var oldAnonymous = new OAuthLog
            {
                Id = OAuthLog.Meta.Factory.Snow.GetId(now.AddDays(-40)),
                Provider = "Test",
                Success = false,
                CreateTime = now.AddDays(-40),
                UserId = 0
            };
            oldAnonymous.Insert();

            var oldBound = new OAuthLog
            {
                Id = OAuthLog.Meta.Factory.Snow.GetId(now.AddDays(-40).AddMinutes(-1)),
                Provider = "Test",
                Success = false,
                CreateTime = now.AddDays(-40),
                UserId = 7
            };
            oldBound.Insert();

            var recent = new OAuthLog
            {
                Id = OAuthLog.Meta.Factory.Snow.GetId(now.AddDays(-2)),
                Provider = "Test",
                Success = true,
                CreateTime = now.AddDays(-2),
                UserId = 0
            };
            recent.Insert();

            var count = OAuthLog.DeleteBefore(now.AddDays(-10), 100);

            Assert.Equal(2, count);
            Assert.Null(OAuthLog.FindById(oldAnonymous.Id));
            Assert.Null(OAuthLog.FindById(oldBound.Id));
            Assert.NotNull(OAuthLog.FindById(recent.Id));
        }
        finally
        {
            try
            {
                if (File.Exists(dbFile)) File.Delete(dbFile);
            }
            catch
            {
                // ignore cleanup failure
            }
        }
    }
}



