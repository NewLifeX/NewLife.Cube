using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>值集实体行权守卫（OSC-260926c2b8）</summary>
/// <remarks>
/// 值集 <c>entity:</c> 协议与实体 <c>BatchLabel</c> 是实体列表之外的取数出口，此前绕过目标实体的菜单与行权。
/// 本守卫复用与列表相同的判定：目标实体菜单 Detail、租户过滤（<see cref="TenantScopeHelper"/>）、
/// DataScope 行权（<see cref="DataScopeHelper"/>），并按 <c>ReadOnlyEntityController2.GetDataScopeContext</c> 同源构造上下文，
/// 不写 <c>DataScopeContext.Current</c>（实体层拦截器保持休眠）。
/// </remarks>
public static class LovEntityGuard
{
    #region 菜单
    /// <summary>检查目标实体菜单 Detail 权限。找不到菜单、未登录或缺少 Detail 一律失败关闭</summary>
    /// <param name="factory">目标实体工厂</param>
    /// <returns>true 表示当前用户可查看该实体数据</returns>
    public static Boolean CheckMenu(IEntityFactory factory)
    {
        if (factory == null) return false;

        var user = GetUser();
        if (user == null) return false;

        var menu = FindMenu(factory);
        if (menu == null) return false;

        return user.Has(menu, PermissionFlags.Detail);
    }

    /// <summary>按实体类型的已注册页面路径找到菜单（与实体路由一致）</summary>
    /// <param name="factory">目标实体工厂</param>
    /// <returns>菜单；未注册页面或查不到时返回 null</returns>
    private static IMenu FindMenu(IEntityFactory factory)
    {
        var url = EntityPageRegistry.Get(factory.EntityType)?.Url;
        if (url.IsNullOrEmpty()) return null;

        var mf = ManageProvider.Menu;
        return mf?.FindByUrl(url) ?? mf?.FindByUrl("~" + url);
    }
    #endregion

    #region 过滤与判定
    /// <summary>获取租户 + 行权过滤表达式，供 entity: 列表查询 AND 叠加</summary>
    /// <param name="factory">目标实体工厂</param>
    /// <returns>过滤表达式；null 表示无附加限制</returns>
    public static Expression? GetFilter(IEntityFactory factory)
    {
        if (factory == null) return null;

        var exp = TenantScopeHelper.GetExpression(factory);

        // 行权（OSC-2608273d95）：上下文由当前用户显式构造，不用 DataScopeContext.Current 宿主态
        var scope = DataScopeHelper.GetFilter(factory, GetScopeContext());
        if (scope != null) exp = exp == null ? scope : exp & scope;

        return exp;
    }

    /// <summary>单行可见判定。与列表 FindData 同序：先 DataScope 归属，再租户</summary>
    /// <param name="factory">目标实体工厂</param>
    /// <param name="entity">实体行</param>
    /// <returns>true 表示该行可见；entity 为空返回 false</returns>
    public static Boolean CanAccess(IEntityFactory factory, Object entity)
    {
        if (entity == null) return false;

        var ctx = GetScopeContext();
        if (ctx != null)
        {
            var ok = entity switch
            {
                IDataScope ds => DataScopeHelper.CanAccess(ds, ctx),
                IUserScope us => DataScopeHelper.CanAccess(us, ctx),
                IDepartmentScope dp => DataScopeHelper.CanAccess(dp, ctx),
                _ => true,
            };
            if (!ok) return false;
        }

        return TenantScopeHelper.CanAccess(factory, entity);
    }
    #endregion

    #region 辅助
    /// <summary>当前登录用户。请求项优先，再回落 ManageProvider</summary>
    /// <returns>用户；未登录返回 null</returns>
    private static IUser GetUser()
    {
        var http = NewLife.Web.HttpContext.Current;
        var user = http?.Items["CurrentUser"] as IUser;
        return user ?? ManageProvider.User as IUser;
    }

    /// <summary>请求级数据权限上下文。与 ReadOnlyEntityController2.GetDataScopeContext 同源，按请求缓存</summary>
    /// <returns>数据范围上下文；未登录返回 null</returns>
    private static DataScopeContext GetScopeContext()
    {
        var http = NewLife.Web.HttpContext.Current;
        if (http?.Items["DataScopeContext"] is DataScopeContext cached) return cached;

        var user = GetUser();
        if (user == null) return null;

        var ctx = DataScopeContext.Create(user);
        if (ctx != null && http != null) http.Items["DataScopeContext"] = ctx;
        return ctx;
    }
    #endregion
}
