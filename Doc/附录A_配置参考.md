# 附录A 配置参考

> 本附录提供魔方应用的完整配置说明。

---

## A.1 appsettings.json 完整示例

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  
  "ConnectionStrings": {
    "Membership": "Data Source=..\\Data\\Membership.db;Provider=SQLite",
    "Cube": "Data Source=..\\Data\\Cube.db;Provider=SQLite",
    "Log": "Data Source=..\\Data\\Log.db;Provider=SQLite",
    "School": "Data Source=..\\Data\\School.db;Provider=SQLite"
  },
  
  "Cube": {
    "IsNew": true,
    "Debug": false,
    "ShowRunTime": true,
    "ResourceDebug": false,
    "Skin": "LayuiAdmin",
    "Layout": "_Layout",
    "Theme": "purple",
    "DisplayName": "魔方管理平台",
    "DefaultRole": "普通用户",
    "AllowLogin": true,
    "AllowRegister": true,
    "AutoRegister": false,
    "ForceBinding": "mobile",
    "SessionTimeout": 20,
    "DefaultPermission": 15,
    "CookieDomain": "",
    "CookiePath": "/",
    "Token": "token",
    "JwtSecret": "your_jwt_secret_key_here",
    "TokenExpire": 7200,
    "RefreshTokenExpire": 604800,
    "SsoServer": "",
    "SsoAppId": "",
    "SsoSecret": "",
    "LoginTip": "",
    "WebOnline": true,
    "LogPath": "Log",
    "UploadPath": "Uploads",
    "AvatarPath": "Avatars",
    "MaxUploadSize": 10485760,
    "PluginServer": "https://x.newlifex.com/",
    "EmbeddedFileExpire": 600,
    "CacheExpire": 60,
    "Robot": "Googlebot|AdsBot|bingbot|Baiduspider|Sogou|360Spider|YisouSpider|Bytespider"
  },
  
  "XCode": {
    "Debug": false,
    "ShowSQL": false,
    "SQLPath": "Log",
    "TraceSQLTime": 1000,
    "Migration": 2,
    "Cache": {
      "Expiration": 60,
      "SingleCacheExpiration": 60,
      "EntityCacheExpiration": 60
    }
  },
  
  "OAuth": {
    "QQ": {
      "AppId": "",
      "Secret": "",
      "Scope": "get_user_info"
    },
    "WeChat": {
      "AppId": "",
      "Secret": "",
      "Scope": "snsapi_userinfo"
    },
    "GitHub": {
      "AppId": "",
      "Secret": "",
      "Scope": "user"
    },
    "DingTalk": {
      "AppId": "",
      "Secret": "",
      "Scope": ""
    }
  },
  
  "Star": {
    "Server": "http://star.newlifex.com:6600",
    "AppId": "MyApp",
    "Secret": ""
  }
}
```

---

## A.2 CubeSetting 配置项一览

### 基础设置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `IsNew` | Boolean | true | 是否新系统，首次运行后自动改为 false |
| `Debug` | Boolean | false | 调试模式 |
| `ShowRunTime` | Boolean | true | 显示运行时间 |
| `ResourceDebug` | Boolean | false | 资源调试 |

### 界面设置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `Skin` | String | "" | 皮肤名称（LayuiAdmin/Metronic/AdminLTE 等） |
| `Layout` | String | "_Layout" | 布局页名称 |
| `Theme` | String | "" | 主题颜色 |
| `DisplayName` | String | "魔方" | 系统显示名称 |

### 用户登录设置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `DefaultRole` | String | "普通用户" | 新注册用户默认角色 |
| `AllowLogin` | Boolean | true | 是否允许登录 |
| `AllowRegister` | Boolean | true | 是否允许注册 |
| `AutoRegister` | Boolean | false | 第三方登录后自动注册 |
| `ForceBinding` | String | "" | 强制绑定方式（mobile/email/username） |
| `SessionTimeout` | Int32 | 20 | 会话超时时间（分钟） |

### 权限设置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `DefaultPermission` | Int32 | 15 | 默认权限（1查看+2添加+4修改+8删除） |

### Cookie/Token 设置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `CookieDomain` | String | "" | Cookie 域名 |
| `CookiePath` | String | "/" | Cookie 路径 |
| `Token` | String | "token" | Token 在 Header/Cookie 中的名称 |
| `JwtSecret` | String | "" | JWT 签名密钥 |
| `TokenExpire` | Int32 | 7200 | Token 有效期（秒） |
| `RefreshTokenExpire` | Int32 | 604800 | 刷新 Token 有效期（秒） |

### SSO 设置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `SsoServer` | String | "" | SSO 服务器地址 |
| `SsoAppId` | String | "" | SSO 应用 ID |
| `SsoSecret` | String | "" | SSO 应用密钥 |

### 文件存储设置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `LogPath` | String | "Log" | 日志文件目录 |
| `UploadPath` | String | "Uploads" | 上传文件目录 |
| `AvatarPath` | String | "Avatars" | 头像目录 |
| `MaxUploadSize` | Int64 | 10485760 | 最大上传大小（字节） |

### 其他设置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `PluginServer` | String | "https://x.newlifex.com/" | 插件服务器地址 |
| `EmbeddedFileExpire` | Int32 | 600 | 嵌入式文件缓存时间（秒） |
| `CacheExpire` | Int32 | 60 | 缓存过期时间（秒） |
| `WebOnline` | Boolean | true | 记录用户在线状态 |
| `Robot` | String | "..." | 机器人 UA 标识 |

---

## A.3 连接字符串格式

### SQLite

```
Data Source=..\\Data\\MyApp.db;Provider=SQLite
```

### MySQL

```
Server=localhost;Port=3306;Database=MyApp;Uid=root;Pwd=password;Provider=MySql
```

### SQL Server

```
Server=localhost;Database=MyApp;Uid=sa;Pwd=password;Provider=SqlServer
```

或使用 Windows 身份验证：

```
Server=localhost;Database=MyApp;Integrated Security=True;Provider=SqlServer
```

### PostgreSQL

```
Server=localhost;Port=5432;Database=MyApp;Uid=postgres;Pwd=password;Provider=PostgreSQL
```

### Oracle

```
Data Source=localhost:1521/ORCL;User Id=system;Password=password;Provider=Oracle
```

### 达梦

```
Server=localhost;Port=5236;Database=MyApp;Uid=SYSDBA;Pwd=password;Provider=DaMeng
```

### 连接字符串选项

| 选项 | 说明 |
|------|------|
| `Provider` | 数据库提供者（必需） |
| `Migration` | 迁移模式（0关闭/1只检查/2全自动） |
| `TablePrefix` | 表名前缀 |
| `Owner` | 模式/所有者 |
| `Readonly` | 只读模式 |

---

## A.4 XCode 配置

```json
{
  "XCode": {
    "Debug": false,
    "ShowSQL": false,
    "SQLPath": "Log",
    "TraceSQLTime": 1000,
    "Migration": 2,
    "Cache": {
      "Expiration": 60,
      "SingleCacheExpiration": 60,
      "EntityCacheExpiration": 60
    }
  }
}
```

### XCode 配置说明

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `Debug` | Boolean | false | 调试模式 |
| `ShowSQL` | Boolean | false | 输出 SQL 语句 |
| `SQLPath` | String | "" | SQL 日志保存目录 |
| `TraceSQLTime` | Int32 | 1000 | 慢查询阈值（毫秒） |
| `Migration` | Int32 | 2 | 迁移模式（0关闭/1检查/2全自动） |
| `Cache.Expiration` | Int32 | 60 | 查询缓存过期时间（秒） |
| `Cache.SingleCacheExpiration` | Int32 | 60 | 单对象缓存过期时间（秒） |
| `Cache.EntityCacheExpiration` | Int32 | 60 | 实体缓存过期时间（秒） |

---

## A.5 OAuth 配置

### QQ 登录

```json
{
  "OAuth": {
    "QQ": {
      "AppId": "你的AppID",
      "Secret": "你的AppKey",
      "Scope": "get_user_info"
    }
  }
}
```

### 微信登录

```json
{
  "OAuth": {
    "WeChat": {
      "AppId": "你的AppID",
      "Secret": "你的AppSecret",
      "Scope": "snsapi_userinfo"
    }
  }
}
```

### GitHub 登录

```json
{
  "OAuth": {
    "GitHub": {
      "AppId": "你的Client ID",
      "Secret": "你的Client Secret",
      "Scope": "user"
    }
  }
}
```

### 钉钉登录

```json
{
  "OAuth": {
    "DingTalk": {
      "AppId": "你的AppKey",
      "Secret": "你的AppSecret",
      "Scope": ""
    }
  }
}
```

### 企业微信

```json
{
  "OAuth": {
    "WeWork": {
      "AppId": "企业ID",
      "Secret": "应用Secret",
      "AgentId": "应用AgentId"
    }
  }
}
```

---

## A.6 日志配置

### NewLife 日志配置

```json
{
  "NewLife": {
    "Log": {
      "Level": "Info",
      "Path": "Log",
      "FileFormat": "{0:yyyy_MM_dd}.log",
      "MaxBytes": 10485760,
      "Backups": 10
    }
  }
}
```

### ASP.NET Core 日志配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "NewLife": "Debug"
    }
  }
}
```

---

## A.7 星尘配置（可选）

```json
{
  "Star": {
    "Server": "http://star.newlifex.com:6600",
    "AppId": "你的应用ID",
    "Secret": "你的应用密钥",
    "TracerServer": "http://star.newlifex.com:6600",
    "ConfigServer": "http://star.newlifex.com:6680"
  }
}
```

---

## A.8 环境变量配置

魔方支持通过环境变量覆盖配置：

```bash
# 设置连接字符串
export ConnectionStrings__Membership="Server=localhost;Database=MyApp;..."

# 设置魔方配置
export Cube__DisplayName="我的系统"
export Cube__JwtSecret="my_jwt_secret"
```

Windows 命令：

```cmd
set ConnectionStrings__Membership=Server=localhost;Database=MyApp;...
set Cube__DisplayName=我的系统
```

Docker 环境变量：

```yaml
services:
  myapp:
    image: myapp:latest
    environment:
      - ConnectionStrings__Membership=Server=db;Database=MyApp;...
      - Cube__DisplayName=我的系统
      - Cube__JwtSecret=my_jwt_secret
```

---

## A.9 配置优先级

配置加载顺序（后加载的覆盖先加载的）：

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. 用户机密（开发环境）
4. 环境变量
5. 命令行参数
6. `Config/*.config` 文件

---

## 本附录小结

本附录提供了完整的配置参考：

1. appsettings.json 完整示例
2. CubeSetting 所有配置项说明
3. 各数据库连接字符串格式
4. XCode 配置
5. OAuth 配置
6. 日志配置
7. 环境变量配置方式

---

**下一章**：[附录B API参考](附录B_API参考.md) - 标准接口说明
