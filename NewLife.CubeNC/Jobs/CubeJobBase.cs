using NewLife.Serialization;

namespace NewLife.Cube.Jobs;

/// <summary>CronJob作业基类</summary>
/// <typeparam name="TArgument"></typeparam>
public abstract class CubeJobBase<TArgument> : ICubeJob where TArgument : class, new()
{
    /// <summary>执行</summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    public virtual async Task<String> Execute(String argument)
    {
        var arg = new TArgument();
        if (!argument.IsNullOrEmpty())
        {
            arg = argument.ToJsonEntity(typeof(TArgument)) as TArgument;
        }

        return await OnExecute(arg);
    }

    /// <summary>执行</summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected abstract Task<String> OnExecute(TArgument argument);
}
