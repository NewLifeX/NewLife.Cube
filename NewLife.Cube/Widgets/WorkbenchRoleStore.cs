using XCode.Membership;

namespace NewLife.Cube.Widgets;

/// <summary>角色工作台模板。Parameter(UserID=0, Category=Workbench.Role, Name={roleId}) 仅写 LongValue。</summary>
public static class WorkbenchRoleStore
{
    /// <summary>字典分类</summary>
    public const String Category = "Workbench.Role";

    /// <summary>读取角色 JSON（只看 LongValue）</summary>
    public static String Get(Int32 roleId)
    {
        if (roleId <= 0) return null;
        var p = Parameter.FindByUserIDAndCategoryAndName(0, Category, roleId + "");
        return p?.LongValue;
    }

    /// <summary>保存角色 JSON 到 LongValue，清空短 Value</summary>
    public static void Save(Int32 roleId, String json)
    {
        if (roleId <= 0) throw new ArgumentOutOfRangeException(nameof(roleId));
        var p = Parameter.GetOrAdd(0, Category, roleId + "");
        p.Kind = ParameterKinds.String;
        p.Enable = true;
        p.Value = null;
        p.LongValue = json ?? "";
        p.Save();
    }

    /// <summary>清除角色模板</summary>
    public static void Clear(Int32 roleId)
    {
        if (roleId <= 0) return;
        var p = Parameter.FindByUserIDAndCategoryAndName(0, Category, roleId + "");
        if (p == null) return;
        p.Value = null;
        p.LongValue = null;
        p.Save();
    }
}
