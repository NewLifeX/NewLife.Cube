using System.Linq;
using NewLife.Cube.ViewModels;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>B2 请求期字段配置钩子测试</summary>
public class ControllerFieldHookTests
{
    private sealed class HookedUserController : ReadOnlyEntityController<User>
    {
        public FieldCollection Fields(ViewKinds kind) => OnGetFields(kind, null);

        protected override void OnConfigureFields(ViewKinds kind, FieldCollection fields)
        {
            if (kind == ViewKinds.List) fields.RemoveField("Name");
        }
    }

    private sealed class AuditController : ReadOnlyEntityController<NewLife.Cube.Entity.AppLog>
    {
        public FieldCollection Fields(ViewKinds kind) => OnGetFields(kind, null);
    }

    [Fact(DisplayName = "列表默认隐藏审计定位字段，表单与详情不受影响")]
    public void ListFields_HideAuditColumnsByDefault()
    {
        var controller = new AuditController();

        var list = controller.Fields(ViewKinds.List);
        Assert.DoesNotContain(list, e => e.Name.EqualIgnoreCase("CreateIP", "UpdateIP", "CreateUserID", "UpdateUserID"));

        var form = controller.Fields(ViewKinds.EditForm);
        Assert.Contains(form, e => e.Name.EqualIgnoreCase("CreateIP"));
    }

    [Fact(DisplayName = "请求期钩子可统一调整字段集合")]
    public void OnConfigureFields_AppliesPerRequest()
    {
        var controller = new HookedUserController();

        var fields = controller.Fields(ViewKinds.List);

        Assert.DoesNotContain(fields, e => e.Name.EqualIgnoreCase("Name"));
        // 钩子只影响当前副本，静态字段集合仍保留原字段
        var again = controller.Fields(ViewKinds.EditForm);
        Assert.Contains(again, e => e.Name.EqualIgnoreCase("Name"));
    }
}
