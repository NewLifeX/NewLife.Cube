/**
 * 实体页探测
 *
 * 菜单驱动的路由只覆盖「后端菜单树里存在的节点」，而部分控制器页面在特定部署下没有菜单行
 * （例如未开启多租户时的租户/租户成员、被菜单可见性过滤掉的隐藏页）。MVC 侧这些页面仍可通过
 * 控制器路由直接访问，SPA 需要同样能力：探测后端是否提供该控制器的 `/meta`，有则按动态页渲染。
 */
import request from './request';
import { routeToApiPrefix } from './url';

/** 实体页探测结果 */
export interface EntityPageProbe {
  /** 后端是否存在该控制器页面 */
  exists: boolean;
  /** 实体显示名（来自 `/meta` 的 `entity.displayName`），用于页头标题 */
  displayName?: string;
}

/**
 * 由前端路由推导实体元数据地址
 * @param path 前端路由路径，如 `/Admin/Tenant`
 * @returns 元数据地址，如 `/api/Admin/Tenant/meta`
 */
export function buildMetaUrl(path: string): string {
  const prefix = routeToApiPrefix(path);
  return `${prefix.replace(/\/+$/, '')}/meta`;
}

/**
 * 解析探测响应，判断是否代表「后端存在该实体页」
 *
 * - HTTP 200 且业务体成功码（`code` 为 0/200）才算存在；
 * - 404 / HTML 错误页 / 空响应 / 参数缺失等错误信封一律视为不存在。
 * @param status HTTP 状态码
 * @param body 已解包或未解包的响应体
 * @returns 探测结果
 */
export function parseEntityPageProbe(status: number, body: unknown): EntityPageProbe {
  if (status !== 200) return { exists: false };
  if (!body || typeof body !== 'object') return { exists: false };

  const obj = body as Record<string, unknown>;
  if (obj.code !== 0 && obj.code !== 200) return { exists: false };

  const data = obj.data as { entity?: { displayName?: string } } | null | undefined;
  const displayName = data?.entity?.displayName;

  return { exists: true, displayName: displayName || undefined };
}

/**
 * 探测指定路由是否存在后端实体页（含配置页）
 * @param path 前端路由路径，如 `/Admin/Tenant`
 * @returns 探测结果
 */
export async function probeEntityPage(path: string): Promise<EntityPageProbe> {
  const segments = path.replace(/^\/+/, '').split('/').filter(Boolean);
  if (segments.length === 0) return { exists: false };

  try {
    const res: any = await request({
      url: buildMetaUrl(path),
      method: 'get',
      validateStatus: () => true,
    });

    // request 已解包为业务体；兼容未解包时带 status 的原始响应
    const status = typeof res?.status === 'number' ? res.status : 200;
    const body = typeof res?.status === 'number' ? res?.data : res;
    return parseEntityPageProbe(status, body);
  } catch {
    return { exists: false };
  }
}
