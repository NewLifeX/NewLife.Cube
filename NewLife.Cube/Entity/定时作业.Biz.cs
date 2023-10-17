using System;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Data;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Entity;

/// <summary>定时作业。定时执行任务</summary>
public partial class CronJob : Entity<CronJob>
{
    #region 对象操作
    static CronJob()
    {
        // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
        //var df = Meta.Factory.AdditionalFields;
        //df.Add(nameof(Kind));

        // 过滤器 UserModule、TimeModule、IPModule
        Meta.Modules.Add<UserModule>();
        Meta.Modules.Add<TimeModule>();
        Meta.Modules.Add<IPModule>();

        // 单对象缓存
        var sc = Meta.SingleCache;
        sc.FindSlaveKeyMethod = k => Find(_.Name == k);
        sc.GetSlaveKeyMethod = e => e.Name;
    }

    /// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>
    /// <param name="isNew">是否插入</param>
    public override void Valid(Boolean isNew)
    {
        // 如果没有脏数据，则不需要进行任何处理
        if (!HasDirty) return;

        // 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
        if (Name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Name), "名称不能为空！");

        // 建议先调用基类方法，基类方法会做一些统一处理
        base.Valid(isNew);
    }

    /// <summary>已重载。优先显示名</summary>
    /// <returns></returns>
    public override String ToString() => !DisplayName.IsNullOrEmpty() ? DisplayName : Name;
    #endregion

    #region 扩展属性
    #endregion

    #region 扩展查询
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static CronJob FindById(Int32 id)
    {
        if (id <= 0) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

        //// 单对象缓存
        //return Meta.SingleCache[id];

        return Find(_.Id == id);
    }

    /// <summary>根据名称查找</summary>
    /// <param name="name">名称</param>
    /// <returns>实体对象</returns>
    public static CronJob FindByName(String name)
    {
        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Name.EqualIgnoreCase(name));

        // 单对象缓存
        //return Meta.SingleCache.GetItemWithSlaveKey(name) as CronJob;

        return Find(_.Name == name);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="name">名称</param>
    /// <param name="start">更新时间开始</param>
    /// <param name="end">更新时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<CronJob> Search(String name, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (!name.IsNullOrEmpty()) exp &= _.Name == name;
        exp &= _.UpdateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= _.DisplayName.Contains(key) | _.Method.Contains(key) | _.Argument.Contains(key) | _.Remark.Contains(key) | _.CreateIP.Contains(key) | _.UpdateIP.Contains(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 业务操作
    /// <summary>检查并添加定时作业</summary>
    /// <param name="name">作业名称。唯一</param>
    /// <param name="method">作业方法。定时执行</param>
    /// <param name="cron">Cron表达式。仅用于首次创建，后续可通过页面修改</param>
    /// <param name="enable">创建时是否启用。后续可通过页面修改</param>
    /// <returns></returns>
    public static CronJob Add(String name, MethodInfo method, String cron, Boolean enable = true)
    {
        if (method == null || !method.IsStatic) throw new ArgumentOutOfRangeException(nameof(method), "定时作业执行方法必须是带有单个String参数的静态方法。");

        if (name.IsNullOrEmpty()) name = method.Name;
        var job = FindByName(name);
        if (job != null) return job;

        job = new CronJob
        {
            Name = name,
            DisplayName = method.GetDisplayName(),
            Method = $"{method.DeclaringType.FullName}.{method.Name}",
            Cron = cron,
            Enable = enable,
            EnableLog = true,
            Remark = method.GetDescription(),
        };

        job.Insert();

        return job;
    }

    /// <summary>检查并添加定时作业</summary>
    /// <param name="name">作业名称。唯一</param>
    /// <param name="action">作业回调</param>
    /// <param name="cron">Cron表达式。仅用于首次创建，后续可通过页面修改</param>
    /// <param name="enable">创建时是否启用。后续可通过页面修改</param>
    /// <returns></returns>
    public static CronJob Add(String name, Action<String> action, String cron, Boolean enable = true) => Add(name, action.Method, cron, enable);

    ///// <summary>添加定时作业</summary>
    ///// <param name="name"></param>
    ///// <param name="action"></param>
    ///// <param name="cron"></param>
    ///// <param name="enable"></param>
    ///// <returns></returns>
    //public static CronJob Add(String name, Action<CronJob> action, String cron, Boolean enable = true) => Add(name, action.Method, cron, enable);
    #endregion
}