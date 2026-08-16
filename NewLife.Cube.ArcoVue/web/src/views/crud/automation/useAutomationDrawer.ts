import { computed, ref, watch } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import type { EntityAutomationListItem } from '@cube/api-core';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { formatDateTime } from '@/core/utils/datetime';
import { parseAutomationGraph } from '@/core/utils/automationGraph';
import { TRIGGER_OPTIONS } from './useAutomationEditor';

interface AutomationDrawerProps {
  visible: boolean;
  typePath: string;
}

interface AutomationDrawerEmits {
  'update:visible': [boolean];
}

type Emit = <K extends keyof AutomationDrawerEmits>(event: K, ...args: AutomationDrawerEmits[K]) => void;

/** 触发类型 → IconPark */
export const TRIGGER_ICON: Record<string, string> = {
  insert: 'file-addition',
  update: 'edit',
  delete: 'delete',
  insertOrUpdateIf: 'switch',
  fieldChange: 'edit-two',
  dateArrive: 'calendar',
  schedule: 'time',
  button: 'click',
  webhook: 'link',
};

/** 动作类型 → IconPark（卡片右侧） */
export const ACTION_ICON: Record<string, string> = {
  notify: 'message',
  updateRecord: 'edit',
  createRecord: 'file-addition',
  findRecords: 'search',
  httpRequest: 'network-tree',
  delay: 'time',
  runAutomation: 'lightning',
  addComment: 'comments',
  aiText: 'robot-one',
};

export function useAutomationDrawer(props: AutomationDrawerProps, emit: Emit) {
  const list = ref<EntityAutomationListItem[]>([]);
  const loading = ref(false);
  const editorId = ref<number | 'new' | null>(null);

  const width = computed(() => {
    if (typeof window === 'undefined') return 960;
    if (window.innerWidth < 768) return '100%';
    // 中心与编辑共用同一宽度，避免切换时抽屉跳变
    return Math.min(960, window.innerWidth - 48);
  });
  const editing = computed(() => editorId.value != null);
  const title = computed(() => (editing.value ? '自动化流程' : '自动化中心'));
  const editorRef = ref<{
    saveOnly: () => void | Promise<void>;
    saveAndEnable: () => void | Promise<void>;
  } | null>(null);
  const editSaving = ref(false);
  const editSummary = ref('');

  function onEditorFooter(payload: { saving: boolean; summary: string }) {
    editSaving.value = payload.saving;
    editSummary.value = payload.summary;
  }

  async function footerSaveOnly() {
    await editorRef.value?.saveOnly();
  }
  async function footerSaveAndEnable() {
    await editorRef.value?.saveAndEnable();
  }

  async function loadList() {
    if (!props.typePath) {
      list.value = [];
      return;
    }
    loading.value = true;
    try {
      const res = await cubeApi.automation.list({ typePath: props.typePath.replace(/^\/+/, '') });
      list.value = res.data ?? [];
    } catch (err) {
      list.value = [];
      Message.error(formatApiError(err, '加载自动化失败'));
    } finally {
      loading.value = false;
    }
  }

  watch(
    () => [props.visible, props.typePath] as const,
    ([vis]) => {
      if (!vis) {
        editorId.value = null;
        return;
      }
      editorId.value = null;
      void loadList();
    },
  );

  function onVisible(v: boolean) {
    emit('update:visible', v);
  }

  function createNew() {
    editorId.value = 'new';
  }

  function openEdit(row: EntityAutomationListItem) {
    editorId.value = row.id;
  }

  function backToList() {
    editorId.value = null;
    editSaving.value = false;
    editSummary.value = '';
    void loadList();
  }

  function triggerLabelOf(kind: string) {
    return TRIGGER_OPTIONS.find((x) => x.value === kind)?.label || kind;
  }

  function cardTitle(row: EntityAutomationListItem) {
    return `${triggerLabelOf(row.triggerKind)} → 自动化`;
  }

  function cardDesc(row: EntityAutomationListItem) {
    const on = row.enable ? '已启用' : '未启用';
    return row.name ? `${row.name} · ${on}` : on;
  }

  function lastRunText(row: EntityAutomationListItem) {
    if (!row.lastRunTime) return '尚未执行';
    return `上次执行 ${formatDateTime(row.lastRunTime)}`;
  }

  async function onToggleEnable(row: EntityAutomationListItem, enable: boolean | string | number) {
    const on = enable === true || enable === 'true' || enable === 1;
    try {
      const detail = await cubeApi.automation.get(row.id);
      const d = detail.data;
      if (!d) return;
      let triggerConfig: Record<string, unknown> = {};
      try {
        triggerConfig = JSON.parse(d.triggerConfig || '{}') as Record<string, unknown>;
      } catch {
        triggerConfig = {};
      }
      let filter: { logic: string; conditions: { field: string; op: string; value?: unknown }[] } = {
        logic: 'all',
        conditions: [],
      };
      let actions: { type: string; data?: Record<string, unknown> }[] = [];
      try {
        const parsed = parseAutomationGraph(JSON.parse(d.graphJson || '{}'));
        filter = parsed.filter;
        actions = parsed.actions;
      } catch {
        /* keep empty */
      }
      await cubeApi.automation.update({
        id: d.id,
        typePath: d.typePath,
        name: d.name,
        enable: on,
        priority: d.priority,
        triggerKind: d.triggerKind,
        triggerConfig,
        version: d.version,
        filter,
        actions,
      });
      row.enable = on;
    } catch (err) {
      Message.error(formatApiError(err, '保存失败'));
      await loadList();
    }
  }

  function removeRow(row: EntityAutomationListItem) {
    Modal.confirm({
      title: '删除自动化？',
      content: `将删除「${row.name}」`,
      onOk: async () => {
        try {
          await cubeApi.automation.remove(row.id);
          Message.success('已删除');
          await loadList();
        } catch (err) {
          Message.error(formatApiError(err, '删除失败'));
        }
      },
    });
  }

  return {
    list,
    loading,
    editorId,
    editing,
    width,
    title,
    editorRef,
    editSaving,
    editSummary,
    onEditorFooter,
    footerSaveOnly,
    footerSaveAndEnable,
    loadList,
    onVisible,
    createNew,
    openEdit,
    backToList,
    onToggleEnable,
    removeRow,
    triggerLabelOf,
    cardTitle,
    cardDesc,
    lastRunText,
    TRIGGER_ICON,
    ACTION_ICON,
  };
}
