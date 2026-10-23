<script setup lang="ts">
/**
 * 地区级联选择器（ItemType=area1~4）
 *
 * 数据源为魔方内置地区接口：
 * - `GET /Cube/AreaChilds?id=` 懒加载子级；
 * - `GET /Cube/AreaParents?id=&isContainSelf=true` 回显已有值。
 *
 * 通过 `el-cascader` 的 lazy 模式按层加载，达到 `level` 层级即视为叶子；
 * `v-model` 只暴露最后一级地区编号（与后端 AreaId 字段一致）。
 */
import { ref, watch } from 'vue';
import request from '../../utils/request';
import {
  areaPathIds,
  normalizeAreaResponse,
  toAreaOptions,
  type AreaOption,
} from './areaSelect.logic';

const props = withDefaults(
  defineProps<{
    /** 当前地区编号 */
    modelValue?: number | string | null;
    /** 目标层级：1 省 / 2 市 / 3 区 / 4 街道，默认 3 */
    level?: number;
    /** 是否禁用 */
    disabled?: boolean;
    /** 占位文案 */
    placeholder?: string;
  }>(),
  {
    modelValue: null,
    level: 3,
    disabled: false,
    placeholder: '请选择地区',
  },
);

const emit = defineEmits<{
  'update:modelValue': [value: number | undefined];
}>();

const selected = ref<number[]>([]);
const loading = ref(false);

/** 拉取直接子级 */
async function fetchChildren(parentId: number): Promise<AreaOption[]> {
  const res = await request({
    url: '/Cube/AreaChilds',
    method: 'get',
    params: { id: parentId },
  });
  return toAreaOptions(normalizeAreaResponse(res), props.level);
}

/** el-cascader lazy 配置 */
const cascaderProps = {
  value: 'value',
  label: 'label',
  lazy: true,
  lazyLoad: (node: { value?: unknown } | undefined, resolve: (options?: AreaOption[]) => void) => {
    const id = Number(node?.value ?? 0);
    loading.value = true;
    fetchChildren(id)
      .then((options) => resolve(options))
      .catch((err) => {
        console.error('[AreaSelect] 地区子级加载失败:', err);
        resolve([]);
      })
      .finally(() => {
        loading.value = false;
      });
  },
};

/** 按已有编号回显完整父级路径 */
async function syncPath(value: unknown): Promise<void> {
  const id = Number(value);
  if (!id) {
    selected.value = [];
    return;
  }

  try {
    const res = await request({
      url: '/Cube/AreaParents',
      method: 'get',
      params: { id, isContainSelf: true },
    });
    const path = areaPathIds(normalizeAreaResponse(res));
    selected.value = path.length ? path : [id];
  } catch (err) {
    console.error('[AreaSelect] 地区路径加载失败:', err);
    selected.value = [id];
  }
}

watch(
  () => props.modelValue,
  (value) => {
    // 用户选择时已把路径写入 selected，避免重复请求
    if (selected.value.at(-1) === Number(value)) return;
    void syncPath(value);
  },
  { immediate: true },
);

function onChange(value: unknown): void {
  const ids = Array.isArray(value) ? value.map(Number) : [];
  selected.value = ids;
  emit('update:modelValue', ids.length ? ids[ids.length - 1] : undefined);
}
</script>

<template>
  <el-cascader
    v-model="selected"
    class="area-select"
    :options="[]"
    :props="cascaderProps"
    :disabled="disabled || loading"
    :placeholder="placeholder"
    clearable
    @update:model-value="onChange"
  />
</template>

<style scoped>
.area-select {
  width: 100%;
}
</style>
