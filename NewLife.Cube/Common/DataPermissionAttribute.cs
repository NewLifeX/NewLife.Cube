using System;
using System.Linq;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>数据权限过滤器</summary>
    public class DataPermissionAttribute : Attribute
    {
        /// <summary>不受数据权限限制的系统角色</summary>
        public String SystemRoles { get; set; }

        /// <summary>数据权限表达式，用于构造查询条件，如 linkid in {#SiteIds} or CreateUserID={$user.Id}</summary>
        public String Expression { get; set; }

        /// <summary>数据权限过滤器</summary>
        /// <param name="systemRoles"></param>
        /// <param name="expression"></param>
        public DataPermissionAttribute(String systemRoles, String expression)
        {
            SystemRoles = systemRoles;
            Expression = expression;

            //_srs = systemRoles.Split(',', ';');
        }

        //private String[] _srs;
        /// <summary>验证是否系统角色</summary>
        /// <param name="roles"></param>
        /// <returns></returns>
        public Boolean Valid(IRole[] roles)
        {
            var rs = SystemRoles;
            if (rs == null || rs.Length == 0) return false;

            var _srs = rs.Split(',', ';');
            return roles.Any(e => _srs.Contains(e.Name));
        }
    }
}