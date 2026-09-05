using System.Collections.Concurrent;
using NewLife.Cube.Entity;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>
/// 值集数据存取。枚举值、列表配置、搜索字段、表格列没有真实表，
/// 数据聚合以 JSON 存入 <see cref="Parameter"/> 字典参数表（UserID=0，Category=各类别，Name=值集定义编号）。
/// 原为 4 个 XCode 实体的 .Biz 静态方法，实体去表化后收敛到本类。
/// </summary>
public static class LovStore
{
    // 分类常量。Parameter 记录键：UserID=0 + Category + Name(值集定义编号)
    private const String CategoryEnumItem = "Lov.EnumItem";
    private const String CategoryListConfig = "Lov.ListConfig";
    private const String CategorySearchField = "Lov.SearchField";
    private const String CategoryTableColumn = "Lov.TableColumn";

    // 并发覆盖保护：按值集定义编号加进程内锁，避免同键并发读写互相覆盖
    private static readonly ConcurrentDictionary<Int32, Object> _locks = new();

    #region 枚举值
    /// <summary>读取枚举值列表。按值集定义编号聚合为一条 Parameter 记录</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <returns>枚举值列表</returns>
    public static IList<LovEnumItemModel> FindEnumItems(Int32 lovDefId)
    {
        var json = FindJson(lovDefId, CategoryEnumItem);
        if (json == null) return [];

        return json.ToJsonEntity<List<LovEnumItemModel>>() ?? [];
    }

    /// <summary>保存枚举值列表。整表覆盖为一条 Parameter 记录</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <param name="list">枚举值列表</param>
    /// <returns>影响行数</returns>
    public static Int32 SaveEnumItems(Int32 lovDefId, IList<LovEnumItemModel> list)
        => SaveJson(lovDefId, CategoryEnumItem, list, ParameterKinds.List);
    #endregion

    #region 列表配置
    /// <summary>读取列表配置（单条）。不存在时返回 null</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <returns>列表配置，不存在时返回 null</returns>
    public static LovListConfigModel FindListConfig(Int32 lovDefId)
    {
        var json = FindJson(lovDefId, CategoryListConfig);
        if (json == null) return null;

        return json.ToJsonEntity<LovListConfigModel>();
    }

    /// <summary>保存列表配置。覆盖写入为一条 Parameter 记录</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <param name="config">列表配置</param>
    /// <returns>影响行数</returns>
    public static Int32 SaveListConfig(Int32 lovDefId, LovListConfigModel config)
        => SaveJson(lovDefId, CategoryListConfig, config, ParameterKinds.Hash);
    #endregion

    #region 搜索字段
    /// <summary>读取搜索字段列表。按值集定义编号聚合为一条 Parameter 记录</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <returns>搜索字段列表</returns>
    public static IList<LovSearchFieldModel> FindSearchFields(Int32 lovDefId)
    {
        var json = FindJson(lovDefId, CategorySearchField);
        if (json == null) return [];

        return json.ToJsonEntity<List<LovSearchFieldModel>>() ?? [];
    }

    /// <summary>保存搜索字段列表。整表覆盖为一条 Parameter 记录</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <param name="list">搜索字段列表</param>
    /// <returns>影响行数</returns>
    public static Int32 SaveSearchFields(Int32 lovDefId, IList<LovSearchFieldModel> list)
        => SaveJson(lovDefId, CategorySearchField, list, ParameterKinds.List);
    #endregion

    #region 表格列
    /// <summary>读取表格列列表。按值集定义编号聚合为一条 Parameter 记录</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <returns>表格列列表</returns>
    public static IList<LovTableColumnModel> FindTableColumns(Int32 lovDefId)
    {
        var json = FindJson(lovDefId, CategoryTableColumn);
        if (json == null) return [];

        return json.ToJsonEntity<List<LovTableColumnModel>>() ?? [];
    }

    /// <summary>保存表格列列表。整表覆盖为一条 Parameter 记录</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <param name="list">表格列列表</param>
    /// <returns>影响行数</returns>
    public static Int32 SaveTableColumns(Int32 lovDefId, IList<LovTableColumnModel> list)
        => SaveJson(lovDefId, CategoryTableColumn, list, ParameterKinds.List);
    #endregion

    #region 辅助
    /// <summary>读取指定分类的 Parameter JSON。不存在或无内容时返回 null</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <param name="category">分类。Lov.EnumItem/Lov.ListConfig 等</param>
    /// <returns>JSON 内容，无则返回 null</returns>
    private static String FindJson(Int32 lovDefId, String category)
    {
        var p = Parameter.FindByUserIDAndCategoryAndName(0, category, lovDefId.ToString());
        if (p == null) return null;

        var json = p.Value ?? p.LongValue;
        return json.IsNullOrEmpty() ? null : json;
    }

    /// <summary>把对象序列化写入 Parameter。存什么取什么，含审计字段；JSON 小于 200 存 Value，否则存 LongValue</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <param name="category">分类。Lov.EnumItem/Lov.ListConfig 等</param>
    /// <param name="model">待存储对象（单个模型或模型列表）</param>
    /// <param name="kind">Parameter 数据类型</param>
    /// <returns>影响行数</returns>
    private static Int32 SaveJson(Int32 lovDefId, String category, Object model, ParameterKinds kind)
    {
        var json = model.ToJson();

        var gate = _locks.GetOrAdd(lovDefId, _ => new Object());
        lock (gate)
        {
            var p = Parameter.FindByUserIDAndCategoryAndName(0, category, lovDefId.ToString());
            if (p == null) p = new Parameter { UserID = 0, Category = category, Name = lovDefId.ToString() };

            if (json.Length < 200) { p.Value = json; p.LongValue = null; }
            else { p.Value = null; p.LongValue = json; }

            p.Kind = kind;
            p.Enable = true;
            // 不设 Remark（用户指令）

            return p.Save();
        }
    }
    #endregion
}
