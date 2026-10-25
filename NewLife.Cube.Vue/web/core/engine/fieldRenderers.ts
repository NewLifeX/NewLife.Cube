/**
 * 字段级全局渲染器注册表（F1.5）
 *
 * 后端在字段元数据里下发 `view` 名称与 `props`，前端在这里按名注册一次组件，
 * 列表单元格、搜索控件、表单控件三处按同一个名字复用，避免为单个字段复制整页组件。
 *
 * 契约（与 `toolbarRenderers.ts` 一致）：
 * - 渲染器组件收到 `field` / `value`，列表追加 `row` / `column` / `ctx`，表单与搜索追加 `model` / `disabled` / `ctx`；
 * - 表单与搜索通过 `update:value` 回写值，列表单元格只读；
 * - 名称未注册时告警一次并回退到内置控件，不阻断页面渲染。
 */
import type { Component } from 'vue';
import type { FieldMeta } from '../types/field';
import type { CubeEngineContext } from './types';

/** 渲染器组件统一入参 */
export interface FieldRendererProps {
  /** 字段元数据 */
  field: FieldMeta;
  /** 当前值。列表为单元格原始值，搜索 / 表单为模型值 */
  value: unknown;
  /** 当前行。列表单元格使用 */
  row?: Record<string, unknown>;
  /** 当前表单或搜索模型。表单 / 搜索使用 */
  model?: Record<string, unknown>;
  /** 引擎上下文。列表 / 搜索使用 */
  ctx?: CubeEngineContext;
  /** 是否只读 / 禁用。表单查看态与只读字段使用 */
  disabled?: boolean;
}

/** 渲染器登记项 */
export interface FieldRenderer {
  /** 渲染组件 */
  component: Component;
  /** 透传给组件的额外 props */
  props?: Record<string, unknown>;
}

const registry = new Map<string, FieldRenderer>();
const warned = new Set<string>();

/** 注册字段渲染器。同名后注册覆盖先注册 */
export function registerFieldRenderer(name: string, renderer: FieldRenderer): void {
  if (!name) return;
  registry.set(name, renderer);
  warned.delete(name);
}

/** 取字段渲染器。未注册时告警一次并返回 undefined，调用方回退内置控件 */
export function getFieldRenderer(name?: string | null): FieldRenderer | undefined {
  if (!name) return undefined;
  const hit = registry.get(name);
  if (!hit && !warned.has(name)) {
    warned.add(name);
    console.warn(`[CubeTable] Field renderer not registered: ${name}`);
  }
  return hit;
}

/** 按字段元数据解析渲染器。字段未声明 view 时返回 undefined */
export function resolveFieldRenderer(field?: Pick<FieldMeta, 'view'> | null): FieldRenderer | undefined {
  return getFieldRenderer(field?.view);
}

/** 清空注册表（测试用） */
export function clearFieldRenderers(): void {
  registry.clear();
  warned.clear();
}
