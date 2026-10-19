<script setup lang="ts">
/**
 * 列表页「图表」按钮 + 弹窗（GetChartData）
 *
 * 魔方后台的通用能力，但不属于 `CubeTable` 的职责（组件只管一张表的 CRUD），
 * 所以从默认页里抽出来单独复用：默认列表页与需要它的自定义视图页都用这一个。
 * echarts 由弹窗组件懒加载，未点按钮时不进包。
 */
import { computed, defineAsyncComponent, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { DataLine } from '@element-plus/icons-vue';
import request from '../../utils/request';
import { routeToApiPrefix } from '../../utils/url';
import { useRoute } from 'vue-router';

const props = withDefaults(
  defineProps<{
    /** 实体前缀；缺省按当前路由推导（与列表页同一口径） */
    api?: string;
    /** 按钮文案 */
    label?: string;
  }>(),
  { api: undefined, label: '图表' },
);

const ChartDialog = defineAsyncComponent(
  () => import('./ListChartDialog.vue'),
);

const route = useRoute();
const prefix = computed(() => props.api ?? routeToApiPrefix(route.path));
const visible = ref(false);
const charts = ref<Record<string, unknown>[]>([]);

/** 后端未实现 GetChartData 时按失败提示，而不是弹一个空图表窗 */
async function open(): Promise<void> {
  try {
    const res = (await request.get(`${prefix.value}/GetChartData`)) as { data?: unknown };
    const data = res?.data ?? res;
    charts.value = Array.isArray(data) ? (data as Record<string, unknown>[]) : [];
    visible.value = true;
  } catch (err) {
    ElMessage.error(err instanceof Error ? err.message : '获取图表失败');
  }
}
</script>

<template>
  <el-button :icon="DataLine" @click="open">{{ label }}</el-button>
  <ChartDialog v-model:visible="visible" :charts="charts" />
</template>
