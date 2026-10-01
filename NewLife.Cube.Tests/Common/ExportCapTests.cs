using NewLife.Cube.Common;
using Xunit;

namespace NewLife.Cube.Tests.Common;

public class ExportCapTests
{
    [Fact]
    public void RequestedWithinCap_UsesRequested()
    {
        Assert.Equal(20_000, ExportCap.ResolveExportCap(20_000, 1_000_000));
    }

    [Fact]
    public void RequestedAboveCap_UsesHardCap()
    {
        Assert.Equal(1_000_000, ExportCap.ResolveExportCap(2_000_000, 100));
        Assert.Equal(1_000_000, ExportCap.ExportHardCap);
    }

    [Fact]
    public void MissingRequest_UsesPositiveSettingWithinCap()
    {
        Assert.Equal(5_000, ExportCap.ResolveExportCap(0, 5_000));
        Assert.Equal(5_000, ExportCap.ResolveExportCap(-1, 5_000));
    }

    [Fact]
    public void MissingOrOversizedSetting_UsesHardCap()
    {
        Assert.Equal(1_000_000, ExportCap.ResolveExportCap(0, 0));
        Assert.Equal(1_000_000, ExportCap.ResolveExportCap(0, -1));
        Assert.Equal(1_000_000, ExportCap.ResolveExportCap(0, 10_000_000));
    }
}
