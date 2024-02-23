namespace NewLife.Cube.Jobs;

/// <summary>Cron定时作业特性</summary>
public class CronJobAttribute(String name, String cron) : Attribute
{
    /// <summary>名称。作业唯一名</summary>
    public String Name { get; set; } = name;

    /// <summary>Cron表达式。仅用于创建作业，后续以用户修改后的为准</summary>
    public String Cron { get; set; } = cron;
}
