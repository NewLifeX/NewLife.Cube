<script setup lang="ts">
/**
 * 自动分页包装：根据后端响应形态在「列表页」与「单对象页」之间分流，
 * 避免对象控制器（ObjectController/ConfigController）因缺少 meta 而白屏。
 *
 * 判定规则（无 404 探测，两种控制器均返回 200）：
 * - 列表控制器 Index 返回 { data:[...], page:{...} }（无 code 字段）
 * - 对象控制器 Index 返回 { code:0, data:{...} }（带 code 且无 page）
 */
import { ref, onMounted, computed, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute } from 'vue-router';
import request from '@newlifex/cube-vue/core/utils/request';
import { routeToApiPrefix } from '@newlifex/cube-vue/core/utils/url';

const route = useRoute();
const { t } = useI18n();
const apiPrefix = computed(() => routeToApiPrefix(String(route.path)));
const mode = ref<'pending' | 'list' | 'object' | 'unsupported'>('pending');

const IndexView = defineAsyncComponent(() => import('./index.vue'));
const ObjectView = defineAsyncComponent(() => import('./object.vue'));

onMounted(async () => {
  try {
    // validateStatus 放行 4xx：接口型控制器（只有自定义 Action）本来就没有列表/表单元数据，
    // 这里的 404 属于预期结果，不能弹全局错误提示，也不该退化成「空表 + 点了就报错的工具栏」。
    const res: any = await request({ url: apiPrefix.value, method: 'get', validateStatus: () => true });
    if (res === null || res === undefined || typeof res !== 'object') {
      mode.value = 'unsupported';
    } else if ('code' in res && !('page' in res)) {
      mode.value = 'object';
    } else {
      mode.value = 'list';
    }
  } catch (e: any) {
    // 其它异常（如未授权）时回退为列表，由 IndexView 自行报错/跳转
    mode.value = 'list';
    console.warn('[AutoPage] detect failed, fallback list:', e?.message);
  }
});
</script>

<template>
    <div v-if="mode === 'pending'" class="autopage-loading">{{ t('common.loading') }}</div>
  <div v-else-if="mode === 'unsupported'" class="autopage-unsupported">
    <el-empty :description="t('autoPage.noEntityPage')" />
  </div>
  <IndexView v-else-if="mode === 'list'" />
  <ObjectView v-else />
</template>

<style lang="scss" scoped>
.autopage-loading {
  padding: 40px;
  color: var(--el-text-color-secondary);
  text-align: center;
}

.autopage-unsupported {
  padding: 40px 0;
}
</style>
