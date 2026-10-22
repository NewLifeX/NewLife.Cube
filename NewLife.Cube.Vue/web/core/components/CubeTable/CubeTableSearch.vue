<script setup lang="ts">
/**
 * CubeTable 搜索区（一体式，对应 MES ListPage 的 `.search-wrap`）
 *
 * 常驻每行三列的栅格（`.cts-grid`）：第一格关键字框，第三格（第一行行尾）固定放
 * 「展开/收起 + 重置 + 搜索」——展开态也钉在第一行，不随条件流到尾部；
 * 其余每格一个搜索条件，从第二格起自动铺格、满行换行（收起态只渲染第一个条件，
 * 第一行即 关键字 + 首个条件 + 动作钮）；不再单独出条件面板，
 * 只有业务页用 `#advanced` 自带面板时才渲染面板区域。
 * 条件由 `meta` 元数据生成（业务页也可用 `#advanced` 整块替换成自己的表单）。
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
  { placeholder: '搜索关键字' },
);

const slots = useSlots();
const keyword = props.ctx.keyword;
const expanded = ref(false);
const controls = computed(() => buildSearchControls(props.ctx.searchFields.value));
/** 有元数据条件或业务自带面板才给展开入口（收起态露首个条件也依赖它） */
const showSearchControls = computed(() => Boolean(slots.advanced) || showSearchToggle(controls.value.length));

function toggle(): void {
  expanded.value = !expanded.value;
}
</script>

<template>
  <div class="cts-wrap" :class="{ expanded }">
    <!-- 整行接管：业务页把搜索条换成自己的布局时用这个 -->
    <slot name="bar" :advanced="expanded" :toggle="toggle" :ctx="ctx">
      <!-- 常驻三列栅格：收起态第一行 = 关键字 + 第一个条件 + 动作格，展开铺入其余条件 -->
      <div class="cts-grid">
        <el-input
          v-model="keyword"
          class="cts-input cts-cell"
          clearable
          :placeholder="placeholder"
          :prefix-icon="Search"
          @keyup.enter="ctx.search()"
          @clear="ctx.search()"
        />
        <!-- 动作格 DOM 在条件之前，配合显式钉格（第一行第三列）：自动流的条件会跳过它，
             展开后按钮始终在第一行行尾而不是条件最后面 -->
        <div class="cts-cell cts-cell--end">
          <button v-if="showSearchControls" type="button" class="cts-toggle" @click="toggle">
            <el-icon :size="15"><Filter /></el-icon>
            <span>{{ expanded ? '收起' : '展开' }}</span>
            <el-icon :size="14">
              <ArrowUp v-if="expanded" />
              <ArrowDown v-else />
            </el-icon>
          </button>
          <div class="cts-actions">
            <el-button @click="ctx.resetSearch()">重置</el-button>
            <el-button type="primary" @click="ctx.search()">搜索</el-button>
          </div>
        </div>
        <div v-for="c in expanded ? controls : controls.slice(0, 1)" :key="c.key" class="cts-item cts-cell">
          <span class="cts-label">{{ c.field.displayName || c.field.name }}</span>

          <el-input
            v-if="c.kind === 'text'"
            v-model="ctx.searchModel[c.key] as string"
            class="cts-ctl"
            clearable
            @keyup.enter="ctx.search()"
          />

          <el-select
            v-else-if="c.kind === 'dict'"
            v-model="ctx.searchModel[c.key] as string | number | undefined"
            class="cts-ctl"
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
            class="cts-ctl"
            type="daterange"
            value-format="YYYY-MM-DD"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
          />

          <el-date-picker
            v-else-if="c.kind === 'datetimeRange'"
            v-model="ctx.searchModel[c.key] as [string, string] | null"
            class="cts-ctl"
            type="datetimerange"
            value-format="YYYY-MM-DDTHH:mm:ss"
            start-placeholder="开始时间"
            end-placeholder="结束时间"
          />

          <LovSelect
            v-else-if="c.kind === 'lov' || c.kind === 'lovMulti'"
            v-model="ctx.searchModel[c.key] as string | number | string[] | undefined"
            class="cts-ctl"
            :code="c.field.lovCode ?? ''"
            :multiple="c.kind === 'lovMulti'"
          />

          <el-select v-else v-model="ctx.searchModel[c.key] as string" class="cts-ctl" clearable placeholder="全部">
            <el-option v-for="o in c.options" :key="o.value" :label="o.label" :value="o.value" />
          </el-select>
        </div>
      </div>
    </slot>

    <!-- 业务自带条件面板：默认条件已内联到搜索条，面板区域只在给了 #advanced 时存在 -->
    <div v-if="expanded && slots.advanced" class="cts-panel">
      <slot name="advanced" :ctx="ctx" :controls="controls" />
    </div>
  </div>
</template>

<style scoped>
/* 一体式卡片：搜索栏与业务自带面板无缝衔接（收起时只有第一行三格） */
.cts-wrap {
  flex-shrink: 0;
  margin-bottom: 8px;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  overflow: hidden;
}
/* 常驻三列栅格：收起态只铺第一行（关键字+首个条件+动作格），展开逐行铺满三列换行；
   dense 让条件自动补位时跳过动作格钉掉的格；窄屏 ≤900px 降为两列 */
.cts-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  grid-auto-flow: row dense;
  gap: 10px 24px;
  padding: 10px 12px;
  background: var(--el-bg-color-overlay);
}
@media (max-width: 900px) {
  .cts-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
  .cts-cell--end {
    grid-column: 2;
  }
}
.cts-cell {
  min-width: 0;
  min-height: 32px;
}
.cts-input {
  flex: 1;
  min-width: 0;
}
.cts-input :deep(.el-input__wrapper) {
  height: 32px;
  border-radius: 6px;
  box-shadow: 0 0 0 1px var(--el-border-color) inset !important;
}
.cts-input :deep(.el-input__inner) {
  font-size: 13px;
}
/* 展开/收起：胶囊按钮，常驻动作格靠右位 */
.cts-toggle {
  height: 30px;
  padding: 0 12px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 5px;
  flex-shrink: 0;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 15px;
  background: var(--el-bg-color-overlay);
  color: var(--el-text-color-regular);
  font-size: 13px;
  line-height: 1;
  cursor: pointer;
  transition: background-color 0.15s ease, color 0.15s ease, border-color 0.15s ease;
}
.cts-toggle:hover {
  background: var(--el-color-primary-light-9);
  border-color: var(--el-color-primary);
  color: var(--el-color-primary);
}
.cts-panel {
  padding: 16px 16px 4px;
  background: var(--el-bg-color-overlay);
  border-top: 1px solid var(--el-border-color-lighter);
}
/* 条件格：标签固定、控件吃满剩余宽度，同列宽度天然对齐 */
.cts-item {
  display: flex;
  align-items: center;
  gap: 6px;
}
.cts-ctl {
  flex: 1;
  min-width: 0;
  width: 100%;
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
  min-width: 0;
}
.cts-range > * {
  flex: 1;
  min-width: 0;
}
.cts-sep {
  flex: 0 0 auto;
  color: var(--el-text-color-secondary);
}
/* 动作格：显式钉死第一行最右列（三列/两列通吃），展开收起+重置/搜索整体靠右，
   不随条件流到最后一行 */
.cts-cell--end {
  grid-row: 1;
  grid-column: -2 / -1;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
}
.cts-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
