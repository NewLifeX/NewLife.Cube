<script setup lang="ts">
/**
 * 地区级联选择器（ItemType=area/area1~area4）
 *
 * 数据来源为魔方地区接口：
 * - `GET /Cube/AreaChilds?id=` 按层懒加载子级；
 * - `GET /Cube/AreaParents?id=&isContainSelf=true` 回显已有编号的完整路径。
 *
 * v-model 只暴露最后一级地区编号（与后端 AreaId 字段一致）。
 */
import { ref, watch } from 'vue';
import { ElCascader } from 'element-plus';
import type { CascaderProps } from 'element-plus';
import type { AreaNode } from '@newlifex/api-core';
import { cubeApi } from '@/api/cube';

defineOptions({ name: 'AreaSelect' });

interface AreaOption {
  value: number;
  label: string;
  level: number;
  leaf: boolean;
  children?: AreaOption[];
}

const props = withDefaults(
  defineProps<{
    /** 当前地区编号 */
    modelValue?: number | string | null;
    /** 目标层级：1 省 / 2 市 / 3 区 / 4 街道，默认 3 */
    level?: number;
    /** 占位文案 */
    placeholder?: string;
  }>(),
  {
    modelValue: null,
    level: 3,
    placeholder: undefined
  }
);

const emit = defineEmits<{
  'update:modelValue': [value: number | undefined];
}>();

const selected = ref<number[]>([]);
const options = ref<AreaOption[]>([]);
let syncToken = 0;

/** 后端已下发层级时直接使用，否则按行政区划代码推断 */
function levelOf(node: AreaNode) {
  if (node.level && node.level > 0) return node.level;

  const id = Number(node.id);
  if (String(id).length > 6) return 4;
  if (id % 100 !== 0) return 3;
  if (id % 10000 !== 0) return 2;
  return 1;
}

function toOption(node: AreaNode): AreaOption {
  const level = levelOf(node);

  return { value: node.id, label: node.name, level, leaf: level >= props.level };
}

async function fetchChilds(parentId: number): Promise<AreaOption[]> {
  const res = await cubeApi.area.getChilds(parentId);

  return (res.data ?? []).map(toOption);
}

const cascaderProps: CascaderProps = {
  value: 'value',
  label: 'label',
  lazy: true,
  lazyLoad: (node, resolve) => {
    const parentId = Number((node as unknown as { value?: unknown } | undefined)?.value ?? 0);
    const done = resolve as unknown as (data: AreaOption[]) => void;
    fetchChilds(parentId)
      .then(done)
      .catch(() => done([]));
  }
};

/** 预加载已有编号所在路径的各级兄弟节点，保证下拉能正常展开并显示名称 */
async function preloadPath(chain: AreaNode[]) {
  let list = await fetchChilds(0);
  options.value = list;

  for (const node of chain) {
    const current = list.find(item => item.value === node.id);
    if (!current || current.leaf) break;

    const childs = await fetchChilds(node.id);
    if (!childs.length) break;

    current.children = childs;
    list = childs;
  }
}

async function syncPath(value: unknown) {
  const token = ++syncToken;
  const id = Number(value);

  if (!id) {
    selected.value = [];
    options.value = [];
    return;
  }

  try {
    const res = await cubeApi.area.getParents(id);
    if (token !== syncToken) return;

    const chain = (res.data ?? []).slice().sort((a, b) => levelOf(a) - levelOf(b));
    if (!chain.length) {
      selected.value = [id];
      return;
    }

    await preloadPath(chain);
    if (token !== syncToken) return;

    selected.value = chain.map(item => item.id);
  } catch {
    if (token === syncToken) selected.value = [id];
  }
}

function onChange(value: unknown) {
  const ids = Array.isArray(value) ? value.map(Number) : [];
  selected.value = ids;
  emit('update:modelValue', ids.length ? ids[ids.length - 1] : undefined);
}

watch(
  () => props.modelValue,
  value => {
    // 用户选择时已写入 selected，避免回显请求
    if (selected.value[selected.value.length - 1] === Number(value)) return;
    void syncPath(value);
  },
  { immediate: true }
);
</script>

<template>
  <ElCascader
    v-model="selected"
    class="w-full"
    :options="options"
    :props="cascaderProps"
    :placeholder="placeholder"
    clearable
    @update:model-value="onChange"
  />
</template>
