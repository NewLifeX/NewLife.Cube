using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Log;
using NewLife.Web;
using XCode;
using XCode.Configuration;
using XCode.Membership;

namespace CubeDemo.Areas.Test.Controllers;

/// <summary>字段类型全覆盖测试控制器。配合 测试字段 实体，向前端默认模板下发覆盖全部控件类型的字段元数据。</summary>
[TestArea]
[DisplayName("字段类型测试")]
[Menu(0, true, Mode = MenuModes.Admin | MenuModes.Tenant)]
public class TestFieldController : EntityController<测试字段, 测试字段Model>
{
    static TestFieldController()
    {
        // 统一为 枚举 / 单选 / 多选 三类值集字段下发 lovCode。
        // 值集由 LovAutoRegisterService 自动注册为 Enum.CubeDemo.Areas.Test.测试枚举。
        var lovCode = $"Enum.{typeof(测试枚举).FullName}";

        SetLov(ListFields, 测试字段._.Kind, lovCode);
        SetLov(AddFormFields, 测试字段._.Kind, lovCode);
        SetLov(EditFormFields, 测试字段._.Kind, lovCode);
        SetLov(SearchFields, 测试字段._.Kind, lovCode);

        SetLov(ListFields, 测试字段._.SingleVal, lovCode);
        SetLov(AddFormFields, 测试字段._.SingleVal, lovCode);
        SetLov(EditFormFields, 测试字段._.SingleVal, lovCode);
        SetLov(SearchFields, 测试字段._.SingleVal, lovCode);

        SetLov(ListFields, 测试字段._.MultiVal, lovCode);
        SetLov(AddFormFields, 测试字段._.MultiVal, lovCode);
        SetLov(EditFormFields, 测试字段._.MultiVal, lovCode);
        SetLov(SearchFields, 测试字段._.MultiVal, lovCode);

        // 下拉表格字段：使用列表型值集 List.CubeDemo.Role（由本控制器 RoleList 方法上的 [LovList] 自动注册）。
        // 该值集 ProxyRequest=false，前端直接请求同应用接口 /Test/TestField/RoleList（不代理）。
        const String roleLovCode = "List.CubeDemo.Role";
        SetLov(ListFields, 测试字段._.ListVal, roleLovCode);
        SetLov(AddFormFields, 测试字段._.ListVal, roleLovCode);
        SetLov(EditFormFields, 测试字段._.ListVal, roleLovCode);
        SetLov(SearchFields, 测试字段._.ListVal, roleLovCode);

        // 下拉表格·多选字段：复用同一个列表型值集 List.CubeDemo.Role，前端以多选弹窗表格呈现。
        SetLov(ListFields, 测试字段._.ListMVal, roleLovCode);
        SetLov(AddFormFields, 测试字段._.ListMVal, roleLovCode);
        SetLov(EditFormFields, 测试字段._.ListMVal, roleLovCode);
        SetLov(SearchFields, 测试字段._.ListMVal, roleLovCode);
    }

    /// <summary>为字段集合中的指定字段设置 LOV 值集编码</summary>
    /// <param name="fields">字段集合</param>
    /// <param name="field">目标字段（FieldItem）</param>
    /// <param name="lovCode">值集编码</param>
    private static void SetLov(FieldCollection fields, Field field, String lovCode)
    {
        var df = fields.GetField(field);
        if (df != null) df.LovCode = lovCode;
    }

    /// <summary>角色列表数据源。返回角色列表 JSON，供 List.CubeDemo.Role 值集（前端直连，不代理）使用。
    /// 同时用 [LovList] 声明该列表型值集，由 LovAutoRegisterService 在初始化时自动注册。</summary>
    /// <returns>角色列表与总数</returns>
    [LovList(
        LovCode = "List.CubeDemo.Role",
        Name = "角色",
        RequestUrl = "/Test/TestField/RoleList",
        Method = "GET",
        Pageable = true,
        ValueField = "id",
        LabelField = "name",
        DataPath = "data",
        TotalPath = "total",
        ProxyRequest = false,
        Columns = new[] { "id:编号:80:left", "name:角色名:200:left" },
        SearchFields = new[] { "name:角色名:input:BODY:false" }
    )]
    [HttpGet]
    public Object RoleList(Int32 pageIndex = 1, Int32 pageSize = 10, String name = null)
    {
        // 真实角色（来自 Membership）
        var real = Role.FindAll().Select(r => new Dictionary<String, Object>
        {
            ["id"] = r.ID,
            ["name"] = r.Name,
        }).ToList();

        // 演示用扩展角色：凑足分页数据，便于演示翻页（id 从 1001 起，避免与真实角色冲突）
        var list = new List<Dictionary<String, Object>>(real);
        for (var i = 1; i <= 21; i++)
        {
            list.Add(new Dictionary<String, Object>
            {
                ["id"] = 1000 + i,
                ["name"] = $"演示角色 {i:D2}",
            });
        }

        // 名称过滤：支持搜索栏按角色名模糊查询（不区分大小写）
        if (!name.IsNullOrEmpty())
        {
            var kw = name.Trim();
            list = list.Where(d => d["name"].ToString().Contains(kw, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var total = list.Count;
        var paged = list
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // 显式字典投影，确保序列化键为小驼峰 id/name，与 ValueField/LabelField 对齐
        return new
        {
            Data = (Object)paged,
            Total = total,
        };
    }
}
