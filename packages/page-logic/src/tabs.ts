/**
 * 多标签页（页签导航）纯逻辑
 *
 * Vue 皮肤以 Pinia store 实现，其余皮肤此前没有。本模块把「打开/关闭/关闭其他/关闭全部」
 * 的判定收敛为框架无关的纯函数，各皮肤只负责渲染与路由跳转。
 */

/** 单个标签页 */
export interface TabItem {
  /** 唯一标识：路径 + query 序列化结果 */
  id: string;
  /** 路由路径（不含 query） */
  path: string;
  /** 完整路径（含 query/hash） */
  fullPath: string;
  /** 标签标题 */
  title: string;
  /** 是否可关闭。首页标签不可关闭 */
  closable: boolean;
}

/** 首页路径。命中时标签不可关闭 */
export const HOME_PATHS = ['/', '/home'];

/** 判断路径是否为不可关闭的首页 */
export function isHomePath(path: string): boolean {
  return HOME_PATHS.includes(path);
}

/** 由路径与完整路径生成标签 id；query 不同视为不同标签，hash 不参与 */
export function buildTabId(path: string, fullPath: string): string {
  const queryIndex = fullPath.indexOf('?');
  if (queryIndex < 0) return path;

  const hashIndex = fullPath.indexOf('#', queryIndex);
  const query = hashIndex >= 0 ? fullPath.slice(queryIndex, hashIndex) : fullPath.slice(queryIndex);

  return path + query;
}

/**
 * 打开标签：已存在则更新标题并保持顺序，不存在则追加到末尾
 *
 * @param tabs 当前标签列表
 * @param tab 待打开的标签
 * @returns 新数组
 */
export function openTab(tabs: TabItem[], tab: TabItem): TabItem[] {
  const list = [...(tabs ?? [])];
  const index = list.findIndex((item) => item.id === tab.id);
  if (index >= 0) {
    list[index] = { ...list[index], title: tab.title, fullPath: tab.fullPath, path: tab.path };
    return list;
  }

  return [...list, tab];
}

/**
 * 关闭标签
 *
 * @param tabs 当前标签列表
 * @param id 待关闭标签 id
 * @param activeId 当前激活标签 id
 * @returns 新的标签列表；若关闭后需要切换激活标签，`nextActiveId` 给出目标；无需切换时为 null
 */
export function closeTab(
  tabs: TabItem[],
  id: string,
  activeId: string,
): { tabs: TabItem[]; nextActiveId: string | null } {
  const list = [...(tabs ?? [])];
  const index = list.findIndex((item) => item.id === id);
  if (index < 0 || !list[index].closable) return { tabs: list, nextActiveId: null };

  list.splice(index, 1);
  if (activeId !== id) return { tabs: list, nextActiveId: null };

  const next = list[Math.min(index, list.length - 1)];
  return { tabs: list, nextActiveId: next?.id ?? null };
}

/** 关闭其他标签：保留不可关闭的标签与指定标签 */
export function closeOtherTabs(tabs: TabItem[], id: string): TabItem[] {
  return (tabs ?? []).filter((item) => !item.closable || item.id === id);
}

/** 关闭全部标签：只保留不可关闭的标签 */
export function closeAllTabs(tabs: TabItem[]): TabItem[] {
  return (tabs ?? []).filter((item) => !item.closable);
}

/** 由路径与标题构造标签 */
export function createTab(path: string, fullPath: string, title: string): TabItem {
  return {
    id: buildTabId(path, fullPath),
    path,
    fullPath,
    title: title || path,
    closable: !isHomePath(path),
  };
}
