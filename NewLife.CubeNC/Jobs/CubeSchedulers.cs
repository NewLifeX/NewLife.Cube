using NewLife.Cube.Entity;
using NewLife.Reflection;

namespace NewLife.Cube.Jobs;

/// <summary>
/// 调度器
/// </summary>
public class CubeSchedulers
{
    /// <summary>
    /// 扫描并添加作业
    /// </summary>
    public async Task ScanJobsAsync()
    {
        var jobs = typeof(ICubeJob).GetAllSubclasses().Select(e => e.CreateInstance() as ICubeJob).ToList();

        if (jobs == null)
            return;

        foreach (var job in jobs)
        {
            if (job is not CubeJobBase DhJob)
                throw new InvalidOperationException("NewLife.Cube.Jobs.CubeJobBase派生");

            var Name = DhJob.GetJobName();
            if (Name.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("作业名称为空");
            }

            var cron = DhJob.GetCron();
            if (cron.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Cron表达工为空");
            }

            var model = CronJob.FindByName(Name);
            if (model != null)
            {
                throw new InvalidOperationException("已存在指定名称的作业");
            }

            var executeMethod = job?.GetType().GetMethod("Execute");

            model = new CronJob();
            model.Name = Name;
            model.Cron = cron;
            model.DisplayName = executeMethod.GetDisplayName();
            model.Method = $"{executeMethod?.DeclaringType?.FullName}.{executeMethod?.Name}";
            model.Enable = true;
            model.Remark = executeMethod.GetDescription();
            model.Insert();

            await Task.CompletedTask;
        }
    }
}
