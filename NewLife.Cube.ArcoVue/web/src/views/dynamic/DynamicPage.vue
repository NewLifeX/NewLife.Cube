<template>
  <component :is="overrideComp" v-if="overrideComp" />
  <div v-else-if="!pageKind" class="dynamic-loading">
    <a-spin />
  </div>
  <DefaultList v-else-if="pageKind === 'entity'" :type="typePath" :auth-id="authId" />
  <DefaultObject v-else-if="pageKind === 'object'" :type="typePath" :auth-id="authId" />
  <DefaultHome v-else-if="pageKind === 'home'" />
  <DbPage v-else-if="pageKind === 'custom' && isDbPage" />
  <FilePage v-else-if="pageKind === 'custom' && isFilePage" />
  <ServiceApiPage v-else-if="pageKind === 'custom'" :type="typePath" />
  <a-empty v-else description="无法识别页面类型" />
</template>

<script setup lang="ts">
/**
 * DynamicPage — 薄宿主
 * 契约：仅接收 type / authId；不读取布局/主题 store。
 * 分发（OSC-2608139feb）：override → entity(DefaultList) → object(DefaultObject)
 * → home(DefaultHome) → custom(Db/File/服务控制器) → unknown(a-empty)。
 */
import { defineAsyncComponent } from 'vue';
import { useDynamicPage } from './useDynamicPage';

/** 异步拉 DefaultList，避免把 VTable 打进 DynamicPage 主 chunk */
const DefaultList = defineAsyncComponent(() => import('@/views/crud/DefaultList.vue'));
/** 通用 ObjectController 页（OSC-2608139feb） */
const DefaultObject = defineAsyncComponent(() => import('@/views/object/DefaultObject.vue'));
/** 主页仪表盘（OSC-2608139feb） */
const DefaultHome = defineAsyncComponent(() => import('@/views/home/DefaultHome.vue'));
/** Admin/Db 专用页（OSC-2608139feb） */
const DbPage = defineAsyncComponent(() => import('@/views/admin/db/index.vue'));
/** Admin/File 专用页（OSC-2608139feb） */
const FilePage = defineAsyncComponent(() => import('@/views/admin/file/index.vue'));
/** Auth/Sso/Mfa 等服务控制器（菜单可在 vTest1 等 Area 下） */
const ServiceApiPage = defineAsyncComponent(() => import('@/views/service/ServiceApiPage.vue'));

const props = defineProps<{
  type?: string;
  authId?: number;
}>();

const {
  typePath,
  authId,
  overrideComp,
  pageKind,
  isDbPage,
  isFilePage,
} = useDynamicPage(props);
</script>

<style scoped>
.dynamic-loading {
  display: flex;
  justify-content: center;
  padding: 64px 0;
}
</style>
