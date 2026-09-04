using NewLife.Cube.Entity;
using NewLife.Log;
using XCode;

namespace NewLife.Cube;

/// <summary>在线高峰告警服务。当前在线数超过近7天最高纪录的固定比例时，向系统管理员发送一条系统级站内信广播（UserId=0）；每天最多一条，避免刷屏</summary>
public static class OnlineAlertService
{
    #region 属性
    /// <summary>比较窗口天数。固定取近7天（不含今天）每日最大在线</summary>
    public const Int32 Days = 7;

    /// <summary>触发比例。当前在线数需超过近7天最高纪录的该倍数才告警，1.2表示高出20%</summary>
    public const Double Ratio = 1.2;
    #endregion

    #region 方法
    /// <summary>检查当前在线数是否触发告警。由用户在线统计定时任务在在线数变化时调用</summary>
    /// <param name="total">当前在线数（清理过期会话后的表内总数）</param>
    /// <param name="now">当前时间</param>
    public static void Check(Int32 total, DateTime now) => Check(total, now, CubeSetting.Current);

    /// <summary>检查当前在线数是否触发告警。显式传入魔方设置，便于测试与解耦配置读取</summary>
    /// <param name="total">当前在线数</param>
    /// <param name="now">当前时间</param>
    /// <param name="set">魔方设置。需开启用户在线与用户统计才有可比的历史纪录</param>
    public static void Check(Int32 total, DateTime now, CubeSetting set)
    {
        // 统计未开启时无历史基线可比，直接跳过（EnableUserOnline=0不记录在线，1仅登录，2全部）
        if (set == null || set.EnableUserOnline <= 0 || !set.EnableUserStat) return;

        try
        {
            // 近7天（不含今天）最高纪录
            var max = GetWindowMax(now.Date);
            // 尚无历史纪录（全新系统），先积累基线，不告警
            if (max <= 0) return;
            // 未超过固定比例，不告警
            if (total <= max * Ratio) return;
            // 每天最多一条，避免持续高位刷屏
            if (HasAlertedToday(now)) return;

            Send(total, max, now);
        }
        catch (Exception ex)
        {
            // 告警逻辑不得影响主统计流程
            XTrace.WriteException(ex);
        }
    }

    /// <summary>读取近7天（不含今天）每日最大在线的最高纪录</summary>
    /// <param name="today">今天</param>
    /// <returns>历史最高在线数，无纪录时返回0</returns>
    private static Int32 GetWindowMax(DateTime today)
    {
        var start = today.AddDays(-Days);
        var list = UserStat.FindAll(UserStat._.MaxOnline > 0 & UserStat._.Date >= start & UserStat._.Date < today,
            UserStat._.MaxOnline.Desc(), null, 0, 1);

        return list != null && list.Count > 0 ? list[0].MaxOnline : 0;
    }

    /// <summary>是否今日已发送过在线告警</summary>
    /// <param name="now">当前时间</param>
    /// <returns>是否已发送</returns>
    private static Boolean HasAlertedToday(DateTime now)
        => NotificationRecord.FindCount(NotificationRecord._.Action == "OnlineAlert" & NotificationRecord._.CreateTime >= now.Date) > 0;

    /// <summary>写入系统级站内信告警广播（UserId=0，系统管理员可见，任意一人已读即全体消除）</summary>
    /// <param name="total">当前在线数</param>
    /// <param name="max">近7天最高纪录</param>
    /// <param name="now">当前时间</param>
    private static void Send(Int32 total, Int32 max, DateTime now)
    {
        var record = new NotificationRecord
        {
            Action = "OnlineAlert",
            Channel = "InApp",
            Title = $"在线数高峰预警 {now:MM-dd HH:mm}",
            Content = $"当前在线 {total} 人，超过近 {Days} 天最高纪录 {max} 人（阈值 {max * Ratio:0} 人）。可能是推广活动、爬虫扫描或异常流量，建议查看在线列表与登录日志排查。",
            Success = true,
            Result = $"近{Days}天纪录{max}",
            CreateTime = now,
        };
        record.Insert();

        XTrace.WriteLine("在线高峰预警：当前在线{0}人，超过近{1}天纪录{2}人的{3:0%}", total, Days, max, Ratio - 1);
    }
    #endregion
}
