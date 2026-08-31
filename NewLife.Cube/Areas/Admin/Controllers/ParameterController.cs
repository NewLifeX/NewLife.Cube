using System.ComponentModel;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>字典参数</summary>
[DisplayName("字典参数")]
[AdminArea]
[Menu(30, false, Icon = "Tools")]
public class ParameterController : EntityController<Parameter, ParameterModel>
{
    static ParameterController()
    {
        LogOnChange = true;

        ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6", "UpdateUserID", "UpdateIP");
        ListFields.RemoveCreateField();

        // 长数值改为大文本（对齐 MVC：字段无长度信息时按 length<0||>=300 渲染 textarea，rows=3）
        foreach (var fields in new[] { AddFormFields, EditFormFields })
        {
            var df = fields.GetField("LongValue");
            if (df != null) df.ItemType = "textarea";
        }
    }
}