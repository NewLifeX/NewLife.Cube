using NewLife.Cube.Services;
using Xunit;

namespace NewLife.Cube.Tests.Services;

public class LovLabelQueryTests
{
    [Fact]
    public void PendingEmpty_DoesNotQuery()
    {
        var plan = LovLabelQuery.PlanListLabelQuery("Id", ["Id"], 0, true);
        Assert.Equal("none", plan.Mode);
        Assert.Equal(0, plan.MaxPages);
    }

    [Fact]
    public void SearchFieldMatchesValueField_UsesKeyedOneShot()
    {
        var plan = LovLabelQuery.PlanListLabelQuery("id", ["Name", "ID"], 12, true);
        Assert.Equal("keyed", plan.Mode);
        Assert.Equal(1, plan.MaxPages);
        Assert.Equal(12, plan.PageSize);
    }

    [Fact]
    public void KeyedPageSize_CapsAt500()
    {
        var plan = LovLabelQuery.PlanListLabelQuery("Id", ["Id"], 800, true);
        Assert.Equal("keyed", plan.Mode);
        Assert.Equal(500, plan.PageSize);
    }

    [Fact]
    public void PageableScan_UsesTenPages()
    {
        var plan = LovLabelQuery.PlanListLabelQuery("Id", ["Name"], 4, true);
        Assert.Equal("scan", plan.Mode);
        Assert.Equal(LovLabelQuery.ListLabelScanPages, plan.MaxPages);
        Assert.Equal(10, plan.MaxPages);
        Assert.Equal(500, plan.PageSize);
    }

    [Fact]
    public void NonPageableScan_UsesOnePage()
    {
        var plan = LovLabelQuery.PlanListLabelQuery("Id", ["Name"], 30, false);
        Assert.Equal("scan", plan.Mode);
        Assert.Equal(1, plan.MaxPages);
        Assert.Equal(200, plan.PageSize);

        var large = LovLabelQuery.PlanListLabelQuery("Id", null, 900, false);
        Assert.Equal(500, large.PageSize);
    }
}
