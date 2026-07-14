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

    // 读：用 Find（禁用 GetOrAdd，避免无数据时插入空占位孤儿行）
    public static IList<LovTableColumn> FindAllByLovDefId(Int32 lovDefId)
    {
        var p = Parameter.FindByUserIDAndCategoryAndName(0, Category, lovDefId.ToString());
        if (p == null) return [];
        var json = p.Value ?? p.LongValue;
        if (json.IsNullOrEmpty()) return [];
        var models = json.ToJsonEntity<List<LovTableColumnModel>>() ?? [];
        return models.Select(m => { var e = new LovTableColumn(); e.Copy(m); return e; }).ToList();
    }

    // 写（主路径）：整表覆盖为一条 Parameter
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

    // 业务键去重（防 Id=0 新行误删）：表格列按 Field
    protected virtual Boolean IsSameKey(LovTableColumn other) => other != null && other.Field == Field;

    // 兜底：单行 Insert/Update/Delete → 读整表-改-写整表
    protected override Int32 OnInsert() => UpsertOne();
    protected override Int32 OnUpdate() => UpsertOne();
    private Int32 UpsertOne()
    {
        var list = FindAllByLovDefId(LovDefId).Where(e => !IsSameKey(e)).ToList();
        list.Add(this);
        return SaveAllByLovDefId(LovDefId, list);
    }
    protected override Int32 OnDelete()
    {
        var list = FindAllByLovDefId(LovDefId).Where(e => !IsSameKey(e)).ToList();
        return SaveAllByLovDefId(LovDefId, list);
    }

    // 实体转模型（数据列+审计字段，用于 JSON 序列化存 Parameter）
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
