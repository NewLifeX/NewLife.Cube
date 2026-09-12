using System.ComponentModel;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>字典参数</summary>
[DataPermission(null, "UserID={#userId}")]
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

    /// <summary>验证数据</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected override Boolean Valid(Parameter entity, DataObjectMethodType type, Boolean post)
    {
        // 非系统角色只能管理自己的参数：新增/修改一律归到当前用户名下，防止写入系统级（UserID=0）或他人参数
        if (type is DataObjectMethodType.Insert or DataObjectMethodType.Update)
        {
            var user = HttpContext.Items["CurrentUser"] as IUser;
            user ??= ManageProvider.User;
            if (user != null && user.Roles?.Any(e => e.IsSystem) != true) entity.UserID = user.ID;
        }

        return base.Valid(entity, type, post);
    }
}