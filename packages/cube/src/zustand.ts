/**
 * React 系（Zustand）适配器入口
 *
 * 把框架无关的认证与列表页逻辑桥接为 Zustand store / hooks。
 * `zustand` 为可选 peer 依赖，只有从本入口引入时才需要安装。
 */

export * from './auth/zustand';
export * from './page/zustand';
