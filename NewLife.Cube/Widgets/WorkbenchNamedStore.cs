using XCode;
using XCode.Membership;

namespace NewLife.Cube.Widgets;

/// <summary>命名工作台存储（OSC-260902ef43）。Parameter(UserID=0, Category=Workbench.Named, Name={slug})：Value 标题、LongValue 归一化配置；发布自动挂系统菜单 Url=/Workbench/{slug}，父分组 Sort 顶置保证永远显示在菜单第一组。</summary>
public static class WorkbenchNamedStore
{
    /// <summary>字典分类</summary>
    public const String Category = "Workbench.Named";

    /// <summary>菜单父分组 Name（内部标识，不变）</summary>
    public const String ParentName = "Workbench";

    /// <summary>菜单父分组显示名（侧栏顶级菜单组名）</summary>
    public const String ParentTitle = "系统看板";

    /// <summary>命名项</summary>
    public sealed class NamedItem
    {
        /// <summary>唯一标识（小写）</summary>
        public String Slug { get; set; }

        /// <summary>标题（≤40）</summary>
        public String Title { get; set; }
    }

    /// <summary>校验 slug 是否合法：小写字母开头，仅 [a-z0-9-]，1~32 位</summary>
    /// <param name="slug">命名工作台唯一标识</param>
    /// <returns>是否合法</returns>
    public static Boolean IsValidSlug(String slug)
    {
        if (slug.IsNullOrEmpty() || slug.Length > 32) return false;
        if (!(slug[0] >= 'a' && slug[0] <= 'z')) return false;
        foreach (var ch in slug)
        {
            if (ch >= 'a' && ch <= 'z') continue;
            if (ch >= '0' && ch <= '9') continue;
            if (ch == '-') continue;
            return false;
        }
        return true;
    }

    /// <summary>校验标题是否合法</summary>
    /// <param name="title">标题</param>
    /// <returns>是否合法（非空且 ≤40）</returns>
    public static Boolean IsValidTitle(String title) => !title.IsNullOrWhiteSpace() && title.Trim().Length <= 40;

    /// <summary>全部命名工作台槽（含未挂菜单的悬空脏数据），按 slug 升序</summary>
    public static IList<NamedItem> GetList()
    {
        var list = Parameter.Meta.Cache.FindAll(e => e.UserID == 0 && e.Category == Category);
        return list
            .Where(e => !e.Name.IsNullOrEmpty())
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Select(e => new NamedItem { Slug = e.Name, Title = e.Value })
            .ToList();
    }

    /// <summary>可见命名工作台（菜单行存在且叶子到父链全部 Visible；列表/切换用，隐藏悬空脏数据与已隐藏项）</summary>
    public static IList<NamedItem> GetVisibleList()
    {
        var list = GetList();
        return list.Where(e => IsChainVisible(e.Slug)).ToList();
    }

    /// <summary>叶子菜单行及其祖先链是否全部可见（GetVisibleList 用；不含角色声明判定）</summary>
    static Boolean IsChainVisible(String slug)
    {
        var cur = FindMenu(slug);
        while (cur != null)
        {
            if (!cur.Visible) return false;
            cur = cur.ParentID > 0 ? FindById(cur.ParentID) : null;
        }
        return true;
    }

    /// <summary>读取命名工作台配置 JSON（LongValue）。不存在返回 null。</summary>
    /// <param name="slug">唯一标识</param>
    /// <returns>配置 JSON</returns>
    public static String Get(String slug)
    {
        var p = Find(slug);
        return p?.LongValue;
    }

    /// <summary>保存命名工作台（upsert）。标题写 Value、配置写 LongValue。</summary>
    /// <param name="slug">唯一标识（已校验）</param>
    /// <param name="title">标题（≤40）</param>
    /// <param name="json">归一化后配置 JSON</param>
    public static void Save(String slug, String title, String json)
    {
        if (!IsValidSlug(slug)) throw new ArgumentException("slug 非法", nameof(slug));

        var p = Parameter.GetOrAdd(0, Category, slug);
        p.Kind = ParameterKinds.String;
        p.Enable = true;
        p.Value = title?.Trim();
        p.LongValue = json;
        p.Save();
    }

    /// <summary>删除命名工作台 Parameter 行</summary>
    /// <param name="slug">唯一标识</param>
    public static void Delete(String slug)
    {
        var p = Find(slug);
        p?.Delete();
    }

    /// <summary>查找命名工作台元数据（Parameter 行）。不存在返回 null。</summary>
    /// <param name="slug">唯一标识</param>
    public static NamedItem FindItem(String slug)
    {
        var p = Find(slug);
        return p == null ? null : new NamedItem { Slug = p.Name, Title = p.Value };
    }

    /// <summary>槽或菜单行是否存在（用于区分 404/403）</summary>
    /// <param name="slug">唯一标识</param>
    public static Boolean Exists(String slug) => Find(slug) != null || FindMenu(slug) != null;

    static Parameter Find(String slug)
    {
        if (slug.IsNullOrEmpty()) return null;
        return Parameter.FindByUserIDAndCategoryAndName(0, Category, slug);
    }

    /// <summary>当前用户对该命名工作台对应菜单行是否可访问（沿父链，与 /Cube/MenuTree 同规则：主角色 + 附加角色并集）。</summary>
    /// <param name="user">当前用户</param>
    /// <param name="slug">唯一标识</param>
    /// <remarks>
    /// 规则：叶子到根的整条父链每一级都须 Visible；某级菜单一旦被任何角色声明权限（permissionedIds），
    /// 则当前用户必须拥有该级（allowedIds）；整链都未声明 → 默认全员可见。菜单行不存在返回 false。
    /// </remarks>
    public static Boolean IsAccessible(IUser user, String slug)
    {
        if (user == null || !IsValidSlug(slug)) return false;
        var menu = FindMenu(slug);
        if (menu == null) return false;

        // 主角色 + 附加角色（user.Roles 不含主 RoleID 时须显式并入）
        var ids = new HashSet<Int32> { user.RoleID };
        if (user.Roles != null)
        {
            foreach (var r in user.Roles)
            {
                if (r.ID > 0) ids.Add(r.ID);
            }
        }
        var roles = Role.FindAll();
        var allowedIds = roles.Where(r => ids.Contains(r.ID)).SelectMany(e => e.Resources).ToArray();
        var permissionedIds = roles.SelectMany(e => e.Resources).ToArray();

        // 沿父链逐级校验：祖先不可达（隐藏/未授权）则子树不可达（与 MenuTree 从根递归一致）
        var cur = menu;
        while (cur != null)
        {
            if (!cur.Visible) return false;
            if (permissionedIds.Contains(cur.ID) && !allowedIds.Contains(cur.ID)) return false;
            cur = cur.ParentID > 0 ? FindById(cur.ParentID) : null;
        }
        return true;
    }

    static IMenu FindById(Int32 id) => Menu.Meta.Cache.FindAll(e => e.ID == id).FirstOrDefault();

    static IMenu FindMenu(String slug)
    {
        if (slug.IsNullOrEmpty()) return null;
        return Menu.Meta.Cache
            .FindAll(e => !e.Url.IsNullOrEmpty() && e.Url.EqualIgnoreCase("/Workbench/" + slug))
            .FirstOrDefault();
    }

    /// <summary>挂载菜单行：确保父分组存在并顶置，挂/改子菜单。返回子菜单行。</summary>
    /// <param name="slug">唯一标识（已校验）</param>
    /// <param name="title">标题（≤40）</param>
    public static IMenu MountMenu(String slug, String title)
    {
        if (!IsValidSlug(slug)) throw new ArgumentException("slug 非法", nameof(slug));

        var group = EnsureFirstGroup();
        var menu = FindMenu(slug);
        if (menu == null)
        {
            menu = group.Add(slug, title?.Trim(), "NewLife.Cube.Workbench." + slug, "/Workbench/" + slug);
            menu.Icon = "fa-th-large";
            ((IEntity)menu).Update();
        }
        else
        {
            menu.DisplayName = title?.Trim();
            ((IEntity)menu).Update();
        }
        return menu;
    }

    /// <summary>卸载菜单行；父分组无其它子菜单时一并移除（下次发布自动重建并重新顶置）。</summary>
    /// <param name="slug">唯一标识</param>
    public static void UnmountMenu(String slug)
    {
        var menu = FindMenu(slug);
        if (menu == null) return;

        var parentId = menu.ParentID;
        ((IEntity)menu).Delete();

        // 父分组是否已无其它子菜单（用实体缓存判断，避免实例 Childs 缓存残留）
        if (parentId > 0 && !Menu.Meta.Cache.FindAll(e => e.ParentID == parentId).Any())
        {
            var parent = Menu.Meta.Cache.FindAll(e => e.ID == parentId).FirstOrDefault();
            if (parent != null && parent.Name == ParentName) ((IEntity)parent).Delete();
        }
    }

    /// <summary>确保「系统看板」父分组存在，并把其 Sort 顶置为当前根级顶级菜单最大 Sort+1（BigSort=true、Sort 降序 → 永远第一组）。</summary>
    /// <returns>父分组菜单行</returns>
    public static IMenu EnsureFirstGroup()
    {
        if (Menu.Root is not IMenu root) throw new InvalidOperationException("菜单根不可用");

        var rootMenus = root.Childs;
        var group = rootMenus.FirstOrDefault(e => e.Name == ParentName);
        if (group == null)
        {
            group = root.Add(ParentName, ParentTitle, "NewLife.Cube.Workbench", null);
            ((IEntity)group).Update();
        }
        // 已存在（旧版本叫「工作台」）→ 幂等同步显示名，保证改名后侧栏组名即时生效
        else if (group.DisplayName != ParentTitle)
        {
            group.DisplayName = ParentTitle;
            ((IEntity)group).Update();
        }

        var maxSort = rootMenus.Where(e => e.ID != group.ID).Select(e => e.Sort).DefaultIfEmpty(0).Max();
        if (group.Sort <= maxSort)
        {
            group.Sort = maxSort == Int32.MaxValue ? Int32.MaxValue : maxSort + 1;
            ((IEntity)group).Update();
        }
        return group;
    }

    static IMenu FindGroup()
    {
        if (Menu.Root is not IMenu root) return null;
        return root.Childs.FirstOrDefault(e => e.Name == ParentName);
    }
}
