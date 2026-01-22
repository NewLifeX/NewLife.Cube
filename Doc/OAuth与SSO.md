# 第13章 OAuth 与 SSO

> 本章介绍魔方的 OAuth 第三方登录和 SSO 单点登录功能。
> 
> 支持微信、QQ、钉钉、GitHub 等多种第三方登录，以及企业内部的 SSO 集成。

---

## 13.1 OAuth 概述

### 什么是 OAuth

OAuth（Open Authorization）是一种开放标准的授权协议，允许用户授权第三方应用访问其在某服务提供商上的信息，而无需将用户名和密码提供给第三方应用。

### OAuth 2.0 授权流程

```
┌────────┐                              ┌────────────┐
│  用户  │                              │ 第三方平台  │
└────┬───┘                              │(微信/QQ等) │
     │                                  └─────┬──────┘
     │ 1.点击第三方登录                        │
     v                                        │
┌────────┐  2.重定向到授权页  ┌────────┐      │
│ 魔方   │─────────────────>│ 授权页 │<─────┘
│ 应用   │                   └───┬────┘
└────┬───┘                       │
     │                           │ 3.用户授权
     │                           v
     │  4.返回授权码      ┌────────────┐
     │<───────────────────│ 回调接口   │
     │                    └────────────┘
     │
     │ 5.用授权码换取令牌
     │───────────────────────────────────>
     │                                    │
     │ 6.返回访问令牌                       │
     │<───────────────────────────────────┤
     │                                    │
     │ 7.获取用户信息                       │
     │───────────────────────────────────>
     │                                    │
     │ 8.返回用户信息                       │
     │<───────────────────────────────────┤
     v
┌────────┐
│ 登录   │
│ 完成   │
└────────┘
```

---

## 13.2 支持的第三方登录

魔方内置支持以下第三方登录：

| 平台 | 类名 | 说明 |
|------|------|------|
| QQ | QQClient | QQ 互联 |
| 微信 | WeixinClient | 微信开放平台 |
| 微博 | WeiboClient | 新浪微博 |
| 支付宝 | AlipayClient | 支付宝开放平台 |
| 淘宝 | TaobaoClient | 淘宝开放平台 |
| GitHub | GithubClient | GitHub OAuth |
| 百度 | BaiduClient | 百度开放平台 |
| 钉钉 | DingTalkClient | 钉钉开放平台 |
| 企业微信 | WorkWeixinClient | 企业微信 |
| 微软 | MicrosoftClient | Microsoft Azure AD |
| IdentityServer4 | IdentityServer4Client | IdentityServer4 |

### QQ / 微信 / 微博

```csharp
// QQ 登录
services.AddOAuth<QQClient>(options =>
{
    options.AppId = "your-app-id";
    options.Secret = "your-app-secret";
});

// 微信登录
services.AddOAuth<WeixinClient>(options =>
{
    options.AppId = "your-app-id";
    options.Secret = "your-app-secret";
    options.Scope = "snsapi_login";
});

// 微博登录
services.AddOAuth<WeiboClient>(options =>
{
    options.AppId = "your-app-id";
    options.Secret = "your-app-secret";
});
```

### 支付宝 / 淘宝

```csharp
// 支付宝登录
services.AddOAuth<AlipayClient>(options =>
{
    options.AppId = "your-app-id";
    options.PrivateKey = "your-private-key";
    options.PublicKey = "alipay-public-key";
});

// 淘宝登录
services.AddOAuth<TaobaoClient>(options =>
{
    options.AppId = "your-app-id";
    options.Secret = "your-app-secret";
});
```

### GitHub / 百度

```csharp
// GitHub 登录
services.AddOAuth<GithubClient>(options =>
{
    options.AppId = "your-client-id";
    options.Secret = "your-client-secret";
});

// 百度登录
services.AddOAuth<BaiduClient>(options =>
{
    options.AppId = "your-api-key";
    options.Secret = "your-secret-key";
});
```

### 钉钉 / 企业微信

```csharp
// 钉钉登录
services.AddOAuth<DingTalkClient>(options =>
{
    options.AppId = "your-app-id";
    options.Secret = "your-app-secret";
});

// 企业微信登录
services.AddOAuth<WorkWeixinClient>(options =>
{
    options.CorpId = "your-corp-id";
    options.AgentId = "your-agent-id";
    options.Secret = "your-secret";
});
```

### 微软 / IdentityServer4

```csharp
// 微软登录
services.AddOAuth<MicrosoftClient>(options =>
{
    options.AppId = "your-client-id";
    options.Secret = "your-client-secret";
    options.Tenant = "common";  // 或具体租户ID
});

// IdentityServer4
services.AddOAuth<IdentityServer4Client>(options =>
{
    options.Server = "https://your-ids4-server.com";
    options.AppId = "your-client-id";
    options.Secret = "your-client-secret";
});
```

---

## 13.3 OAuth 客户端配置

### OAuthConfig 配置

在 `appsettings.json` 中配置 OAuth：

```json
{
  "OAuth": {
    "QQ": {
      "AppId": "your-qq-app-id",
      "Secret": "your-qq-secret",
      "Enable": true
    },
    "Weixin": {
      "AppId": "your-weixin-app-id",
      "Secret": "your-weixin-secret",
      "Enable": true
    },
    "GitHub": {
      "AppId": "your-github-client-id",
      "Secret": "your-github-secret",
      "Enable": true
    },
    "DingTalk": {
      "AppId": "your-dingtalk-app-id",
      "Secret": "your-dingtalk-secret",
      "Enable": true
    }
  }
}
```

### 应用 Key / Secret

获取第三方平台的应用密钥：

| 平台 | 申请地址 | 说明 |
|------|---------|------|
| QQ | https://connect.qq.com | QQ 互联 |
| 微信 | https://open.weixin.qq.com | 微信开放平台 |
| GitHub | https://github.com/settings/developers | OAuth Apps |
| 钉钉 | https://open.dingtalk.com | 钉钉开放平台 |

### 回调地址设置

配置 OAuth 回调地址：

```csharp
// 回调地址格式
// https://your-domain.com/Sso/LoginCallback/{provider}

// 示例
// QQ：https://your-domain.com/Sso/LoginCallback/QQ
// 微信：https://your-domain.com/Sso/LoginCallback/Weixin
// GitHub：https://your-domain.com/Sso/LoginCallback/GitHub
```

---

## 13.4 SSO 单点登录

### SsoClient（SSO 客户端）

将应用接入 SSO 用户中心：

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // 配置 SSO 客户端
    services.AddSsoClient(options =>
    {
        options.Server = "https://sso.your-domain.com";
        options.AppId = "your-app-id";
        options.Secret = "your-app-secret";
    });
}
```

### SsoProvider（SSO 服务端）

搭建 SSO 用户中心服务端：

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // 配置 SSO 服务端
    services.AddSsoProvider(options =>
    {
        options.Issuer = "https://sso.your-domain.com";
        options.TokenExpireMinutes = 120;
    });
}

public void Configure(IApplicationBuilder app)
{
    // 启用 SSO 服务端
    app.UseSsoProvider();
}
```

### OAuthServer（OAuth 服务端）

魔方可以作为 OAuth 服务端，为其他应用提供认证服务：

```csharp
// 配置 OAuth 服务端
services.AddOAuthServer(options =>
{
    options.Issuer = "https://oauth.your-domain.com";
    options.AccessTokenExpireMinutes = 120;
    options.RefreshTokenExpireDays = 30;
});
```

### SSO 登录流程

```
┌─────────┐     ┌─────────┐     ┌─────────────┐
│ 子系统A │     │ 子系统B │     │ SSO用户中心 │
└────┬────┘     └────┬────┘     └──────┬──────┘
     │               │                  │
     │ 1.访问子系统A，未登录             │
     │──────────────────────────────────>
     │                                  │
     │ 2.重定向到SSO登录页               │
     │<──────────────────────────────────
     │                                  │
     │ 3.用户登录                        │
     │──────────────────────────────────>
     │                                  │
     │ 4.登录成功，重定向回子系统A        │
     │<──────────────────────────────────
     │                                  │
     │ 5.创建本地会话                    │
     │                                  │
     │               │ 6.访问子系统B      │
     │               │─────────────────>│
     │               │                  │
     │               │ 7.检测到SSO会话   │
     │               │<─────────────────│
     │               │                  │
     │               │ 8.自动登录        │
     v               v                  v
```

---

## 13.5 用户绑定策略

### 自动注册

OAuth 登录时自动创建新用户：

```csharp
public class OAuthUserService
{
    /// <summary>处理 OAuth 登录</summary>
    public User ProcessOAuthLogin(OAuthUserInfo userInfo)
    {
        // 查找已绑定的用户
        var binding = UserBinding.FindByOpenId(userInfo.Provider, userInfo.OpenId);
        if (binding != null)
        {
            return User.FindById(binding.UserId);
        }
        
        // 自动注册新用户
        if (CubeSetting.Current.OAuthAutoRegister)
        {
            var user = new User
            {
                Name = GenerateUsername(userInfo),
                DisplayName = userInfo.NickName,
                Avatar = userInfo.Avatar,
                Enable = true
            };
            user.Insert();
            
            // 创建绑定
            CreateBinding(user, userInfo);
            
            return user;
        }
        
        return null;
    }
}
```

### 强制绑定

要求 OAuth 用户绑定已有账户：

```csharp
// 强制绑定策略
public enum OAuthBindingPolicy
{
    /// <summary>用户名绑定</summary>
    Username,
    
    /// <summary>手机号绑定</summary>
    Phone,
    
    /// <summary>邮箱绑定</summary>
    Email,
    
    /// <summary>工号绑定</summary>
    Code,
    
    /// <summary>昵称匹配</summary>
    NickName
}

// 实现绑定逻辑
public User BindExistingUser(OAuthUserInfo userInfo, OAuthBindingPolicy policy)
{
    User user = policy switch
    {
        OAuthBindingPolicy.Phone => User.FindByMobile(userInfo.Phone),
        OAuthBindingPolicy.Email => User.FindByMail(userInfo.Email),
        OAuthBindingPolicy.NickName => User.FindByDisplayName(userInfo.NickName),
        _ => null
    };
    
    if (user != null)
    {
        CreateBinding(user, userInfo);
    }
    
    return user;
}
```

### 角色同步 / 部门同步

从 SSO 同步角色和部门信息：

```csharp
public void SyncUserInfo(User user, SsoUserInfo ssoInfo)
{
    // 同步角色
    if (CubeSetting.Current.SsoSyncRoles)
    {
        var roleIds = new List<Int32>();
        foreach (var roleName in ssoInfo.Roles)
        {
            var role = Role.GetOrAdd(roleName);
            roleIds.Add(role.Id);
        }
        user.RoleIds = roleIds.Join(",");
    }
    
    // 同步部门
    if (CubeSetting.Current.SsoSyncDepartment)
    {
        var dept = Department.FindByName(ssoInfo.Department);
        if (dept != null)
        {
            user.DepartmentId = dept.Id;
        }
    }
    
    user.Update();
}
```

---

## 13.6 委托代理（Principal Agent）

### 代理登录

允许管理员代理其他用户登录：

```csharp
[EntityAuthorize(PermissionFlags.All)]
public ActionResult SwitchUser(Int32 userId)
{
    // 检查权限
    var currentUser = ManageProvider.User as User;
    if (!currentUser.IsAdmin)
        return Unauthorized();
    
    // 获取目标用户
    var targetUser = User.FindById(userId);
    if (targetUser == null)
        return NotFound();
    
    // 记录代理日志
    WriteLog("代理登录", $"{currentUser.Name} 代理登录为 {targetUser.Name}");
    
    // 保存原用户信息
    Session["OriginalUser"] = currentUser.Id;
    
    // 切换到目标用户
    ManageProvider.Provider.Current = targetUser;
    
    return RedirectToAction("Index", "Home");
}

[HttpPost]
public ActionResult RestoreUser()
{
    var originalUserId = Session["OriginalUser"] as Int32?;
    if (originalUserId == null)
        return BadRequest();
    
    var originalUser = User.FindById(originalUserId.Value);
    if (originalUser == null)
        return NotFound();
    
    // 恢复原用户
    ManageProvider.Provider.Current = originalUser;
    Session.Remove("OriginalUser");
    
    return RedirectToAction("Index", "Home");
}
```

---

## OAuth 配置示例

### 完整配置示例

```json
{
  "OAuth": {
    "Enable": true,
    "AutoRegister": true,
    "BindingPolicy": "Phone",
    "SyncRoles": true,
    "SyncDepartment": true,
    
    "QQ": {
      "Enable": true,
      "AppId": "your-qq-app-id",
      "Secret": "your-qq-secret"
    },
    
    "Weixin": {
      "Enable": true,
      "AppId": "your-weixin-app-id",
      "Secret": "your-weixin-secret",
      "Scope": "snsapi_login"
    },
    
    "GitHub": {
      "Enable": true,
      "AppId": "your-github-client-id",
      "Secret": "your-github-secret",
      "Scope": "read:user user:email"
    },
    
    "DingTalk": {
      "Enable": true,
      "AppId": "your-dingtalk-app-id",
      "Secret": "your-dingtalk-secret"
    }
  },
  
  "SSO": {
    "Enable": true,
    "Server": "https://sso.your-domain.com",
    "AppId": "your-sso-app-id",
    "Secret": "your-sso-secret",
    "AutoLogin": true
  }
}
```

### 登录页面集成

```html
<div class="oauth-login">
    <h4>第三方登录</h4>
    <div class="oauth-buttons">
        @if (ViewBag.OAuthProviders.Contains("QQ"))
        {
            <a href="/Sso/Login?provider=QQ" class="btn btn-qq">
                <i class="fa fa-qq"></i> QQ登录
            </a>
        }
        @if (ViewBag.OAuthProviders.Contains("Weixin"))
        {
            <a href="/Sso/Login?provider=Weixin" class="btn btn-wechat">
                <i class="fa fa-weixin"></i> 微信登录
            </a>
        }
        @if (ViewBag.OAuthProviders.Contains("GitHub"))
        {
            <a href="/Sso/Login?provider=GitHub" class="btn btn-github">
                <i class="fa fa-github"></i> GitHub登录
            </a>
        }
    </div>
</div>
```

---

## 本章小结

通过本章学习，你应该掌握了：

1. **OAuth 原理**：授权码流程和令牌机制
2. **第三方登录**：QQ、微信、钉钉、GitHub 等配置
3. **SSO 单点登录**：客户端和服务端配置
4. **用户绑定**：自动注册和强制绑定策略
5. **角色同步**：从 SSO 同步用户角色和部门

**下一步**：

- 学习 [视图体系](视图体系.md) 了解登录页面定制
- 了解 [自定义登录与启动页](自定义登录与启动页.md) 的定制方法

---

## 参考资源

- [魔方登录验证机制](https://newlifex.com/cube/cube_auth)
- [自定义登录页](https://newlifex.com/cube/login)
