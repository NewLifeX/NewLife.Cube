using NewLife.Cube.Entity;
using NewLife.Log;

namespace NewLife.Cube.Services;

/// <summary>
/// 值集管理页样例种子（手工定义）。对照系统常见枚举模式（是否/性别/优先级）与列表型数据源，
/// 供管理员打开「值集」页学习配置；已存在同编码定义时不覆盖。
/// </summary>
public static class LovSampleSeeds
{
    /// <summary>确保样例值集存在。可在 <c>UseCube</c> 后调用；失败仅写日志。</summary>
    public static void Ensure()
    {
        try
        {
            EnsureYesNo();
            EnsureSex();
            EnsurePriority();
            EnsureUserList();
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }
    }

    static void EnsureYesNo()
    {
        const String code = "Enum.Sample.YesNo";
        if (LovStore.FindDef(code) != null) return;

        LovStore.SaveDef(new LovDefModel
        {
            LovCode = code,
            Name = "是否（样例）",
            Type = "ENUM",
            ValueField = "value",
            LabelField = "label",
            Enabled = true,
            Remark = "对照常见启用/是否字段；取值 0=否、1=是",
        });
        LovStore.SaveEnumItems(code, new List<LovEnumItemModel>
        {
            new() { Value = "0", Label = "否", Sort = 0, Enabled = true },
            new() { Value = "1", Label = "是", Sort = 1, Enabled = true },
        });
    }

    static void EnsureSex()
    {
        const String code = "Enum.Sample.Sex";
        if (LovStore.FindDef(code) != null) return;

        LovStore.SaveDef(new LovDefModel
        {
            LovCode = code,
            Name = "性别（样例）",
            Type = "ENUM",
            ValueField = "value",
            LabelField = "label",
            Enabled = true,
            Remark = "对照 XCode.Membership.SexKinds（未知/男/女）",
        });
        LovStore.SaveEnumItems(code, new List<LovEnumItemModel>
        {
            new() { Value = "0", Label = "未知", Sort = 0, Enabled = true },
            new() { Value = "1", Label = "男", Sort = 1, Enabled = true },
            new() { Value = "2", Label = "女", Sort = 2, Enabled = true },
        });
    }

    static void EnsurePriority()
    {
        const String code = "Enum.Sample.Priority";
        if (LovStore.FindDef(code) != null) return;

        LovStore.SaveDef(new LovDefModel
        {
            LovCode = code,
            Name = "优先级（样例）",
            Type = "ENUM",
            ValueField = "value",
            LabelField = "label",
            Enabled = true,
            Remark = "手工枚举多档取值示例；可在「配置」中增删项",
        });
        LovStore.SaveEnumItems(code, new List<LovEnumItemModel>
        {
            new() { Value = "low", Label = "低", Sort = 0, Enabled = true },
            new() { Value = "medium", Label = "中", Sort = 1, Enabled = true },
            new() { Value = "high", Label = "高", Sort = 2, Enabled = true },
        });
    }

    static void EnsureUserList()
    {
        const String code = "List.Sample.User";
        if (LovStore.FindDef(code) != null) return;

        LovStore.SaveDef(new LovDefModel
        {
            LovCode = code,
            Name = "用户列表（样例）",
            Type = "LIST",
            ValueField = "id",
            LabelField = "name",
            Enabled = true,
            Remark = "列表型样例：同源 /api/Admin/User；配置里可看请求地址、搜索字段与表格列",
        });
        LovStore.SaveListConfig(code, new LovListConfigModel
        {
            RequestUrl = "/api/Admin/User",
            Method = "GET",
            Pageable = true,
            PageNumField = "pageIndex",
            PageSizeField = "pageSize",
            DataPath = "data",
            TotalPath = "page.totalCount",
            ProxyRequest = false,
        });
        LovStore.SaveSearchFields(code, new List<LovSearchFieldModel>
        {
            new() { Field = "Q", Title = "关键字", ComponentType = "input", ParamType = "QUERY", Sort = 0 },
        });
        LovStore.SaveTableColumns(code, new List<LovTableColumnModel>
        {
            new() { Field = "id", Title = "编号", Width = 80, Align = "left", Sort = 0 },
            new() { Field = "name", Title = "名称", Width = 160, Align = "left", Sort = 1 },
            new() { Field = "enable", Title = "启用", Width = 80, Align = "center", Sort = 2 },
        });
    }
}
