using System;
using System.IO;
using NewLife.Cube.Entity;
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
        // 本测试会临时覆写全局 Cube 连接串：先记录原值，结束后恢复，避免污染其它测试的 Cube/Log→Membership 映射
        String? oldCube = null;
        var hadCube = DAL.ConnStrs != null && DAL.ConnStrs.TryGetValue("Cube", out oldCube);
        try
        {
            DAL.AddConnStr("Cube", $"Data Source={dbFile}", null, "SQLite");
            DAL.CreateTable();

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
            // 恢复被覆写的全局 Cube 连接串：MapTo 类字符串按 XCode 约定直接写回 ConnStrs；再重置该连接名缓存的 DAL 实例，使映射在下次使用时重新解析（Reset 为实例方法）
            if (DAL.ConnStrs != null)
            {
                if (hadCube) DAL.ConnStrs["Cube"] = oldCube!;
                else DAL.ConnStrs.TryRemove("Cube", out _);
            }
            DAL.Create("Cube").Reset();

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
