/**
 * 工具栏自定义渲染器注册表。
 *
 * 对应 MVC 的 `_List_Toolbar_Custom.cshtml` 里那些需要弹窗、上传或图表的按钮：
 * 后端在 `/meta` 的 `toolbar` 中用 `kind: 'custom'` + `renderer` 声明名字，
 * 前端在这里注册一次，所有页面按名复用。未注册的名字会被静默忽略并告警一次。
 */
import type { Component } from 'vue';
import type { BackendToolbarItem } from '../../engine/types';

export interface ToolbarRenderer {
  /** 渲染组件。收到 `ctx` 与 `item` 两个 props */
  component: Component;
  /** 透传给组件的额外 props */
  props?: Record<string, unknown>;
}

const registry = new Map<string, ToolbarRenderer>();
const warned = new Set<string>();

/** 注册工具栏渲染器 */
export function registerToolbarRenderer(name: string, renderer: ToolbarRenderer): void {
  if (!name) return;
  registry.set(name, renderer);
  warned.delete(name);
}

/** 取工具栏渲染器，未注册时告警一次并返回 undefined */
export function getToolbarRenderer(name?: string | null): ToolbarRenderer | undefined {
  if (!name) return undefined;
  const hit = registry.get(name);
  if (!hit && !warned.has(name)) {
    warned.add(name);
    console.warn(`[CubeTable] toolbar renderer not registered: ${name}`);
  }
  return hit;
}

/** 清空注册表（测试用） */
export function clearToolbarRenderers(): void {
  registry.clear();
  warned.clear();
}

/** 判断按钮是否需要按选中状态禁用 */
export function toolbarDisabled(item: BackendToolbarItem, selectedCount: number): boolean {
  return Boolean(item.requiresSelection) && selectedCount === 0;
}
