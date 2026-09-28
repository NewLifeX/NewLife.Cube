using NewLife.Cube.ViewModels;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.ViewModels;

/// <summary>排序候选：主键首列与索引最左列标记 Indexed，供前端优先展示。</summary>
public class DataFieldIndexTests
{
    [Fact(DisplayName = "索引最左列与主键首列标记 indexed，非索引列不下发")]
    public void Fill_Marks_OrderByIndexColumns()
    {
        var name = new DataField(Department._.Name);
        Assert.True(name.Indexed);
        Assert.Equal(true, name.ToDictionary()["indexed"]);

        var id = new DataField(Department._.ID);
        Assert.True(id.Indexed);
        Assert.Equal(true, id.ToDictionary()["indexed"]);

        var full = new DataField(Department._.FullName);
        Assert.False(full.Indexed);
        Assert.False(full.ToDictionary().ContainsKey("indexed"));

        // 未建索引的列不优先，避免排序时全表扫描
        var type = new DataField(Department._.Type);
        Assert.False(type.Indexed);
    }
}
