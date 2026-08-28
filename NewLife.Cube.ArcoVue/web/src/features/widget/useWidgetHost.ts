import { computed, inject, ref } from 'vue';
import { Message } from '@arco-design/web-vue';
import { emptyDashboard, maxWidgetsFor, type WidgetInstance } from '@cube/api-core';
import { WIDGET_SURFACE_KEY } from './context';
import { getWidget } from './registry';
import { isUnlinkedWidget } from './legacy';
import { useWidgetQuery } from './useWidgetQuery';
import LockedWidget from './LockedWidget.vue';
import UnknownWidget from './UnknownWidget.vue';
import LegacyChartWidget from './LegacyChartWidget.vue';

export function useWidgetHost() {
  const ctx = inject(WIDGET_SURFACE_KEY);
  const dashboard = computed(() => ctx?.dashboard ?? emptyDashboard());
  const maxWidgets = computed(() => maxWidgetsFor(ctx?.surface));
  const widgets = computed(() => {
    let list = [...(dashboard.value.widgets ?? [])].sort((a, b) => a.layout.order - b.layout.order);
    if (ctx?.surface === 'insight') {
      list = list.filter(
        (w) => w.kind !== 'miniKanban' && w.kind !== 'dataList' && w.kind !== 'dataCard',
      );
    }
    return list;
  });
  const hostTypePath = computed(() => ctx?.hostTypePath);
  const hostFilter = computed(() => ctx?.hostFilter ?? null);
  const canEdit = computed(() => !!ctx?.canEdit);
  const isWorkbench = computed(() => ctx?.surface === 'workbench');
  const { states } = useWidgetQuery(widgets, hostTypePath, hostFilter);

  const configVisible = ref(false);
  const editing = ref<WidgetInstance | null>(null);

  const hasWidgets = computed(() => widgets.value.length > 0);

  function resolveComponent(w: WidgetInstance) {
    const st = states.value[w.id];
    if (st?.locked) return LockedWidget;
    if (w.kind === 'legacyChart') return LegacyChartWidget;
    return getWidget(w.kind)?.component ?? UnknownWidget;
  }

  async function renameWidget(id: string, title: string) {
    const next = title.trim().slice(0, 40);
    if (!next) return;
    await persist(
      widgets.value.map((w) => (w.id === id ? { ...w, title: next } : w)),
    );
  }

  function cardProps(w: WidgetInstance) {
    const st = states.value[w.id];
    return {
      widget: w,
      result: st?.result,
      loading: st?.loading,
      error: st?.error,
      locked: st?.locked,
      // 查询未完成时也按跨实体规则显示未联动，避免图标闪没
      unlinked: st?.unlinked ?? isUnlinkedWidget(w, hostTypePath.value),
      canEdit: canEdit.value && w.kind !== 'legacyChart',
      onTitleCommit: (title: string) => {
        void renameWidget(w.id, title);
      },
    };
  }

  function openAdd() {
    if (widgets.value.length >= maxWidgets.value) {
      Message.warning(`部件数量不能超过 ${maxWidgets.value}`);
      return;
    }
    editing.value = null;
    configVisible.value = true;
  }

  function openEdit(w: WidgetInstance) {
    if (w.kind === 'legacyChart') return;
    editing.value = w;
    configVisible.value = true;
  }

  function openUpgrade(w: WidgetInstance) {
    editing.value = {
      ...w,
      kind: 'miniChart',
      title: w.title === '来自旧图表' ? '迷你图表' : w.title,
      style: { ...w.style, chartType: 'bar' },
      query: { ...w.query, groupBy: w.query?.groupBy },
    };
    configVisible.value = true;
  }

  async function persist(next: WidgetInstance[]) {
    if (!ctx) return;
    const filtered =
      ctx.surface === 'insight'
        ? next.filter(
            (w) => w.kind !== 'miniKanban' && w.kind !== 'dataList' && w.kind !== 'dataCard',
          )
        : next;
    const widgetsNext = filtered.map((w, i) => ({ ...w, layout: { ...w.layout, order: i } }));
    await ctx.saveDashboard({
      ...dashboard.value,
      version: 1,
      widgets: widgetsNext,
    });
  }

  async function onConfigSave(inst: WidgetInstance) {
    if (ctx?.surface === 'insight' && inst.kind === 'miniKanban') {
      Message.error('页面仪表盘不支持数据看板');
      return;
    }
    if (ctx?.surface === 'insight' && inst.kind === 'dataList') {
      Message.error('页面仪表盘不支持数据列表');
      return;
    }
    if (ctx?.surface === 'insight' && inst.kind === 'dataCard') {
      Message.error('页面仪表盘不支持数据卡片');
      return;
    }
    const list = [...widgets.value];
    const idx = list.findIndex((w) => w.id === inst.id);
    if (idx >= 0) {
      // 编辑：原地替换，保留 order / 相对位置
      const prev = list[idx];
      list[idx] = {
        ...inst,
        layout: {
          ...inst.layout,
          order: prev.layout.order,
        },
      };
    } else {
      if (list.length >= maxWidgets.value) {
        Message.warning(`部件数量不能超过 ${maxWidgets.value}`);
        return;
      }
      // 新增：追加到末尾
      list.push(inst);
    }
    try {
      await persist(list);
      configVisible.value = false;
    } catch (e) {
      Message.error(e instanceof Error ? e.message : '保存部件失败');
    }
  }

  async function removeWidget(id: string) {
    await persist(widgets.value.filter((w) => w.id !== id));
  }

  async function moveWidget(id: string, dir: -1 | 1) {
    const list = [...widgets.value];
    const i = list.findIndex((w) => w.id === id);
    const j = i + dir;
    if (i < 0 || j < 0 || j >= list.length) return;
    const tmp = list[i];
    list[i] = list[j];
    list[j] = tmp;
    await persist(list);
  }

  return {
    ctx,
    widgets,
    canEdit,
    hasWidgets,
    isWorkbench,
    states,
    resolveComponent,
    cardProps,
    configVisible,
    editing,
    maxWidgets,
    openAdd,
    openEdit,
    openUpgrade,
    onConfigSave,
    removeWidget,
    moveWidget,
  };
}

export type WidgetHostExpose = { openAdd: () => void };
