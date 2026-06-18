---
name: cube-add-api
description: Cube API 前端请求封装。根据 NewLife.Cube 后端控制器定义，生成符合项目规范的 API 请求模块（.ts 文件）。当你需要创建或修改 Cube 前端 API、调用后端接口时使用。
---

# cube-add-api

Cube API 前端请求封装技能。根据 NewLife.Cube 后端控制器定义，生成符合项目规范的 API 请求模块。

## 技能触发

当需要创建或修改 Cube 前端 API 文件时使用，例如：
- 新增 Cube 控制器对应的前端 API
- 为现有 API 添加新方法

## 输入参数

| 参数             | 说明                     | 示例                                                            |
| ---------------- | ------------------------ | --------------------------------------------------------------- |
| `controllerName` | 控制器名称（PascalCase） | `Product`, `Device`, `Alarm`                                    |
| `area`           | 业务区域                 | `Device`, `Base`, `Life`, `Stats`, `WorkOrder`, `Admin`         |
| `extends`        | 控制器基类类型           | `ApiEntityController`（RESTful）或 `BaseController`（方法路由） |
| `customActions`  | 自定义业务方法（可选）   | `[{ name: 'SetPersistConfig', method: 'POST' }]`                |

## 输出位置

```
NewLife.Cube.Vue/src/api/{controllerName}.ts
```

## API 路由规范

### ApiEntityController（RESTful 标准路由）

继承 `ApiEntityController` 的控制器使用 RESTful 标准 CRUD：

| 操作 | HTTP 方法 | 路由                                 | 参数                                     |
| ---- | --------- | ------------------------------------ | ---------------------------------------- |
| 列表 | `GET`     | `/api/cube/{Area}/{Controller}`      | Query: `pageIndex`, `pageSize`, `key` 等 |
| 详情 | `GET`     | `/api/cube/{Area}/{Controller}/{id}` | URL 参数                                 |
| 新增 | `POST`    | `/api/cube/{Area}/{Controller}`      | Body: JSON                               |
| 修改 | `PUT`     | `/api/cube/{Area}/{Controller}`      | Body: JSON                               |
| 删除 | `DELETE`  | `/api/cube/{Area}/{Controller}/{id}` | URL 参数                                 |

### AreaBaseController（方法名路由）

继承 `AreaBaseController` 的控制器使用 `[action]` 占位符：

| 操作 | HTTP 方法 | 路由                                       |
| ---- | --------- | ------------------------------------------ |
| 查询 | `GET`     | `/api/cube/{Area}/{Controller}/GetXxx`     |
| 操作 | `POST`    | `/api/cube/{Area}/{Controller}/ActionName` |

> 注意：Area 格式为 `cube/{Region}`（如 `cube/Life`、`cube/Device`），路由前缀为 `api/cube/`（大写 Cube）

## 请求库

使用 `cube-front/core/utils/request`，该拦截器已对响应做统一处理：

```typescript
import request from 'cube-front/core/utils/request'
```

### ⚠️ 重要：拦截器已解包 ApiResponse

`request` 拦截器在 `handleResponseSuccess` 中已将 `ApiResponse<T>` 外层解包：

```typescript
// 后端返回：{ code: 0, data: [...], page: {...}, stat: null }
// 经过拦截器后，request() 直接返回 res.data（即业务数据本身）
```

因此前端拿到的 `res` **已经是** 业务数据，不需要再写 `res.data`：

| 场景         | ❌ 错误写法                | ✅ 正确写法           |
| ------------ | ------------------------- | -------------------- |
| 列表（数组） | `Array.isArray(res.data)` | `Array.isArray(res)` |
| 详情（对象） | `res.data.name`           | `res.name`           |
| 分页数据     | `res.data.list`           | `res.list`           |

> **Why:** 拦截器在 `handleResponseSuccess` 中判断响应含有 `code` 字段时，直接返回 `apiResponse.data`（成功时）或抛出 `Error`（失败时）。前端拿到的就是解包后的数据。
>
> **How to apply:** 所有 API 调用后，直接用 `res` 本身取值，不需要 `.data`。只有分页场景下 `res.page` 才是分页信息。

## 输出格式模板

```typescript
import request from 'cube-front/core/utils/request'

// API 路由前缀：/api/cube/{Area}/{Controller}
// {Controller}Controller 继承 {BaseClass}，使用 {路由模式}
// 详细说明...
const API_PREFIX = '/api/cube/{Area}/{Controller}'

// ==================== CRUD 接口 ====================

export function get{Controller}List(params) {
  return request({
    url: API_PREFIX,
    method: 'get',
    params
  })
}

export function get{Controller}Detail(id) {
  return request({
    url: `${API_PREFIX}/${id}`,
    method: 'get',
    params: { id }
  })
}

export function add{Controller}(data) {
  return request({
    url: API_PREFIX,
    method: 'post',
    data
  })
}

export function update{Controller}(data) {
  return request({
    url: API_PREFIX,
    method: 'put',
    data
  })
}

export function delete{Controller}(id) {
  return request({
    url: `${API_PREFIX}/${id}`,
    method: 'delete',
    params: { id }
  })
}

// ==================== 业务接口 ====================

{customActions.map(action => `
export function ${action.name}(params) {
  return request({
    url: \`\${API_PREFIX}/${action.name}\`,
    method: '${action.method}',
    ${action.method === 'get' ? 'params' : 'data'}: params
  })
}
`).join('\n\n')}
```

## 使用示例

### 示例 1：创建 Product API（ApiEntityController）

输入：
- controllerName: `Product`
- area: `Device`
- extends: `ApiEntityController`

输出文件 `src/api/product.ts`：
```typescript
import request from 'cube-front/core/utils/request'

// API 路由前缀：/api/cube/{Area}/{Controller}
// ProductController 继承 ApiEntityController，使用 RESTful 标准路由
// 详细说明...
const API_PREFIX = '/api/cube/Device/Product'

export function getProductList(params) {
  return request({
    url: API_PREFIX,
    method: 'get',
    params
  })
}

export function getProductDetail(id) {
  return request({
    url: `${API_PREFIX}/${id}`,
    method: 'get',
    params: { id }
  })
}

export function addProduct(data) {
  return request({
    url: API_PREFIX,
    method: 'post',
    data
  })
}

export function updateProduct(data) {
  return request({
    url: API_PREFIX,
    method: 'put',
    data
  })
}

export function deleteProduct(id) {
  return request({
    url: `${API_PREFIX}/${id}`,
    method: 'delete',
    params: { id }
  })
}
```

### 示例 2：创建带自定义方法的 API

输入：
- controllerName: `Device`
- area: `Device`
- extends: `BaseController`
- customActions: `[{ name: 'GetDevices', method: 'GET' }, { name: 'SetOnline', method: 'POST' }]`

输出文件 `src/api/device.ts`：
```typescript
import request from 'cube-front/core/utils/request'

// API 路由前缀：/api/cube/{Area}/{Controller}
// DeviceController 继承 AreaBaseController，使用 [action] 方法路由
// GET  /api/cube/Device/Device/GetDevices
// POST /api/cube/Device/Device/SetOnline
const API_PREFIX = '/api/cube/Device/Device'

// ==================== 业务接口 ====================

export function getDevices(params) {
  return request({
    url: `${API_PREFIX}/GetDevices`,
    method: 'get',
    params
  })
}

export function setOnline(params) {
  return request({
    url: `${API_PREFIX}/SetOnline`,
    method: 'post',
    data: params
  })
}
```

## 注意事项

1. **路由前缀**：始终使用 `/api/cube/{Area}/{Controller}` 格式（大写 Cube）
2. **HTTP 方法**：
   - 查询/列表用 `GET`，参数放 `params`
   - 新增/修改/操作用 `POST`/`PUT`/`DELETE`，数据放 `data`
3. **删除参数**：DELETE 方法也用 `params` 传递 id
4. **命名规范**：函数名使用 camelCase，如 `getProductList`、`addDevice`
5. **TypeScript**：使用 `.ts` 扩展名，支持类型提示

## 前端调用示例

```typescript
import { getProductList, addProduct } from '@/api/product'

// 列表查询 - res 直接就是业务数据数组，不需要 .data
const productList = await getProductList({ pageIndex: 1, pageSize: 20 })
// productList === [{ id: 1, name: '产品A', ... }, ...]

// 分页场景 - res.page 是分页信息，res.data 是业务数据
const res = await getProductList({ pageIndex: 1, pageSize: 20 })
// res.data  ← 业务数据数组
// res.page  ← 分页信息 { pageIndex, pageSize, totalCount }

// 新增数据
await addProduct({ name: '新产品', category: 'sensor' })
```