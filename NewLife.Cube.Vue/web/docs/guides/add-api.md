# 新增 API 调用

## 默认原则

标准实体 CRUD 优先由默认页面引擎和后端元数据完成。只有仪表盘、工作区或特殊业务交互才直接编写 API 调用。

## 步骤

1. 在业务边界定义请求参数和响应数据 TypeScript 类型。
2. 通过 `core/utils/request.ts` 发起请求；不要创建独立 Axios 实例。
3. 标准响应使用 `ApiResponse<T>`；分页使用 `DataSet`、`normalizePageParams()` 或 `createPageApiWrapper()`。
4. 让请求层处理认证、401、网络错误和标准业务失败；页面只处理可恢复的领域分支。
5. 对写操作提供明确反馈，对查询操作不弹无意义的成功提示。
6. 为响应映射、分页或错误分支添加单元测试。

完整约束见 [api-contract.md](../standards/api-contract.md)。
