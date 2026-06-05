/**
 * 测试响应处理逻辑
 */
import { handleApiResponse } from './response';

// 模拟你提供的API响应数据
const mockResponse = {
  "code": 0,
  "message": null,
  "data": [
    {
      "id": 190,
      "name": "测试2",
      "displayName": "测试2",
      "mail": "测试2@qq.com",
      "enable": true
    }
  ],
  "traceId": null,
  "page": {
    "pageIndex": 1,
    "pageSize": 10,
    "totalCount": 189,
    "longTotalCount": "189"
  },
  "stat": null
};

// 测试处理逻辑
console.log('=== 测试API响应处理 ===');

const result = handleApiResponse(mockResponse, {
  showSuccessMessage: false,
  showErrorMessage: true
});

console.log('处理结果:', {
  success: result.success,
  dataLength: Array.isArray(result.data) ? result.data.length : 'not array',
  hasPage: !!result.page,
  totalCount: result.page?.totalCount
});

console.log('预期结果: success=true, dataLength=1, hasPage=true, totalCount=189');

export { };
