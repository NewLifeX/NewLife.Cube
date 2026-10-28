/**
 * 魔方前端公共逻辑（框架无关）
 *
 * 统一出口：API 调用层、页面工具函数、字段映射、认证与列表页业务编排。
 * 需要状态管理适配器时从子路径引入——`@newlifex/cube/pinia`（Vue）、`@newlifex/cube/zustand`（React）。
 */

export * from './api';
export * from './utils';
export * from './field';
export * from './auth';
export * from './page';
