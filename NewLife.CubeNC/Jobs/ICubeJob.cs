namespace NewLife.Cube.Jobs;

/// <summary>
/// CronJob作业
/// </summary>
public interface ICubeJob
{
    /// <summary>执行定时作业</summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    Task<String> Execute(String argument);
}
