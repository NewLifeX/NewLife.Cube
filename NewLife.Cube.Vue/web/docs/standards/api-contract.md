# API 与请求契约

## 请求入口

所有业务 HTTP 请求通过 `core/utils/request.ts`。它负责基地址、认证、401 跳转、网络错误通知、响应拦截和标准响应的失败处理。

相对 URL 会与 `getConfig().request.baseUrl` 拼接；以 `/_api` 开头或绝对 URL 不会被拼接。

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
