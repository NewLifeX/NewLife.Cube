import { gotoPage } from './router';
import { normalizeMenuUrl } from './url';

/**
 * 打开菜单对应的标签页
 * @param options 打开标签页的配置项
 * @param options.url 标签页对应的URL路径
 * @param options.title 标签页的标题（可选）
 */
export function openMenuTab(options: { url: string; title?: string }): void {
  const { url, title } = options;

  // 转换 URL 为短横线风格，确保与注册的路由匹配
  const normalizedUrl = normalizeMenuUrl(url);

  // 使用路由跳转到指定的页面
  gotoPage(normalizedUrl);

  // 可以在这里添加标签页的其他处理逻辑
  // 例如保存打开的标签页到本地存储，或者更新标签页状态
  console.log(`打开标签页: ${title || url}`);

  // 如果项目中有标签页管理的状态，可以在这里更新
  // 例如: store.dispatch('tabs/addTab', { url, title });
}
