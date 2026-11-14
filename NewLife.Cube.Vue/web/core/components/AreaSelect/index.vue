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
import i18n from '../../i18n';
import request from '../../utils/request';
import {
  areaPathIds,
  createLazyGuard,
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
    placeholder: undefined,
  },
);

const emit = defineEmits<{
  'update:modelValue': [value: number | undefined];
}>();

const selected = ref<number[]>([]);
const loading = ref(false);
/**
 * 级联选项必须是稳定引用。
 * 模板里写空数组会在重绘时变成新引用，el-cascader 会拆掉 store 再调一次 lazyLoad。
 */
const cascaderOptions: AreaOption[] = [];
/** 同一父级只发一次子级请求。级联在空结果时会反复 lazyLoad，这里把后续调用并到同一次响应 */
const childRequests = new Map<number, Promise<AreaOption[]>>();

/** 风暴短路守卫：同节点失败/空结果后不再重复请求 */
const lazyGuard = createLazyGuard();

/** 拉取直接子级。childLevel 由级联深度推导，后端未返回 level 时同样能正确判定叶子 */
function fetchChildren(parentId: number, childLevel: number): Promise<AreaOption[]> {
  const pending = childRequests.get(parentId);
  if (pending) return pending;

  const task = request({
    url: '/Cube/AreaChilds',
    method: 'get',
    params: { id: parentId },
  })
    .then((res) => toAreaOptions(normalizeAreaResponse(res), props.level, childLevel))
    .catch((err) => {
      childRequests.delete(parentId);
      throw err;
    });
  childRequests.set(parentId, task);
  return task;
}

/** el-cascader lazy 配置 */
const cascaderProps = {
  value: 'value',
  label: 'label',
  lazy: true,
  lazyLoad: (node: { value?: unknown; level?: number } | undefined, resolve: (options?: AreaOption[]) => void) => {
    const id = Number(node?.value ?? 0);
    // 风暴短路：同节点失败/空结果后不再重复请求
    if (!lazyGuard.canLoad(id)) {
      resolve([]);
      return;
    }
    loading.value = true;
    // 根节点（level=0）展开得到第 1 级，其余子级 = 当前节点层级 + 1
    const level = (node?.level ?? 0) + 1;
    fetchChildren(id, level)
      // 仅失败/空结果才 mark（logic.ts createLazyGuard 契约）：成功后允许重开面板走缓存重载。
      // 若「发起即 mark」，cascader-panel watch(options) 重建 store 清空 lazy 数据后，
      // 重触发 lazyLoad 会被 canLoad 短路 → 面板永久「暂无数据」。
      .then((options) => {
        if (options.length === 0) lazyGuard.mark(id);
        resolve(options);
      })
      .catch((err) => {
        console.error('[AreaSelect] Failed to load child areas:', err);
        lazyGuard.mark(id);
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
    console.error('[AreaSelect] Failed to load area path:', err);
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
  const ids = Array.isArray(value) ? value.map(Number).filter((id) => Number.isFinite(id) && id > 0) : [];
  selected.value = ids;
  const next = ids.length ? ids[ids.length - 1] : undefined;
  const current = Number(props.modelValue);
  const alreadyEmpty = next == null && (props.modelValue == null || props.modelValue === '' || !current);
  if (alreadyEmpty || (next != null && current === next)) return;
  emit('update:modelValue', next);
}
</script>

<template>
  <el-cascader
    v-model="selected"
    class="area-select"
    :options="cascaderOptions"
    :props="cascaderProps"
    :disabled="disabled || loading"
    :placeholder="placeholder ?? i18n.global.t('form.placeholderArea')"
    clearable
    @update:model-value="onChange"
  />
</template>

<style scoped>
.area-select {
  width: 100%;
}
</style>
