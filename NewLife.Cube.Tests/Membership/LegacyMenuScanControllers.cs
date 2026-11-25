using NewLife.Cube;
using XCode.Membership;

namespace NewLife.Cube.Tests.LegacyMenuScan.Controllers;

/// <summary>没有任何授权特性</summary>
public class PlainScanController { }

/// <summary>仅要求登录</summary>
[EntityAuthorize]
public class LoginOnlyScanController { }

/// <summary>显式声明权限位</summary>
public class PermissionScanController
{
    /// <summary>需要查看权限的动作</summary>
    [EntityAuthorize(PermissionFlags.Detail)]
    public void Index() { }
}
