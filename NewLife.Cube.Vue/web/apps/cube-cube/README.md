# Cube-Cube 魔方管理系统

这是基于 Cube OpenAPI 文档生成的前端页面模块，包含了魔方系统的核心功能页面。

## 功能模块

### 1. 应用管理 (/Cube/App)
- 应用的增删改查
- 应用启动配置管理
- 应用版本管理

### 2. 应用日志 (/Cube/AppLog)
- 应用操作日志查看
- 日志筛选和搜索
- 日志详情查看

### 3. 地区管理 (/Cube/Area)
- 省市区县地区数据管理
- 地区层级关系维护
- 地区信息编辑

### 4. 附件管理 (/Cube/Attachment)
- 文件上传和管理
- 附件分类管理
- 文件下载统计

### 5. 定时任务 (/Cube/CronJob)
- Cron定时任务配置
- 任务执行状态管理
- 立即执行任务功能

### 6. 订单管理 (/Cube/OrderManager)
- 订单信息管理
- 订单状态跟踪
- 订单信息查询

### 7. 主体代理 (/Cube/PrincipalAgent)
- 代理主体信息管理
- 代理类型分类
- 联系信息维护

## 技术栈

- Vue 3 + TypeScript
- Element Plus UI 组件库
- Vue Router 路由管理
- Vite 构建工具

## API 接口对接

所有页面都严格按照 `Cube_OpenAPI.json` 文档中的接口规范实现：

- 使用标准的 REST API 规范
- 支持分页查询
- 统一的错误处理
- 完整的 CRUD 操作

## 页面特性

- 响应式设计，支持各种屏幕尺寸
- 统一的搜索和筛选功能
- 表格分页和排序
- 表单验证和提交
- 详情查看和编辑对话框
- 操作确认和反馈提示

## 使用方法

1. 确保核心模块 `@newlifex/cube-vue` 已正确配置
2. 导入路由配置：
   ```typescript
   import { routes } from 'cube-cube'
   ```
3. 将路由添加到主路由配置中

## 注意事项

- 所有接口调用都使用 `@newlifex/cube-vue/utils/request` 统一的请求工具
- 页面样式遵循 Element Plus 设计规范
- 支持国际化（中文为主）
- 包含完整的 TypeScript 类型定义
