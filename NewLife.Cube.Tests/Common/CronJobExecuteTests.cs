using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Areas.Cube.Controllers;
using NewLife.Cube.Entity;
using NewLife.Cube.Tests.Membership;
using NewLife.Serialization;
using NewLife.Web;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// SYS-7 定时作业调度测试。覆盖"马上执行"把启用作业的下一次时间置为当前并返回成功、停用作业不受影响、作业不存在时安全返回。
/// </summary>
[Collection("TenantAuth")]
public class CronJobExecuteTests
{
    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public CronJobExecuteTests(TenantAuthFixture fixture) { }

    #region 辅助
    private static CronJobController CreateController()
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            var controller = new CronJobController();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            return controller;
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    private static String Unique(String prefix) => prefix + "_" + Guid.NewGuid().ToString("N")[..8];

    private static CronJob AddJob(String name, Boolean enable = true)
    {
        var tomorrow = DateTime.Now.AddDays(1);
        var job = new CronJob
        {
            Name = name,
            Cron = "0 0 8 * * *",
            Method = "NewLife.Cube.Tests.Common.CronJobExecuteTests.Noop",
            Enable = enable,
            NextTime = tomorrow,
        };
        job.Insert();

        return job;
    }

    /// <summary>作业方法占位，供作业登记校验（不被调度执行）</summary>
    public static void Noop(String argument) { }

    private static Dictionary<String, Object> ReadJson(ActionResult rs)
    {
        var result = Assert.IsType<ContentResult>(rs);
        var json = new JsonParser(result.Content!).Decode() as IDictionary<String, Object>;

        return json == null ? [] : new Dictionary<String, Object>(json);
    }
    #endregion

    [Fact(DisplayName = "定时作业_马上执行把启用作业的下一次时间置为当前")]
    public void ExecuteNow_EnabledJob_SetsNextTimeToNow()
    {
        var job = AddJob(Unique("Exec"));
        var tomorrow = job.NextTime;
        Assert.True(tomorrow > DateTime.Now.AddHours(1));

        var ctrl = CreateController();
        var rs = ctrl.ExecuteNow(job.Id + "");

        Assert.Equal(0, ReadJson(rs)["code"].ToInt());
        var found = CronJob.FindById(job.Id);
        Assert.NotNull(found);
        Assert.True(found!.NextTime <= DateTime.Now.AddSeconds(1));
        Assert.True(found.NextTime > DateTime.Now.AddMinutes(-1));
    }

    [Fact(DisplayName = "定时作业_停用作业马上执行不改变下次时间")]
    public void ExecuteNow_DisabledJob_NoChange()
    {
        var job = AddJob(Unique("Disa"), false);
        var planned = job.NextTime;

        var ctrl = CreateController();
        var rs = ctrl.ExecuteNow(job.Id + "");

        Assert.Equal(0, ReadJson(rs)["code"].ToInt());
        Assert.Equal(planned, CronJob.FindById(job.Id)!.NextTime);
    }

    [Fact(DisplayName = "定时作业_作业不存在时安全返回")]
    public void ExecuteNow_MissingJob_NoThrow()
    {
        var ctrl = CreateController();

        var rs = ctrl.ExecuteNow("999999");

        Assert.Equal(0, ReadJson(rs)["code"].ToInt());
    }
}
