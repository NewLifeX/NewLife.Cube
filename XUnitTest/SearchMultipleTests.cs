using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>搜索多选参数测试。多选搜索项按逗号拆分后走 In，而不是整串等值匹配</summary>
[Collection("SqliteDb")]
public class SearchMultipleTests
{
    private sealed class MultiUserController : ReadOnlyEntityController<User>
    {
        protected override FieldCollection OnGetFields(ViewKinds kind, Object model)
        {
            var fields = base.OnGetFields(kind, model);
            if (kind == ViewKinds.Search)
            {
                var field = fields.GetField("Name") as SearchField;
                if (field == null)
                {
                    field = new SearchField { Name = "Name", DisplayName = "姓名", Type = typeof(String) };
                    fields.Add(field);
                }
                field.Multiple = true;
            }
            return fields;
        }

        public IEnumerable<User> SearchUsers(Pager pager) => Search(pager);

        public FieldCollection GetSearchFields() => OnGetFields(ViewKinds.Search, null);
    }

    [Fact(DisplayName = "多选搜索项按逗号拆分后用 In 查询")]
    public void Search_Multiple_UsesIn()
    {
        SqliteDb.Ensure();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var role = new Role { Name = $"搜索角色_{suffix}", Enable = true };
        role.Insert();

        var names = new[] { $"multi_a_{suffix}", $"multi_b_{suffix}", $"multi_c_{suffix}" };
        try
        {
            foreach (var name in names)
            {
                new User { Name = name, DisplayName = name, RoleID = role.ID, Enable = true }.Insert();
            }

            var controller = new MultiUserController();
            var searchField = controller.GetSearchFields().GetField("Name") as SearchField;
            Assert.NotNull(searchField);
            Assert.True(searchField!.Multiple);

            var pager = new Pager { PageSize = 20 };
            pager["Name"] = $"{names[0]},{names[1]}";

            var users = controller.SearchUsers(pager).ToList();

            Assert.Equal(2, users.Count);
            Assert.Contains(users, e => e.Name == names[0]);
            Assert.Contains(users, e => e.Name == names[1]);
            Assert.DoesNotContain(users, e => e.Name == names[2]);
        }
        finally
        {
            User.Delete(User._.Name.StartsWith("multi_"));
            role.Delete();
        }
    }
}
