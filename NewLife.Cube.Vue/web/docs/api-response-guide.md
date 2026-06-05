# API 响应处理工具使用指南

## 概述

本项目已统一了 API 响应处理逻辑，包括：

1. 统一请求参数的默认分页值（pageSize=10, pageIndex=1）
2. 标准化响应数据处理（成功/失败提示、traceId输出、分页数据处理等）
3. 提供便捷的工具函数简化开发

## 标准 API 响应格式

```typescript
{
  "code": 0,                    // 0或200表示成功
  "message": "操作成功",         // 提示消息，可能为null
  "data": {...},               // 响应数据，可能是对象、数组、基本类型或null
  "traceId": "abc123",         // 链路追踪ID，可能为null或空字符串
  "page": {                    // 分页信息，可能为null
    "pageIndex": 1,
    "pageSize": 10,
    "totalCount": 189,
    "longTotalCount": "189"
  },
  "stat": {...}                // 统计信息，可能为null
}
```

## 使用方式

### 1. 基础 API 调用

```typescript
import { request } from '@/core/utils/request';
import { handleApiResponse, type ApiResponse } from '@/core/utils/response';

// 定义响应数据类型
interface UserInfo {
  id: number;
  name: string;
  email: string;
}

// API 调用
async function getUserInfo(userId: number) {
  try {
    const response = await request.get<ApiResponse<UserInfo>>(`/api/user/${userId}`);
    
    // 使用统一响应处理
    const result = handleApiResponse(response.data);
    
    if (result.success) {
      console.log('用户信息:', result.data);
      return result.data;
    } else {
      console.error('获取用户信息失败:', result.message);
      return null;
    }
  } catch (error) {
    console.error('请求失败:', error);
    return null;
  }
}
```

### 2. 使用 API 包装器（推荐）

```typescript
import { createApiWrapper } from '@/core/utils/response';

// 创建包装后的API函数
const getUserInfoApi = createApiWrapper(
  async (userId: number) => {
    const response = await request.get<ApiResponse<UserInfo>>(`/api/user/${userId}`);
    return response.data;
  },
  {
    showSuccessMessage: false, // 不显示成功提示
    showErrorMessage: true,    // 显示错误提示
  }
);

// 使用
async function loadUserInfo(userId: number) {
  const result = await getUserInfoApi(userId);
  
  if (result.success) {
    // 自动处理了提示消息和traceId输出
    return result.data;
  }
  return null;
}
```

### 3. 分页 API 调用

```typescript
import { createPageApiWrapper, normalizePageParams } from '@/core/utils/response';

interface UserListParams {
  keyword?: string;
  status?: number;
  pageIndex?: number;
  pageSize?: number;
}

// 创建分页API包装器
const getUserListApi = createPageApiWrapper(
  async (params: UserListParams & Required<PageParams>) => {
    const response = await request.get<ApiResponse<UserInfo[]>>('/api/users', { params });
    return response.data;
  }
);

// 使用 - 会自动设置默认分页参数
async function loadUserList(params: UserListParams = {}) {
  const result = await getUserListApi(params);
  
  if (result.success) {
    return {
      data: result.data,
      page: result.page, // 分页信息
      stat: result.stat, // 统计信息
    };
  }
  return null;
}
```

### 4. DataSet 中的使用

DataSet 已经内置了分页参数的处理，会自动添加 pageSize=10 和 pageIndex=1 的默认值。

```typescript
import { DataSet } from '@/core/dataset';

const userDataSet = new DataSet<UserInfo, UserListParams>({
  transport: {
    read: ({ params }) => ({
      url: '/api/users',
      method: 'GET',
      params: params, // 已自动包含 pageSize 和 pageIndex
    }),
  },
  // 其他配置...
});

// 使用
await userDataSet.query({ keyword: 'test' });
```

### 5. 手动处理响应

如果需要更精细的控制，可以直接使用工具函数：

```typescript
import { isApiSuccess, extractResponseData, extractPageInfo } from '@/core/utils/response';

async function customApiCall() {
  const response = await request.get<ApiResponse<UserInfo[]>>('/api/users');
  const apiResponse = response.data;
  
  // 检查是否成功
  if (isApiSuccess(apiResponse)) {
    // 提取数据
    const data = extractResponseData(apiResponse);
    
    // 提取分页信息
    const pageInfo = extractPageInfo(apiResponse);
    
    // 处理 traceId
    if (apiResponse.traceId) {
      console.log('TraceId:', apiResponse.traceId);
    }
    
    return { data, pageInfo };
  }
  
  return null;
}
```

## 自动处理的功能

1. **成功提示**: 当 code=0 或 code=200 且 message 不为空时，自动显示成功提示
2. **错误提示**: 当 code 不等于 0 且不等于 200 时，自动显示错误提示
3. **TraceId 输出**: 自动输出 traceId 到控制台（如果存在）
4. **分页参数标准化**: 自动设置默认的 pageSize=10 和 pageIndex=1
5. **空值处理**: 自动处理 null、undefined 和空对象的情况

## 注意事项

1. 请求已经在 `request.ts` 中集成了响应处理逻辑，会自动显示提示消息
2. 如果使用包装器，可以通过配置项控制是否显示提示
3. 所有分页请求都会自动添加默认的分页参数
4. TraceId 会自动输出到控制台，用于问题排查
