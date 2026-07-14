using System;
using System.Collections.Concurrent;
using System.Linq;
using NewLife;
using XCode;
using XCode.Membership;

using NewLife.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>值集列表配置业务扩展（1:1）。数据不落真实表，聚合存入 XCode.Membership.Parameter（Category=Lov.ListConfig，Kind=Hash）。</summary>
public partial class LovListConfig : Entity<LovListConfig>
{
    // 分类常量：避免每个方法硬编码
    private const String Category = "Lov.ListConfig";
    // 并发覆盖保护：按 lovDefId 加进程内锁
    private static readonly ConcurrentDictionary<Int32, Object> _locks = new();

    static LovListConfig()
    {
        // 过滤器 UserInterceptor、TimeInterceptor、IPInterceptor
        Meta.Interceptors.Add(new UserInterceptor { AllowEmpty = false });
        Meta.Interceptors.Add<TimeInterceptor>();
        Meta.Interceptors.Add(new IPInterceptor { AllowEmpty = false });

        // 禁止自动建表（逆向），数据改存 Parameter
        Meta.Table.DataTable.Properties["Migration"] = "Off";
    }

    // 读（1:1）：禁用 GetOrAdd，避免无数据时插入空占位孤儿行
    public static LovListConfig FindByLovDefId(Int32 lovDefId)
    {
        var p = Parameter.FindByUserIDAndCategoryAndName(0, Category, lovDefId.ToString());
        if (p == null) return null;
        var json = p.Value ?? p.LongValue;
        if (json.IsNullOrEmpty()) return null;
        var model = json.ToJsonEntity<LovListConfigModel>();
        if (model == null) return null;
        var e = new LovListConfig();
        e.Copy(model);
        return e;
    }

    // 写（主路径）：覆盖为一条 Parameter
    public static Int32 SaveByLovDefId(Int32 lovDefId, LovListConfig entity)
    {
        var p = Parameter.FindByUserIDAndCategoryAndName(0, Category, lovDefId.ToString());
        if (p == null) p = new Parameter { UserID = 0, Category = Category, Name = lovDefId.ToString() };
        var json = entity.ToModel().ToJson();
        if (json.Length < 200) { p.Value = json; p.LongValue = null; }
        else { p.Value = null; p.LongValue = json; }
        p.Kind = ParameterKinds.Hash;   // 22
        p.Enable = true;
        // 不设 Remark（用户指令）
        var gate = _locks.GetOrAdd(lovDefId, _ => new Object());
        lock (gate) return p.Save();
    }

    // 兜底：单行 Insert/Update/Delete → 写整表
    protected override Int32 OnInsert() => SaveByLovDefId(LovDefId, this);
    protected override Int32 OnUpdate() => SaveByLovDefId(LovDefId, this);
    protected override Int32 OnDelete()
    {
        var p = Parameter.FindByUserIDAndCategoryAndName(0, Category, LovDefId.ToString());
        return p != null ? p.Delete() : 0;
    }

    // 实体转模型（数据列+审计字段，用于 JSON 序列化存 Parameter）
    public LovListConfigModel ToModel()
    {
        return new LovListConfigModel
        {
            Id = Id,
            LovDefId = LovDefId,
            RequestUrl = RequestUrl,
            Method = Method,
            Pageable = Pageable,
            PageNumField = PageNumField,
            PageSizeField = PageSizeField,
            DataPath = DataPath,
            TotalPath = TotalPath,
            FixedParams = FixedParams,
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
