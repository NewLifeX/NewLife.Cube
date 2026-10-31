import { type FlatMenuItem } from '../stores/menu';

/** 菜单搜索结果。path 为从顶层到当前菜单的显示名路径 */
export interface MenuSearchResult {
  menu: FlatMenuItem;
  path: string;
}

/** 关键词切分：忽略大小写，空白分隔 */
export function getSearchTokens(keyword?: string): string[] {
  return (keyword ?? '')
    .trim()
    .toLowerCase()
    .split(/\s+/)
    .filter((token) => token.length > 0);
}

/** 是否参与搜索：隐藏菜单不参与，无路径的分类节点也不参与 */
export function isSearchableMenu(menu: FlatMenuItem): boolean {
  return menu.visible !== false && Boolean(menu.path);
}

/**
 * 获取菜单完整层级路径
 *
 * 沿 parentId 向上回溯全部祖先，以 `›` 连接。parentId 断裂或成环时安全退出。
 * @param menus 平铺菜单
 * @param menu 当前菜单
 * @returns 层级路径，如 `系统管理 › 用户`
 */
export function getMenuPath(menus: FlatMenuItem[], menu: FlatMenuItem): string {
  return getMenuPathInfo(
    new Map(menus.map((item) => [item.id, item])),
    menu,
  ).path;
}

/** 祖级中是否存在隐藏菜单。隐藏父菜单的整棵子树不属于导航 */
function hasHiddenAncestor(map: Map<string, FlatMenuItem>, menu: FlatMenuItem): boolean {
  const visited = new Set<string>();
  let current = menu.parentId ? map.get(menu.parentId) : undefined;

  while (current && !visited.has(current.id)) {
    if (current.visible === false) return true;
    visited.add(current.id);
    current = current.parentId ? map.get(current.parentId) : undefined;
  }

  return false;
}

/** 菜单路径及其拼音。pinyin/initials 由后端的库中源文显示名计算 */
interface MenuPathInfo {
  path: string;
  pinYin: string;
  initials: string;
}

/** 基于预构建的菜单索引获取层级路径与拼音，避免搜索时对每个候选重复建表 */
function getMenuPathInfo(map: Map<string, FlatMenuItem>, menu: FlatMenuItem): MenuPathInfo {
  const names: string[] = [];
  const pinyins: string[] = [];
  const initials: string[] = [];
  const visited = new Set<string>();
  let current: FlatMenuItem | undefined = menu;

  while (current && !visited.has(current.id)) {
    visited.add(current.id);
    const name = current.title || current.name;
    if (name) names.unshift(name);
    if (current.pinYin) pinyins.unshift(current.pinYin);
    if (current.initials) initials.unshift(current.initials);
    current = current.parentId ? map.get(current.parentId) : undefined;
  }

  return {
    path: names.join(' › '),
    pinYin: pinyins.join(' '),
    initials: initials.join(' '),
  };
}

/**
 * 显示名匹配优先级
 *
 * 0 完全相同，1 显示名前缀，2 首字母前缀，3 全拼前缀，4 名称前缀，
 * 5 显示名包含，6 名称包含，7 路径拼音前缀，8 地址包含，9 其它。
 */
export function scoreMenu(
  menu: FlatMenuItem,
  keyword: string,
  pathPinYin = '',
  pathInitials = '',
): number {
  const title = (menu.title || '').toLowerCase();
  const name = (menu.name || '').toLowerCase();
  const path = (menu.path || '').toLowerCase();
  const initials = (menu.initials || '').toLowerCase();
  const pinyin = (menu.pinYin || '').toLowerCase();
  const pathPy = pathPinYin.toLowerCase();
  const pathIni = pathInitials.toLowerCase();

  if (title === keyword) return 0;
  if (title.startsWith(keyword)) return 1;
  if (initials.startsWith(keyword)) return 2;
  if (pinyin.startsWith(keyword)) return 3;
  if (name.startsWith(keyword)) return 4;
  if (title.includes(keyword)) return 5;
  if (name.includes(keyword)) return 6;
  if (pathIni.startsWith(keyword) || pathPy.startsWith(keyword)) return 7;
  if (path.includes(keyword)) return 8;
  return 9;
}

/**
 * 菜单模糊搜索
 *
 * 空格分隔多关键字时要求全部命中；显示名、菜单名、URL 任一命中即可。
 * 结果按匹配优先级排序（完全相同 → 前缀 → 包含 → 地址），同优先级按路径长度升序。
 * @param menus 平铺菜单
 * @param keyword 搜索关键字
 * @param limit 最大返回条数
 * @returns 搜索结果，未命中返回空数组
 */
export function searchMenus(
  menus: FlatMenuItem[] | undefined,
  keyword: string,
  limit = 20,
  recentUrls: string[] = [],
): MenuSearchResult[] {
  const tokens = getSearchTokens(keyword);
  if (tokens.length === 0 || !menus?.length) return [];

  const pathMap = new Map(menus.map((item) => [item.id, item]));
  const results: MenuSearchResult[] = [];
  for (const menu of menus) {
    if (!isSearchableMenu(menu) || hasHiddenAncestor(pathMap, menu)) continue;

    // 父级显示名/拼音也参与匹配：输入“系统 用户”或“xitong yh”可命中“系统管理 › 用户”
    const info = getMenuPathInfo(pathMap, menu);
    const haystack = [
      menu.title,
      menu.name,
      menu.path,
      menu.pinYin,
      menu.initials,
      info.path,
      info.pinYin,
      info.initials,
    ]
      .join(' ')
      .toLowerCase();
    if (!tokens.every((token) => haystack.includes(token))) continue;

    results.push({ menu, path: info.path });
  }

  const first = tokens[0];
  results.sort((a, b) => {
    // 最近使用优先，其次按匹配质量
    const ra = recentUrls.indexOf(a.menu.path);
    const rb = recentUrls.indexOf(b.menu.path);
    if (ra >= 0 || rb >= 0) {
      if (ra < 0) return 1;
      if (rb < 0) return -1;
      if (ra !== rb) return ra - rb;
    }

    const infoA = getMenuPathInfo(pathMap, a.menu);
    const infoB = getMenuPathInfo(pathMap, b.menu);
    const scoreA = scoreMenu(a.menu, first, infoA.pinYin, infoA.initials);
    const scoreB = scoreMenu(b.menu, first, infoB.pinYin, infoB.initials);
    return scoreA !== scoreB ? scoreA - scoreB : a.path.length - b.path.length;
  });

  return results.slice(0, limit);
}

/**
 * 最近使用的菜单
 *
 * 按 recentUrls 顺序映射为可搜索菜单，去重并跳过已失效/隐藏的菜单。
 * @param menus 平铺菜单
 * @param recentUrls 最近使用的菜单 URL，最近的在前
 * @param limit 最大返回条数
 * @returns 最近使用菜单，未命中返回空数组
 */
export function getRecentMenus(
  menus: FlatMenuItem[] | undefined,
  recentUrls: string[] | undefined,
  limit = 8,
): MenuSearchResult[] {
  if (!menus?.length || !recentUrls?.length) return [];

  const pathMap = new Map(menus.map((item) => [item.id, item]));
  const byUrl = new Map<string, FlatMenuItem>();
  for (const menu of menus) {
    if (!isSearchableMenu(menu) || hasHiddenAncestor(pathMap, menu)) continue;
    if (!byUrl.has(menu.path)) byUrl.set(menu.path, menu);
  }

  const results: MenuSearchResult[] = [];
  const used = new Set<string>();
  for (const url of recentUrls) {
    if (used.has(url)) continue;
    used.add(url);

    const menu = byUrl.get(url);
    if (!menu) continue;

    results.push({ menu, path: getMenuPathInfo(pathMap, menu).path });
    if (results.length >= limit) break;
  }

  return results;
}

/** 高亮分段。返回纯文本片段，由模板决定用 <mark> 包裹，避免使用 v-html */
export function highlightSegments(
  text: string,
  keyword: string,
): Array<{ text: string; match: boolean }> {
  if (!text) return [];
  const kw = (keyword ?? '').trim().toLowerCase();
  if (!kw) return [{ text, match: false }];

  const index = text.toLowerCase().indexOf(kw);
  if (index < 0) return [{ text, match: false }];

  const segments: Array<{ text: string; match: boolean }> = [];
  if (index > 0) segments.push({ text: text.slice(0, index), match: false });
  segments.push({ text: text.slice(index, index + kw.length), match: true });
  if (index + kw.length < text.length)
    segments.push({ text: text.slice(index + kw.length), match: false });

  return segments;
}
