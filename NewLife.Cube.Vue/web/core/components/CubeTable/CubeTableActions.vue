<script setup lang="ts">
/**
 * CubeTable 操作按钮行（列表页第一行，对应 MES ListPage 的 `.page-actions`）
 *
 * 排布与 MES 一致：业务按钮在最左（默认插槽），内置通用按钮（添加 / 下载模板 / 导入 / 导出 /
 * 批量删除）排在其后，右侧留给业务动作（`#extra`）。
 * 一行都没有内容时整行不渲染，不留一条空白带。
 */
import { computed, ref, useSlots, type Component } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import * as ElementPlusIconsVue from '@element-plus/icons-vue';
import { Document, Download, Delete, Plus, Upload } from '@element-plus/icons-vue';
import request from '../../utils/request';
import { buildToolbarPayload, parseToolbarResult, resolveToolbarFlags, toolbarKeyField, toolbarQueryString } from './cubeTable.logic';
import { getToolbarRenderer, toolbarDisabled } from './toolbarRenderers';
import { usePagePermissions } from '../../composables/usePagePermissions';
import type { BackendToolbarItem, CubeEngineContext } from '../../engine/types';
import type { CubeTableActions, CubeToolbarFlags } from './types';

const props = withDefaults(
  defineProps<{
    ctx: CubeEngineContext;
    actions: CubeTableActions;
    /** 显式覆盖按钮开关；不传跟随后端 pageSetting */
    flags?: CubeToolbarFlags;
  }>(),
  { flags: () => ({}) },
);

const slots = useSlots();
const { t } = useI18n();
const permissions = usePagePermissions(props.ctx);
/** 隐藏的导入文件框（模板里 ref 会自动解包成元素本体传给 openFilePicker） */
const fileInputRef = ref<HTMLInputElement | null>(null);
const flags = computed(() =>
  resolveToolbarFlags(
    {
      add: props.flags.add,
      batchDelete: props.flags.batchDelete,
      importFile: props.flags.importFile,
      exportFile: props.flags.exportFile,
      downloadTemplate: props.flags.downloadTemplate,
    },
    props.ctx.pageSetting.value,
    {
      add: permissions.canAdd.value,
      delete: permissions.canDelete.value,
      export: permissions.canExport.value,
    },
  ),
);
const selectedCount = computed(() => props.ctx.selected.value.length);
const busy = computed(() => props.ctx.submitting.value);
/** 正在执行的工具栏按钮名，防止重复提交 */
const pending = ref<string | null>(null);

type ElButtonType = 'default' | 'primary' | 'success' | 'warning' | 'info' | 'danger';

/** 后端声明的自定义按钮；custom 类型只保留已注册渲染器的（未注册时告警一次并跳过） */
const toolbarViews = computed(() =>
  (props.ctx.toolbar?.value ?? [])
    .map((item) => {
      const renderer = item.kind === 'custom' ? getToolbarRenderer(item.renderer) : undefined;
      return {
        item,
        component: renderer?.component,
        rendererProps: renderer?.props ?? {},
        buttonType: (item.type || 'default') as ElButtonType,
      };
    })
    .filter((e) => e.item.kind !== 'custom' || e.component),
);
const keyField = computed(() => toolbarKeyField(props.ctx.listFields.value));

/** 后端图标名为 Element Plus 图标组件的 PascalCase 名称（如 Upload / Refresh） */
const iconOf = (name?: string): Component | undefined =>
  name ? (ElementPlusIconsVue as unknown as Record<string, Component>)[name] : undefined;

const uploadItem = ref<BackendToolbarItem | null>(null);
const uploadRef = ref<HTMLInputElement | null>(null);
/** 只有存在 upload 按钮时才渲染隐藏的文件框，避免和内置导入的文件框抢选择器 */
const hasUploadItem = computed(() => toolbarViews.value.some((e) => e.item.kind === 'upload'));

/** 整行是否有任何内容 */
const visible = computed(
  () =>
    Boolean(slots.default || slots.extra) ||
    toolbarViews.value.length > 0 ||
    (!flags.value.readOnly && (flags.value.add || flags.value.batchDelete || flags.value.importFile || flags.value.downloadTemplate)) ||
    flags.value.exportFile,
);

/** 当前查询条件（搜索模型），用于 link 的 appendQuery 与 action 的 include */
function currentQuery(): Record<string, unknown> {
  return { ...(props.ctx.searchModel ?? {}) };
}

async function onToolbar(item: BackendToolbarItem): Promise<void> {
  if (pending.value) return;
  if (item.kind === 'link') {
    let url = item.url ?? '';
    if (!url) return;
    if (item.appendQuery) {
      const qs = toolbarQueryString(props.ctx.keyword.value, currentQuery());
      if (qs) url += (url.includes('?') ? '&' : '?') + qs;
    }
    window.open(url, item.target || '_self');
    return;
  }
  if (item.kind === 'upload') {
    uploadItem.value = item;
    uploadRef.value?.click();
    return;
  }
  if (item.kind === 'custom') return;

  await sendToolbarRequest(item);
}

async function onToolbarFile(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  const item = uploadItem.value;
  input.value = '';
  uploadItem.value = null;
  if (!file || !item) return;

  await sendToolbarRequest(item, file);
}

/** 发送工具栏请求。返回体沿用 MVC 的 onActionComplete 约定：message / data.refresh / data.url / data.time */
async function sendToolbarRequest(item: BackendToolbarItem, file?: File): Promise<void> {
  const url = item.url || (item.action ? `${props.ctx.apiPrefix.value}/${item.action}` : '');
  if (!url) return;

  if (item.confirm) {
    try {
      await ElMessageBox.confirm(item.confirm, t('button.confirm'), { type: 'warning' });
    } catch {
      return;
    }
  }

  const payload = buildToolbarPayload({
    include: item.include,
    selected: props.ctx.selected.value,
    keyField: keyField.value,
    query: currentQuery(),
  });

  const method = (item.method ?? (file ? 'POST' : 'GET')).toUpperCase();
  pending.value = item.name;
  try {
    let data: unknown;
    let headers: Record<string, string> | undefined;
    if (file) {
      const form = new FormData();
      form.append('file', file);
      for (const [k, v] of Object.entries(payload)) form.append(k, String(v));
      data = form;
      headers = { 'Content-Type': 'multipart/form-data' };
    } else if (method !== 'GET') {
      // 与 MVC data-action 的表单提交一致：简单参数走 form-urlencoded，后端无需 [FromBody]
      const form = new URLSearchParams();
      for (const [k, v] of Object.entries(payload)) form.append(k, String(v));
      data = form;
      headers = { 'Content-Type': 'application/x-www-form-urlencoded' };
    }
    const res = (await request({
      url,
      method,
      params: method === 'GET' ? payload : undefined,
      data: method === 'GET' ? undefined : data,
      headers,
    })) as { message?: string; data?: Record<string, unknown> } | undefined;

    applyToolbarResult(res);
  } catch (err) {
    // 请求层已提示业务错误，这里兜底避免未捕获 rejection
    console.error('[CubeTable] toolbar action failed:', err);
  } finally {
    pending.value = null;
  }
}

function applyToolbarResult(res?: { message?: string; data?: Record<string, unknown> }): void {
  const result = parseToolbarResult(res);
  if (result.message) ElMessage.success(result.message);

  if (result.refresh) {
    if (result.delay > 0) window.setTimeout(() => props.ctx.refresh(), result.delay);
    else props.ctx.refresh();
    return;
  }
  if (result.url) {
    const url = result.url;
    window.setTimeout(() => {
      window.location.href = url;
    }, result.delay);
  }
}
</script>

<template>
  <div v-if="visible" class="cta">
    <div class="cta-left">
      <!-- 业务按钮先行，内置通用按钮排在其后（与 MES 一致） -->
      <slot :ctx="ctx" />

      <!-- 后端声明的自定义按钮（meta.toolbar），对应 MVC 的 _List_Toolbar_Custom / _Batch -->
      <template v-for="view in toolbarViews" :key="view.item.name">
        <component
          :is="view.component"
          v-if="view.component"
          v-bind="view.rendererProps"
          :ctx="ctx"
          :item="view.item"
        />
        <el-button
          v-else
          :type="view.buttonType"
          :icon="iconOf(view.item.icon)"
          :disabled="toolbarDisabled(view.item, selectedCount) || pending === view.item.name"
          :loading="pending === view.item.name || (busy && view.item.kind !== 'link')"
          @click="onToolbar(view.item)"
        >
          {{ view.item.displayName || view.item.name }}
        </el-button>
      </template>
      <input
        v-if="hasUploadItem"
        ref="uploadRef"
        class="cta-file"
        type="file"
        data-role="toolbar-upload"
        :accept="uploadItem?.accept || ''"
        @change="onToolbarFile"
      />

      <el-button v-if="flags.add && !flags.readOnly" type="primary" :icon="Plus" @click="ctx.openCreate()">
        {{ t('button.add') }}
      </el-button>

      <template v-if="!flags.readOnly">
        <input
          ref="fileInputRef"
          class="cta-file"
          type="file"
          accept=".xlsx,.xls,.csv,.json,.zip"
          @change="actions.onImportFileChange"
        />
        <el-button
          v-if="flags.downloadTemplate"
          :icon="Document"
          @click="ctx.downloadTemplate()"
        >
          {{ t('grid.downloadTemplate') }}
        </el-button>
        <el-button v-if="flags.importFile" :icon="Upload" :loading="busy" @click="actions.openFilePicker(fileInputRef)">
          {{ t('grid.import') }}
        </el-button>
      </template>

      <el-button v-if="flags.exportFile" :icon="Download" @click="actions.exportNow()">{{ t('grid.export') }}</el-button>

      <el-button
        v-if="flags.batchDelete && !flags.readOnly"
        type="danger"
        plain
        :icon="Delete"
        :disabled="selectedCount === 0"
        @click="actions.removeSelectedRows()"
      >
        {{ selectedCount > 0 ? t('grid.batchDeleteCount', { count: selectedCount }) : t('grid.batchDelete') }}
      </el-button>
    </div>

    <div class="cta-right">
      <slot name="extra" :ctx="ctx" />
    </div>
  </div>
</template>

<style scoped>
.cta {
  flex-shrink: 0;
  margin-bottom: 8px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  min-height: 40px;
  padding: 8px 14px;
  box-sizing: border-box;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
}
.cta-left {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  min-width: 0;
}
.cta-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}
/* 按钮间距统一由 flex gap 提供：取消 el-button 默认 margin-left，避免双倍间距 */
.cta :deep(.el-button + .el-button) {
  margin-left: 0;
}
.cta-file {
  display: none;
}
</style>
