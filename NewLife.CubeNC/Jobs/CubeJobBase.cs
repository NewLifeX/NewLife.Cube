namespace NewLife.Cube.Jobs;

/// <summary>
/// CronJob作业基类
/// </summary>
public abstract class CubeJobBase : ICubeJob
{
    /// <summary>
    /// 作业名称
    /// </summary>
    private readonly String _jobName;

    /// <summary>
    /// 获取作业名称
    /// </summary>
    public virtual String GetJobName()
    {
        return _jobName;
    }

    /// <summary>
    /// 获取Cron表达式
    /// </summary>
    public virtual String GetCron()
    {
        return null;
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="argument">参数格式</param>
    public abstract void Execute(String argument);
}
