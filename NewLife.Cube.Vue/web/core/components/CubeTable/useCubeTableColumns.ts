/**
 * 「设置列」状态与持久化（localStorage，按实体前缀分桶）
 *
 * 合并 / 解析口径在 `core/engine/logic.ts` 的纯函数里（`mergeColumnVisibility`、
 * `parseColumnVisibility`），本文件只负责读存储与响应式联动。
 * 新增列默认显示：老用户的历史配置里没有新字段，若不默认显示就会出现「升级后某列凭空消失」。
 */
import { computed, ref, watch, type Ref } from 'vue';
import {
  columnItemsFromFields,
  defaultColumnsStorageKey,
} from './cubeTable.logic';
import { mergeColumnVisibility, parseColumnVisibility } from '../../engine/logic';
import type { FieldMeta } from '../../types/field';

/** 存储介质注入点（默认 localStorage；单测传内存实现） */
export interface ColumnStorageLike {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
}

export interface UseCubeTableColumnsOptions {
  /** 实体前缀来源（用于默认存储键） */
  prefix: () => string;
  /** 是否启用设置列 */
  enabled: boolean;
  /** 显式存储键（覆盖默认按前缀分桶） */
  storageKey?: string;
  /** 存储介质 */
  storage?: ColumnStorageLike;
}

export function useCubeTableColumns(
  fields: Ref<FieldMeta[]> | (() => FieldMeta[]),
  options: UseCubeTableColumnsOptions,
) {
  const readStorage = (): ColumnStorageLike | null => {
    try {
      return options.storage ?? (globalThis.localStorage as ColumnStorageLike);
    } catch {
      return null;
    }
  };

  const items = computed(() => columnItemsFromFields(typeof fields === 'function' ? fields() : fields.value));
  const storageKey = computed(() => options.storageKey || defaultColumnsStorageKey(options.prefix()));
  const visibility = ref<Record<string, boolean>>({});

  function load(): void {
    const storage = readStorage();
    let saved: Record<string, boolean> | null = null;
    try {
      saved = storage ? parseColumnVisibility(storage.getItem(storageKey.value)) : null;
    } catch {
      // 存储被禁用（无痕模式 / 企业策略）时按默认全显示继续，而不是把整页打挂
      saved = null;
    }
    visibility.value = mergeColumnVisibility(items.value, saved);
  }

  function persist(): void {
    const storage = readStorage();
    if (!storage) return;
    try {
      storage.setItem(storageKey.value, JSON.stringify(visibility.value));
    } catch {
      /* 隐私模式 / 配额不足时仅本次生效，不打断页面 */
    }
  }

  function setVisible(key: string, visible: boolean): void {
    visibility.value = { ...visibility.value, [key]: visible };
    persist();
  }

  function reset(): void {
    visibility.value = mergeColumnVisibility(items.value, null);
    persist();
  }

  if (options.enabled) {
    load();
    // 字段集在运行期可能因换实体 / 元数据刷新而变，重新合并一次。
    // 必须 sync：默认 flush 要等下一个 tick，调用方紧接着读 visibility 会拿到旧列集
    watch(items, load, { flush: 'sync' });
  }

  return { items, visibility, setVisible, reset, storageKey };
}
