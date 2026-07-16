# 验证码服务

魔方内置多渠道验证码服务，支持短信和邮件两种验证码发送方式，覆盖登录、重置密码、绑定手机/邮箱三种业务场景。

## 架构设计

```
UserService（登录/注册）
  ├─ ISmsVerifyCode（短信验证码接口）
  │    └─ AliyunSmsVerifyCode（阿里云短信实现）
  └─ IMailVerifyCode（邮件验证码接口）
       └─ SmtpMailVerifyCode（SMTP邮件实现）
```

验证码服务通过接口抽象，支持自定义实现和扩展。框架默认提供阿里云短信和 SMTP 邮件两种实现。

## 业务场景

两个接口均定义了三种验证码场景：

| 方法 | 场景 | 说明 |
|------|------|------|
| `SendLogin` | 登录验证码 | 用户通过手机号/邮箱登录时发送 |
| `SendReset` | 重置密码 | 用户找回密码时发送 |
| `SendBind` | 绑定验证 | 用户绑定手机号/邮箱时发送 |

## 短信验证码

### ISmsVerifyCode 接口

```csharp
public interface ISmsVerifyCode
{
    Task<String> SendLogin(String mobile, String code, Int32 expire, SmsVerifyCodeOptions options = null);
    Task<String> SendReset(String mobile, String code, Int32 expire, SmsVerifyCodeOptions options = null);
    Task<String> SendBind(String mobile, String code, Int32 expire, SmsVerifyCodeOptions options = null);
}
```

### 阿里云短信实现

`AliyunSmsVerifyCode` 通过阿里云短信服务发送验证码：

```csharp
services.AddSingleton<ISmsVerifyCode>(sp =>
{
    var sms = new AliyunSmsVerifyCode
    {
        SignName = "您的签名",
        SchemaName = "您的方案名",
        CodeLength = 6,
    };
    sms.Client.AccessKeyId = "your-key-id";
    sms.Client.AccessKeySecret = "your-key-secret";
    return sms;
});
```

| 属性 | 说明 |
|------|------|
| `SignName` | 短信签名名称 |
| `SchemaName` | 方案名称 |
| `CodeLength` | 验证码长度，默认 4 位 |
| `Client` | 阿里云客户端，Endpoint 为 `dypnsapi.aliyuncs.com` |

### 管理后台配置

短信配置也可以在管理后台 **系统管理 → 短信配置** 中管理，支持多个短信服务商配置。

## 邮件验证码

### IMailVerifyCode 接口

```csharp
public interface IMailVerifyCode
{
    Task<String> SendLogin(String mail, String code, Int32 expire, MailVerifyCodeOptions options = null);
    Task<String> SendReset(String mail, String code, Int32 expire, MailVerifyCodeOptions options = null);
    Task<String> SendBind(String mail, String code, Int32 expire, MailVerifyCodeOptions options = null);
}
```

### SMTP 邮件实现

`SmtpMailVerifyCode` 通过 SMTP 服务器发送验证码邮件：

```csharp
services.AddSingleton<IMailVerifyCode>(sp =>
{
    return new SmtpMailVerifyCode
    {
        Server = "smtp.example.com",
        Port = 465,
        EnableSsl = true,
        From = "noreply@example.com",
        DisplayName = "我的系统",
        Username = "noreply@example.com",
        Password = "your-password",
    };
});
```

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `Server` | — | SMTP 服务器地址 |
| `Port` | 25 | SMTP 端口 |
| `EnableSsl` | false | 是否使用 SSL |
| `From` | — | 发件人邮箱 |
| `DisplayName` | — | 发件人显示名称 |
| `Username` | — | SMTP 用户名 |
| `Password` | — | SMTP 密码 |
| `SubjectTemplate` | "您的验证码" | 邮件主题模板 |
| `BodyTemplate` | 含 {code} 和 {expire} 占位符 | 邮件正文模板 |

### 管理后台配置

邮件配置在管理后台 **系统管理 → 邮件配置** 中管理。

## 验证码记录

`VerifyCodeRecord` 实体记录每次验证码发送的详细信息，支持审计追踪：
- 发送目标（手机号/邮箱）
- 验证码内容
- 发送时间和 IP
- 业务场景
- 租户隔离（TenantModule）

## 自定义实现

如需对接其它短信/邮件服务商，实现对应接口并注册到 DI 容器即可：

```csharp
// 自定义短信服务
public class MySmsSender : ISmsVerifyCode
{
    public async Task<String> SendLogin(String mobile, String code, Int32 expire, SmsVerifyCodeOptions options = null)
    {
        // 调用第三方 API
        return "发送成功";
    }
    // ... 其它方法
}

// 注册
services.AddSingleton<ISmsVerifyCode, MySmsSender>();
```
