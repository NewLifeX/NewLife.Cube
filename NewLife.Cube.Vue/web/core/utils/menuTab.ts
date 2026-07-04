import { gotoPage } from './router';
import { normalizeMenuUrl, type RouteNamingStyle } from './url';
import { getConfig } from '../configure';
import { useTabsStore } from '../stores/tabs';

/**
 * 打开菜单对应的标签页
 *
 * 通过路由跳转并自动将标签页加入 tabs store。
 * 标签页的实际管理（去重、关闭等）由 tabs store 和 TabsView 组件完成，
 * RootLayout 的路由 watch 会自动将新路由添加为标签。
 *
 * @param options 打开标签页的配置项
 * @param options.url 标签页对应的URL路径
 * @param options.title 标签页的标题（可选）
 */
export function openMenuTab(options: { url: string; title?: string }): void {
  const { url, title } = options;

  // 读取路由命名风格配置，确保与注册的路由匹配
  const { router: { routeNamingStyle } } = getConfig();
  const toStyle: RouteNamingStyle = routeNamingStyle === 'kebab' ? 'kebab' : 'pascal';
  const normalizedUrl = normalizeMenuUrl(url, toStyle);

  // 使用路由跳转，RootLayout 的 watch 会自动把路由加入标签
  gotoPage(normalizedUrl);

  // 如果有明确标题，先预注入标签（路由 watch 也会更新）
  if (title) {
    const tabsStore = useTabsStore();
    tabsStore.addTab({
      path: normalizedUrl,
      fullPath: normalizedUrl,
      name: title,
      meta: { title },
      params: {},
      query: {},
      hash: '',
      matched: [],
      redirectedFrom: undefined,
    } as any);
  }

  console.log(`打开标签页: ${title || url}`);
}
