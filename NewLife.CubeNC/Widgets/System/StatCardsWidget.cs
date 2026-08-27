using System.Diagnostics;
using System.IO;

namespace NewLife.Cube.Widgets.System;

/// <summary>系统概览。CPU/内存/磁盘/运行时长等数字指标卡片</summary>
[Widget("StatCards", "系统概览", Icon = "fa-tachometer", Cols = 12, Sort = 10, Category = "系统", AdminOnly = true)]
public class StatCardsWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    /// <returns>统计指标匿名对象</returns>
    public Object GetData()
    {
        var mi = MachineInfo.Current ?? new MachineInfo();
        var total = mi.Memory / 1024 / 1024;
        var avail = mi.AvailableMemory / 1024 / 1024;

        // 磁盘总容量与剩余，单位 GB
        Int64 diskTotal = 0, diskFree = 0;
        try
        {
            foreach (var di in DriveInfo.GetDrives())
            {
                if (!di.IsReady) continue;
                diskTotal += di.TotalSize;
                diskFree += di.TotalFreeSpace;
            }
        }
        catch { }

        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        var process = Process.GetCurrentProcess();

        return new
        {
            CpuRate = mi.CpuRate,
            Temperature = mi.Temperature,
            MemoryTotal = total,
            MemoryUsed = total - avail,
            MemoryUsedRate = total <= 0 ? 0d : (Double)(total - avail) / total,
            DiskTotal = diskTotal / 1024 / 1024 / 1024,
            DiskFree = diskFree / 1024 / 1024 / 1024,
            DiskUsedRate = diskTotal <= 0 ? 0d : (Double)(diskTotal - diskFree) / diskTotal,
            Uptime = uptime.ToString(@"dd\.hh\:mm\:ss"),
            ThreadCount = process.Threads.Count,
            OSName = mi.OSName,
        };
    }
}
