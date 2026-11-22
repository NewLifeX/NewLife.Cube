import { reactive } from 'vue';
import type { PageMeta } from '@newlifex/api-core';
import { cubeApi } from '@/api/cube';

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
 * 设置中心条目。顺序与魔方 MVC/官方 Vue 设置页导航一致：
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
  { controller: 'AccessRule', path: '/Admin/AccessRule', kind: 'list' }
];

/**
 * 是否命中某个设置页
 * @param path 当前路由路径
 * @returns 命中的设置页条目；非设置页返回 undefined
 */
export function findSettingsEntry(path: string): SettingsEntry | undefined {
  const normalized = path.replace(/\/+$/, '').toLowerCase();
  return settingsEntries.find(entry => entry.path.toLowerCase() === normalized);
}

type LoadStatus = 'pending' | 'ok' | 'error';

/**
 * 跨页面共享的 meta 状态：设置页之间切换时不再重复请求 /meta。
 * 标题取后端本地化的 entity.displayName，字段仍由各页面自己的 /meta 响应驱动。
 */
const state = reactive({
  titles: {} as Record<string, string>,
  metas: {} as Record<string, PageMeta | undefined>,
  status: {} as Record<string, LoadStatus>
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

  const task = cubeApi.page
    .getPage(`/Admin/${controller}`)
    .then(res => {
      const meta = res?.data;
      // 设置页的 /meta 必然是对象；字符串（HTML 错误页）说明拿到的不是页面元数据
      if (!meta || typeof meta !== 'object') throw new Error('Invalid meta response');

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

/** 并发加载全部设置页的 meta，用于渲染 Tab 标题 */
export function loadAllSettingsMeta(): Promise<void[]> {
  return Promise.all(settingsEntries.map(entry => loadSettingsMeta(entry.controller)));
}

/**
 * 读取设置页共享状态
 * @param controller 当前控制器名，或返回控制器名的 getter（页内切换时保持响应）
 * @returns 当前页 meta、加载状态、各 Tab 标题
 */
export function useSettingsMeta(controller: string | (() => string)) {
  const getName = typeof controller === 'function' ? controller : () => controller;
  const meta = () => {
    const name = getName();
    return name ? state.metas[name] : undefined;
  };
  const status = (): LoadStatus => {
    const name = getName();
    return name ? state.status[name] ?? 'pending' : 'error';
  };
  const titleOf = (name: string) => state.titles[name] || '';

  return { meta, status, titleOf };
}
