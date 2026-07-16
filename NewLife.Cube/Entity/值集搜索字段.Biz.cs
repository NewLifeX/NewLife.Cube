using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NewLife;
using XCode;
using XCode.Membership;
using NewLife.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>值集搜索字段业务扩展。数据不落真实表，聚合存入 XCode.Membership.Parameter（Category=Lov.SearchField，Kind=List）。</summary>
public partial class LovSearchField : Entity<LovSearchField>
{
    // 分类常量：避免每个方法硬编码
    private const String Category = "Lov.SearchField";
    // 并发覆盖保护：按 lovDefId 加进程内锁
    private static readonly ConcurrentDictionary<Int32, Object> _locks = new();

    static LovSearchField()
    {
        // 过滤器 UserInterceptor、TimeInterceptor、IPInterceptor
        Meta.Interceptors.Add(new UserInterceptor { AllowEmpty = false });
        Meta.Interceptors.Add<TimeInterceptor>();
        Meta.Interceptors.Add(new IPInterceptor { AllowEmpty = false });

        // 禁止自动建表（逆向），数据改存 Parameter
        Meta.Table.DataTable.Properties["Migration"] = "Off";
    }

    /// <summary>根据值集定义编号查找所有搜索字段。使用 Find 读取 Parameter，禁用 GetOrAdd 避免无数据时插入空占位孤儿行</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <returns>搜索字段列表</returns>
    public static IList<LovSearchField> FindAllByLovDefId(Int32 lovDefId)
    {
        var p = Parameter.FindByUserIDAndCategoryAndName(0, Category, lovDefId.ToString());
        if (p == null) return [];
        var json = p.Value ?? p.LongValue;
        if (json.IsNullOrEmpty()) return [];
        var models = json.ToJsonEntity<List<LovSearchFieldModel>>() ?? [];
        return models.Select(m => { var e = new LovSearchField(); e.Copy(m); return e; }).ToList();
    }

    /// <summary>保存值集搜索字段列表。整表覆盖为一条 Parameter 记录</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <param name="list">搜索字段列表</param>
    /// <returns>影响行数</returns>
    public static Int32 SaveAllByLovDefId(Int32 lovDefId, IList<LovSearchField> list)
    {
        var models = list.Select(e => e.ToModel()).ToList();   // 存什么取什么，含审计字段
        var p = Parameter.FindByUserIDAndCategoryAndName(0, Category, lovDefId.ToString());
        if (p == null) p = new Parameter { UserID = 0, Category = Category, Name = lovDefId.ToString() };
        var json = models.ToJson();
        if (json.Length < 200) { p.Value = json; p.LongValue = null; }
        else { p.Value = null; p.LongValue = json; }
        p.Kind = ParameterKinds.List;   // 21
        p.Enable = true;
        // 不设 Remark（用户指令）
        var gate = _locks.GetOrAdd(lovDefId, _ => new Object());
        lock (gate) return p.Save();
    }

    /// <summary>判断是否为同一业务键。用于搜索字段去重，按 Field 字段比较</summary>
    /// <param name="other">另一个搜索字段</param>
    /// <returns>是否相同</returns>
    protected virtual Boolean IsSameKey(LovSearchField other) => other != null && other.Field == Field;

    /// <summary>插入时执行整表覆盖写入（读整表-改-写整表）</summary>
    /// <returns>影响行数</returns>
    protected override Int32 OnInsert() => UpsertOne();
    /// <summary>更新时执行整表覆盖写入（读整表-改-写整表）</summary>
    /// <returns>影响行数</returns>
    protected override Int32 OnUpdate() => UpsertOne();
    private Int32 UpsertOne()
    {
        var list = FindAllByLovDefId(LovDefId).Where(e => !IsSameKey(e)).ToList();
        list.Add(this);
        return SaveAllByLovDefId(LovDefId, list);
    }
    /// <summary>删除时执行整表覆盖写入（排除当前项）</summary>
    /// <returns>影响行数</returns>
    protected override Int32 OnDelete()
    {
        var list = FindAllByLovDefId(LovDefId).Where(e => !IsSameKey(e)).ToList();
        return SaveAllByLovDefId(LovDefId, list);
    }

    /// <summary>转换为搜索字段模型。包含数据列和审计字段，用于 JSON 序列化存储到 Parameter</summary>
    /// <returns>搜索字段模型</returns>
    public LovSearchFieldModel ToModel()
    {
        return new LovSearchFieldModel
        {
            Id = Id,
            LovDefId = LovDefId,
            Field = Field,
            Title = Title,
            ComponentType = ComponentType,
            ParamType = ParamType,
            Required = Required,
            DefaultValue = DefaultValue,
            Sort = Sort,
            RefLovCode = RefLovCode,
            CreateUserID = CreateUserID,
            CreateIP = CreateIP,
            CreateTime = CreateTime,
            UpdateUserID = UpdateUserID,
            UpdateIP = UpdateIP,
            UpdateTime = UpdateTime,
            Remark = Remark
        };
    }
}
