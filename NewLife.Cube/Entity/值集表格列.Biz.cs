using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NewLife;
using XCode;
using XCode.Membership;

using NewLife.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>值集表格列业务扩展。数据不落真实表，聚合存入 XCode.Membership.Parameter（Category=Lov.TableColumn，Kind=List）。</summary>
public partial class LovTableColumn : Entity<LovTableColumn>
{
    // 分类常量：避免每个方法硬编码
    private const String Category = "Lov.TableColumn";
    // 并发覆盖保护：按 lovDefId 加进程内锁
    private static readonly ConcurrentDictionary<Int32, Object> _locks = new();

    static LovTableColumn()
    {
        // 过滤器 UserInterceptor、TimeInterceptor、IPInterceptor
        Meta.Interceptors.Add(new UserInterceptor { AllowEmpty = false });
        Meta.Interceptors.Add<TimeInterceptor>();
        Meta.Interceptors.Add(new IPInterceptor { AllowEmpty = false });

        // 禁止自动建表（逆向），数据改存 Parameter
        Meta.Table.DataTable.Properties["Migration"] = "Off";
    }

    /// <summary>根据值集定义编号查找所有表格列。使用 Find 读取 Parameter，禁用 GetOrAdd 避免无数据时插入空占位孤儿行</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <returns>表格列列表</returns>
    public static IList<LovTableColumn> FindAllByLovDefId(Int32 lovDefId)
    {
        var p = Parameter.FindByUserIDAndCategoryAndName(0, Category, lovDefId.ToString());
        if (p == null) return [];
        var json = p.Value ?? p.LongValue;
        if (json.IsNullOrEmpty()) return [];
        var models = json.ToJsonEntity<List<LovTableColumnModel>>() ?? [];
        return models.Select(m => { var e = new LovTableColumn(); e.Copy(m); return e; }).ToList();
    }

    /// <summary>保存值集表格列列表。整表覆盖为一条 Parameter 记录</summary>
    /// <param name="lovDefId">值集定义编号</param>
    /// <param name="list">表格列列表</param>
    /// <returns>影响行数</returns>
    public static Int32 SaveAllByLovDefId(Int32 lovDefId, IList<LovTableColumn> list)
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

    /// <summary>判断是否为同一业务键。用于表格列去重，按 Field 字段比较</summary>
    /// <param name="other">另一个表格列</param>
    /// <returns>是否相同</returns>
    protected virtual Boolean IsSameKey(LovTableColumn other) => other != null && other.Field == Field;

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

    /// <summary>转换为表格列模型。包含数据列和审计字段，用于 JSON 序列化存储到 Parameter</summary>
    /// <returns>表格列模型</returns>
    public LovTableColumnModel ToModel()
    {
        return new LovTableColumnModel
        {
            Id = Id,
            LovDefId = LovDefId,
            Field = Field,
            Title = Title,
            Width = Width,
            Align = Align,
            Sortable = Sortable,
            RefLovCode = RefLovCode,
            FormatType = FormatType,
            Sort = Sort,
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
