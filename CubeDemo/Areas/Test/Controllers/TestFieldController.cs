using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Log;
using NewLife.Web;
using XCode.Configuration;

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
}
