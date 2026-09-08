<script setup lang="ts">
/**
 * 自动分页包装：根据后端响应形态在「列表页」与「单对象页」之间分流，
 * 避免对象控制器（ObjectController/ConfigController）因缺少 GetPage 而白屏。
 *
 * 判定规则（无 404 探测，两种控制器均返回 200）：
 * - 列表控制器 Index 返回 { data:[...], page:{...} }（无 code 字段）
 * - 对象控制器 Index 返回 { code:0, data:{...} }（带 code 且无 page）
 */
import { ref, onMounted, computed, defineAsyncComponent } from 'vue';
import { useRoute } from 'vue-router';
import request from '@newlifex/cube-vue/core/utils/request';
import { routeToApiPrefix } from '@newlifex/cube-vue/core/utils/url';

const route = useRoute();
const apiPrefix = computed(() => routeToApiPrefix(String(route.path)));
const mode = ref<'pending' | 'list' | 'object'>('pending');

const IndexView = defineAsyncComponent(() => import('./index.vue'));
const ObjectView = defineAsyncComponent(() => import('./object.vue'));

onMounted(async () => {
  try {
    const res: any = await request({ url: apiPrefix.value, method: 'get' });
    if (res && typeof res === 'object' && 'code' in res && !('page' in res)) {
      mode.value = 'object';
    } else {
      mode.value = 'list';
    }
  } catch (e: any) {
    // 请求异常（如未授权）时回退为列表，由 IndexView 自行报错/跳转
    mode.value = 'list';
    console.warn('[AutoPage] detect failed, fallback list:', e?.message);
  }
});
</script>

<template>
  <div v-if="mode === 'pending'" class="autopage-loading">加载中…</div>
  <IndexView v-else-if="mode === 'list'" />
  <ObjectView v-else />
</template>

<style lang="scss" scoped>
.autopage-loading {
  padding: 40px;
  color: var(--el-text-color-secondary);
  text-align: center;
}
</style>
