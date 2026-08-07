# API 与请求契约

## 请求入口

所有业务 HTTP 请求通过 `core/utils/request.ts`。它负责基地址、认证、401 跳转、网络错误通知、响应拦截和标准响应的失败处理。

相对 URL 统一与 `getConfig().request.baseUrl` 拼接；以 `http(s)://` 开头的绝对地址不拼接（按接口自身指定主机）。`baseUrl` 只承载主机，**不再内含 `/api` 前缀**。

各请求路径自行决定是否带 `/api` 前缀，请求层不再按前缀猜测是否拼接：

- **实体 / 区域控制器**（如 `/Cube/App`、`/Admin/Lov`、`/Admin/User`）：路由派生自页面路由，由 `routeToApiPrefix()` 统一拼 `/api`，如 `/device/device-profile` → `/api/Device/DeviceProfile`。
- **服务控制器**（Auth / Sso / Mfa / OAuth 及 `/Cube` 的服务动作）：后端无 `/api` 前缀，由各自硬编码路径直接写 `/Auth/Login`、`/Sso/Login` 等，不流经 `routeToApiPrefix`。注意 `/Cube` **不是整类服务接口**——仅 Info/Apis/UserSearch/GetArea/GetPageConfig/SaveLayout/Lookup/MenuTree/Setting/File 等少数服务动作不带 `/api`，其余 `/Cube/App` 等实体仍带 `/api`。

### 兜底补全（request.ts）

`core/utils/request.ts` 拦截器统一拼接 `API_HOST` 后，经 `ensureApiPrefix(url)` 兜底：

- 服务接口（`isServiceApiPath`，即 /Auth /Sso /Mfa /OAuth 及 /Cube 服务动作）→ 原样、不补 /api；
- 已以 `/api` 开头 → 原样、不重复补；
- 其余相对路径（实体 / 区域）→ 统一补 `/api`，最终 `${API_HOST}/api/...`。

等价旧 `baseUrl 内含 /api` 行为，apps 下硬编码的 `/Admin/*`、`/Cube/App` 等无需逐一改。

### 另一条链路：cubeApi（@cube/api-core）

`core/composables/useCubeApi.ts` 的 `cubeApi`（含 `page`/`client`/`user`/`menu`/`config`）是 `@cube/api-core` 的 `createCubeApi` 实例，**不经过 request.ts 拦截器**。其 `createCubeApi` 约定 `baseURL` 为「实体 base（含 /api）」，服务客户端由 `getServiceBaseUrl` 自动去 /api 派生。故 `useCubeApi.ts` 在调用时把 `cfg.request.baseUrl`（现仅主机、不含 /api）补回 `/api` 再传入（`API_BASE`），确保 `page`/`client` 实体请求 `/api/{area}/...`，`user`/`menu`/`config` 服务请求经派生后不带 /api。`usePageApi('Admin','Lov')`、`cubeApi.client.request('/Admin/Lov/SaveConfig')` 均走此链路，务必保持 `API_BASE` 补 /api。

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

- 网络与 HTTP 错误由请求层处理；页面只处理能够恢复的领域行为。
- 查询成功默认不显示成功提示；写操作如需反馈，使用统一 `Notification` 或 Element Plus 反馈组件。
- `traceId` 仅用于排障输出，不应展示为普通用户文案。

## 文件与非标准响应

二进制下载和非标准响应在请求层直接透传。调用方必须根据 `content-type` 和接口契约处理，不要把二进制数据当作 `ApiResponse` 解析。
