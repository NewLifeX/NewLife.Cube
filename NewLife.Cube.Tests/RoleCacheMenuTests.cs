using System.Collections.Generic;
using System.Linq;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>OSC-2609307879 MenuTree 改走角色实体缓存后，Resources 并集与 FindAll 一致。</summary>
public class RoleCacheMenuTests
{
    [Fact(DisplayName = "FindAllWithCache 与 FindAll 的 Resources 并集相同")]
    public void RoleCache_Resources_Match_FindAll()
    {
        var fromSql = Union(Role.FindAll());
        var fromCache = Union(Role.FindAllWithCache());
        Assert.Equal(fromSql.OrderBy(id => id), fromCache.OrderBy(id => id));
    }

    static List<System.Int32> Union(IEnumerable<IRole> roles)
    {
        var set = new HashSet<System.Int32>();
        foreach (var role in roles)
        {
            if (role?.Resources == null) continue;
            foreach (var id in role.Resources) set.Add(id);
        }
        return set.ToList();
    }
}
