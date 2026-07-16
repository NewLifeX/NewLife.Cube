# 密码策略

魔方通过 `PasswordService` 提供密码强度验证，支持正则表达式自定义密码策略，可在管理后台动态配置。密码策略是安全体系的第一道防线，配合登录失败限制和账号锁定等机制，共同构成用户认证安全保障。

## 核心组件

`PasswordService` 位于 `NewLife.CubeNC/Services/PasswordService.cs`，使用编译缓存的正则表达式进行密码验证。

```csharp
public class PasswordService
{
    private Regex _regex;
    private String _old;

    /// <summary>验证密码强度</summary>
    /// <param name="password">待验证的密码</param>
    /// <returns>是否通过验证</returns>
    public Boolean Valid(String password)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));

        var set = CubeSetting.Current;
        // 未配置或星号表示无限制
        if (set.PaswordStrength.IsNullOrEmpty() || set.PaswordStrength == "*")
            return true;

        // 编译正则并缓存，配置变更时自动重建
        if (_regex == null || set.PaswordStrength != _old)
        {
            _regex = new Regex(set.PaswordStrength, RegexOptions.Compiled);
            _old = set.PaswordStrength;
        }

        return _regex.IsMatch(password);
    }
}
```

### 设计要点

- **正则编译缓存**：首次调用时编译正则表达式并缓存，后续调用直接复用，避免重复编译开销
- **动态感知配置**：当管理后台修改 `PaswordStrength` 配置后，下次验证自动使用新规则
- **空值安全**：`password` 为 `null` 时抛出参数异常，配置为空或 `*` 时直接放行

## 默认策略

默认密码正则表达式：

```
^(?=.*\d.*)(?=.*[a-z].*)(?=.*[A-Z].*)(?=.*[^(0-9a-zA-Z)].*).{8,32}$
```

该正则要求密码同时满足以下所有条件：

| 条件 | 正则部分 | 说明 |
|------|---------|------|
| 包含数字 | `(?=.*\d.*)` | 至少一个 0-9 数字 |
| 包含小写字母 | `(?=.*[a-z].*)` | 至少一个 a-z 字母 |
| 包含大写字母 | `(?=.*[A-Z].*)` | 至少一个 A-Z 字母 |
| 包含特殊字符 | `(?=.*[^(0-9a-zA-Z)].*)` | 至少一个非字母数字字符 |
| 长度限制 | `.{8,32}` | 8 到 32 个字符 |

### 密码示例

| 密码 | 是否通过 | 原因 |
|------|---------|------|
| `Abc@1234` | 通过 | 含大小写、数字、符号，长度 8 |
| `abc12345` | 不通过 | 缺少大写字母和特殊字符 |
| `ABCDEFGH` | 不通过 | 缺少小写字母、数字和特殊字符 |
| `Ab@1` | 不通过 | 长度不足 8 位 |
| `Qwer!234` | 通过 | 满足所有条件 |

## 配置方式

通过 `CubeSetting.PaswordStrength` 配置密码强度正则表达式。可在管理后台 **系统管理 → 系统设置 → 魔方设置** 中动态修改，无需重启应用。

### 配置选项

| 配置值 | 效果 | 适用场景 |
|--------|------|---------|
| 默认正则 | 要求数字+大小写+符号，8-32 位 | 生产环境 |
| `^(?=.*\d.*)(?=.*[a-zA-Z].*).{6,32}$` | 仅要求数字+字母，6 位起 | 内部测试 |
| `^.{8,}$` | 仅要求 8 位以上，不限字符类型 | 宽松模式 |
| `*` | 无任何限制 | 开发环境 |
| 空值 | 无任何限制 | 开发环境 |

### appsettings.json 配置示例

```json
{
  "CubeSetting": {
    "PaswordStrength": "^(?=.*\\d.*)(?=.*[a-z].*)(?=.*[A-Z].*)(?=.*[^(0-9a-zA-Z)].*).{8,32}$"
  }
}
```

## 关联安全配置

密码策略与以下安全机制协同工作：

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `PaswordStrength` | String | 默认正则 | 密码强度正则表达式 |
| `MaxLoginError` | Int32 | 5 | 同一用户或 IP 连续登录失败达此次数后禁止登录 |
| `LoginForbiddenTime` | Int32 | 300 | 触发限制后的禁止时间（秒） |

### 登录失败限制

当同一用户或同一 IP 地址在短时间内连续登录失败达到 `MaxLoginError` 次后，系统自动封禁该账号和 IP 地址 `LoginForbiddenTime` 秒。该机制有效防止暴力破解攻击。

## 使用方式

### 依赖注入

`PasswordService` 在 `AddCube()` 中自动注册为单例服务：

```csharp
public class MyController : Controller
{
    private readonly PasswordService _passwordService;

    public MyController(PasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    public IActionResult ChangePassword(String newPassword)
    {
        if (!_passwordService.Valid(newPassword))
            return BadRequest("密码不符合强度要求");

        // 执行密码修改逻辑...
    }
}
```

### 内置集成点

`PasswordService` 在以下场景自动调用：

| 场景 | 触发位置 | 说明 |
|------|---------|------|
| 用户注册 | `User.Register()` | 注册时验证新密码 |
| 修改密码 | `User.ChangePassword()` | 用户或管理员修改密码时验证 |
| 重置密码 | `UserController.ResetPassword()` | 管理后台重置密码时验证 |

## 密码存储

魔方使用**加盐哈希**存储密码，即使两个用户使用相同明文密码，由于随机盐不同，存储在数据库中的密码哈希值也不同。在最坏的拖库情况下，攻击者无法通过字典爆破得到明文密码。

## 最佳实践

1. **生产环境**：使用默认正则或更严格的规则，确保密码包含多种字符类型
2. **禁用密码登录**：企业级应用推荐对接 SSO 用户中心，关闭密码登录功能
3. **部署后改密**：系统部署完成后，务必修改默认 admin 密码或创建新管理员并禁用 admin
4. **定期更新**：根据安全审计要求，可随时在后台调整密码策略，立即生效
5. **开发环境**：可设置为 `*` 简化测试，但切勿在生产环境使用

## 缓存机制

`PasswordService` 内部缓存编译后的正则表达式。当配置变更时自动重新编译，避免重复编译开销。

## 登录集成

`UserService` 在用户注册和修改密码时自动调用 `PasswordService.Valid()` 验证密码强度。密码不符合要求时将返回错误。
