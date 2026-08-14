/**
 * Object 配置页发现纯函数（OSC-2608139feb 魔方设置优化）。
 *
 * 配置中心左侧列表需要自动注入所有 ObjectController 配置页。
 * 这里提供「菜单树 → 候选对象页」的纯函数：两层 URL（/Area/Controller）、
 * 可见菜单、去重、排除当前页；探测（entity/object）由 useDefaultObject 完成。
 */
import type { MenuItem } from '@cube/api-core';

export interface ObjectPageRef {
  /** 类型路径（如 /Admin/Cube），兼做菜单 key */
  type: string;
  /** 展示名：菜单 displayName 优先 */
  name: string;
}

/**
 * 收集菜单树中的对象页候选（两层 URL 且可见；排除当前页与重复项）。
 * @param menus 菜单树
 * @param currentType 当前类型路径（排除自身）
 */
export function collectObjectCandidates(
  menus: MenuItem[],
  currentType?: string,
): ObjectPageRef[] {
  const out: ObjectPageRef[] = [];
  const seen = new Set<string>();
  const current = (currentType ?? '').replace(/\/+$/, '').toLowerCase();

  const walk = (items: MenuItem[]) => {
    for (const m of items ?? []) {
      const url = m.url ?? '';
      const key = url.toLowerCase();
      if (url && m.visible !== false && !seen.has(key)) {
        seen.add(key);
        const segs = url.split('/').filter(Boolean);
        if (segs.length === 2 && url.replace(/\/+$/, '').toLowerCase() !== current) {
          out.push({ type: url, name: m.displayName || m.name || segs[1] });
        }
      }
      if (m.children?.length) walk(m.children);
    }
  };
  walk(menus);
  return out;
}
