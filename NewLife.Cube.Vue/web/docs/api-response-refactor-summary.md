# API 响应处理重构总结

## 重构目标

根据项目需求，对页面接口请求和返回数据处理进行了统一重构，实现以下目标：

1. 统一请求参数的默认分页值（pageSize=10, pageIndex=1）
2. 标准化API响应处理
3. 统一消息提示逻辑
4. 统一traceId输出
5. 统一分页和统计数据处理

## 已完成的工作

### 1. 创建统一响应处理工具 (`/core/utils/response.ts`)

- **ApiResponse 接口**: 定义标准API响应格式
- **PageParams 接口**: 定义分页参数接口
- **normalizePageParams**: 标准化分页参数函数
- **handleApiResponse**: 核心响应处理函数
- **createApiWrapper**: API请求包装器
- **createPageApiWrapper**: 分页API请求包装器
- **工具函数**: extractResponseData, extractPageInfo, isApiSuccess

### 2. 更新请求工具 (`/core/utils/request.ts`)

- 集成新的响应处理逻辑
- 自动处理标准API响应格式
- 自动显示成功/错误提示
- 自动输出traceId到控制台
- 移除冗余代码和调试语句

### 3. 更新通用工具 (`/core/utils/common.ts`)

- 扩展 `getResponse` 函数以支持新的标准API响应格式
- 保持与旧格式的兼容性
- 统一消息提示逻辑

### 4. 确保DataSet默认分页参数

- 验证DataSet已正确设置默认分页参数（pageSize=10, pageIndex=1）
- 所有分页请求都会自动包含标准分页参数

## 响应处理规则

### 成功处理（code=0 或 code=200）
- 显示 message 中的成功信息（如果存在）
- message 为 null 时不显示提示
- 自动处理并返回 data 部分

### 错误处理（code != 0 且 code != 200）
- 显示 message 中的错误信息
- message 为空时显示默认错误信息

### TraceId 处理
- 自动输出到控制台（如果存在）
- 处理 null、空字符串或不存在的情况

### 分页数据处理
- page 对象包含 pageIndex, pageSize, totalCount, longTotalCount
- 处理 null、空对象或不存在的情况

### 统计数据处理
- stat 对象可能为 null、空对象或不存在
- 统一处理并传递给业务代码

## 使用方式

### 1. 简单API调用
```typescript
import { createApiWrapper } from '@/core/utils/response';

const getUserInfo = createApiWrapper(
  async (userId: number) => {
    const response = await request.get(`/api/user/${userId}`);
    return response.data;
  }
);
```

### 2. 分页API调用
```typescript
import { createPageApiWrapper } from '@/core/utils/response';

const getUserList = createPageApiWrapper(
  async (params) => {
    const response = await request.get('/api/users', { params });
    return response.data;
  }
);
```

### 3. 直接使用request（已集成处理）
```typescript
// 会自动处理响应，显示提示消息，输出traceId
const response = await request.get('/api/data');
```

## 兼容性

- 保持与现有代码的完全兼容
- 现有的 `getResponse` 函数支持新旧两种格式
- DataSet 自动使用标准分页参数
- 现有请求代码无需修改即可享受新的响应处理

## 文件清单

1. **核心文件**:
   - `/core/utils/response.ts` - 新增的响应处理工具
   - `/core/utils/request.ts` - 更新的请求工具
   - `/core/utils/common.ts` - 更新的通用工具

2. **示例文件**:
   - `/core/utils/api-example.ts` - API使用示例
   - `/docs/api-response-guide.md` - 使用指南

3. **文档文件**:
   - `/docs/api-response-refactor-summary.md` - 本总结文档

## 测试建议

1. 测试现有页面功能是否正常
2. 验证分页请求参数是否正确（pageSize=10, pageIndex=1）
3. 验证成功/错误提示是否正常显示
4. 检查控制台是否正确输出traceId
5. 测试新的API包装器函数

## 后续工作建议

1. 逐步将现有API调用迁移到新的包装器
2. 为关键业务模块创建类型化的API服务类
3. 考虑添加请求重试和缓存机制
4. 完善错误处理和用户体验优化
