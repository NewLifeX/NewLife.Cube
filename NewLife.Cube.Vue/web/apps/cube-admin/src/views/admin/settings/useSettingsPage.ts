import { computed, reactive } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import request from '@newlifex/cube-vue/core/utils/request';
import { useMenuStore } from '@newlifex/cube-vue/core/stores/menu';
import { getConfig } from '@newlifex/cube-vue/core/configure';
import { normalizeMenuUrl, type RouteNamingStyle } from '@newlifex/cube-vue/core/utils/url';

/** 设置页类型：单对象配置表单 / 列表型配置实体 */
export type SettingsKind = 'form' | 'list';

/** 设置中心的一项配置页 */
export interface SettingsEntry {
  /** 控制器名，如 Core、MailConfig */
  controller: string;
  /** 独立路由地址 */
  path: string;
  /** 页面形态 */
  kind: SettingsKind;
}

/**
 * 设置中心条目。顺序与 MVC 设置页导航一致：
 * 五个单对象配置页在前，四个列表型配置实体在后。
 */
export const settingsEntries: SettingsEntry[] = [
  { controller: 'Core', path: '/Admin/Core', kind: 'form' },
  { controller: 'Sys', path: '/Admin/Sys', kind: 'form' },
  { controller: 'Star', path: '/Admin/Star', kind: 'form' },
  { controller: 'XCode', path: '/Admin/XCode', kind: 'form' },
  { controller: 'Cube', path: '/Admin/Cube', kind: 'form' },
  { controller: 'SmsConfig', path: '/Admin/SmsConfig', kind: 'list' },
  { controller: 'MailConfig', path: '/Admin/MailConfig', kind: 'list' },
  { controller: 'OAuthConfig', path: '/Admin/OAuthConfig', kind: 'list' },
  { controller: 'AccessRule', path: '/Admin/AccessRule', kind: 'list' },
];

/** 路由命名风格：设置页的入口地址需与菜单动态路由保持同一风格 */
const routeStyle: RouteNamingStyle =
  getConfig().router.routeNamingStyle === 'kebab' ? 'kebab' : 'pascal';

/**
 * 设置页实际路由地址
 * @param entry 设置页条目
 * @returns 按 routeNamingStyle 归一化后的路径
 */
export function settingsRoutePath(entry: SettingsEntry): string {
  return normalizeMenuUrl(entry.path, routeStyle);
}

/**
 * 判断当前路由是否指向指定设置页
 * @param entry 设置页条目
 * @param path 当前路由路径
 * @returns 是否命中
 */
function isCurrentEntry(entry: SettingsEntry, path: unknown): boolean {
  return settingsRoutePath(entry).toLowerCase() === String(path ?? '').toLowerCase();
}

type LoadStatus = 'pending' | 'ok' | 'error';

/**
 * 跨页面共享的 meta 状态：设置页之间切换时不再重复请求 /meta。
 * 标题取后端本地化的 entity.displayName，字段仍由各页面自己的 /meta 响应驱动。
 */
const state = reactive({
  titles: {} as Record<string, string>,
  metas: {} as Record<string, Record<string, any> | undefined>,
  status: {} as Record<string, LoadStatus>,
});

const loading = new Map<string, Promise<void>>();

/**
 * 加载指定配置页的 /meta。按控制器去重，并发调用共享同一个请求。
 * @param controller 控制器名
 * @returns 加载完成后的 Promise
 */
export function loadSettingsMeta(controller: string): Promise<void> {
  const status = state.status[controller];
  if (status === 'ok' || status === 'error') return Promise.resolve();

  const exist = loading.get(controller);
  if (exist) return exist;

  const task = request({ url: `/api/Admin/${controller}/meta`, method: 'get' })
    .then((res: any) => {
      const meta = res?.data ?? res;
      // 设置页的 /meta 必然带 form 字段数组；缺失说明拿到的不是页面元数据（如错误信封）
      if (!meta || typeof meta !== 'object' || !Array.isArray(meta.form))
        throw new Error('Invalid meta response');

      state.metas[controller] = meta;
      const displayName = meta.entity?.displayName;
      if (displayName) state.titles[controller] = displayName;
      state.status[controller] = 'ok';
    })
    .catch(() => {
      state.status[controller] = 'error';
    })
    .finally(() => {
      loading.delete(controller);
    });

  loading.set(controller, task);
  return task;
}

/** 并发加载全部设置页的 meta，用于渲染 Tab 标题与当前页可访问性 */
export function loadAllSettingsMeta(): Promise<void[]> {
  return Promise.all(settingsEntries.map((entry) => loadSettingsMeta(entry.controller)));
}

/** 设置页通用状态：当前条目、meta、标题与 Tab 列表 */
export function useSettingsPage() {
  const route = useRoute();
  const router = useRouter();
  const menuStore = useMenuStore();

  const entry = computed(() => settingsEntries.find((item) => isCurrentEntry(item, route.path)));
  const controller = computed(() => entry.value?.controller ?? '');
  const status = computed<LoadStatus>(() =>
    controller.value ? (state.status[controller.value] ?? 'pending') : 'error',
  );
  const meta = computed(() => (controller.value ? state.metas[controller.value] : undefined));

  /** 菜单里的本地化标题，作为 /meta 未返回标题时的兜底，避免 Tab 文案闪烁 */
  const menuTitleOf = (item: SettingsEntry) => {
    const hit = menuStore.flatMenus?.find(
      (menu) => String(menu.path ?? '').toLowerCase() === item.path.toLowerCase(),
    );
    return hit?.title || hit?.name || '';
  };

  const tabs = computed(() =>
    settingsEntries.map((item) => ({
      ...item,
      title: state.titles[item.controller] || menuTitleOf(item) || item.controller,
    })),
  );

  /** 当前配置页标题（/meta 的实体显示名优先） */
  const title = computed(() => {
    const item = entry.value;
    if (!item) return '';
    return state.titles[item.controller] || menuTitleOf(item) || '';
  });

  /** 设置中心总标题：沿用菜单「设置中心」的本地化名称，缺失时回退当前页标题 */
  const pageTitle = computed(() => {
    const cube = settingsEntries.find((item) => item.controller === 'Cube');
    return (cube && menuTitleOf(cube)) || title.value;
  });

  const activeController = computed(() => entry.value?.controller ?? '');

  /**
   * 切换到指定配置页
   * @param target 目标设置页
   */
  const go = (target: SettingsEntry) => {
    if (!isCurrentEntry(target, route.path)) router.push(settingsRoutePath(target));
  };

  return {
    entry,
    activeController,
    status,
    meta,
    pageTitle,
    tabs,
    go,
    loadAll: loadAllSettingsMeta,
  };
}
