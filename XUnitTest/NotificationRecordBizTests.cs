using System;
using System.ComponentModel;
using System.Linq;
using NewLife.Cube.Entity;
using XCode;
using Xunit;

namespace XUnitTest;

/// <summary>
/// 通知记录站内信未读/已读语义测试（SQLite 轻量集成，与同集合用例共用库）：
/// 广播(UserId=0)仅系统管理员可见可读、个人消息仅本人可见可读、任意一人已读广播即全局消除、全部已读
/// </summary>
[Collection("SqliteDb")]
public class NotificationRecordBizTests : IDisposable
{
    public NotificationRecordBizTests() => SqliteDb.Ensure();

    public void Dispose()
    {
        NotificationRecord.Meta.Session.ClearCache(nameof(NotificationRecordBizTests), true);
    }

    /// <summary>清空测试数据并播种一批站内信记录，返回 (r1广播, r2本人1001未读, r4用户1002未读)</summary>
    private static (NotificationRecord r1, NotificationRecord r2, NotificationRecord r4) Seed()
    {
        // 清理本类与在线告警用例遗留的站内信，保证计数口径独立
        NotificationRecord.Delete(NotificationRecord._.Remark == "单测");
        NotificationRecord.Delete(NotificationRecord._.Action == "OnlineAlert");

        var r1 = new NotificationRecord { Channel = "InApp", UserId = 0, Title = "广播", Content = "系统广播", Remark = "单测" };
        r1.Insert();

        var r2 = new NotificationRecord { Channel = "InApp", UserId = 1001, Title = "个人1001", Content = "你好", Remark = "单测" };
        r2.Insert();

        var r3 = new NotificationRecord { Channel = "InApp", UserId = 1001, Title = "已读1001", Content = "旧消息", Read = true, ReadTime = DateTime.Now, Remark = "单测" };
        r3.Insert();

        var r4 = new NotificationRecord { Channel = "InApp", UserId = 1002, Title = "个人1002", Content = "你好", Remark = "单测" };
        r4.Insert();

        // 非站内信（邮件），不应计入未读站内信
        var r5 = new NotificationRecord { Channel = "Mail", UserId = 1001, Title = "邮件", Content = "验证码", Remark = "单测" };
        r5.Insert();

        return (r1, r2, r4);
    }

    [Fact]
    [DisplayName("CountUnread_广播仅管理员计入_个人仅本人")]
    public void CountUnread_Visibility()
    {
        Seed();

        // 普通用户 1001：仅自己的未读站内信（r2），r3已读、r5非站内信均不计
        Assert.Equal(1, NotificationRecord.CountUnread(1001, false));
        // 系统管理员 1001：自己的未读 + 广播(r1)
        Assert.Equal(2, NotificationRecord.CountUnread(1001, true));
        // 普通用户 1002：仅自己的（r4），广播对普通用户不可见
        Assert.Equal(1, NotificationRecord.CountUnread(1002, false));
        // 系统管理员 1002：自己的 + 广播
        Assert.Equal(2, NotificationRecord.CountUnread(1002, true));
        // 匿名用户不产生任何未读
        Assert.Equal(0, NotificationRecord.CountUnread(0, false));
    }

    [Fact]
    [DisplayName("GetRecentUnread_可见集合与排序")]
    public void GetRecentUnread_Visible()
    {
        var (r1, _, r4) = Seed();

        var list = NotificationRecord.GetRecentUnread(1002, true, 10);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, e => e.Id == r1.Id);
        Assert.Contains(list, e => e.Id == r4.Id);

        // 数量限制
        var one = NotificationRecord.GetRecentUnread(1002, true, 1);
        Assert.Single(one);

        // 普通用户看不到广播
        var normal = NotificationRecord.GetRecentUnread(1002, false, 10);
        Assert.Single(normal);
        Assert.Equal(r4.Id, normal[0].Id);
    }

    [Fact]
    [DisplayName("MarkRead_广播仅管理员可读_个人仅本人可读")]
    public void MarkRead_Permission()
    {
        var (r1, r2, r4) = Seed();

        // 普通用户不能读广播
        Assert.Null(NotificationRecord.MarkRead(r1.Id, 1001, "小明", false));
        // 系统管理员读广播 → 全局已读
        var rd = NotificationRecord.MarkRead(r1.Id, 1002, "管理员A", true);
        Assert.NotNull(rd);
        Assert.True(rd!.Read);
        Assert.Contains("管理员A", rd.Result);
        // 广播已读后，管理员未读数减少
        Assert.Equal(1, NotificationRecord.CountUnread(1002, true));

        // 非本人不能读他人个人消息
        Assert.Null(NotificationRecord.MarkRead(r4.Id, 1001, "王五", false));
        // 本人可读自己的个人消息（即使非管理员）
        Assert.NotNull(NotificationRecord.MarkRead(r4.Id, 1002, "李四", false));
        // 管理员也不能读他人个人消息（数据权限按本人）
        Assert.Null(NotificationRecord.MarkRead(r2.Id, 1002, "管理员A", true));

        // 不存在的记录
        Assert.Null(NotificationRecord.MarkRead(999999999, 1002, "管理员A", true));
        // 非站内信不可标记已读
        var mail = NotificationRecord.Find(NotificationRecord._.Channel == "Mail");
        Assert.NotNull(mail);
        Assert.Null(NotificationRecord.MarkRead(mail!.Id, 1001, "小明", false));
    }

    [Fact]
    [DisplayName("MarkAllRead_管理员全部已读含广播_普通用户仅自己的")]
    public void MarkAllRead_Scope()
    {
        Seed();

        // 普通用户 1001 全部已读：仅自己的 r2（r3已读）
        var n1 = NotificationRecord.MarkAllRead(1001, "小明", false);
        Assert.Equal(1, n1);
        Assert.Equal(0, NotificationRecord.CountUnread(1001, false));
        // 广播仍为未读，管理员仍可见
        Assert.Equal(2, NotificationRecord.CountUnread(1002, true));

        // 系统管理员 1002 全部已读：广播 r1 + 自己的 r4
        var n2 = NotificationRecord.MarkAllRead(1002, "管理员A", true);
        Assert.Equal(2, n2);
        Assert.Equal(0, NotificationRecord.CountUnread(1002, true));
        Assert.Equal(0, NotificationRecord.CountUnread(1001, true));
    }
}
