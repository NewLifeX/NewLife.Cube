using System;
using NewLife.Cube.Entity;
using XCode.DataAccessLayer;
using Xunit;

namespace XUnitTest;

/// <summary>OSC-0002：UserProfile / EntityViewProfile / EntityComment 实体与业务方法</summary>
public class ProfileCommentEntityTests
{
    public ProfileCommentEntityTests()
    {
        // 隔离测试库，避免污染开发库
        DAL.AddConnStr("Cube", "Data Source=ProfileCommentTests;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    [Fact(DisplayName = "UserProfile：Upsert 绑定当前用户并忽略模型中的他人 UserId")]
    public void UserProfile_Upsert_BindsCurrentUser()
    {
        var uid = Environment.TickCount & 0x0FFF_FFFF | 0x1000_0000;
        var model = new UserProfileModel
        {
            UserId = uid + 99,
            LayoutJson = """{"mode":"side"}""",
            ThemeJson = """{"appearance":"light"}""",
            WorkspaceJson = """{"defaultView":"table","pageSize":20}""",
            Version = 1,
        };

        var saved = UserProfile.UpsertForUser(uid, model);
        Assert.Equal(uid, saved.UserId);
        Assert.Equal(model.LayoutJson, saved.LayoutJson);

        var found = UserProfile.FindByUserId(uid);
        Assert.NotNull(found);
        Assert.Equal(uid, found.UserId);
        Assert.Null(UserProfile.FindByUserId(uid + 99));
    }

    [Fact(DisplayName = "EntityViewProfile：按 typePath 隔离，Delete 后找不到")]
    public void EntityViewProfile_TypePath_IsolationAndDelete()
    {
        var uid = (Environment.TickCount & 0x0FFF_FFFF) | 0x2000_0000;
        var pathA = "Admin/User";
        var pathB = "Admin/Role";

        EntityViewProfile.UpsertForUser(uid, pathA, new EntityViewProfileModel
        {
            TypePath = pathA,
            View = "table",
            ColumnsJson = """[{"key":"Name","visible":true}]""",
            Version = 1,
        });
        EntityViewProfile.UpsertForUser(uid, pathB, new EntityViewProfileModel
        {
            TypePath = pathB,
            View = "tree",
            Version = 1,
        });

        Assert.Equal("table", EntityViewProfile.FindByUserIdAndTypePath(uid, pathA)?.View);
        Assert.Equal("tree", EntityViewProfile.FindByUserIdAndTypePath(uid, pathB)?.View);

        Assert.True(EntityViewProfile.DeleteForUser(uid, pathA));
        Assert.Null(EntityViewProfile.FindByUserIdAndTypePath(uid, pathA));
        Assert.NotNull(EntityViewProfile.FindByUserIdAndTypePath(uid, pathB));
    }

    [Fact(DisplayName = "EntityViewProfile：ViewsJson/ActiveViewId upsert 持久化")]
    public void EntityViewProfile_NamedViews_Upsert()
    {
        var uid = (Environment.TickCount & 0x0FFF_FFFF) | 0x2100_0000;
        var path = "Admin/Menu";
        var views = """[{"id":"default","name":"列表","view":"table","columns":[{"key":"Name","visible":true}]}]""";

        EntityViewProfile.UpsertForUser(uid, path, new EntityViewProfileModel
        {
            TypePath = path,
            View = "table",
            ViewsJson = views,
            ActiveViewId = "default",
            ColumnsJson = """[{"key":"Name","visible":true}]""",
            Version = 1,
        });

        var saved = EntityViewProfile.FindByUserIdAndTypePath(uid, path);
        Assert.NotNull(saved);
        Assert.Equal("default", saved.ActiveViewId);
        Assert.Contains("列表", saved.ViewsJson);
    }

    [Fact(DisplayName = "EntityComment：按 category+linkId 列表；同表回复 ParentId/RootId；非作者非管理员不可删")]
    public void EntityComment_ListReplyAndDeleteAuth()
    {
        var authorId = (Environment.TickCount & 0x0FFF_FFFF) | 0x3000_0000;
        var otherId = authorId + 1;
        var category = "Admin/User";
        var linkId = 42L;

        var c1 = EntityComment.AddComment(authorId, "author", category, linkId, "hello");
        Assert.Equal(0, c1.ParentId);
        Assert.Equal(c1.Id, c1.RootId);

        var reply = EntityComment.AddComment(otherId, "replier", category, linkId, "re: hello", parentId: c1.Id);
        Assert.Equal(c1.Id, reply.ParentId);
        Assert.Equal(c1.Id, reply.RootId);
        Assert.Equal(authorId, reply.ReplyUserId);
        Assert.Equal("author", reply.ReplyUser);

        EntityComment.AddComment(authorId, "author", category, linkId + 1, "other-link");

        var list = EntityComment.FindList(category, linkId);
        Assert.Contains(list, e => e.Id == c1.Id);
        Assert.Contains(list, e => e.Id == reply.Id);
        Assert.DoesNotContain(list, e => e.LinkId == linkId + 1);

        var tops = EntityComment.FindList(category, linkId, parentId: 0);
        Assert.Contains(tops, e => e.Id == c1.Id);
        Assert.DoesNotContain(tops, e => e.Id == reply.Id);

        Assert.False(EntityComment.TryDelete(c1.Id, otherId, isAdmin: false));
        Assert.NotNull(EntityComment.FindById(c1.Id));

        Assert.True(EntityComment.TryDelete(c1.Id, otherId, isAdmin: true));
        Assert.Null(EntityComment.FindById(c1.Id));
    }

    [Fact(DisplayName = "鉴权语义：userId<=0 的 Upsert/评论抛出")]
    public void AuthSemantics_RejectsInvalidUserId()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UserProfile.UpsertForUser(0, new UserProfileModel()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EntityViewProfile.UpsertForUser(0, "Admin/User", new EntityViewProfileModel { TypePath = "Admin/User" }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EntityComment.AddComment(0, "x", "Admin/User", 1, "c"));
    }
}
