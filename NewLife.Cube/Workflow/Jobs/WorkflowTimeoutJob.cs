using System.ComponentModel;
using NewLife.Cube.Jobs;
using NewLife.Log;

namespace NewLife.Cube.Workflow;

/// <summary>审批超时扫描。每 5 分钟对到期任务按节点策略 pass/reject/transfer</summary>
[DisplayName("审批超时扫描")]
[Description("扫描到期审批任务并执行超时动作")]
[CronJob("WorkflowTimeoutTick", "0 */5 * * * ?", Enable = true)]
public class WorkflowTimeoutJob : CubeJobBase
{
    /// <summary>执行超时扫描</summary>
    /// <param name="argument">作业参数</param>
    /// <returns>处理汇总</returns>
    public override Task<String> Execute(String argument)
    {
        try
        {
            var rs = WorkflowEngine.TimeoutTick();
            XTrace.WriteLine("审批超时扫描：{0}", rs);
            return Task.FromResult(rs);
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
            return Task.FromResult("fail: " + ex.Message);
        }
    }
}
