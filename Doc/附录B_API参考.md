# 附录B API参考

> 本附录提供魔方 WebAPI 版的标准接口说明和错误码对照。

---

## B.1 标准接口清单

### 用户认证接口

| 接口 | 方法 | 路径 | 说明 |
|------|------|------|------|
| 登录 | POST | `/Admin/User/Login` | 用户名密码登录 |
| 登出 | GET/POST | `/Admin/User/Logout` | 退出登录 |
| 获取当前用户 | GET | `/Admin/User/Info` | 获取当前登录用户信息 |
| 修改密码 | POST | `/Admin/User/ChangePassword` | 修改当前用户密码 |
| 注册 | POST | `/Admin/User/Register` | 新用户注册 |

### 登录接口详情

**POST** `/Admin/User/Login`

请求：
```json
{
  "username": "admin",
  "password": "admin"
}
```

响应：
```json
{
  "code": 0,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "abc123...",
    "expire": 7200,
    "user": {
      "id": 1,
      "name": "admin",
      "displayName": "管理员",
      "avatar": "/Avatars/1.png",
      "roleName": "管理员"
    }
  }
}
```

### 菜单接口

| 接口 | 方法 | 路径 | 说明 |
|------|------|------|------|
| 获取菜单树 | GET | `/Admin/Menu/GetMenuTree` | 获取用户有权限的菜单 |
| 获取菜单列表 | GET | `/Admin/Menu` | 菜单管理列表 |

**GET** `/Admin/Menu/GetMenuTree`

响应：
```json
{
  "code": 0,
  "data": [
    {
      "id": 1,
      "name": "系统管理",
      "displayName": "系统管理",
      "url": "/Admin",
      "icon": "fa fa-cogs",
      "sort": 100,
      "children": [
        {
          "id": 2,
          "name": "用户管理",
          "displayName": "用户管理",
          "url": "/Admin/User",
          "icon": "fa fa-users",
          "sort": 1
        }
      ]
    }
  ]
}
```

### 实体 CRUD 接口

以学生管理为例：

| 接口 | 方法 | 路径 | 说明 |
|------|------|------|------|
| 列表查询 | GET | `/School/Student` | 分页查询学生列表 |
| 获取详情 | GET | `/School/Student/{id}` | 获取学生详情 |
| 新增 | POST | `/School/Student` | 添加学生 |
| 修改 | PUT | `/School/Student` | 更新学生 |
| 删除 | DELETE | `/School/Student/{id}` | 删除学生 |
| 批量删除 | DELETE | `/School/Student` | 批量删除学生 |

### 列表查询详情

**GET** `/School/Student?pageIndex=1&pageSize=20&classId=1&name=张`

响应：
```json
{
  "code": 0,
  "data": [
    {
      "id": 1,
      "name": "张三",
      "classId": 1,
      "className": "一年级一班",
      "score": 95.5,
      "status": 1,
      "createTime": "2024-01-15 10:30:00"
    }
  ],
  "page": {
    "pageIndex": 1,
    "pageSize": 20,
    "totalCount": 100
  },
  "stat": {
    "总人数": 100,
    "平均分": 85.5
  }
}
```

### 获取字段元数据

**GET** `/School/Student/GetFields?kind=List`

响应：
```json
{
  "code": 0,
  "data": [
    {
      "name": "Id",
      "displayName": "编号",
      "type": "Int32",
      "nullable": false,
      "primaryKey": true
    },
    {
      "name": "Name",
      "displayName": "姓名",
      "type": "String",
      "length": 50,
      "nullable": false
    },
    {
      "name": "ClassId",
      "displayName": "班级",
      "type": "Int32",
      "mapField": "ClassName",
      "mapUrl": "/School/Class?id={ClassId}"
    }
  ]
}
```

### 数据导出

| 接口 | 方法 | 路径 | 说明 |
|------|------|------|------|
| 导出 Excel | GET | `/School/Student/ExportExcel` | 导出为 Excel |
| 导出 CSV | GET | `/School/Student/ExportCsv` | 导出为 CSV |
| 导出 JSON | GET | `/School/Student/ExportJson` | 导出为 JSON |

---

## B.2 统一响应格式

### 成功响应

```json
{
  "code": 0,
  "message": "success",
  "data": { ... }
}
```

### 分页响应

```json
{
  "code": 0,
  "message": "success",
  "data": [ ... ],
  "page": {
    "pageIndex": 1,
    "pageSize": 20,
    "totalCount": 100
  },
  "stat": {
    "统计项1": "值1",
    "统计项2": "值2"
  }
}
```

### 错误响应

```json
{
  "code": 500,
  "message": "错误描述"
}
```

---

## B.3 错误码对照表

### 系统错误码

| 错误码 | 名称 | 说明 |
|--------|------|------|
| 0 | Ok | 成功 |
| 1 | Unknown | 未知错误 |
| 2 | Error | 一般错误 |
| 400 | BadRequest | 请求参数错误 |
| 401 | Unauthorized | 未授权/未登录 |
| 403 | Forbidden | 禁止访问/无权限 |
| 404 | NotFound | 资源不存在 |
| 500 | InternalError | 服务器内部错误 |

### 业务错误码

| 错误码 | 名称 | 说明 |
|--------|------|------|
| 1001 | InvalidUser | 用户名或密码错误 |
| 1002 | UserDisabled | 用户已禁用 |
| 1003 | UserLocked | 用户已锁定 |
| 1004 | TokenExpired | Token 已过期 |
| 1005 | TokenInvalid | Token 无效 |
| 1006 | PasswordWeak | 密码强度不足 |
| 1007 | PasswordExpired | 密码已过期 |
| 2001 | DataNotFound | 数据不存在 |
| 2002 | DataExists | 数据已存在 |
| 2003 | DataInvalid | 数据验证失败 |
| 2004 | ForeignKeyError | 外键约束错误 |
| 3001 | FileNotFound | 文件不存在 |
| 3002 | FileTooBig | 文件太大 |
| 3003 | FileTypeNotAllowed | 文件类型不允许 |

---

## B.4 权限标志说明

### PermissionFlags 枚举

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | None | 无权限 |
| 1 | Detail | 查看详情 |
| 2 | Insert | 添加 |
| 4 | Update | 修改 |
| 8 | Delete | 删除 |
| 16 | Custom1 | 自定义权限1 |
| 32 | Custom2 | 自定义权限2 |
| 64 | Custom3 | 自定义权限3 |
| 128 | Custom4 | 自定义权限4 |
| 255 | All | 所有权限 |

### 权限计算示例

```
查看 + 添加 + 修改 = 1 + 2 + 4 = 7
查看 + 添加 + 修改 + 删除 = 1 + 2 + 4 + 8 = 15
所有权限 = 255
```

---

## B.5 分页参数说明

### 请求参数

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| pageIndex | Int32 | 否 | 1 | 页码，从 1 开始 |
| pageSize | Int32 | 否 | 20 | 每页数量 |
| sort | String | 否 | "" | 排序字段 |
| desc | Boolean | 否 | false | 是否降序 |

### 响应分页信息

```json
{
  "page": {
    "pageIndex": 1,
    "pageSize": 20,
    "totalCount": 100,
    "pageCount": 5
  }
}
```

---

## B.6 Token 传递方式

### Header 方式（推荐）

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

或

```http
X-Token: eyJhbGciOiJIUzI1NiIs...
```

### Cookie 方式

```http
Cookie: token=eyJhbGciOiJIUzI1NiIs...
```

### Query 方式（不推荐）

```
/api/data?token=eyJhbGciOiJIUzI1NiIs...
```

---

## B.7 常用 Header

### 请求 Header

| Header | 说明 |
|--------|------|
| Authorization | Bearer Token 认证 |
| X-Token | Token（兼容） |
| Content-Type | 请求内容类型 |
| X-Request-Id | 请求追踪 ID |

### 响应 Header

| Header | 说明 |
|--------|------|
| X-Run-Time | 运行时间（毫秒） |
| X-Request-Id | 请求追踪 ID |
| X-Server | 服务器标识 |

---

## B.8 文件上传接口

### 单文件上传

**POST** `/Admin/File/Upload`

请求（multipart/form-data）：
```
file: [二进制文件]
category: "avatar"
```

响应：
```json
{
  "code": 0,
  "data": {
    "id": 1,
    "fileName": "avatar.png",
    "url": "/Uploads/avatar_20240115.png",
    "size": 10240,
    "contentType": "image/png"
  }
}
```

### 多文件上传

**POST** `/Admin/File/UploadMultiple`

请求（multipart/form-data）：
```
files: [二进制文件1]
files: [二进制文件2]
```

响应：
```json
{
  "code": 0,
  "data": [
    {
      "id": 1,
      "fileName": "file1.png",
      "url": "/Uploads/file1.png"
    },
    {
      "id": 2,
      "fileName": "file2.png",
      "url": "/Uploads/file2.png"
    }
  ]
}
```

---

## B.9 批量操作接口

### 批量删除

**DELETE** `/School/Student`

请求：
```json
{
  "ids": [1, 2, 3]
}
```

响应：
```json
{
  "code": 0,
  "message": "成功删除 3 条数据"
}
```

### 批量启用/禁用

**POST** `/School/Student/SetEnable`

请求：
```json
{
  "ids": [1, 2, 3],
  "enable": true
}
```

响应：
```json
{
  "code": 0,
  "message": "成功更新 3 条数据"
}
```

---

## B.10 搜索接口规范

### 基本搜索

```
GET /School/Student?name=张&classId=1&status=1
```

### 范围搜索

```
# 日期范围
GET /School/Student?dtStart=2024-01-01&dtEnd=2024-12-31

# 数值范围
GET /School/Student?scoreMin=60&scoreMax=100
```

### 模糊搜索

```
# 包含
GET /School/Student?name=*张*

# 开头
GET /School/Student?name=张*

# 结尾
GET /School/Student?name=*三
```

### 多值搜索

```
# 多个值（OR）
GET /School/Student?classId=1,2,3
```

---

## 本附录小结

本附录提供了 WebAPI 版的完整接口参考：

1. 标准接口清单（认证、菜单、CRUD）
2. 统一响应格式
3. 错误码对照表
4. 权限标志说明
5. 分页参数规范
6. Token 传递方式
7. 文件上传接口
8. 批量操作接口
9. 搜索接口规范

---

**下一章**：[附录C 实体参考](附录C_实体参考.md) - 内置实体说明
