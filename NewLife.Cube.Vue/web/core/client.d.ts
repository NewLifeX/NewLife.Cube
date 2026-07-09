declare module 'virtual:@newlifex/cube-vue-app' {
  import { DefineComponent } from 'vue';
  const App: DefineComponent;
  export { App };
}

declare module 'virtual:@newlifex/cube-vue-routes' {
  import { RouteRecordRaw } from 'vue-router';
  const routes: RouteRecordRaw[];
  export default routes;
}

declare module 'virtual:@newlifex/cube-vue-micro-apps' {
  import type { MicroAppConfigItem } from './microAppRouter';
  const appConfigs: MicroAppConfigItem[];
  export default appConfigs;
}

declare module 'virtual:@newlifex/cube-vue-config' {
  const configData: Record<string, string>;
  const currentEnv: string;
  const config: { configData: Record<string, string>; currentEnv: string };
  export { configData, currentEnv };
  export default config;
}

declare module 'virtual:@newlifex/cube-vue-sections' {
  /** 子应用 views 目录中扫描到的 Section 覆盖组件懒加载映射。
   *  key 格式：`./views/<folderPath>/<SectionName>.vue`
   *  由 vite:@newlifex/cube-vue-sections 插件在构建时自动生成，开发模式下支持 HMR。
   */
  const modules: Record<string, () => Promise<{ default: unknown }>>;
  export default modules;
}

interface Window {
  router: import('vue-router').Router;
  store: import('pinia').Pinia;
}

// declare module '@newlifex/cube-vue' {
//   import { PluginOption } from 'vite';

//   declare function cubeFront(): PluginOption;

//   export { cubeFront as default };
// }
