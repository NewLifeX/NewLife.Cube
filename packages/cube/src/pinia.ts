/**
 * Vue 系（Pinia）适配器入口
 *
 * 把框架无关的认证与列表页逻辑桥接为 Pinia store / composable。
 * `pinia` 与 `vue` 为可选 peer 依赖，只有从本入口引入时才需要安装。
 */

export * from './auth/pinia';
export * from './page/pinia';
