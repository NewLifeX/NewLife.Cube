# API 与请求契约

## 请求入口

所有业务 HTTP 请求通过 `core/utils/request.ts`。它**只保留与 UI 强相关**的少量逻辑——错误/业务错误的弹窗展示、401 跳转与导航。**其余请求逻辑已全部下沉至 `@cube/api-core` 的 `createApiClient`**（基地址拼接、`/api` 前缀补全、Token 头注入、附加请求头、`withCredentials`、content-type 透传、traceId、204 处理、错误分类归一化、响应钩子 `responseIntercept`、业务错误 reject、401 基础处理），并通过 option / 回调参数化，所有皮肤共享。`request.ts` 仅以回调把 cube-vue 的 UI 行为与配置（附加头、请求钩子、响应钩子）接线到 api-core，对外导出（`request` / `cubeAxios` / `redirectToLogin` / `toReLogin`）保持兼容，业务文件无需改动。

### api-core `createApiClient` 关键选项（非 UI 逻辑载体）

| 选项 | 作用 | cube-vue 用法 |
|---|---|---|
| `baseURL` | 主机（可含 /api），地址由 `resolveRequestUrl` 统一解析 | 传纯主机 `API_HOST` |
| `tokenHeaderPrefix` | Authorization 头前缀 | `'bearer '` |
| `unwrapResponse` | 成功响应是否解包为 `ApiResponse`（response.data） | `true`（业务拿到的就是 ApiResponse） |
| `withCredentials` | 跨域凭证 | `true` |
| `additionalRequestHeaders` | 静态/函数式附加请求头 | 取自 `getConfig().request.additionalRequestHeaders` |
| `onRequestHook` | 请求钩子（同 axios 拦截器，可返回 Promise） | 接 `getConfig().request.requestInterceptor` |
| `onResponseHook` | 响应钩子（成功/失败均触发，即 `responseIntercept`） | 接 `getConfig().request.responseIntercept` |
| `onUnauthorized` | 401 回调（收到请求 url） | `handleUnauthorized`（跳转登录/未授权页） |
| `onBusinessError` | 业务错误（code≠0）回调 | `notification.error` 弹窗 |
| `onResponseError` | 非 401 响应错误回调，传入**已归一化**的 `ResponseErrorInfo`（含 `isNetwork`/`message`/`description`，无文案） | `showErrorNotification`（本地化后 `notification.autoNotification`） |

相对 URL 统一与 `getConfig().request.baseUrl` 拼接；以 `http(s)://` 开头的绝对地址不拼接（按接口自身指定主机）。`baseUrl` 只承载主机（可含也可不含 `/api` 前缀，见下方 resolveRequestUrl 规则），**cube-vue 实际传纯主机**。

各请求路径自行决定是否带 `/api` 前缀，请求层不再按前缀猜测是否拼接：

- **实体 / 区域控制器**（如 `/Cube/App`、`/Admin/Lov`、`/Admin/User`）：路由派生自页面路由，由 `routeToApiPrefix()` 统一拼 `/api`，如 `/device/device-profile` → `/api/Device/DeviceProfile`。
- **服务控制器**（Auth / Sso / Mfa / OAuth 及 `/Cube` 的服务动作）：后端无 `/api` 前缀，由各自硬编码路径直接写 `/Auth/Login`、`/Sso/Login` 等，不流经 `routeToApiPrefix`。注意 `/Cube` **不是整类服务接口**——仅 Info/Apis/UserSearch/GetArea/GetPageConfig/SaveLayout/Lookup/MenuTree/Setting/File 等少数服务动作不带 `/api`，其余 `/Cube/App` 等实体仍带 `/api`。

### 两条链路均已统一到 api-core

- **request.ts 链路**：`cubeAxios = createApiClient({ baseURL: API_HOST, tokenHeaderPrefix: 'bearer ', unwrapResponse: true, withCredentials: true, additionalRequestHeaders, onRequestHook, onResponseHook, onUnauthorized, onBusinessError, onResponseError })`，`API_HOST` 为纯主机；最终地址由 api-core 请求拦截经 `resolveRequestUrl(baseURL, url)` 解析；`unwrapResponse: true` 使业务直接拿到 `ApiResponse`。
- **cubeApi 链路**：`core/composables/useCubeApi.ts` 的 `cubeApi`（`page`/`client`/`user`/`menu`/`config`）由 `@cube/api-core` 的 `createCubeApi({ baseURL: API_HOST, tokenStorage: 'localStorage', ... })` 创建；实体/区域请求同样经 `resolveRequestUrl` 解析为 `${API_HOST}/api/...`；服务接口（Auth/Sso/Mfa 及 /Cube 服务动作）由 `getServiceBaseUrl(baseURL)` 派生主机且不带 `/api`。

> **返回值语义差异（务必注意，避免混用）**：`request.ts` 链路 `unwrapResponse: true`，业务调用 `request.get(...)` 等**直接返回 `ApiResponse`**；而 `cubeApi.client` 是裸 axios 实例（`unwrapResponse` 默认 `false`），`cubeApi.client.request(...)` 返回**完整 `AxiosResponse`**，需自行 `.then(r => r.data)` 取 `ApiResponse`。两者返回值类型不同，消费时不要混用期望值（现有 `usePageApi().getAction` 已正确做了 `.then(res => res.data)`，可作范本）。

两条链路共用同一 `resolveRequestUrl(baseUrl, url)`（位于 `@cube/api-core` 的 `service-path.ts`），按「baseUrl 是否含 /api 前缀」分两种情形：

- **baseUrl 含 /api 前缀**（如 `http://host:5000/api`）：实体请求保留 /api；服务接口去掉 /api；url 自身若已带 /api 则去重（避免 `http://host/api/api/...`）。
- **baseUrl 不含 /api 前缀**（如 `http://host:5000`，cube-vue 实际传此）：服务接口不补前缀；非服务接口已带 /api 的不重复补、缺 /api 则补。

等价旧 `baseUrl 内含 /api` 行为，apps 下硬编码的 `/Admin/*`、`/Cube/App` 等无需逐一改。`usePageApi('Admin','Lov')`、`cubeApi.client.request('/Admin/Lov/SaveConfig')` 均走 api-core 链路。

> AI 组件（SSE 流式）仍用原生 `fetch`，不经上述请求层，保持独立。

## 标准响应

`core/utils/response.ts` 定义标准响应：

```ts
interface ApiResponse<T> {
  code: number;
  message?: string | null;
  data?: T;
  traceId?: string | null;
  page?: {
    pageIndex: number;
    pageSize: number;
    totalCount: number;
    longTotalCount: string;
  } | null;
  stat?: Record<string, unknown> | null;
}
```

`code === 0 || code === 200` 视为成功。请求拦截器对标准失败响应显示错误通知并抛出异常；业务代码必须按成功/失败分支处理，不要假设所有 `200 HTTP` 都代表成功业务结果。

## 分页

`normalizePageParams()` 和 `createPageApiWrapper()` 默认补齐：

```ts
{ pageIndex: 1, pageSize: 10 }
```

需要列表查询时优先使用 `DataSet` 或现有默认页面能力；手写请求时才使用响应包装器。不要让各页面各自定义不同的分页字段或默认值。

## 错误与反馈

- 错误**分类与归一化**在 api-core（`buildResponseErrorInfo` 产出 `ResponseErrorInfo`：网络错误标 `isNetwork`、HTTP 错误提取 `message`/`description`），**展示（弹窗/本地化）在 cube-vue**（`onResponseError` 回调）。各皮肤自行决定如何呈现，api-core 不持有任何 UI 文案。
- 业务错误（`code≠0`）由 api-core 经 `onBusinessError` 回调弹窗并 reject `ApiError`；`onResponseError` 不会重复触发，避免重复弹窗。
- 查询成功默认不显示成功提示；写操作如需反馈，使用统一 `Notification` 或 Element Plus 反馈组件。
- `traceId` 仅用于排障输出，不应展示为普通用户文案。

## 文件与非标准响应

二进制下载和非标准响应在请求层直接透传。调用方必须根据 `content-type` 和接口契约处理，不要把二进制数据当作 `ApiResponse` 解析。
