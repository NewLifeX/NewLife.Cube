# 应用系统与令牌

魔方提供应用系统管理功能，支持多应用认证、密钥管理和 JWT 令牌颁发，是 OAuth 服务端和 API 网关的基础设施。

## 应用系统（App）

### 实体说明

`App` 实体表示一个接入魔方的应用系统：

| 字段 | 说明 |
|------|------|
| `Name` | 应用名称（唯一标识） |
| `DisplayName` | 显示名称 |
| `Secret` | 应用密钥（AppSecret） |
| `Enable` | 是否启用 |
| `Auths` | 认证次数（累加字段） |
| `WhiteIPs` | IP 白名单 |
| `BlackIPs` | IP 黑名单 |
| `ExpireDate` | 过期日期 |
| `HomepageUrl` | 主页地址 |
| `CallbackUrl` | 回调地址（OAuth 用） |
| `RoleIds` | 角色集合（授权映射） |

### 自动创建

当未知应用首次请求认证时，魔方会自动创建一条 App 记录，但处于 **禁用状态**，需管理员在后台手动启用。

## 令牌服务（TokenService）

`TokenService` 负责应用认证和 JWT 令牌的颁发与验证。

### 应用认证

```csharp
// 通过应用名和密钥认证
var app = tokenService.Authorize(username, password, autoRegister: true, ip);
```

认证流程：
1. 按用户名查找应用（支持自动注册）
2. 验证应用启用状态和过期时间
3. 验证密钥（AppSecret）
4. 检查 IP 黑白名单
5. 认证成功，累加认证次数

### JWT 令牌颁发

```csharp
var tokenModel = tokenService.IssueToken(appName, secret, expire, userId);
// tokenModel.AccessToken  — 访问令牌
// tokenModel.RefreshToken — 刷新令牌
// tokenModel.Expire       — 有效期（秒）
```

支持自定义 JWT 算法和密钥：
- 默认使用 `CubeSetting.JwtSecret`
- 应用级密钥格式：`HS256:your-secret-key`
- 令牌中可嵌入自定义 ID

### 令牌有效期

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `TokenExpire` | 7200 秒 | 访问令牌有效期 |
| `JwtSecret` | 自动生成 | JWT 签名密钥 |

## 用户令牌（UserToken）

`UserToken` 实体记录已颁发的令牌信息，支持令牌审计和管理：
- 查看在线令牌
- 令牌关联用户
- 强制失效令牌

## 管理后台

- **魔方管理 → 应用系统**：管理接入应用、配置密钥和白名单
- **系统管理 → 用户令牌**：查看已颁发令牌、强制失效
- **魔方管理 → 应用日志**：查看应用操作日志
