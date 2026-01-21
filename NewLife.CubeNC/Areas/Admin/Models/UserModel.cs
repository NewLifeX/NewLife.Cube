using NewLife.Collections;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Models;

/// <summary> 继承此接口，可通过json方式传值 </summary>
public interface ICubeModel { }

/// <summary> 登录模型 </summary>
public class LoginModel : ICubeModel
{  
    /// <summary> 登录用户名、手机号码、邮箱 </summary>
    public String Username { get; set; }

    /// <summary> 密码 </summary>
    public String Password { get; set; }

    /// <summary> 记住登录状态 </summary>
    public Boolean Remember { get; set; }

    /// <summary> 秘钥key </summary>
    public String Pkey { get; set; }
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
}

/// <summary> 用户信息 </summary>
public class UserInfo
{
    /// <summary>
    /// 编号
    /// </summary>
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
        var ps = new Dictionary<Int32, UInt32>();
        foreach (var role in roles)
        {
            foreach (var rolePermission in role.Permissions)
            {
                if (!ps.ContainsKey(rolePermission.Key))
                {
                    ps[rolePermission.Key] = Convert.ToUInt32(rolePermission.Value);
                    continue;
                }

                var permission = ps[rolePermission.Key];
                var addPermission = Convert.ToUInt32(rolePermission.Value);

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