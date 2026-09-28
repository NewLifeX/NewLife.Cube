using NewLife.Log;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>租户过滤助手</summary>
/// <remarks>
/// 从 <c>ReadOnlyEntityController2.CreateWhere</c> 的多租户分支抽出（OSC-260926c2b8），
/// 作为实体列表与值集等取数出口共用的单一租户规则，避免两套判定漂移。
/// <para>判定顺序：未启用多租户 / 非 ITenantScope 实体 / 管理后台模式 → 不加过滤；</para>
/// <para>Shadow 期无租户上下文 → 兼容放行（影子日志）；Enforce 无租户上下文 → fail-closed；</para>
/// <para>ThrowOnMissingTenant 策略 → 抛 <see cref="NoPermissionException"/>；租户模式 → 按 TenantId 过滤（Tenant 实体按主键）。</para>
/// </remarks>
public static class TenantScopeHelper
{
    #region 辅助
    /// <summary>租户过滤形态</summary>
    private enum TenantFilterKinds
    {
        /// <summary>不加租户过滤</summary>
        None,

        /// <summary>fail-closed 空集</summary>
        Empty,

        /// <summary>按租户标识过滤</summary>
        Tenant,
    }

    /// <summary>解析租户过滤形态。含影子日志/关闭策略副作用，与列表查询保持一致</summary>
    /// <remarks>按请求缓存：BatchLabel 等逐行出口对同一工厂反复解析，缓存避免影子日志写放大与重复租户查询。</remarks>
    /// <param name="factory">实体工厂</param>
    /// <returns>过滤形态与目标租户编号（仅 Tenant 形态有效）</returns>
    private static (TenantFilterKinds Kind, Int32 TenantId) Resolve(IEntityFactory factory)
    {
        if (factory == null) return (TenantFilterKinds.None, 0);

        var http = NewLife.Web.HttpContext.Current;
        var cacheKey = "TenantScope:" + (factory.EntityType?.FullName ?? factory.TableName);
        if (http != null && http.Items[cacheKey] is ValueTuple<TenantFilterKinds, Int32> cached) return cached;

        var result = ResolveCore(factory);
        if (http != null) http.Items[cacheKey] = result;

        return result;
    }

    /// <summary>解析租户过滤形态（核心判定，含日志副作用）</summary>
    /// <param name="factory">实体工厂</param>
    /// <returns>过滤形态与目标租户编号（仅 Tenant 形态有效）</returns>
    private static (TenantFilterKinds Kind, Int32 TenantId) ResolveCore(IEntityFactory factory)
    {
        if (factory == null) return (TenantFilterKinds.None, 0);

        var set = CubeSetting.Current;
        if (!set.EnableTenant) return (TenantFilterKinds.None, 0);
        if (!typeof(ITenantScope).IsAssignableFrom(factory.EntityType)) return (TenantFilterKinds.None, 0);

        var ctxTenant = TenantContext.Current;

        // 无租户上下文（未设置/匿名请求）：
        // [TenantCompat] 影子期规则A：不加租户过滤（等同多租户开启前），仅记录影子日志；
        // Enforce 严格 fail-closed，返回空集，防止无租户场景看到全量数据。
        if (ctxTenant.GetTenantMode() == TenantMode.None)
        {
            if (set.TenantEnforceMode == TenantEnforceModes.Shadow)
            {
                XTrace.WriteLine($"[TenantCompat] 无租户上下文，兼容放行不加过滤：{factory.EntityType.Name}");
                // 数据日志（CreateLog 落库）；无租户上下文场景用户信息非重点
                ManagerProviderHelper.WriteTenantCompatDataLog("影子兼容放行", $"无租户上下文，兼容放行不加过滤 实体[{factory.EntityType.Name}]", null, NewLife.Web.HttpContext.Current?.Connection?.RemoteIpAddress + "");
                return (TenantFilterKinds.None, 0);
            }

            if (set.TenantQueryPolicy == TenantQueryPolicies.ThrowOnMissingTenant)
            {
                // 对外 API 可配置为显式抛错，而不是"假空数据"（P2-7）
                throw new NoPermissionException(PermissionFlags.None, $"缺少租户上下文，禁止查询{factory.EntityType.Name}");
            }

            XTrace.WriteLine($"多租户模式下缺少租户上下文，禁止查询{factory.EntityType.Name}");
            return (TenantFilterKinds.Empty, 0);
        }

        // 管理后台模式，不限制租户数据，管理后台要能看到所有租户的数据
        if (ctxTenant.GetTenantMode() == TenantMode.AdminBackend) return (TenantFilterKinds.None, 0);

        // 租户模式（TenantId>0）：校验租户存在且启用，无效则 fail-closed，防止伪造租户ID绕过数据隔离
        var tenant = ctxTenant.Tenant;
        tenant ??= Tenant.FindById(ctxTenant.TenantId);
        if (tenant == null || !tenant.Enable)
        {
            XTrace.WriteLine($"多租户模式下租户[{ctxTenant.TenantId}]不存在或已禁用，禁止查询{factory.EntityType.Name}");
            return (TenantFilterKinds.Empty, 0);
        }

        return (TenantFilterKinds.Tenant, tenant.Id);
    }
    #endregion

    #region 过滤表达式
    /// <summary>获取租户过滤字符串表达式。供 <c>ReadOnlyEntityController2.CreateWhere</c> 构建 WhereBuilder</summary>
    /// <param name="factory">实体工厂</param>
    /// <returns>表达式文本；null 表示不加过滤。"1=0" 表示 fail-closed；租户模式为 "TenantId={#TenantId}"（Tenant 实体用 "Id={#TenantId}"）</returns>
    public static String? GetFilter(IEntityFactory factory)
    {
        var (kind, tenantId) = Resolve(factory);
        switch (kind)
        {
            case TenantFilterKinds.Empty:
                return "1=0";
            case TenantFilterKinds.Tenant:
                // 租户编号写入请求项，WhereBuilder 的 {#TenantId} 从 HttpContext.Items 取值
                var http = NewLife.Web.HttpContext.Current;
                if (http != null) http.Items["TenantId"] = tenantId;

                return factory.EntityType == typeof(Tenant) ? "Id={#TenantId}" : "TenantId={#TenantId}";
            default:
                return null;
        }
    }

    /// <summary>获取租户过滤查询表达式。供值集 Guard 直接 AND 进实体查询</summary>
    /// <param name="factory">实体工厂</param>
    /// <returns>查询表达式；null 表示不加过滤；fail-closed 时为恒假的 "1=0"</returns>
    public static Expression? GetExpression(IEntityFactory factory)
    {
        var (kind, tenantId) = Resolve(factory);
        switch (kind)
        {
            case TenantFilterKinds.Empty:
                return new Expression("1=0");
            case TenantFilterKinds.Tenant:
                var field = factory.EntityType == typeof(Tenant)
                    ? factory.Unique
                    : factory.Table.FindByName(nameof(ITenantScope.TenantId));
                return field?.Equal(tenantId);
            default:
                return null;
        }
    }
    #endregion

    #region 单行判定
    /// <summary>单行租户判定。与列表过滤表达式同源，供 BatchLabel 等逐行出口使用</summary>
    /// <param name="factory">实体工厂</param>
    /// <param name="entity">实体对象</param>
    /// <returns>true 表示该行在租户范围内；entity 为空返回 false</returns>
    public static Boolean CanAccess(IEntityFactory factory, Object entity)
    {
        if (entity == null) return false;

        var (kind, tenantId) = Resolve(factory);
        return kind switch
        {
            TenantFilterKinds.None => true,
            TenantFilterKinds.Empty => false,
            _ => entity is ITenantScope ts && ts.TenantId == tenantId,
        };
    }
    #endregion
}
