<script setup lang="ts">
/**
 * 列表页（框架默认模板）
 *
 * 只有一条渲染路径：整页交给 `CubeTable`（操作栏 → 搜索条件 → 表格统计 → 表格 → 分页），
 * 字段元数据、取数、分页、增删改查、导入导出全部由页面引擎（`useCubeEngine`）按当前路由完成。
 *
 * 本文件只补两件 CubeTable 不该内置的事：
 *   - 页头（标题可由 `ListPageHeader` Section 覆盖，默认 `ListPageHeader.vue`）；
 *   - 「图表」（`GetChartData` 是魔方后台专属能力，放统计行右侧与「设置列」「刷新」同组）。
 */
import { defineAsyncComponent, inject } from 'vue';
import { useRoute } from 'vue-router';
import {
  ListPageHeaderKey,
  PageSectionRegistryKey,
} from '@newlifex/cube-vue/core/composables/useSections';
import DefaultListPageHeader from '@newlifex/cube-vue/core/views/components/ListPageHeader.vue';
import ListChartButton from '@newlifex/cube-vue/core/views/components/ListChartButton.vue';
import CubeTable from '@newlifex/cube-vue/core/components/CubeTable/CubeTable.vue';
import { routeToApiPrefix } from '@newlifex/cube-vue/core/utils/url';
import type { Component } from 'vue';

type SectionRegistry = Record<string, Record<string, () => Promise<{ default: unknown }>>>;

interface Props {
  /** 页头标题（默认由菜单名给出） */
  title?: string;
  /** 页头副标题 */
  subtitle?: string;
}

const props = withDefaults(defineProps<Props>(), { title: undefined, subtitle: undefined });

const route = useRoute();
const apiPrefix = routeToApiPrefix(route.path);

/**
 * 页头组件：本路由的 ListPageHeader 覆盖 > 全局注入 > 框架默认。
 * 只在 setup 取一次：`inject` 不能在 computed 里延迟调用，
 * 而 `defineAsyncComponent` 放进 computed 会每次求值都造新构造器 → 页面反复重挂载。
 */
const registry = inject(PageSectionRegistryKey, {} as SectionRegistry);
const headerLoader = registry[route.path.toLowerCase()]?.ListPageHeader;
const PageHeaderComp: Component = headerLoader
  ? defineAsyncComponent(headerLoader as () => Promise<{ default: Component }>)
  : inject(ListPageHeaderKey, DefaultListPageHeader);
</script>

<template>
  <!-- :key 让换页时重建引擎，避免上一个实体的搜索模型/勾选/分页残留 -->
  <CubeTable :key="route.path">
    <template #header>
      <component :is="PageHeaderComp" :title="props.title" :subtitle="props.subtitle" />
    </template>

    <template #toolbar-extra>
      <ListChartButton :api="apiPrefix" />
    </template>
  </CubeTable>
</template>
