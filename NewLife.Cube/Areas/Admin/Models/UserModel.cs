using NewLife.Collections;
using NewLife.Cube.Enums;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Models;

/// <summary> 继承此接口，可通过json方式传值 </summary>
public interface ICubeModel { }

/// <summary> 登录模型 </summary>
public class LoginModel : ICubeModel
{
    /// <summary>登录类型</summary>
    public AuthCategory Category { get; set; } = AuthCategory.Password;

    /// <summary> 登录用户名、手机号码、邮箱 </summary>
    public String Username { get; set; }

    /// <summary> 密码 </summary>
    public String Password { get; set; }

    /// <summary> 记住登录状态 </summary>
    public Boolean Remember { get; set; }

    /// <summary> 挑战标识。调用 /Auth/Challenge 获取，登录时原样回传 </summary>
    public String ChallengeId { get; set; }

    /// <summary>验证码 ID。调用 /Auth/Captcha 获取，登录时原样回传；仅在登录场景需要验证码时必填 </summary>
    public String CaptchaId { get; set; }

    /// <summary>验证码用户输入。仅在登录场景需要验证码时必填 </summary>
    public String CaptchaCode { get; set; }

    /// <summary> 兼容旧版字段，建议改用 ChallengeId </summary>
    [Obsolete("Use ChallengeId instead")]
    public String Pkey { get => ChallengeId; set => ChallengeId = value; }
}


/// <summary>注册模型</summary>
public class RegisterModel : ICubeModel
{
    /// <summary>
    /// 电子邮箱
    /// </summary>
    public String Email { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public String Username { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public String Password { get; set; }

    /// <summary>
    /// 确认密码
    /// </summary>
    public String Password2 { get; set; }
}

/// <summary>统一认证注册模型</summary>
public class AuthRegisterModel : ICubeModel
{
    /// <summary>注册类型</summary>
    public AuthCategory Category { get; set; } = AuthCategory.Password;

    /// <summary>用户名</summary>
    public String Username { get; set; }

    /// <summary>邮箱</summary>
    public String Email { get; set; }

    /// <summary>手机号</summary>
    public String Mobile { get; set; }

    /// <summary>密码</summary>
    public String Password { get; set; }

    /// <summary>确认密码</summary>
    public String ConfirmPassword { get; set; }

    /// <summary>验证码（手机/邮箱注册时必填）</summary>
    public String Code { get; set; }

    /// <summary>OAuth 临时令牌（category=oauth 时必填）</summary>
    public String OAuthToken { get; set; }

    /// <summary>验证码 ID。调用 /Auth/Captcha 获取，注册时原样回传；仅在注册场景需要验证码时必填 </summary>
    public String CaptchaId { get; set; }

    /// <summary>验证码用户输入。仅在注册场景需要验证码时必填 </summary>
    public String CaptchaCode { get; set; }

    /// <summary>兼容旧版字段，建议改用 ConfirmPassword</summary>
    [Obsolete("Use ConfirmPassword instead")]
    public String Password2 { get => ConfirmPassword; set => ConfirmPassword = value; }
}

/// <summary>注册结果。正常注册返回访问令牌，需要邮箱/手机验证时返回待激活信息</summary>
public class RegisterResult
{
    /// <summary>访问令牌。待激活时为 null</summary>
    public String AccessToken { get; set; }

    /// <summary>刷新令牌。待激活时为 null</summary>
    public String RefreshToken { get; set; }

    /// <summary>是否待激活。true 表示注册成功但需先激活邮箱/手机后才能登录</summary>
    public Boolean PendingActivation { get; set; }

    /// <summary>已发送激活的渠道列表。mail/sms</summary>
    public String[] Channels { get; set; }

    /// <summary>对应渠道的脱敏目标。与 Channels 一一对应</summary>
    public String[] Targets { get; set; }

    /// <summary>激活有效期（秒）</summary>
    public Int32 ExpireIn { get; set; }
}

/// <summary>待激活注册信息。注册后需激活邮箱/手机才能登录</summary>
public class ActivatePendingModel
{
    /// <summary>已发送激活的渠道列表。mail/sms</summary>
    public String[] Channels { get; set; }

    /// <summary>对应渠道的脱敏目标。与 Channels 一一对应</summary>
    public String[] Targets { get; set; }

    /// <summary>激活有效期（秒）</summary>
    public Int32 ExpireIn { get; set; } = 3600;
}

/// <summary>联系方式验证状态。安全中心验证/更换后返回</summary>
public class VerifyStatusModel
{
    /// <summary>邮箱已验证</summary>
    public Boolean MailVerified { get; set; }

    /// <summary>手机已验证</summary>
    public Boolean MobileVerified { get; set; }
}

/// <summary>激活模型。邮箱/手机验证码激活</summary>
public class ActivateModel
{
    /// <summary>渠道。mail/sms</summary>
    public String Channel { get; set; }

    /// <summary>邮箱或手机号</summary>
    public String Account { get; set; }

    /// <summary>验证码</summary>
    public String Code { get; set; }
}

/// <summary>验证联系方式模型。安全中心验证/更换邮箱或手机</summary>
public class VerifyContactModel
{
    /// <summary>渠道。mail/sms</summary>
    public String Channel { get; set; }

    /// <summary>新邮箱或手机号</summary>
    public String Account { get; set; }

    /// <summary>验证码（经 SendCode action=bind 发送）</summary>
    public String Code { get; set; }
}

/// <summary>OAuth回跳待注册信息</summary>
public class OAuthPendingInfoModel : ICubeModel
{
    /// <summary>提供者名称</summary>
    public String Provider { get; set; }

    /// <summary>建议用户名</summary>
    public String Username { get; set; }

    /// <summary>邮箱</summary>
    public String Email { get; set; }

    /// <summary>手机号</summary>
    public String Mobile { get; set; }

    /// <summary>头像</summary>
    public String Avatar { get; set; }
}

/// <summary>重置密码模型</summary>
public class ResetPwdModel : ICubeModel
{
    /// <summary> 用户名/手机号 </summary>
    public String Username { get; set; }

    /// <summary> 验证码 </summary>
    public String Code { get; set; }

    /// <summary> 新密码 </summary>
    public String NewPassword { get; set; }

    /// <summary> 确认密码 </summary>
    public String ConfirmPassword { get; set; }

    /// <summary>挑战标识</summary>
    public String ChallengeId { get; set; }
}

/// <summary> 用户信息 </summary>
public class UserInfo
{
    /// <summary> 编号 </summary>
    public Int32 ID { get; set; }

    /// <summary>名称。登录用户名</summary>
    public String Name { get; set; }

    /// <summary>密码</summary>
    public String Password { get; set; }

    /// <summary>昵称</summary>
    public String DisplayName { get; set; }

    /// <summary>性别。未知、男、女</summary>
    public XCode.Membership.SexKinds Sex { get; set; }

    /// <summary>邮件</summary>
    public String Mail { get; set; }

    /// <summary>手机</summary>
    public String Mobile { get; set; }

    /// <summary>邮箱已验证。安全中心展示邮箱验证状态</summary>
    public Boolean MailVerified { get; set; }

    /// <summary>手机已验证。安全中心展示手机验证状态</summary>
    public Boolean MobileVerified { get; set; }

    /// <summary>代码。身份证、员工编号等</summary>
    public String Code { get; set; }

    /// <summary>头像</summary>
    public String Avatar { get; set; }

    /// <summary>角色。主要角色</summary>
    public Int32 RoleID { get; set; }

    /// <summary>角色组。次要角色集合</summary>
    public String RoleIds { get; set; }

    /// <summary>
    /// 主要角色名
    /// </summary>
    public String RoleName { get; set; }

    /// <summary>
    /// 角色集合名，逗号隔开
    /// </summary>
    public String RoleNames { get; set; }

    /// <summary>部门。组织机构</summary>
    public Int32 DepartmentID { get; set; }

    /// <summary>在线</summary>
    public Boolean Online { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>登录次数</summary>
    public Int32 Logins { get; set; }

    /// <summary>最后登录</summary>
    public DateTime LastLogin { get; set; }

    /// <summary>最后登录IP</summary>
    public String LastLoginIP { get; set; }

    /// <summary>注册时间</summary>
    public DateTime RegisterTime { get; set; }

    /// <summary>注册IP</summary>
    public String RegisterIP { get; set; }

    /// <summary>扩展1</summary>
    public Int32 Ex1 { get; set; }

    /// <summary>扩展2</summary>
    public Int32 Ex2 { get; set; }

    /// <summary>扩展3</summary>
    public Double Ex3 { get; set; }

    /// <summary>扩展4</summary>
    public String Ex4 { get; set; }

    /// <summary>扩展5</summary>
    public String Ex5 { get; set; }

    /// <summary>扩展6</summary>
    public String Ex6 { get; set; }

    /// <summary>更新者</summary>
    public String UpdateUser { get; set; }

    /// <summary>更新用户</summary>
    public Int32 UpdateUserID { get; set; }

    /// <summary>更新地址</summary>
    public String UpdateIP { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>备注</summary>
    public String Remark { get; set; }

    /// <summary>
    /// 包括角色组的权限集合
    /// </summary>
    public String Permission { get; set; }

    /// <summary>
    /// 设置用户权限集合
    /// </summary>
    /// <param name="roles"></param>
    public void SetPermission(IRole[] roles)
    {
        var ps = new Dictionary<Int32, Int32>();
        foreach (var role in roles)
        {
            foreach (var rolePermission in role.Permissions)
            {
                if (!ps.ContainsKey(rolePermission.Key))
                {
                    ps[rolePermission.Key] = rolePermission.Value.ToInt();
                    continue;
                }

                var permission = ps[rolePermission.Key];
                var addPermission = rolePermission.Value.ToInt();

                // 总权限=旧权限+新权限-重复权限
                // 比如，旧权限1+2+8=11，新权限1+2+16=19，重复权限11&19=3，总权限=11+19-3=27
                ps[rolePermission.Key] = (permission + addPermission) - (permission & addPermission);
            }
        }

        var sb = Pool.StringBuilder.Get();

        // 根据资源按照从小到大排序一下
        foreach (var item in ps.OrderBy(e => e.Key))
        {
            if (sb.Length > 0) sb.Append(',');
            sb.AppendFormat("{0}#{1}", item.Key, item.Value);
        }

        Permission = sb.Return(true);
    }

    /// <summary>
    /// 设置所有角色名
    /// </summary>
    /// <param name="roles"></param>
    public void SetRoleNames(IRole[] roles)
    {
        if (roles == null) return;
        if (!RoleNames.IsNullOrWhiteSpace()) return;

        RoleNames = roles.Select(s => s.Name).Join();
    }
}