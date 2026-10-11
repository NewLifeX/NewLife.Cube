declare module 'virtual:@newlifex/cube-vue-app' {
  import type { DefineComponent } from 'vue';
  const App: DefineComponent;
  export { App };
}

declare module 'virtual:@newlifex/cube-vue-routes' {
  import type { RouteRecordRaw } from 'vue-router';
  const routes: RouteRecordRaw[];
  export default routes;
}

declare module 'virtual:@newlifex/cube-vue-micro-apps' {
  import type { ConfigRoute } from './typings';
  interface MicroAppConfigItem {
    name: string;
    module: () => Promise<{ routes: ConfigRoute[] }>;
    prefix?: string;
  }
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

// vue-json-editor 为框架直接依赖但未提供 TS 类型声明（main 为 .vue 文件），JsonEditor.vue 引入会触发消费方 TS7016。
// 声明置于本消费方类型入口（core/client.d.ts），凡通过 types:["@newlifex/cube-vue/core/client"] 加载框架的消费方均全局生效，无需各自补 shim。
declare module 'vue-json-editor' {
  import type { DefineComponent } from 'vue';
  const JsonEditor: DefineComponent<Record<string, unknown>, Record<string, unknown>, unknown>;
  export default JsonEditor;
}

interface Window {
  router: import('vue-router').Router;
  store: import('pinia').Pinia;
  /** dva 应用句柄（SSO 场景注入），用于读取全局状态 */
  getDvaApp?: () => { _store: { getState: () => any } };
  /** 当前组织 ID（开发模式下注入） */
  getCurrentOrganizationId?: () => string | undefined;
}

// declare module '@newlifex/cube-vue' {
//   import { PluginOption } from 'vite';

//   declare function cubeFront(): PluginOption;

//   export { cubeFront as default };
// }
