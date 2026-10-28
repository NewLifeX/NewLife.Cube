# 附录B API参考

> 本附录只补充第 36 章未展开的内容：服务接口清单、权限枚举、常用 Header、文件管理与批量导出入口。
> 路由、响应信封、错误码、分页排序、搜索过滤、写操作语义、认证与异常边界统一见 [第36章 WebAPI接口规范](Api/WebAPI接口规范.md)；字段级契约见 `/swagger`。

---

## B.1 服务接口清单

服务控制器（`Auth`、`Sso`、`Cube` 的服务动作）由 `[Route("[controller]/[action]")]` 暴露，**不带 `/api` 前缀**。

### 认证与账号

| 接口 | 方法 | 说明 |
|------|------|------|
| `/Auth/Login` | POST | 用户名密码 / 验证码登录（`category` 为空表示密码登录） |
| `/Auth/Refresh` | POST | 刷新访问令牌 |
| `/Auth/Info` | GET | 当前登录用户信息 |
| `/Auth/Logout` | POST | 登出 |
| `/Auth/Register` | POST | 注册（多租户需带 `X-App-Id` 或 `X-Tenant`） |
| `/Auth/SendCode` | POST | 发送手机/邮箱验证码 |
| `/Auth/ResetPassword` | POST | 验证码重置密码 |
| `/Auth/Challenge` | GET | Challenge-Response 公钥与 `challengeId` |
| `/Sso/WxMiniLogin` | POST | 微信小程序登录（`{ code, appId? }`） |
| `/Sso/WxAppLogin` | POST | 微信 App 登录 |

### 站点与公共数据

| 接口 | 方法 | 说明 |
|------|------|------|
| `/Cube/MenuTree` | GET | 当前用户可访问菜单树（`?module=` 过滤） |
| `/Cube/Lookup?codes=enable,sex` | GET | 枚举字典（需登录，最多 20 个已加载枚举） |
| `/Cube/UserSearch` | GET | 用户选择器（`roleId` / `departmentId` / `key`） |
| `/Cube/GetPageConfig?kind=&page=` | GET | 低代码页面配置 |
| `/Cube/Health?state=` | GET | 健康检测（匿名，仅 status/time/state） |
| `/Cube/Info` | GET | 服务器信息（仅系统管理员） |
| `/Cube/Apis` | GET | 端点清单（系统管理员 + `EnableApiExplorer=true`，默认关闭） |

### 实体接口

CRUD、元数据（`meta`）、图表（`charts`）、导入导出（`import` / `export`）与附件（`attachments`）统一见 [第36章 36.2](Api/WebAPI接口规范.md)。

---

## B.2 权限标志说明

`PermissionFlags`（`XCode.Membership`，`UInt32`）：

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | None | 无权限；`[EntityAuthorize]` 不带权限位时表示只需登录 |
| 1 | Detail | 查看 |
| 2 | Insert | 添加 |
| 4 | Update | 修改 |
| 8 | Delete | 删除 |
| 4294967295 | All | 所有权限 |

```text
查看 + 添加 + 修改 = 1 + 2 + 4 = 7
查看 + 添加 + 修改 + 删除 = 1 + 2 + 4 + 8 = 15
```

> 动作与权限的对应关系见 [第36章 36.2](Api/WebAPI接口规范.md)；多个权限位之间是"或"的关系（如附件上传 `Insert` 或 `Update`）。

---

## B.3 常用 Header

### 请求

| Header | 说明 |
|--------|------|
| `Authorization` | `Bearer {token}`，令牌来源见 [36.9](Api/WebAPI接口规范.md) |
| `X-Token` | `{token}`（兼容） |
| `Content-Type` | `application/json` 或 `multipart/form-data` |
| `X-Tenant` | 租户编码，多租户开启时用于定位/校验租户（旧的 `X-Tenant-Id` 数字 ID 头已移除） |
| `X-App-Id` | OAuth 应用 AppId，注册时定位所属租户、微信登录 appId 兜底 |

### 响应

| Header | 说明 |
|--------|------|
| `X-Tenant` | 微信登录成功后返回租户编码，供客户端后续请求携带 |

---

## B.4 文件管理接口

`FileController`（区域 Admin）提供服务器文件管理，路由带 `/api`：

| 接口 | 方法 | 路径 | 说明 |
|------|------|------|------|
| 列表 | GET | `/api/Admin/File?r=` | 浏览目录 |
| 上传 | POST | `/api/Admin/File/Upload` | multipart：`r`（目录）、`file` |
| 删除 | POST | `/api/Admin/File/Delete` | `r`（路径） |
| 压缩 / 解压 | POST | `/api/Admin/File/Compress`、`/Decompress` | 目录打包与还原 |
| 复制 / 粘贴 / 移动 | POST | `/api/Admin/File/Copy`、`/Paste`、`/Move` | |
| 下载 | POST | `/api/Admin/File/Download` | 打包下载 |

> 实体附件上传用 `POST /api/{area}/{controller}/attachments`（multipart：`file` / `id` / `title`，需 Insert 或 Update），与文件管理器不同，见 [第36章 36.2](Api/WebAPI接口规范.md)。

---

## B.5 批量与导入导出

- **批量删除**：`DELETE /api/{area}/{controller}?ids=1,2,3`（单次最多 100 个，超出由前端分片）；`?<搜索条件>` 为条件删除，必须至少一个业务条件，否则返回 `-2`。
- **导入**：`POST /api/{area}/{controller}/import`（multipart），需 Insert 权限。
- **导出**：`GET /api/{area}/{controller}/export?format=excel`（excel/xlsx/csv/json/xml/zip），需 Detail 权限，返回文件流而非信封。
- **批量启用/禁用**：框架未内置，属各项目自定义 Action（MVC 版 `SetEnable` 可参考）。

> 完整语义见 [第36章 36.2 / 36.7](Api/WebAPI接口规范.md)。

---

## 本附录小结

1. 服务接口清单（认证、站点与公共数据）
2. 权限标志枚举与计算
3. 常用请求 / 响应 Header
4. 文件管理接口
5. 批量与导入导出入口

---

**下一章**：[附录C 实体参考](附录C_实体参考.md) - 内置实体说明