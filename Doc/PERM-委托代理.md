# 委托代理

魔方支持用户权限委托代理功能，允许用户将自己的身份和权限临时委托给另一个用户。代理人在下一次登录时将自动获得委托人的身份权限，该机制为一次性消耗（除非配置无限次数），使用后自动失效。

## 设计理念

委托代理是企业级应用中常见的权限管理需求。在实际业务中，经常需要临时授权他人代为操作：

- **领导出差**：将审批权限委托给副手处理紧急事务
- **临时授权**：委托同事代为处理某项业务操作
- **角色切换**：管理员以普通用户身份验证功能表现
- **交接过渡**：人员变动时临时委托权限过渡

## PrincipalAgent 实体

`PrincipalAgent` 实体存储委托代理关系，位于 `NewLife.Cube/Entity/委托代理.cs`。

### 核心字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | Int32 | 编号（主键） |
| `PrincipalId` | Int32 | 委托人 ID，即授出权限的用户 |
| `AgentId` | Int32 | 代理人 ID，即获得权限的用户 |
| `Enable` | Boolean | 是否启用 |
| `Times` | Int32 | 可用次数。0=已用完，-1=无限制，正数=剩余次数 |
| `Expire` | DateTime | 有效期截止时间 |
| `CreateUserId` | Int32 | 创建者 ID |
| `CreateTime` | DateTime | 创建时间 |
| `CreateIP` | String | 创建 IP |
| `Remark` | String | 备注（如安全限制提示） |

### 扩展属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Principal` | IUser | 委托人用户对象（通过缓存关联） |
| `PrincipalName` | String | 委托人名称 |
| `Agent` | IUser | 代理人用户对象（通过缓存关联） |
| `AgentName` | String | 代理人名称 |

### 索引

| 索引 | 字段 | 用途 |
|------|------|------|
| `IX_PrincipalAgent_PrincipalId` | PrincipalId | 按委托人查询 |
| `IX_PrincipalAgent_AgentId` | AgentId | 按代理人查询（登录时使用） |

## 登录集成流程

委托代理在用户登录时自动检查和切换，集成在 `ManageProvider2.CheckAgent()` 方法中。

### 完整流程

```
用户登录 → ManageProvider2.Login()
    ↓
正常身份验证通过
    ↓
CheckAgent(user) 检查委托代理
    ↓
查询 PrincipalAgent 表：AgentId = user.ID AND Enable = true
    ↓
┌──── 无有效记录 ────→ 返回原用户身份
│
├──── 有效记录但委托人是系统管理员 ────→ 拒绝代理，自动禁用该记录
│
└──── 有效记录且合法 ────→ 扣减次数 → 记录审计日志 → 返回委托人身份
```

### CheckAgent 核心逻辑

```csharp
public IManageUser CheckAgent(IManageUser user)
{
    if (user == null) return user;

    // 1. 查找代理人所有有效的代理项
    var list = PrincipalAgent.GetAllValidByAgentId(user.ID);
    if (list.Count == 0) return user;

    // 2. 清理失效记录（次数用完或已过期）
    foreach (var item in list)
    {
        if (item.Enable && (item.Times == 0 ||
            item.Expire.Year > 2000 && item.Expire < DateTime.Now))
        {
            item.Enable = false;
            item.Update();
        }
    }

    // 3. 获取第一个有效代理项
    var pa = list.FirstOrDefault(e => e.Enable);
    if (pa == null || pa.Principal == null) return user;

    // 4. 安全检查：禁止代理系统管理员
    var roles = pa.Principal?.Roles;
    if (roles != null && roles.Any(e => e.IsSystem))
    {
        pa.Enable = false;
        pa.Remark = "安全起见，不得代理系统管理员";
        pa.Update();

        LogProvider.Provider.WriteLog("用户", "代理", false,
            $"安全起见，[{pa.AgentName}]不得代理系统管理员[{pa.PrincipalName}]的身份权限",
            pa.AgentId, pa.AgentName);

        return user;
    }

    // 5. 扣减可用次数
    pa.Times--;
    if (pa.Times == 0) pa.Enable = false;
    pa.Update();

    // 6. 记录审计日志（双方各一条）
    LogProvider.Provider.WriteLog("用户", "委托", true,
        $"委托[{pa.AgentName}]使用[{pa.PrincipalName}]的身份权限",
        pa.PrincipalId, pa.PrincipalName);
    LogProvider.Provider.WriteLog("用户", "代理", true,
        $"[{pa.AgentName}]代理使用[{pa.PrincipalName}]的身份权限",
        pa.AgentId, pa.AgentName);

    // 7. 返回委托人身份
    return pa.Principal as IManageUser;
}
```

## 安全约束

委托代理设计了多层安全防护：

| 约束 | 说明 |
|------|------|
| 不能自我委托 | 委托人和代理人不能是同一个人 |
| 禁止代理系统管理员 | 拥有 `IsSystem=true` 角色的用户不可被代理，防止权限越级 |
| 一次性消耗 | 默认每次代理消耗一次可用次数，用完自动禁用 |
| 过期自动失效 | 超过有效期的委托记录在登录时自动禁用 |
| 审计日志 | 每次代理操作在委托人和代理人双方各记录一条审计日志 |

### 安全限制详解

当代理人试图代理拥有系统管理员角色的用户时，系统会：
1. 自动禁用该委托记录
2. 在 `Remark` 中写入"安全起见，不得代理系统管理员"
3. 记录一条失败的审计日志
4. 返回代理人自己的身份（不切换）

## 查询方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `GetAllValidByAgentId` | `IList<PrincipalAgent>(Int32 agentId)` | 获取代理人所有启用的代理项（登录时调用） |
| `FindAllByPrincipalId` | `IList<PrincipalAgent>(Int32 principalId)` | 获取某委托人创建的所有委托记录 |
| `FindAllByAgentId` | `IList<PrincipalAgent>(Int32 agentId)` | 获取某代理人的所有代理项 |
| `FindById` | `PrincipalAgent(Int32 id)` | 按编号查找单条记录 |

## 管理后台

委托代理在管理后台 **魔方管理 → 委托代理** 中管理。

### 创建委托

新建委托时的默认值：

| 字段 | 默认值 | 说明 |
|------|--------|------|
| `Enable` | true | 默认启用 |
| `Times` | 1 | 默认一次有效 |
| `Expire` | 当前时间 + 20 分钟 | 默认 20 分钟有效期 |

### 管理操作

- **创建**：指定委托人和代理人，设置可用次数和有效期
- **编辑**：修改可用次数、有效期或启用状态
- **禁用**：将 `Enable` 设为 `false`，立即取消委托
- **查看日志**：通过审计日志追溯所有委托和代理行为

## 使用示例

### 场景一：领导出差委托

```
操作步骤：
1. 管理员进入"魔方管理 → 委托代理"
2. 新建记录：委托人=领导，代理人=副手
3. 设置次数=5，有效期=3天后
4. 副手下次登录时自动获得领导身份
5. 操作完成后注销，下次登录恢复自己身份
6. 5次使用后委托自动失效
```

### 场景二：一次性临时授权

```
操作步骤：
1. 委托人在后台创建委托
2. 次数=1，有效期=20分钟（默认）
3. 代理人登录后自动切换，操作完毕注销
4. 委托自动失效，无需手动取消
```

## 注意事项

1. **身份切换是全局的**：代理人登录后，整个会话期间以委托人身份操作，所有行为记录在委托人名下
2. **多条委托**：如果代理人有多条有效委托，取第一条启用的记录
3. **注销恢复**：代理人注销后重新登录，如仍有有效委托记录则继续切换；否则恢复自己身份
4. **无限次数**：将 `Times` 设为 -1 可实现长期委托，需手动禁用
5. **审计追溯**：所有委托和代理行为均记录审计日志，可在日志管理中按"委托"或"代理"搜索
- 查看委托使用记录
