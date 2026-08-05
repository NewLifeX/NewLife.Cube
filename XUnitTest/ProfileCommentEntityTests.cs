using System;
using System.Linq;
using NewLife.Cube.Entity;
using XCode.DataAccessLayer;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTest;

/// <summary>OSC-0002：UserProfile / ViewProfile / EntityComment 实体与业务方法</summary>
public class ProfileCommentEntityTests
{
    private readonly ITestOutputHelper _output;

    public ProfileCommentEntityTests(ITestOutputHelper output)
    {
        _output = output;
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

    [Fact(DisplayName = "ViewProfile：按 typePath 隔离，Delete 后找不到")]
    public void ViewProfile_TypePath_IsolationAndDelete()
    {
        var uid = (Environment.TickCount & 0x0FFF_FFFF) | 0x2000_0000;
        var pathA = "Admin/User";
        var pathB = "Admin/Role";

        ViewProfile.UpsertForUser(uid, pathA, new ViewProfileModel
        {
            TypePath = pathA,
            View = "table",
            ColumnsJson = """[{"key":"Name","visible":true}]""",
            Version = 1,
        });
        ViewProfile.UpsertForUser(uid, pathB, new ViewProfileModel
        {
            TypePath = pathB,
            View = "tree",
            Version = 1,
        });

        Assert.Equal("table", ViewProfile.FindByUserIdAndTypePath(uid, pathA)?.View);
        Assert.Equal("tree", ViewProfile.FindByUserIdAndTypePath(uid, pathB)?.View);

        Assert.True(ViewProfile.DeleteForUser(uid, pathA));
        Assert.Null(ViewProfile.FindByUserIdAndTypePath(uid, pathA));
        Assert.NotNull(ViewProfile.FindByUserIdAndTypePath(uid, pathB));
    }

    [Fact(DisplayName = "ViewProfile：ViewsJson/ActiveViewId upsert 持久化")]
    public void ViewProfile_NamedViews_Upsert()
    {
        var uid = (Environment.TickCount & 0x0FFF_FFFF) | 0x2100_0000;
        var path = "Admin/Menu";
        var views = """[{"id":"default","name":"列表","view":"table","columns":[{"key":"Name","visible":true}]}]""";

        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel
        {
            TypePath = path,
            View = "table",
            ViewsJson = views,
            ActiveViewId = "default",
            ColumnsJson = """[{"key":"Name","visible":true}]""",
            Version = 1,
        });

        var saved = ViewProfile.FindByUserIdAndTypePath(uid, path);
        Assert.NotNull(saved);
        Assert.Equal("default", saved.ActiveViewId);
        Assert.Contains("列表", saved.ViewsJson);
    }

    [Fact(DisplayName = "ViewProfile：PageSize 字段由 Cube.xml 生成并映射")]
    public void ViewProfile_PageSize_FieldMapped()
    {
        Assert.Equal("PageSize", ViewProfile.__.PageSize);
        var f = ViewProfile.Meta.Table.FindByName("PageSize");
        Assert.NotNull(f);
        Assert.Equal(typeof(Int32), f.Type);
    }

    [Fact(DisplayName = "ViewProfile：PageSize 按 PAGE_SIZE_OPTIONS 归一化，0/非法不覆盖已有配置")]
    public void ViewProfile_PageSize_Normalize()
    {
        var uid = (Environment.TickCount & 0x0FFF_FFFF) | 0x2200_0000;
        var path = "Admin/User";

        // 合法选项值保存
        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel { TypePath = path, PageSize = 50, Version = 1 });
        Assert.Equal(50, ViewProfile.FindByUserIdAndTypePath(uid, path)?.PageSize);

        // 非法非选项值归一为 0（未配置）
        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel { TypePath = path, PageSize = 30, Version = 1 });
        Assert.Equal(0, ViewProfile.FindByUserIdAndTypePath(uid, path)?.PageSize);

        // 负数归一为 0
        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel { TypePath = path, PageSize = -5, Version = 1 });
        Assert.Equal(0, ViewProfile.FindByUserIdAndTypePath(uid, path)?.PageSize);

        // 0/缺省表示未配置：不覆盖已有合法配置（旧客户端兼容）
        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel { TypePath = path, PageSize = 100, Version = 1 });
        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel { TypePath = path, PageSize = 0, Version = 1 });
        Assert.Equal(100, ViewProfile.FindByUserIdAndTypePath(uid, path)?.PageSize);
    }

    [Fact(DisplayName = "ViewProfile：FormJson 字段由 Cube.xml 生成并映射")]
    public void ViewProfile_FormJson_FieldMapped()
    {
        Assert.Equal("FormJson", ViewProfile.__.FormJson);
        var f = ViewProfile.Meta.Table.FindByName("FormJson");
        Assert.NotNull(f);
        Assert.Equal(typeof(String), f.Type);
    }

    [Fact(DisplayName = "ViewProfile：FormJson 个人 upsert 持久化；null 不覆盖旧配置")]
    public void ViewProfile_FormJson_Upsert()
    {
        var uid = (Environment.TickCount & 0x0FFF_FFFF) | 0x2300_0000;
        var path = "Admin/User";
        var form = """{"version":1,"edit":{"order":["Name","Code"],"hidden":["Remark"]}}""";

        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel
        {
            TypePath = path,
            FormJson = form,
            Version = 1,
        });
        Assert.Equal(form, ViewProfile.FindByUserIdAndTypePath(uid, path)?.FormJson);

        // null 缺省不覆盖已有 FormJson（旧客户端兼容）
        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel { TypePath = path, Version = 1 });
        Assert.Equal(form, ViewProfile.FindByUserIdAndTypePath(uid, path)?.FormJson);

        // 新值可整体替换
        var form2 = """{"version":1,"detail":{"collapsedCategories":["扩展"]}}""";
        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel { TypePath = path, FormJson = form2, Version = 1 });
        Assert.Equal(form2, ViewProfile.FindByUserIdAndTypePath(uid, path)?.FormJson);
    }

    [Fact(DisplayName = "ViewProfile：SystemJson 配置下 camelCase JSON 可绑定 PascalCase 属性并保存 FormJson")]
    public void ViewProfile_FormJson_CamelCaseDeserialize()
    {
        // 复刻 CubeService 配置（SystemJson.Apply + PropertyNameCaseInsensitive，OSC-0013 修复）：
        // SystemJson.Apply(options, true) 本身不设大小写不敏感，需显式开启，否则前端 camelCase 线缆绑定失败（typePath=null）。
        var options = new System.Text.Json.JsonSerializerOptions();
        NewLife.Serialization.SystemJson.Apply(options, true);
        options.PropertyNameCaseInsensitive = true;

        // 前端 api-core 的 camelCase 线缆
        var json = """{"typePath":"Admin/User","formJson":"{\"version\":1,\"edit\":{\"order\":[\"Name\"],\"hidden\":[\"Remark\"],\"collapsedCategories\":[]}}","filtersJson":"{\"version\":1}","pageSize":100}""";
        var model = System.Text.Json.JsonSerializer.Deserialize<ViewProfileModel>(json, options);

        Assert.NotNull(model);
        Assert.Equal("Admin/User", model.TypePath);
        Assert.Equal(100, model.PageSize);
        Assert.Equal("""{"version":1}""", model.FiltersJson);
        Assert.Contains("Remark", model.FormJson);

        // PascalCase 提交仍兼容（大小写不敏感）
        var jsonPascal = """{"TypePath":"Admin/User","FormJson":"{\"version\":1}","PageSize":50}""";
        var m2 = System.Text.Json.JsonSerializer.Deserialize<ViewProfileModel>(jsonPascal, options);
        Assert.NotNull(m2);
        Assert.Equal("Admin/User", m2.TypePath);
        Assert.Equal(50, m2.PageSize);

        // 绑定后的模型可经 UpsertForUser 持久化 FormJson 与 PageSize
        var uid = (Environment.TickCount & 0x0FFF_FFFF) | 0x2400_0000;
        var saved = ViewProfile.UpsertForUser(uid, model.TypePath, model);
        Assert.NotNull(saved);
        Assert.Equal("Admin/User", saved.TypePath);
        Assert.Equal(100, saved.PageSize);
        Assert.Contains("Remark", saved.FormJson);
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
            ViewProfile.UpsertForUser(0, "Admin/User", new ViewProfileModel { TypePath = "Admin/User" }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EntityComment.AddComment(0, "x", "Admin/User", 1, "c"));
    }

    [Fact(DisplayName = "ViewProfile：全局表单布局（UserId=0）保存/读取/空壳删除，与个人配置隔离")]
    public void ViewProfile_GlobalFormJson_Lifecycle()
    {
        var path = "Admin/GlobalForm" + (Environment.TickCount & 0xFFFF);

        // 初始无全局布局
        Assert.Null(ViewProfile.FindGlobal(path));

        // 管理员保存全局布局 → UserId=0 系统级记录
        var form = """{"version":1,"edit":{"order":["Name","Code"],"hidden":["Remark"],"collapsedCategories":[]}}""";
        var saved = ViewProfile.SaveGlobalFormJson(path, form);
        Assert.NotNull(saved);
        Assert.Equal(ViewProfile.GlobalUserId, saved.UserId);
        Assert.Equal(form, saved.FormJson);

        // FindGlobal 可读（所有用户共享同一份）
        Assert.Equal(form, ViewProfile.FindGlobal(path)?.FormJson);

        // 与个人配置隔离：用户记录不写 FormJson，全局布局不受用户 upsert 影响
        var uid = (Environment.TickCount & 0x0FFF_FFFF) | 0x2500_0000;
        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel { TypePath = path, View = "table", Version = 1 });
        Assert.Null(ViewProfile.FindByUserIdAndTypePath(uid, path)?.FormJson);
        Assert.Equal(form, ViewProfile.FindGlobal(path)?.FormJson);

        // 空壳（无 add/edit/detail 任何模式）等价于恢复默认：删除全局布局
        ViewProfile.SaveGlobalFormJson(path, """{"version":1}""");
        Assert.Null(ViewProfile.FindGlobal(path));
        // 用户个人配置仍在
        Assert.NotNull(ViewProfile.FindByUserIdAndTypePath(uid, path));
    }

    [Fact(DisplayName = "ViewProfile：DeleteGlobalFormJson 删除全局布局；全局记录有其他字段时仅清 FormJson")]
    public void ViewProfile_GlobalFormJson_Delete()
    {
        var path = "Admin/GlobalDel" + (Environment.TickCount & 0xFFFF);

        // 仅有 FormJson 的全局记录：整条删除
        ViewProfile.SaveGlobalFormJson(path, """{"version":1,"detail":{"collapsedCategories":["扩展"]}}""");
        Assert.True(ViewProfile.DeleteGlobalFormJson(path));
        Assert.Null(ViewProfile.FindGlobal(path));

        // 全局记录承载其他字段（未来扩展）：仅清 FormJson，保留记录
        var global = new ViewProfile
        {
            UserId = ViewProfile.GlobalUserId,
            TypePath = path,
            FormJson = """{"version":1,"edit":{"order":["Name"]}}""",
            FiltersJson = """{"version":1}""",
            Version = 1,
        };
        global.Save();
        Assert.True(ViewProfile.DeleteGlobalFormJson(path));
        var left = ViewProfile.FindGlobal(path);
        Assert.NotNull(left);
        Assert.Null(left.FormJson);
        Assert.Equal("""{"version":1}""", left.FiltersJson);
    }

    [Fact(DisplayName = "ViewProfile：全局模板（视图/筛选域）保存/读取/空壳清除，与表单布局共存且与个人隔离")]
    public void ViewProfile_GlobalTemplate_Lifecycle()
    {
        var path = "Admin/Template" + (Environment.TickCount & 0xFFFF);

        // 初始无模板
        Assert.Null(ViewProfile.FindGlobal(path));

        // 管理员保存模板：视图 + 筛选域
        var views = """[{"id":"default","name":"默认列表","view":"table","columns":[{"key":"Name","visible":true}]}]""";
        var filters = """{"version":1,"views":{"default":{"Name":"a"}}}""";
        ViewProfile.SaveGlobalTemplate(path, views, filters);
        var saved = ViewProfile.FindGlobal(path);
        Assert.NotNull(saved);
        Assert.Equal(views, saved.ViewsJson);
        Assert.Equal(filters, saved.FiltersJson);

        // null 不覆盖
        ViewProfile.SaveGlobalTemplate(path, null, null);
        Assert.Equal(views, ViewProfile.FindGlobal(path)?.ViewsJson);
        Assert.Equal(filters, ViewProfile.FindGlobal(path)?.FiltersJson);

        // 空壳清除对应域：视图空数组、筛选空 views map
        ViewProfile.SaveGlobalTemplate(path, "[]", """{"version":1,"views":{}}""");
        var after = ViewProfile.FindGlobal(path);
        Assert.Null(after?.ViewsJson);
        Assert.Null(after?.FiltersJson);

        // 与个人记录隔离：个人 upsert 不写入模板域
        var uid = (Environment.TickCount & 0x0FFF_FFFF) | 0x2600_0000;
        ViewProfile.UpsertForUser(uid, path, new ViewProfileModel { TypePath = path, View = "table", Version = 1 });
        Assert.Null(ViewProfile.FindByUserIdAndTypePath(uid, path)?.ViewsJson);

        // 模板与表单布局共存于同一 UserId=0 记录
        ViewProfile.SaveGlobalTemplate(path, views, filters);
        var form = """{"version":1,"edit":{"order":["Name"],"hidden":[],"collapsedCategories":[]}}""";
        ViewProfile.SaveGlobalFormJson(path, form);
        var both = ViewProfile.FindGlobal(path);
        Assert.Equal(views, both?.ViewsJson);
        Assert.Equal(form, both?.FormJson);

        // 删除模板：保留表单布局（同记录其他域）
        Assert.True(ViewProfile.DeleteGlobalTemplate(path));
        var left = ViewProfile.FindGlobal(path);
        Assert.NotNull(left);
        Assert.Null(left.ViewsJson);
        Assert.Null(left.FiltersJson);
        Assert.Equal(form, left.FormJson);

        // 删除表单布局：整条记录删除（无其他域）；个人记录仍在
        Assert.True(ViewProfile.DeleteGlobalFormJson(path));
        Assert.Null(ViewProfile.FindGlobal(path));
        Assert.NotNull(ViewProfile.FindByUserIdAndTypePath(uid, path));
    }
}
