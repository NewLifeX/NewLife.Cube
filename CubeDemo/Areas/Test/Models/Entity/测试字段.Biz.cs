using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace CubeDemo.Areas.Test;

public partial class 测试字段 : Entity<测试字段>
{
    #region 对象操作
    static 测试字段()
    {
        // 过滤器 UserModule、TimeModule、IPModule
        Meta.Modules.Add<UserModule>();
        Meta.Modules.Add<TimeModule>();
        Meta.Modules.Add<IPModule>();
    }

    /// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>
    /// <param name="isNew">是否插入</param>
    public override void Valid(Boolean isNew)
    {
        // 如果没有脏数据，则不需要进行任何处理
        if (!HasDirty) return;

        // 建议先调用基类方法，基类方法会做一些统一处理
        base.Valid(isNew);
    }
    #endregion

    #region 扩展查询
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static 测试字段 FindById(Int32 id)
    {
        if (id <= 0) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

        // 单对象缓存
        return Meta.SingleCache[id];
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<测试字段> Search(String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (!key.IsNullOrEmpty())
            exp &= _.ShortText.Contains(key) | _.LongText.Contains(key) | _.MailVal.Contains(key) | _.MobileVal.Contains(key) | _.UrlVal.Contains(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 业务操作
    /// <summary>转为模型对象</summary>
    /// <returns></returns>
    public 测试字段Model ToModel()
    {
        var model = new 测试字段Model();
        model.Copy(this);

        return model;
    }
    #endregion
}
