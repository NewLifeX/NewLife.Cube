import { defineComponent, h, type Component } from 'vue';

type AsyncPageLoader = () => Promise<{ default: unknown }>;

/** 为异步路由组件包一层具名壳，便于 keep-alive :include 按路由 name 裁剪 */
export function withRouteComponentName(loader: AsyncPageLoader, name: string): AsyncPageLoader {
  return async () => {
    const mod = await loader();
    const inner = mod.default as Component;
    return {
      default: defineComponent({
        name,
        inheritAttrs: false,
        setup(_, { attrs, slots }) {
          return () => h(inner, attrs, slots);
        },
      }),
    };
  };
}
