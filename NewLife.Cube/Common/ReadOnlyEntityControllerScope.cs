using Microsoft.AspNetCore.Mvc;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>只读实体控制器基类（字段脱敏部分）</summary>
/// <remarks>
/// 本文件被 NewLife.Cube（WebAPI）与 NewLife.CubeNC（MVC）双栈共同编译（后者以 &lt;Compile Include&gt; Link），
/// 保证「敏感字段遮蔽」在两栈行为一致；行权接线见同目录 ReadOnlyEntityController2.cs。
/// </remarks>
public partial class ReadOnlyEntityController<TEntity>
{
    #region 字段脱敏（OSC-2608273d95）
    /// <summary>当前用户不可见（需脱敏）的敏感字段名。实体未实现 IFieldScope 或用户已获敏感查看权限时返回空数组</summary>
    /// <remarks>
    /// 上下文必须取真实用户（<see cref="GetDataScopeContext"/>），<b>不得</b>用 DataScopeContext.Current：
    /// 宿主系统态 ViewSensitive 恒为 false，会把所有人的敏感字段一起脱敏。
    /// 列级按角色、行级按归属：本人行仍由 FieldScopeHelper 行内判定不遮蔽。
    /// </remarks>
    /// <returns>需脱敏的字段名数组；无需脱敏时为空数组</returns>
    [NonAction]
    protected virtual String[] GetSensitiveFieldNames()
    {
        // 缓存键带上实体类型，避免同请求内不同实体（部件/AI 复用上下文）串味
        var key = $"CubeSensitiveFields:{typeof(TEntity).FullName}";
        if (HttpContext?.Items[key] is String[] cached) return cached;

        var names = new List<String>();
        // 上下文为空（未登录）时按无权限处理，仍标记敏感列
        if (GetDataScopeContext()?.ViewSensitive != true && new TEntity() is IFieldScope fieldScope)
        {
            var fs = fieldScope.GetSensitiveFields();
            if (fs != null) names.AddRange(fs);
        }

        var arr = names.ToArray();
        if (HttpContext != null) HttpContext.Items[key] = arr;
        return arr;
    }

    /// <summary>遮蔽单实体敏感字段。角色未授 ViewSensitive 且非本人时，敏感列置 ***</summary>
    /// <param name="entity">实体对象</param>
    [NonAction]
    protected virtual void MaskSensitiveFields(TEntity entity)
    {
        if (entity is IFieldScope) FieldScopeHelper.MaskSensitiveFields((IEntity)entity, GetDataScopeContext());
    }

    /// <summary>当前登录用户是否为系统角色。导出附加数据集等旁路操作的门禁判定</summary>
    /// <returns>true 表示系统角色（未登录返回 false）</returns>
    [NonAction]
    protected virtual Boolean IsSystemUser() => ManageProvider.User?.Roles.Any(e => e.IsSystem) == true;

    /// <summary>遮蔽列表敏感字段，返回可继续枚举的列表</summary>
    /// <remarks>惰性序列先物化，避免后续多次枚举重复查询；实体未实现 IFieldScope 时原样返回。</remarks>
    /// <param name="list">实体列表</param>
    /// <returns>已遮蔽的列表；元素为同一批实体对象引用</returns>
    [NonAction]
    protected virtual IList<TEntity> MaskSensitiveList(IEnumerable<TEntity> list)
    {
        if (list == null) return null;

        var items = list as IList<TEntity> ?? list.ToList();
        if (!typeof(IFieldScope).IsAssignableFrom(typeof(TEntity))) return items;

        FieldScopeHelper.MaskSensitiveFields(items, GetDataScopeContext());
        return items;
    }

    /// <summary>导出数据并遮蔽敏感字段。逐行惰性遮蔽，兼容派生类重写 ExportData</summary>
    /// <param name="max">最大行数</param>
    /// <returns>已遮蔽的实体序列</returns>
    [NonAction]
    protected virtual IEnumerable<TEntity> ExportDataMasked(Int32 max = 0)
    {
        foreach (var entity in ExportData(max))
        {
            if (entity is IFieldScope) FieldScopeHelper.MaskSensitiveFields((IEntity)entity, GetDataScopeContext());
            yield return entity;
        }
    }
    #endregion
}
