import { FieldKind } from '@cube/api-core';
import type { FieldMeta } from '@/core/types/field';
import { toFieldMetas } from '@/core/utils/fieldNormalize';
import { enrichFieldsWithEnumDataSource, enrichFieldsWithLookup } from '@/core/utils/lov-api';
import cubeApi from '@/api';
import { normalizeTypePath } from './legacy';

/** 解包 ApiResponse / 直出，避免 .data 套一层 */
export function unwrapPayload(raw: unknown): unknown {
  let cur = raw;
  for (let i = 0; i < 3; i++) {
    if (cur == null || typeof cur !== 'object' || Array.isArray(cur)) break;
    const o = cur as Record<string, unknown>;
    if ('code' in o && ('data' in o || 'Data' in o)) {
      cur = o.data ?? o.Data;
      continue;
    }
    break;
  }
  return cur;
}

/** 从对象上按 camel / Pascal 取字段 */
function pick<T = unknown>(obj: Record<string, unknown> | null | undefined, ...keys: string[]): T | undefined {
  if (!obj) return undefined;
  for (const k of keys) {
    if (k in obj && obj[k] != null) return obj[k] as T;
  }
  return undefined;
}

function asFieldArray(raw: unknown): unknown[] {
  if (Array.isArray(raw)) return raw;
  if (raw && typeof raw === 'object') {
    const o = raw as Record<string, unknown>;
    const nested = o.list ?? o.List ?? o.fields ?? o.Fields;
    if (Array.isArray(nested)) return nested;
  }
  return [];
}

/** 合并多源字段：同名保留更完整的 displayName / typeName / dataSource */
export function mergeFieldMetas(...sources: FieldMeta[][]): FieldMeta[] {
  const map = new Map<string, FieldMeta>();
  for (const list of sources) {
    for (const f of list) {
      const key = (f.name || '').toLowerCase();
      if (!key) continue;
      const prev = map.get(key);
      if (!prev) {
        map.set(key, { ...f });
        continue;
      }
      const displayName =
        (prev.displayName && prev.displayName !== prev.name ? prev.displayName : '') ||
        (f.displayName && f.displayName !== f.name ? f.displayName : '') ||
        prev.displayName ||
        f.displayName ||
        prev.name;
      const typeName =
        (prev.typeName && prev.typeName !== 'String' ? prev.typeName : '') ||
        (f.typeName && f.typeName !== 'String' ? f.typeName : '') ||
        prev.typeName ||
        f.typeName ||
        'String';
      const dataSource =
        prev.dataSource && Object.keys(prev.dataSource).length
          ? prev.dataSource
          : f.dataSource && Object.keys(f.dataSource).length
            ? f.dataSource
            : prev.dataSource || f.dataSource;
      map.set(key, {
        ...f,
        ...prev,
        name: prev.name || f.name,
        displayName,
        typeName,
        dataSource,
        lovCode: prev.lovCode || f.lovCode,
        itemType: prev.itemType || f.itemType,
        hasTypeName: prev.hasTypeName || f.hasTypeName,
      });
    }
  }
  return [...map.values()];
}

async function loadFromGetPage(tp: string): Promise<FieldMeta[]> {
  try {
    const pageRes = await cubeApi.page.getPage(tp);
    const meta = unwrapPayload(pageRes?.data ?? pageRes) as Record<string, unknown> | null;
    if (!meta || typeof meta !== 'object' || Array.isArray(meta) || typeof meta === 'string') {
      return [];
    }
    const nested = pick<Record<string, unknown>>(meta, 'fields', 'Fields');
    const raw =
      pick(meta, 'list', 'List') ??
      (nested ? pick(nested, 'list', 'List') : undefined);
    return toFieldMetas(asFieldArray(raw) as never).filter((f) => !!f.name);
  } catch {
    return [];
  }
}

async function loadFromGetFields(tp: string): Promise<FieldMeta[]> {
  try {
    const fb = await cubeApi.page.getFields(tp, FieldKind.List);
    return toFieldMetas(asFieldArray(unwrapPayload(fb?.data ?? fb)) as never).filter(
      (f) => !!f.name,
    );
  } catch {
    return [];
  }
}

/** Automation/Meta：配置抽屉同源，通常带中文 displayName + typeName */
async function loadFromAutomationMeta(tp: string): Promise<FieldMeta[]> {
  try {
    const res = await cubeApi.automation.meta(tp);
    const rows = asFieldArray(unwrapPayload(res?.data ?? res));
    return rows
      .map((row) => {
        const o = (row && typeof row === 'object' ? row : {}) as Record<string, unknown>;
        const name = String(pick(o, 'name', 'Name') ?? '').trim();
        if (!name) return null;
        const displayName = String(pick(o, 'displayName', 'DisplayName') ?? name).trim() || name;
        const typeName = String(pick(o, 'typeName', 'TypeName') ?? 'String').trim() || 'String';
        return {
          name,
          displayName,
          typeName,
          primaryKey: Boolean(pick(o, 'primaryKey', 'PrimaryKey')),
          readOnly: Boolean(pick(o, 'readOnly', 'ReadOnly')),
          hasTypeName: !!String(pick(o, 'typeName', 'TypeName') ?? '').trim(),
        } as FieldMeta;
      })
      .filter((f): f is FieldMeta => !!f);
  } catch {
    return [];
  }
}

/**
 * 加载实体 List 字段元数据（含中文显示名、枚举/Lookup dataSource）。
 * 多源合并：GetPage → GetFields → Automation/Meta，互不阻塞。
 */
export async function loadEntityListFields(typePath: string | undefined): Promise<FieldMeta[]> {
  const tp = normalizeTypePath(typePath);
  if (!tp) return [];

  const [fromPage, fromFields, fromAuto] = await Promise.all([
    loadFromGetPage(tp),
    loadFromGetFields(tp),
    loadFromAutomationMeta(tp),
  ]);

  const list = mergeFieldMetas(fromPage, fromFields, fromAuto);
  if (!list.length) return [];

  await enrichFieldsWithEnumDataSource(list);
  await enrichFieldsWithLookup(list);
  return list;
}

export function findFieldMeta(metas: FieldMeta[], name: string): FieldMeta | undefined {
  const key = (name || '').toLowerCase();
  if (!key) return undefined;
  return metas.find((f) => (f.name || '').toLowerCase() === key);
}

/** 解析显示标签：优先中文 displayName */
export function fieldLabelOf(metas: FieldMeta[], name: string): string {
  const hit = findFieldMeta(metas, name);
  const label = hit?.displayName?.trim();
  if (label) return label;
  return (hit?.name || name || '').trim();
}
