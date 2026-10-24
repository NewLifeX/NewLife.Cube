using System.Linq;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using XCode.Code;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>搜索区默认算法测试（API 版 FieldCollection）</summary>
/// <remarks>
/// MVC 兼容层仍在旧版 FieldCollection 中保留自己的搜索字段算法；
/// 本测试只钉住 API 版 `/meta.search` 使用的默认规则。
/// </remarks>
public class SearchFieldCollectionTests
{
    [Fact(DisplayName = "Search 默认算法：关键字第一项，索引列与特殊字段合并，主键/无索引列不参与")]
    public void SearchFields_DefaultAlgorithm_MergesIndexedAndSpecialFields()
    {
        var fields = new FieldCollection(User.Meta.Factory, ViewKinds.Search);

        // 关键字固定第一项，供前端收敛为 q/keyword
        Assert.Equal("Q", fields[0].Name);

        // 默认算法输出的索引/特殊字段全部合并进搜索区（顺序按实体字段顺序）
        var expected = new SearchBuilder(User.Meta.Factory.Table.DataTable).GetColumns();
        Assert.NotEmpty(expected);
        foreach (var column in expected)
        {
            Assert.Contains(fields, f => f.Name.EqualIgnoreCase(column.Name));
        }

        // 无索引的普通字符串列不参与
        Assert.DoesNotContain(fields, f => f.Name == "Password");

        // 主键与自增不参与
        Assert.DoesNotContain(fields, f => f.Name == "ID");

        // 无索引的布尔字段作为特殊字段补入
        Assert.Contains(fields, f => f.Name == "Enable");
    }
}
