<script setup lang="ts">
/**
 * CubeTable 搜索区（内置默认实现）
 *
 * 受控于引擎：条件写进 `ctx.searchModel` / `ctx.keyword`，动作直接调 `ctx.search()` /
 * `ctx.resetSearch()`，组件自身不持业务状态；`expanded` 是纯 UI 状态留在本地。
 * 控件类型判定来自 `core/utils/fieldControl.ts`（唯一真理源），本组件只负责渲染分支。
 */
import { computed, ref } from 'vue';
import { ArrowDown, ArrowUp, Search } from '@element-plus/icons-vue';
import LovSelect from '../LovSelect/index.vue';
import { buildSearchControls, collapseSearchControls } from './cubeTable.logic';
import type { CubeEngineContext } from '../../engine/types';

const props = defineProps<{
  /** 引擎上下文（受控 prop，不 inject，便于单测直接传入） */
  ctx: CubeEngineContext;
}>();

// ctx 里的 Ref 直接取出来绑：写入落到同一个 Ref，语义不变。
// （不用 toRefs(props.ctx)：引擎上下文是普通对象而非 reactive，会告警）
const keyword = props.ctx.keyword;
const expanded = ref(false);
const controls = computed(() => buildSearchControls(props.ctx.searchFields.value));
const shown = computed(() => collapseSearchControls(controls.value, expanded.value));
const searchable = computed(() => controls.value.length > 0);

function toggle(): void {
  expanded.value = !expanded.value;
}
</script>

<template>
  <div class="cts flex flex-wrap items-center gap-2 py-2">
    <el-input
      v-model="keyword"
      class="cts-keyword"
      :prefix-icon="Search"
      clearable
      placeholder="搜索..."
      @keyup.enter="ctx.search()"
      @clear="ctx.search()"
    />

    <template v-if="searchable">
      <div
        v-for="c in shown.shown"
        :key="c.key"
        class="cts-item"
        :class="c.span === 2 ? 'cts-item--wide' : ''"
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
          <span class="cts-range-sep">–</span>
          <el-input-number v-model="ctx.searchModel[c.maxKey!] as number" :controls="false" placeholder="最大" />
        </div>

        <div v-else-if="c.kind === 'timeRange'" class="cts-range">
          <el-time-picker v-model="ctx.searchModel[c.key] as string" value-format="HH:mm:ss" placeholder="起" />
          <span class="cts-range-sep">–</span>
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

      <button
        v-if="shown.hiddenCount > 0 || expanded"
        type="button"
        class="cts-toggle"
        @click="toggle"
      >
        <span>{{ expanded ? '收起' : `更多(${shown.hiddenCount})` }}</span>
        <el-icon><ArrowUp v-if="expanded" /><ArrowDown v-else /></el-icon>
      </button>
    </template>

    <div class="cts-actions flex items-center gap-2">
      <el-button type="primary" @click="ctx.search()">查询</el-button>
      <el-button @click="ctx.resetSearch()">重置</el-button>
    </div>
  </div>
</template>

<style scoped>
.cts {
  row-gap: 8px;
}
.cts-keyword {
  width: 220px;
  flex-shrink: 0;
}
.cts-item {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 220px;
}
.cts-item--wide {
  min-width: 320px;
}
.cts-label {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
}
.cts-range {
  display: flex;
  align-items: center;
  gap: 4px;
  flex: 1;
}
.cts-range-sep {
  color: var(--el-text-color-secondary);
}
.cts-toggle {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  border: 0;
  background: transparent;
  color: var(--el-color-primary);
  font-size: 12px;
  cursor: pointer;
}
.cts-actions {
  margin-left: auto;
}
</style>
