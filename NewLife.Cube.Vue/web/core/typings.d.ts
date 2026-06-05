import { type TransitionProps } from 'vue'
import { type RouteComponent, type RouteRecordRaw } from 'vue-router'

interface RouteMeta {
  /** 是否需要权限
   * @default true
   */
  auth?: boolean
  /** 图标 */
  icon?: string
  /** 布局，不设置的话使用默认布局，设置了false 则不使用布局
   * @default true 使用默认布局
   */
  layout?: string | boolean
  /** 是否缓存页面
   * @default false
   */
  keepAlive?: boolean
  /** 标题 */
  title?: string
  /** 切换路由是否需要过渡
   * @default false
   */
  transition?: boolean | TransitionProps
}

type BaseRoute = RouteRecordRaw & {
  /** 路由元信息 */
  meta?: RouteMeta
}

/** 配置路由，组件为 string */
export type ConfigRoute = BaseRoute
// & {
/** 组件绝对路径地址
 * @example
 * import path from 'node:path'
 * path.resolve(__dirname, 'src/pages/login/index.tsx')
 */
// component: string
// }

/** 组件路由，组件为 解析后模块 */
export type ComponentRoute = BaseRoute & {
  component: RouteComponent | (() => Promise<RouteComponent>)
}
