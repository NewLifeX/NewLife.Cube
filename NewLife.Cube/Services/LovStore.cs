using System.Collections.Concurrent;
using NewLife.Cube.Entity;
using NewLife.Security;
using NewLife.Serialization;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>
/// 值集数据存取。值集定义（运行时手工"名值定义"）与枚举值、列表配置、搜索字段、表格列没有真实表，
/// 数据聚合以 JSON 存入 <see cref="Parameter"/> 字典参数表（UserID=0，Category=各类别，Name=值集编码 LovCode）。
/// 代码声明的枚举与 [LovList] 列表型值集由 <see cref="LovRegistry"/> 反射提供、不落库；本类仅承载运行时手工创建的数据。
/// 原为 5 个 XCode 实体的 .Biz 静态方法（含 LovDefinition 定义表），去表化后收敛到本类。
/// </summary>
public static class LovStore
{
    // 分类常量。Parameter 记录键：UserID=0 + Category + Name(值集编码)
    private const String CategoryDef = "Lov.Def";
    private const String CategoryEnumItem = "Lov.EnumItem";
    private const String CategoryListConfig = "Lov.ListConfig";
    private const String CategorySearchField = "Lov.SearchField";
    private const String CategoryTableColumn = "Lov.TableColumn";

    // 并发覆盖保护：按值集编码键加进程内锁，避免同键并发读写互相覆盖
    private static readonly ConcurrentDictionary<String, Object> _locks = new();

    // Parameter.Name 列长 50，超长值集编码退化为「前缀_哈希」键（确定性；完整 LovCode 保留在 JSON 内）
    private const Int32 MaxNameLength = 48;

    #region 定义
    /// <summary>读取值集定义（手工/名值定义）。不存在时返回 null</summary>
    /// <param name="lovCode">值集编码</param>
    /// <returns>值集定义，不存在时返回 null</returns>
    public static LovDefModel? FindDef(String lovCode)
    {
        var p = Parameter.FindByUserIDAndCategoryAndName(0, CategoryDef, GetKey(lovCode));
        if (p == null) return null;

        return BuildDef(p);
    }

    /// <summary>读取全部手工值集定义。遍历 Category=Lov.Def 的 Parameter 记录</summary>
    /// <returns>值集定义列表</returns>
    public static IList<LovDefModel> FindAllDefs()
    {
        var list = Parameter.FindAll(Parameter._.UserID == 0 & Parameter._.Category == CategoryDef);
        var models = new List<LovDefModel>();
        foreach (var p in list)
        {
            var model = BuildDef(p);
            if (model != null) models.Add(model);
        }
        return models;
    }

    /// <summary>保存值集定义。覆盖写入为一条 Parameter 记录（Name=LovCode）</summary>
    /// <param name="def">值集定义</param>
    /// <returns>影响行数</returns>
    public static Int32 SaveDef(LovDefModel def)
    {
        if (def == null || def.LovCode.IsNullOrEmpty()) throw new ArgumentNullException(nameof(def));

        return SaveJson(def.LovCode, CategoryDef, def, ParameterKinds.Hash);
    }

    /// <summary>删除值集定义，并级联清理其枚举值/列表配置/搜索字段/表格列</summary>
    /// <param name="lovCode">值集编码</param>
    /// <returns>影响行数</returns>
    public static Int32 DeleteDef(String lovCode)
    {
        if (lovCode.IsNullOrEmpty()) return 0;

        var n = 0;
        foreach (var category in new[] { CategoryDef, CategoryEnumItem, CategoryListConfig, CategorySearchField, CategoryTableColumn })
        {
            n += DeleteJson(lovCode, category);
        }
        return n;
    }
    #endregion

    #region 枚举值
    /// <summary>读取枚举值列表。按值集编码聚合为一条 Parameter 记录</summary>
    /// <param name="lovCode">值集编码</param>
    /// <returns>枚举值列表</returns>
    public static IList<LovEnumItemModel> FindEnumItems(String lovCode)
    {
        var json = FindJson(lovCode, CategoryEnumItem);
        if (json == null) return [];

        return json.ToJsonEntity<List<LovEnumItemModel>>() ?? [];
    }

    /// <summary>保存枚举值列表。整表覆盖为一条 Parameter 记录</summary>
    /// <param name="lovCode">值集编码</param>
    /// <param name="list">枚举值列表</param>
    /// <returns>影响行数</returns>
    public static Int32 SaveEnumItems(String lovCode, IList<LovEnumItemModel> list)
        => SaveJson(lovCode, CategoryEnumItem, list, ParameterKinds.List);
    #endregion

    #region 列表配置
    /// <summary>读取列表配置（单条）。不存在时返回 null</summary>
    /// <param name="lovCode">值集编码</param>
    /// <returns>列表配置，不存在时返回 null</returns>
    public static LovListConfigModel? FindListConfig(String lovCode)
    {
        var json = FindJson(lovCode, CategoryListConfig);
        if (json == null) return null;

        return json.ToJsonEntity<LovListConfigModel>();
    }

    /// <summary>保存列表配置。覆盖写入为一条 Parameter 记录</summary>
    /// <param name="lovCode">值集编码</param>
    /// <param name="config">列表配置</param>
    /// <returns>影响行数</returns>
    public static Int32 SaveListConfig(String lovCode, LovListConfigModel config)
        => SaveJson(lovCode, CategoryListConfig, config, ParameterKinds.Hash);
    #endregion

    #region 搜索字段
    /// <summary>读取搜索字段列表。按值集编码聚合为一条 Parameter 记录</summary>
    /// <param name="lovCode">值集编码</param>
    /// <returns>搜索字段列表</returns>
    public static IList<LovSearchFieldModel> FindSearchFields(String lovCode)
    {
        var json = FindJson(lovCode, CategorySearchField);
        if (json == null) return [];

        return json.ToJsonEntity<List<LovSearchFieldModel>>() ?? [];
    }

    /// <summary>保存搜索字段列表。整表覆盖为一条 Parameter 记录</summary>
    /// <param name="lovCode">值集编码</param>
    /// <param name="list">搜索字段列表</param>
    /// <returns>影响行数</returns>
    public static Int32 SaveSearchFields(String lovCode, IList<LovSearchFieldModel> list)
        => SaveJson(lovCode, CategorySearchField, list, ParameterKinds.List);
    #endregion

    #region 表格列
    /// <summary>读取表格列列表。按值集编码聚合为一条 Parameter 记录</summary>
    /// <param name="lovCode">值集编码</param>
    /// <returns>表格列列表</returns>
    public static IList<LovTableColumnModel> FindTableColumns(String lovCode)
    {
        var json = FindJson(lovCode, CategoryTableColumn);
        if (json == null) return [];

        return json.ToJsonEntity<List<LovTableColumnModel>>() ?? [];
    }

    /// <summary>保存表格列列表。整表覆盖为一条 Parameter 记录</summary>
    /// <param name="lovCode">值集编码</param>
    /// <param name="list">表格列列表</param>
    /// <returns>影响行数</returns>
    public static Int32 SaveTableColumns(String lovCode, IList<LovTableColumnModel> list)
        => SaveJson(lovCode, CategoryTableColumn, list, ParameterKinds.List);
    #endregion

    #region 辅助
    /// <summary>从 Parameter 记录构建值集定义，挂接 Parameter 审计字段（CreateTime 等展示用）</summary>
    /// <param name="p">Parameter 记录</param>
    /// <returns>值集定义，无内容或编码为空时返回 null</returns>
    private static LovDefModel? BuildDef(Parameter p)
    {
        var json = p.Value ?? p.LongValue;
        if (json.IsNullOrEmpty()) return null;

        var model = json.ToJsonEntity<LovDefModel>();
        if (model == null || model.LovCode.IsNullOrEmpty()) return null;

        model.CreateUser = p.CreateUser;
        model.CreateTime = p.CreateTime;
        model.UpdateUser = p.UpdateUser;
        model.UpdateTime = p.UpdateTime;
        return model;
    }

    /// <summary>读取指定分类的 Parameter JSON。不存在或无内容时返回 null</summary>
    /// <param name="lovCode">值集编码</param>
    /// <param name="category">分类。Lov.Def/Lov.EnumItem/Lov.ListConfig 等</param>
    /// <returns>JSON 内容，无则返回 null</returns>
    private static String? FindJson(String lovCode, String category)
    {
        var p = Parameter.FindByUserIDAndCategoryAndName(0, category, GetKey(lovCode));
        if (p == null) return null;

        var json = p.Value ?? p.LongValue;
        return json.IsNullOrEmpty() ? null : json;
    }

    /// <summary>把对象序列化写入 Parameter。存什么取什么，含审计字段；JSON 小于 200 存 Value，否则存 LongValue</summary>
    /// <param name="lovCode">值集编码</param>
    /// <param name="category">分类。Lov.Def/Lov.EnumItem/Lov.ListConfig 等</param>
    /// <param name="model">待存储对象（单个模型或模型列表）</param>
    /// <param name="kind">Parameter 数据类型</param>
    /// <returns>影响行数</returns>
    private static Int32 SaveJson(String lovCode, String category, Object model, ParameterKinds kind)
    {
        var json = model.ToJson();

        var key = GetKey(lovCode);
        var gate = _locks.GetOrAdd(key, _ => new Object());
        lock (gate)
        {
            var p = Parameter.FindByUserIDAndCategoryAndName(0, category, key);
            if (p == null)
                p = new Parameter { UserID = 0, Category = category, Name = key, CreateTime = DateTime.Now };
            else
                p.UpdateTime = DateTime.Now;

            if (json.Length < 200) { p.Value = json; p.LongValue = null; }
            else { p.Value = null; p.LongValue = json; }

            p.Kind = kind;
            p.Enable = true;
            // 不设 Remark（用户指令）

            return p.Save();
        }
    }

    /// <summary>删除指定分类的 Parameter 记录</summary>
    /// <param name="lovCode">值集编码</param>
    /// <param name="category">分类</param>
    /// <returns>影响行数</returns>
    private static Int32 DeleteJson(String lovCode, String category)
    {
        var key = GetKey(lovCode);
        var gate = _locks.GetOrAdd(key, _ => new Object());
        lock (gate)
        {
            var p = Parameter.FindByUserIDAndCategoryAndName(0, category, key);
            if (p == null) return 0;

            return p.Delete();
        }
    }

    /// <summary>Parameter.Name 键。Name 列长 50，超长值集编码退化为「前缀_哈希」键，确定性且避免截断冲突</summary>
    /// <param name="lovCode">值集编码</param>
    /// <returns>存储键</returns>
    private static String GetKey(String lovCode)
    {
        if (lovCode.Length <= MaxNameLength) return lovCode;

        return $"{lovCode[..39]}_{lovCode.MD5()[..8]}";
    }
    #endregion
}
