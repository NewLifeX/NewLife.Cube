import { onMounted, ref, watch } from 'vue';
import cubeApi from '@/api';
import { leafFromCascaderChange } from '@/core/utils/cascaderValue';

/**
 * 地区级联选择（User.AreaId）。
 *
 * 后端使用系统内置地区实体 Area（XCode.Membership.Area），层级为
 * 省/市/区，通过 ParentID 组织树。组件懒加载子级（点击展开时按 parentId 拉取），
 * 提交值为叶子节点 ID（绑定到实体的 AreaId 整型字段，与历史 area4 行为一致）。
 *
 * 编辑回显：给定叶子 AreaId 时，逐级向上查 ParentID 还原整条路径，避免一次性拉全量。
 */
interface CascadeOption {
  value: number | string;
  label: string;
  isLeaf?: boolean;
  children?: CascadeOption[];
}

/** 模块级缓存：id → area 记录，避免重复请求同一节点 */
const areaCache = new Map<string, { id: number | string; name: string; parentId: number | string }>();

/** CascaderField 组件 props 类型（与 CascaderField.vue defineProps 泛型逐字一致） */
interface CascaderFieldProps {
  modelValue?: string | number | null;
  disabled?: boolean;
  placeholder?: string;
}

/** CascaderField 组件 emits 类型（与 CascaderField.vue defineEmits 泛型逐字一致） */
interface CascaderFieldEmits {
  'update:modelValue': [value: number | string | undefined];
}

type CascaderFieldEmit = <K extends keyof CascaderFieldEmits>(event: K, ...args: CascaderFieldEmits[K]) => void;

/** CascaderField 组件全部业务 TS：地区树懒加载与叶子路径回显（自 CascaderField.vue script setup 原样搬移） */
export function useCascaderField(props: CascaderFieldProps, emit: CascaderFieldEmit) {
  const options = ref<CascadeOption[]>([]);
  const pathValue = ref<(number | string)[]>([]);
  const loading = ref(false);

  function toArea(rec: Record<string, unknown>): { id: number | string; name: string; parentId: number | string } {
    // 兼容 camelCase / PascalCase / Int64AsString
    const id = (rec.id ?? rec.Id ?? rec.ID) as number | string;
    const name = (rec.name ?? rec.Name) as string;
    const parentId = (rec.parentId ?? rec.ParentId ?? rec.ParentID ?? rec.parentID) as number | string;
    return { id, name: String(name ?? ''), parentId: parentId ?? 0 };
  }

  async function loadChildren(parentId: number | string): Promise<CascadeOption[]> {
    try {
      const res = await cubeApi.page.getList<Record<string, unknown>>('/Cube/Area', {
        parentid: parentId,
        pageSize: 500,
      });
      const list = Array.isArray(res) ? res : ((res as { data?: unknown[] })?.data ?? []);
      return list.map((r) => {
        const a = toArea(r as Record<string, unknown>);
        areaCache.set(String(a.id), a);
        return {
          value: a.id,
          label: a.name,
          isLeaf: false, // 地区最多 4 级，懒加载时再判定
        };
      });
    } catch {
      return [];
    }
  }

  async function ensureChildren(node: CascadeOption) {
    if (node.children && node.children.length) return;
    const children = await loadChildren(node.value);
    if (children.length) {
      node.children = children;
    } else {
      node.isLeaf = true;
    }
  }

  async function loadRoot() {
    loading.value = true;
    try {
      options.value = await loadChildren(0);
    } finally {
      loading.value = false;
    }
  }

  /** 由叶子 ID 向上回溯路径 */
  async function resolvePath(leafId: number | string): Promise<(number | string)[]> {
    const path: (number | string)[] = [leafId];
    let current = areaCache.get(String(leafId));

    // 先抓叶子记录
    if (!current) {
      try {
        const res = await cubeApi.page.getDetail<Record<string, unknown>>('/Cube/Area', leafId);
        const data = (res as unknown as { data?: Record<string, unknown> })?.data;
        if (data && typeof data === 'object') {
          current = toArea(data);
          areaCache.set(String(current.id), current);
        }
      } catch {
        /* ignore */
      }
    }

    // 向上查父级
    let guard = 0;
    while (current && Number(current.parentId) > 0 && guard < 8) {
      const parent = areaCache.get(String(current.parentId));
      if (parent) {
        path.unshift(parent.id);
        current = parent;
        guard++;
        continue;
      }
      try {
        const res = await cubeApi.page.getDetail<Record<string, unknown>>(
          '/Cube/Area',
          current.parentId,
        );
        const data = (res as unknown as { data?: Record<string, unknown> })?.data;
        if (data && typeof data === 'object') {
          const p = toArea(data);
          areaCache.set(String(p.id), p);
          path.unshift(p.id);
          current = p;
          guard++;
        } else {
          break;
        }
      } catch {
        break;
      }
    }

    // 沿路径预加载各级 children，确保 cascader 能展开到叶子
    let layer = options.value;
    for (const id of path) {
      const node = layer.find((n) => String(n.value) === String(id));
      if (!node) break;
      await ensureChildren(node);
      layer = node.children ?? [];
    }
    return path;
  }

  async function refreshPathFromModel() {
    if (props.modelValue == null || props.modelValue === '') {
      pathValue.value = [];
      return;
    }
    if (!options.value.length) await loadRoot();
    const path = await resolvePath(props.modelValue);
    pathValue.value = path;
  }

  function onChange(val: unknown) {
    // 归一提交值：path-mode 下取末段叶子；清空/空数组发 undefined
    const leaf = leafFromCascaderChange(val);
    pathValue.value = Array.isArray(val)
      ? (val as (number | string)[])
      : leaf == null
        ? []
        : [leaf];
    emit('update:modelValue', leaf);
  }

  /** a-cascader load-more：展开节点时懒加载子级，无子则置 isLeaf */
  async function loadMore(option: CascadeOption, done: (children: CascadeOption[]) => void) {
    await ensureChildren(option);
    done(option.children ?? []);
  }

  onMounted(async () => {
    await loadRoot();
    await refreshPathFromModel();
  });

  watch(() => props.modelValue, refreshPathFromModel);

  return {
    options,
    pathValue,
    loading,
    onChange,
    loadMore,
  };
}
