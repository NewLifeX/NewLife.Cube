<script setup lang="ts">
/**
 * CubeTable 搜索区（一体式，对应 MES ListPage 的 `.search-wrap`）
 *
 * 收起态只有一行：关键字输入 + 展开/收起按钮；展开才出现条件面板。
 * 条件由 `GetPage` 元数据生成（业务页也可用 `#advanced` 整块替换成自己的表单）。
 * 状态与动作全部落在引擎上：`ctx.keyword` / `ctx.searchModel` / `ctx.search()` / `ctx.resetSearch()`。
 */
import { computed, ref, useSlots } from 'vue';
import { ArrowDown, ArrowUp, Filter, Search } from '@element-plus/icons-vue';
import LovSelect from '../LovSelect/index.vue';
import { buildSearchControls, showSearchToggle } from './cubeTable.logic';
import type { CubeEngineContext } from '../../engine/types';

const props = withDefaults(
  defineProps<{
    ctx: CubeEngineContext;
    /** 关键字占位文案 */
    placeholder?: string;
  }>(),
  { placeholder: '搜索...' },
);

const slots = useSlots();
const keyword = props.ctx.keyword;
const expanded = ref(false);
const controls = computed(() => buildSearchControls(props.ctx.searchFields.value));
/** 业务自带条件面板时，即便元数据没给搜索字段也要留展开入口 */
const showSearchControls = computed(() => Boolean(slots.advanced) || showSearchToggle(controls.value.length));

function toggle(): void {
  expanded.value = !expanded.value;
}
</script>

<template>
  <div class="cts-wrap" :class="{ expanded }">
    <!-- 整行接管：业务页把「关键词 + 各条件下拉 + 查询/重置」排成一行时用这个 -->
    <slot name="bar" :advanced="expanded" :toggle="toggle" :ctx="ctx">
      <div class="cts-bar">
        <el-input
          v-model="keyword"
          class="cts-input"
          clearable
          :placeholder="placeholder"
          :prefix-icon="Search"
          @keyup.enter="ctx.search()"
          @clear="ctx.search()"
        />
        <button v-if="showSearchControls" type="button" class="cts-toggle" @click="toggle">
          <el-icon :size="15"><Filter /></el-icon>
          <span>{{ expanded ? '收起' : '展开' }}</span>
          <el-icon :size="14">
            <ArrowUp v-if="expanded" />
            <ArrowDown v-else />
          </el-icon>
        </button>
      </div>
    </slot>

    <!-- 高级面板：默认按元数据渲染条件；#advanced 可整块替换 -->
    <div v-if="expanded && showSearchControls" class="cts-panel">
      <slot name="advanced" :ctx="ctx" :controls="controls">
        <div class="cts-grid">
          <div
            v-for="c in controls"
            :key="c.key"
            class="cts-item"
            :class="{ 'cts-item--wide': c.span === 2 }"
          >
            <span class="cts-label">{{ c.field.displayName || c.field.name }}</span>

            <el-input
              v-if="c.kind === 'text'"
              v-model="ctx.searchModel[c.key] as string"
              clearable
              @keyup.enter="ctx.search()"
            />

            <el-select
              v-else-if="c.kind === 'dict'"
              v-model="ctx.searchModel[c.key] as string | number | undefined"
              clearable
              placeholder="全部"
            >
              <el-option v-for="o in c.options" :key="String(o.value)" :label="o.label" :value="o.value" />
            </el-select>

            <div v-else-if="c.kind === 'numberRange'" class="cts-range">
              <el-input-number v-model="ctx.searchModel[c.key] as number" :controls="false" placeholder="最小" />
              <span class="cts-sep">–</span>
              <el-input-number v-model="ctx.searchModel[c.maxKey!] as number" :controls="false" placeholder="最大" />
            </div>

            <div v-else-if="c.kind === 'timeRange'" class="cts-range">
              <el-time-picker v-model="ctx.searchModel[c.key] as string" value-format="HH:mm:ss" placeholder="起" />
              <span class="cts-sep">–</span>
              <el-time-picker v-model="ctx.searchModel[c.maxKey!] as string" value-format="HH:mm:ss" placeholder="止" />
            </div>

            <el-date-picker
              v-else-if="c.kind === 'dateRange'"
              v-model="ctx.searchModel[c.key] as [string, string] | null"
              type="daterange"
              value-format="YYYY-MM-DD"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
            />

            <el-date-picker
              v-else-if="c.kind === 'datetimeRange'"
              v-model="ctx.searchModel[c.key] as [string, string] | null"
              type="datetimerange"
              value-format="YYYY-MM-DDTHH:mm:ss"
              start-placeholder="开始时间"
              end-placeholder="结束时间"
            />

            <LovSelect
              v-else-if="c.kind === 'lov' || c.kind === 'lovMulti'"
              v-model="ctx.searchModel[c.key] as string | number | string[] | undefined"
              :code="c.field.lovCode ?? ''"
              :multiple="c.kind === 'lovMulti'"
            />

            <el-select v-else v-model="ctx.searchModel[c.key] as string" clearable placeholder="全部">
              <el-option v-for="o in c.options" :key="o.value" :label="o.label" :value="o.value" />
            </el-select>
          </div>
        </div>
        <div class="cts-actions">
          <el-button @click="ctx.resetSearch()">重置</el-button>
          <el-button type="primary" @click="ctx.search()">搜索</el-button>
        </div>
      </slot>
    </div>
  </div>
</template>

<style scoped>
/* 一体式卡片：搜索栏与展开面板无缝衔接（收起时只有关键字一行） */
.cts-wrap {
  flex-shrink: 0;
  margin-bottom: 8px;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  overflow: hidden;
}
.cts-bar {
  height: 42px;
  display: flex;
  align-items: center;
  background: var(--el-bg-color-overlay);
}
.cts-input {
  flex: 1;
  min-width: 0;
}
.cts-input :deep(.el-input__wrapper) {
  height: 40px;
  padding-left: 12px;
  box-shadow: none !important;
  border-radius: 0;
}
.cts-input :deep(.el-input__inner) {
  font-size: 13px;
}
.cts-toggle {
  height: 42px;
  min-width: 86px;
  padding: 0 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 5px;
  border: 0;
  border-left: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color-overlay);
  color: var(--el-text-color-regular);
  font-size: 13px;
  line-height: 1;
  cursor: pointer;
  transition: background-color 0.15s ease, color 0.15s ease;
}
.cts-toggle:hover {
  background: var(--el-color-primary-light-9);
  color: var(--el-color-primary);
}
.cts-panel {
  padding: 16px 16px 4px;
  background: var(--el-bg-color-overlay);
  border-top: 1px solid var(--el-border-color-lighter);
}
.cts-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 12px 16px;
}
.cts-item {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 240px;
}
.cts-item--wide {
  min-width: 360px;
}
.cts-label {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  min-width: 3em;
}
.cts-range {
  display: flex;
  align-items: center;
  gap: 4px;
  flex: 1;
}
.cts-sep {
  color: var(--el-text-color-secondary);
}
.cts-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 4px;
  padding-bottom: 12px;
}
</style>
