<script setup lang="ts">
/**
 * 合并后的表格工具栏
 *
 * 一行承载三块：左侧操作（新增/业务/批量条）、中间指标、右侧视图控制
 * （业务追加控件、高级菜单、图表、设置列、刷新）。
 * 取代原先「操作栏 + 指标行」两层横条，表格因此上移一层。
 */
import { computed, useSlots } from 'vue';
import { useI18n } from 'vue-i18n';
import { Refresh } from '@element-plus/icons-vue';
import CubeTableActions from './CubeTableActions.vue';
import CubeTableAdvancedMenu from './CubeTableAdvancedMenu.vue';
import CubeTableMetricList from './CubeTableMetricList.vue';
import { resolveToolbarFlags } from './cubeTable.logic';
import { usePagePermissions } from '../../composables/usePagePermissions';
import type { CubeEngineContext } from '../../engine/types';
import type { CubeTableActions as CubeTableActionsApi, CubeTableMetric, CubeToolbarFlags } from './types';

const props = withDefaults(
  defineProps<{
    ctx: CubeEngineContext;
    actions: CubeTableActionsApi;
    /** 宿主显式覆盖的按钮开关；不传跟随后端 pageSetting */
    flags?: CubeToolbarFlags;
    /** 统计指标（中间区域） */
    metrics?: CubeTableMetric[];
    /** 是否显示刷新按钮 */
    showRefresh?: boolean;
    /** 是否显示「设置列」入口（内容由宿主经 `#column-setting` 提供） */
    showColumnSetting?: boolean;
  }>(),
  {
    flags: () => ({}),
    metrics: () => [],
    showRefresh: true,
    showColumnSetting: false,
  },
);

const slots = useSlots();
const { t } = useI18n();
const permissions = usePagePermissions(props.ctx);

const resolvedFlags = computed(() =>
  resolveToolbarFlags(
    {
      add: props.flags.add,
      batchDelete: props.flags.batchDelete,
      importFile: props.flags.importFile,
      exportFile: props.flags.exportFile,
      refresh: props.flags.refresh,
      advanced: props.flags.advanced,
    },
    props.ctx.pageSetting.value,
    {
      add: permissions.canAdd.value,
      delete: permissions.canDelete.value,
      export: permissions.canExport.value,
    },
  ),
);

const hasMetrics = computed(() => Boolean(slots.metrics) || (props.metrics?.length ?? 0) > 0);
const visible = computed(
  () =>
    hasMetrics.value ||
    props.showRefresh ||
    props.showColumnSetting ||
    Boolean(
      slots.actions ||
        slots['actions-extra'] ||
        slots['toolbar-extra'] ||
        slots['column-setting'],
    ) ||
    resolvedFlags.value.add ||
    resolvedFlags.value.advanced ||
    props.ctx.selected.value.length > 0,
);
</script>

<template>
  <div v-if="visible" class="ctt">
    <div class="ctt-actions">
      <CubeTableActions :ctx="ctx" :actions="actions" :flags="resolvedFlags" inline>
        <template v-if="slots.actions" #default="scope">
          <slot name="actions" v-bind="scope" />
        </template>
      </CubeTableActions>
    </div>

    <CubeTableMetricList v-if="hasMetrics" :ctx="ctx" :metrics="metrics">
      <template v-if="slots.metrics" #metrics="scope">
        <slot name="metrics" v-bind="scope" />
      </template>
    </CubeTableMetricList>

    <div class="ctt-controls">
      <slot name="actions-extra" :ctx="ctx" />
      <CubeTableAdvancedMenu
        v-if="resolvedFlags.advanced"
        :ctx="ctx"
        :actions="actions"
        :flags="resolvedFlags"
      />
      <slot name="toolbar-extra" :ctx="ctx" />
      <slot name="column-setting" />
      <el-button
        v-if="showRefresh"
        :icon="Refresh"
        circle
        :title="t('grid.refresh')"
        @click="ctx.refresh()"
      />
    </div>
  </div>
</template>

<style scoped>
.ctt {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
  min-height: 56px;
  padding: 8px 20px;
  box-sizing: border-box;
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color-overlay);
}
.ctt-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  min-width: 0;
}
.ctt-controls {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-left: auto;
  flex-shrink: 0;
}
</style>
