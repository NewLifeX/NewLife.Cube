# 附录C 实体参考

> 本附录提供魔方内置实体的说明和关系图。

---

## C.1 内置实体一览

### 用户与权限（Membership 库）

| 实体类 | 表名 | 说明 |
|--------|------|------|
| User | User | 用户 |
| Role | Role | 角色 |
| Menu | Menu | 菜单 |
| Department | Department | 部门 |
| Area | Area | 地区 |
| Log | Log | 日志（可分库） |
| UserToken | UserToken | 用户令牌 |
| UserConnect | UserConnect | 用户第三方登录绑定 |
| UserOnline | UserOnline | 用户在线 |
| UserStat | UserStat | 用户统计 |

### 系统管理（Cube 库）

| 实体类 | 表名 | 说明 |
|--------|------|------|
| App | App | 应用系统 |
| AppModule | AppModule | 应用插件 |
| Attachment | Attachment | 附件 |
| CronJob | CronJob | 定时作业 |
| OAuthConfig | OAuthConfig | OAuth 配置 |
| OAuthLog | OAuthLog | OAuth 日志 |
| AccessRule | AccessRule | 访问规则 |
| Tenant | Tenant | 租户 |
| TenantUser | TenantUser | 租户用户 |

---

## C.2 用户实体（User）

### 字段说明

| 字段名 | 类型 | 说明 |
|--------|------|------|
| ID | Int32 | 编号（主键） |
| Name | String | 用户名 |
| Password | String | 密码（MD5 加密） |
| DisplayName | String | 显示名/昵称 |
| Sex | SexKinds | 性别（未知/男/女） |
| Mail | String | 邮箱 |
| Mobile | String | 手机 |
| Code | String | 代码/工号 |
| Avatar | String | 头像 |
| RoleID | Int32 | 角色 ID |
| RoleIds | String | 角色组（多角色） |
| DepartmentID | Int32 | 部门 ID |
| Online | Boolean | 是否在线 |
| Enable | Boolean | 是否启用 |
| Logins | Int32 | 登录次数 |
| LastLogin | DateTime | 最后登录时间 |
| LastLoginIP | String | 最后登录 IP |
| RegisterTime | DateTime | 注册时间 |
| RegisterIP | String | 注册 IP |
| Ex1 ~ Ex6 | Int32/Decimal/String | 扩展字段 |
| Remark | String | 备注 |

### 扩展属性

```csharp
/// <summary>角色</summary>
public Role Role => Extends.Get(nameof(Role), k => Role.FindByID(RoleID));

/// <summary>角色名</summary>
public String RoleName => Role?.Name;

/// <summary>部门</summary>
public Department Department => Extends.Get(nameof(Department), k => Department.FindByID(DepartmentID));

/// <summary>部门名</summary>
public String DepartmentName => Department?.Name;

/// <summary>角色集合</summary>
public IList<Role> Roles { get; }
```

### 常用方法

```csharp
// 根据用户名查找
User.FindByName("admin");

// 根据邮箱查找
User.FindByMail("admin@example.com");

// 根据手机查找
User.FindByMobile("13800138000");

// 登录
User.Login("admin", "password", ip, expire);

// 修改密码
user.SetPassword("newPassword");
```

---

## C.3 角色实体（Role）

### 字段说明

| 字段名 | 类型 | 说明 |
|--------|------|------|
| ID | Int32 | 编号（主键） |
| Name | String | 角色名称 |
| Enable | Boolean | 是否启用 |
| IsSystem | Boolean | 是否系统角色 |
| Permission | String | 权限（JSON 格式） |
| Sort | Int32 | 排序 |
| Ex1 ~ Ex6 | Int32/Decimal/String | 扩展字段 |
| Remark | String | 备注 |

### 权限格式

```json
{
  "Admin/User": 15,
  "Admin/Role": 7,
  "Admin/Menu": 3,
  "School/Student": 15
}
```

### 常用方法

```csharp
// 根据名称查找
Role.FindByName("管理员");

// 获取所有角色
Role.FindAllWithCache();

// 检查权限
role.Has(menu.ID, PermissionFlags.Update);

// 设置权限
role.Set(menu.ID, PermissionFlags.Detail | PermissionFlags.Insert);
```

---

## C.4 菜单实体（Menu）

### 字段说明

| 字段名 | 类型 | 说明 |
|--------|------|------|
| ID | Int32 | 编号（主键） |
| Name | String | 菜单名称 |
| DisplayName | String | 显示名称 |
| ParentID | Int32 | 父菜单 ID |
| Url | String | 链接地址 |
| Icon | String | 图标 |
| Visible | Boolean | 是否可见 |
| NewWindow | Boolean | 新窗口打开 |
| Permission | String | 权限子项 |
| Sort | Int32 | 排序 |
| Remark | String | 备注 |

### 权限子项格式

```
Detail=查看详情,Insert=添加,Update=编辑,Delete=删除,Export=导出,Import=导入
```

### 常用方法

```csharp
// 根据名称查找
Menu.FindByName("用户管理");

// 根据 URL 查找
Menu.FindByUrl("/Admin/User");

// 获取子菜单
menu.Childs;

// 获取父菜单
menu.Parent;

// 获取菜单树
Menu.Root.Childs;
```

---

## C.5 部门实体（Department）

### 字段说明

| 字段名 | 类型 | 说明 |
|--------|------|------|
| ID | Int32 | 编号（主键） |
| TenantId | Int32 | 租户 ID |
| Code | String | 编码 |
| Name | String | 名称 |
| FullName | String | 全名 |
| ParentID | Int32 | 父部门 ID |
| Level | Int32 | 层级 |
| Sort | Int32 | 排序 |
| Enable | Boolean | 是否启用 |
| Visible | Boolean | 是否可见 |
| ManagerId | Int32 | 负责人 ID |
| Ex1 ~ Ex6 | Int32/Decimal/String | 扩展字段 |
| Remark | String | 备注 |

### 树形结构

```csharp
// 父部门
department.Parent;

// 子部门
department.Childs;

// 所有子部门（递归）
department.AllChilds;

// 部门路径
department.FullName; // "总公司/技术部/研发组"
```

---

## C.6 日志实体（Log）

### 字段说明

| 字段名 | 类型 | 说明 |
|--------|------|------|
| ID | Int64 | 编号（主键） |
| Category | String | 类别 |
| Action | String | 操作 |
| Success | Boolean | 是否成功 |
| UserName | String | 用户名 |
| Remark | String | 详细信息 |
| CreateUserID | Int32 | 用户 ID |
| CreateIP | String | IP 地址 |
| CreateTime | DateTime | 时间 |
| TraceId | String | 追踪 ID |

### 日志分表

日志支持按月分表：

```csharp
// 设置分表策略
Log.Meta.ShardPolicy = new TimeShardPolicy(Log._.CreateTime, TimeShardPolicy.ByMonth);

// 查询某月日志
var list = Log.FindAll(Log._.CreateTime.Between(startDate, endDate), null, null, 0, 100);
```

---

## C.7 定时作业实体（CronJob）

### 字段说明

| 字段名 | 类型 | 说明 |
|--------|------|------|
| Id | Int32 | 编号（主键） |
| Name | String | 名称 |
| DisplayName | String | 显示名 |
| Cron | String | Cron 表达式 |
| Method | String | 执行方法 |
| Argument | String | 方法参数 |
| Enable | Boolean | 是否启用 |
| EnableLog | Boolean | 启用日志 |
| LastTime | DateTime | 最后执行时间 |
| NextTime | DateTime | 下次执行时间 |
| LastStatus | String | 最后状态 |
| LastMessage | String | 最后消息 |
| Remark | String | 备注 |

### Cron 表达式

```
秒 分 时 日 月 周
0 0 2 * * ?     每天凌晨2点
0 */5 * * * ?   每5分钟
0 0 8-18 * * ?  每天8点到18点整点
```

---

## C.8 附件实体（Attachment）

### 字段说明

| 字段名 | 类型 | 说明 |
|--------|------|------|
| Id | Int32 | 编号（主键） |
| Category | String | 分类 |
| FileName | String | 文件名 |
| Title | String | 标题 |
| Size | Int64 | 文件大小 |
| Extension | String | 扩展名 |
| ContentType | String | 内容类型 |
| FilePath | String | 存储路径 |
| Hash | String | 文件哈希 |
| Enable | Boolean | 是否启用 |
| UploadTime | DateTime | 上传时间 |
| Source | String | 来源 |
| TraceId | String | 追踪 ID |

### 常用方法

```csharp
// 获取文件路径
attachment.GetFilePath();

// 获取访问 URL
attachment.Url;

// 保存文件
attachment.SaveFile(stream);

// 读取文件
var stream = attachment.OpenRead();
```

---

## C.9 实体关系图

```
┌─────────────────────────────────────────────────────────────────┐
│                         用户与权限模块                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌────────┐    1:N    ┌────────┐    M:N    ┌────────┐          │
│  │  User  │ ───────── │  Role  │ ───────── │  Menu  │          │
│  └────────┘           └────────┘           └────────┘          │
│       │                                         │               │
│       │ N:1                                     │ 自关联        │
│       ▼                                         ▼               │
│  ┌────────────┐                           ┌────────┐           │
│  │ Department │ ─────────────────────────│  Menu  │           │
│  └────────────┘         自关联            │ Parent │           │
│       │                                   └────────┘           │
│       │ 自关联                                                  │
│       ▼                                                        │
│  ┌────────────┐                                                │
│  │ Department │                                                │
│  │   Parent   │                                                │
│  └────────────┘                                                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                         用户扩展模块                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌────────┐    1:N    ┌─────────────┐                          │
│  │  User  │ ───────── │ UserToken   │                          │
│  └────────┘           └─────────────┘                          │
│       │                                                        │
│       │ 1:N           ┌─────────────┐                          │
│       └────────────── │ UserConnect │  (第三方登录绑定)         │
│       │               └─────────────┘                          │
│       │ 1:N           ┌─────────────┐                          │
│       └────────────── │ UserOnline  │  (在线状态)              │
│       │               └─────────────┘                          │
│       │ 1:N           ┌─────────────┐                          │
│       └────────────── │   Log       │  (操作日志)              │
│                       └─────────────┘                          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                         多租户模块                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌────────┐    1:N    ┌────────────┐    N:1    ┌────────┐      │
│  │ Tenant │ ───────── │ TenantUser │ ───────── │  User  │      │
│  └────────┘           └────────────┘           └────────┘      │
│       │                                                        │
│       │ 1:N           ┌────────────┐                           │
│       └────────────── │ Department │  (租户部门)               │
│                       └────────────┘                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                         系统管理模块                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌────────┐           ┌─────────────┐                          │
│  │  App   │ ───────── │  AppModule  │  (应用插件)              │
│  └────────┘    1:N    └─────────────┘                          │
│                                                                 │
│  ┌────────────┐       ┌─────────────┐                          │
│  │ OAuthConfig│ ───── │  OAuthLog   │  (OAuth日志)             │
│  └────────────┘  1:N  └─────────────┘                          │
│                                                                 │
│  ┌────────────┐       ┌─────────────┐                          │
│  │ Attachment │       │  CronJob    │  (独立实体)              │
│  └────────────┘       └─────────────┘                          │
│                                                                 │
│  ┌────────────┐       ┌─────────────┐                          │
│  │ AccessRule │       │   Area      │  (独立实体)              │
│  └────────────┘       └─────────────┘                          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## C.10 实体扩展指南

### 扩展现有实体

```csharp
// 使用 partial 类扩展
public partial class User
{
    /// <summary>自定义属性</summary>
    public String CustomProperty { get; set; }
    
    /// <summary>自定义方法</summary>
    public static IList<User> SearchCustom(String keyword)
    {
        return FindAll(_.Name.Contains(keyword) | _.DisplayName.Contains(keyword));
    }
}
```

### 使用扩展字段

```csharp
// 使用 Ex1-Ex6 扩展字段
public partial class User
{
    /// <summary>积分</summary>
    public Int32 Points
    {
        get => Ex1;
        set => Ex1 = value;
    }
    
    /// <summary>余额</summary>
    public Decimal Balance
    {
        get => Ex4;
        set => Ex4 = value;
    }
}
```

---

## 本附录小结

本附录提供了内置实体的完整参考：

1. 内置实体一览
2. 用户实体详解
3. 角色实体详解
4. 菜单实体详解
5. 部门实体详解
6. 日志实体详解
7. 定时作业实体详解
8. 附件实体详解
9. 实体关系图
10. 实体扩展指南

---

**下一章**：[附录D 版本历史](附录D_版本历史.md) - 版本变更记录
