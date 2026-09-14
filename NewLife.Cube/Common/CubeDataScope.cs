using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>魔方数据权限接线助手</summary>
/// <remarks>
/// 行权（OSC-2608273d95）：合并架构下实体层以宿主系统身份运行（DataScope 拦截器休眠），行权由各出口<b>显式</b>接线。
/// 实体页面出口见 <c>ReadOnlyEntityController2.GetDataScopeExpression/CanAccess</c>；
/// 页面之外的出口（工作台部件、AI 记录上下文、批量导入）统一走本类，避免各出口各行其是导致遗漏。
/// <para>
/// 上下文一律由当前登录用户显式构造，<b>不写</b> <see cref="DataScopeContext.Current"/>，
/// 以保持实体层拦截器休眠（宿主内实体代码不会被隐式收窄）。
/// </para>
/// </remarks>
public static class CubeDataScope
{
    #region 上下文
    /// <summary>构造数据权限上下文</summary>
    /// <param name="user">登录用户</param>
    /// <returns>数据权限上下文；user 为空时返回 null（行权不介入）</returns>
    public static DataScopeContext? Create(IUser? user) => user == null ? null : DataScopeContext.Create(user);

    /// <summary>获取当前请求的数据权限上下文</summary>
    /// <returns>数据权限上下文；未登录返回 null（行权不介入）</returns>
    public static DataScopeContext? GetCurrent() => Create(ManageProvider.User);
    #endregion

    #region 过滤与判定
    /// <summary>按实体工厂获取行权过滤表达式</summary>
    /// <param name="factory">实体工厂</param>
    /// <param name="context">数据权限上下文，为空时取当前登录用户</param>
    /// <returns>行权表达式；未登录、系统态、全部范围或实体未实现归属接口时返回 null（不限制）</returns>
    public static Expression? GetFilter(IEntityFactory factory, DataScopeContext? context = null)
    {
        context ??= GetCurrent();
        return DataScopeHelper.GetFilter(factory, context);
    }

    /// <summary>合并行权条件与业务条件</summary>
    /// <param name="expression">业务条件</param>
    /// <param name="scope">行权条件</param>
    /// <returns>合并后的条件；两者皆空返回 null</returns>
    public static Expression? Merge(Expression? expression, Expression? scope)
    {
        if (expression == null) return scope;
        if (scope == null) return expression;
        return expression & scope;
    }

    /// <summary>实体归属是否已声明</summary>
    /// <remarks>用户标识或部门标识任一大于 0 即视为已声明；全为 0 表示未声明，交由保存链路赋值</remarks>
    /// <param name="entity">实体对象</param>
    /// <returns>true 表示已声明归属</returns>
    public static Boolean HasOwner(Object entity)
    {
        if (entity is IUserScope us && us.UserId > 0) return true;
        if (entity is IDepartmentScope ds && ds.DepartmentId > 0) return true;
        return false;
    }

    /// <summary>实体是否在当前用户数据范围内</summary>
    /// <param name="entity">实体对象</param>
    /// <param name="context">数据权限上下文，为空时取当前登录用户</param>
    /// <param name="allowUnassigned">归属未声明时是否放行（新增/导入场景为 true，交由保存链路赋值）</param>
    /// <returns>true 表示可访问；实体为空返回 false</returns>
    public static Boolean CanAccess(Object entity, DataScopeContext? context = null, Boolean allowUnassigned = true)
    {
        if (entity == null) return false;

        context ??= GetCurrent();
        if (context == null || context.IsSystem) return true;
        if (allowUnassigned && !HasOwner(entity)) return true;

        return entity switch
        {
            IDataScope ds => DataScopeHelper.CanAccess(ds, context),
            IUserScope us => DataScopeHelper.CanAccess(us, context),
            IDepartmentScope dp => DataScopeHelper.CanAccess(dp, context),
            _ => true,
        };
    }

    /// <summary>实体声明的归属是否指向他人</summary>
    /// <remarks>
    /// 与 <c>ReadOnlyEntityController2.ValidPermission</c> 对新增的判定一致：只比对用户标识列，
    /// 用于「新增/导入不得声称为他人数据」的防伪造校验（部门列按范围判定，见 <see cref="CanAccess"/>）。
    /// </remarks>
    /// <param name="entity">实体对象</param>
    /// <param name="context">数据权限上下文，为空时取当前登录用户</param>
    /// <returns>true 表示归属为他人（应拒绝）</returns>
    public static Boolean IsForgedOwner(Object entity, DataScopeContext? context = null)
    {
        if (entity is not IUserScope us || us.UserId <= 0) return false;

        context ??= GetCurrent();
        if (context == null || context.IsSystem) return false;

        return us.UserId != context.UserId;
    }
    #endregion
}
