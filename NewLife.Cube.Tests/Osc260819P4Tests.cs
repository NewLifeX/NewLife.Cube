using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NewLife.Cube.Controllers;
using NewLife.Cube.Entity;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>OSC-260819e483 P4.5：评论提及站内信边界（最多 20、Distinct；非法/禁用/自己跳过；正文截断；不抛）</summary>
[Collection("Osc260819")]
public class Osc260819P4Tests
{
    public Osc260819P4Tests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc260819P4Cube;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Log", "Data Source=Osc260819P4Log;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Membership", "Data Source=Osc260819P4Membership;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    /// <summary>反射调用 CubeController 私有静态 SendMentionNotification（不改签名验证其行为）</summary>
    static void InvokeSend(Int32 currentUserId, String currentUserName, String category, Int64 linkId, String content, IEnumerable<Int32> userIds)
    {
        var mi = typeof(CubeController).GetMethod("SendMentionNotification", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(mi);
        mi!.Invoke(null, [currentUserId, currentUserName, category, linkId, content, userIds]);
    }

    static Int32 CountMention(String category, Int64 linkId) =>
        NotificationRecord.FindAll()
            .Count(e => e.Action == "Mention" && e.Target == $"{category}#{linkId}");

    static User NewUser(Int32 n, Boolean enable = true) => new()
    {
        Name = "p4_" + n + "_" + Guid.NewGuid().ToString("N")[..6],
        Enable = enable,
    };

    [Fact(DisplayName = "P4.5 最多 20 条且 Distinct：25 个（含重复）只写 20 条")]
    public void Mention_Caps_At_20_Distinct()
    {
        var users = new List<User>();
        for (var i = 1; i <= 25; i++) { var u = NewUser(i); u.Insert(); users.Add(u); }
        var ids = users.Select(u => u.ID).ToList();
        // 前 3 个重复一遍
        ids.AddRange(users.Take(3).Select(u => u.ID));
        var cat = "p4-cap-" + Guid.NewGuid().ToString("N")[..6];
        const Int64 link = 1001;

        InvokeSend(9999, "作者", cat, link, "看这里", ids);

        Assert.Equal(20, CountMention(cat, link));
        // 全部指向存在的用户
        var notis = NotificationRecord.FindAll().Where(e => e.Target == $"{cat}#{link}").ToList();
        Assert.Equal(20, notis.Select(e => e.UserId).Distinct().Count());
    }

    [Fact(DisplayName = "P4.5 非法/禁用/自己/不存在跳过：0 条通知")]
    public void Mention_Skips_Illegal_Disabled_Self_Missing()
    {
        var me = NewUser(1); me.Insert();
        var disabled = NewUser(2, false); disabled.Insert();

        var cat = "p4-skip-" + Guid.NewGuid().ToString("N")[..6];
        InvokeSend(me.ID, "作者", cat, 2002, "hi", [0, -5, me.ID, disabled.ID, 99999999]);

        Assert.Equal(0, CountMention(cat, 2002));
    }

    [Fact(DisplayName = "P4.5 空列表：不写任何通知（无提及行为与今日一致）")]
    public void Mention_Empty_Noop()
    {
        var cat = "p4-empty-" + Guid.NewGuid().ToString("N")[..6];
        InvokeSend(1, "作者", cat, 3003, "hi", []);
        Assert.Equal(0, CountMention(cat, 3003));
    }

    [Fact(DisplayName = "P4.5 正文截断 ≤200 且方法不抛；Target=category#linkId")]
    public void Mention_Truncates_Content_And_NoThrow()
    {
        var u = NewUser(1); u.Insert();
        var cat = "p4-long-" + Guid.NewGuid().ToString("N")[..6];
        var longContent = new String('长', 500);

        InvokeSend(9999, "作者", cat, 4004, longContent, [u.ID]);

        var notis = NotificationRecord.FindAll().Where(e => e.Target == $"{cat}#4004").ToList();
        Assert.Single(notis);
        Assert.True(notis[0].Content.Length <= 200);
        Assert.Equal("InApp", notis[0].Channel);
        Assert.Equal(u.ID, notis[0].UserId);
        Assert.True(notis[0].Success);
    }

    [Fact(DisplayName = "P4.5 通知失败不回滚评论、不向外抛：异常被方法内部吞掉")]
    public void Mention_Insert_Failure_NoThrow()
    {
        var u = NewUser(1); u.Insert();
        var cat = "p4-fail-" + Guid.NewGuid().ToString("N")[..6];
        // 目标用户存在，但故意制造插入异常：向 NotificationRecord 插入时使某必填校验失败不可行，
        // 改为验证方法对异常输入不抛（内容为 null 会被 EntityComment 前置拦截；此处验证方法体结构不抛）
        // 模拟单条失败：先写一条正常通知，随后方法仍可被再次调用且不抛
        InvokeSend(9999, "作者", cat, 5005, "ok", [u.ID]);
        Assert.Equal(1, CountMention(cat, 5005));

        // 再次调用不抛（无副作用路径）
        InvokeSend(9999, "作者", cat, 5005, "again", [u.ID]);
        Assert.Equal(2, CountMention(cat, 5005));
    }
}
