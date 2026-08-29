import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import { useRouter } from 'vue-router';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { getActiveView } from '@/core/utils/viewProfile';
import { PAGE_SIZE_OPTIONS } from '@/core/utils/viewMapping';
import { resolveControl } from '@/core/utils/fieldControl';
import { fetchLovMeta } from '@/core/utils/lov-api';
import type { FieldMeta, FieldOption, ControlType } from '@/core/types/field';
import {
  buildOpsPartsWithLinks,
  isOpsLinkKey,
  parseOpsLinkKey,
  type OpsCustomLink,
} from '@/core/utils/opsAction';
import { OPS_LINK_INLINE_MAX } from '@/core/utils/listLinkFields';
import { encodeQueryB64, mapPageKindToAiPage } from '@/core/utils/aiChatContext';
import { mergeFillFormValues } from '@/core/utils/aiFill';
import { resolveFieldsForKind } from '@/core/utils/fieldParts';
import { parseUrlViewFilter } from '@/core/utils/searchFilters';
import { getValueByKey } from '@/core/utils/url';
import { useAppStore } from '@/stores/app';
import { createListContext } from './listContext';
import { useListQuery } from './useListQuery';
import { useListCrud } from './useListCrud';
import { useListViews } from './useListViews';
import { useRecordNav } from './useRecordNav';
import { useListAutomation } from './useListAutomation';
import { runCellFieldLink, runOpsCustomLink } from './useListOpsLinks';

/**
 * DefaultList 组装器（OSC-260813c3e9）：创建共享上下文，组装四个领域 composable，
 * 保留原 watch / onMounted / onBeforeUnmount / bootstrap 生命周期。
 */
export function useDefaultList(props: { type: string; authId?: number }) {
  const router = useRouter();
  const appStore = useAppStore();
  const ctx = createListContext(props);
  const query = useListQuery(ctx);
  const nav = useRecordNav(ctx);
  const crud = useListCrud(ctx, {
    loadData: query.loadData,
    openEdit: nav.openEdit,
    openDetail: nav.openDetail,
  });
  const views = useListViews(ctx, {
    loadData: query.loadData,
    applySearchToForm: query.applySearchToForm,
  });
  const auto = useListAutomation(ctx);

  /** 操作列「更多」溢出菜单（VTable canvas 外挂） */
  const moreMenu = ref<{
    visible: boolean;
    x: number;
    y: number;
    row: Record<string, unknown> | null;
    links: OpsCustomLink[];
  }>({ visible: false, x: 0, y: 0, row: null, links: [] });

  function closeMoreMenu() {
    moreMenu.value = { visible: false, x: 0, y: 0, row: null, links: [] };
  }

  async function onTableAction(payload: {
    action: string;
    row: Record<string, unknown>;
    clientX?: number;
    clientY?: number;
  }) {
    if (payload.action.startsWith('auto:')) {
      void auto.runAutomationButton(payload);
      return;
    }
    if (payload.action === 'more') {
      const { overflowLinks } = buildOpsPartsWithLinks({
        canViewDetail: true,
        canEdit: ctx.flags.value.canEdit,
        canDelete: ctx.flags.value.canDelete,
        automationButtons: auto.automationButtons.value,
        opsLinks: ctx.opsCustomLinks.value,
        inlineMax: OPS_LINK_INLINE_MAX,
      });
      moreMenu.value = {
        visible: true,
        x: payload.clientX ?? 0,
        y: payload.clientY ?? 0,
        row: payload.row,
        links: overflowLinks,
      };
      return;
    }
    if (isOpsLinkKey(payload.action)) {
      const name = parseOpsLinkKey(payload.action);
      const link = ctx.opsCustomLinks.value.find((l) => l.name === name);
      if (link) {
        await runOpsCustomLink({
          link,
          row: payload.row,
          router,
          onDone: () => query.loadData(),
        });
      }
      return;
    }
    crud.onTableAction(payload);
  }

  async function onCellLink(payload: {
    url: string;
    target?: string;
    row: Record<string, unknown>;
  }) {
    await runCellFieldLink({
      urlTemplate: payload.url,
      target: payload.target,
      row: payload.row,
      router,
    });
  }

  async function onOpsLinkClick(link: OpsCustomLink, row: Record<string, unknown>) {
    closeMoreMenu();
    await runOpsCustomLink({
      link,
      row,
      router,
      onDone: () => query.loadData(),
    });
  }

  /** 高级菜单「批量修改」弹窗状态（OSC-260819e483 P3.5）：BatchUpdateFields 对选中 keys 改字段；支持多行字段（像筛选/填色弹窗一样增删行，一次应用多字段） */
  const batchEditVisible = ref(false);

  /** 批量修改单行：字段 + 值 + 该字段下拉选项（按字段 typeName 自适应值控件） */
  interface BatchEditRow {
    field: string;
    value: string;
    options: FieldOption[];
    optionsLoading: boolean;
  }

  const batchEditRows = ref<BatchEditRow[]>([]);

  function newBatchEditRow(field = ''): BatchEditRow {
    return { field, value: '', options: [], optionsLoading: false };
  }

  /** 批量修改可选字段：EditFormFields ∩ !PK ∩ !只读 ∩ 非布尔（布尔仍走 EnableSelect 徽标） */
  const batchEditFieldOptions = computed(() =>
    ctx.editFields.value
      .filter((f) => !f.primaryKey && !f.readOnly && f.typeName !== 'Boolean')
      .map((f) => ({ label: f.displayName || f.name, value: f.name })),
  );

  /** 行的字段元数据（驱动值控件与选项） */
  function batchRowMeta(row: BatchEditRow): FieldMeta | undefined {
    return ctx.editFields.value.find((f) => f.name === row.field);
  }

  /** 行的值控件类型：按字段 typeName 经 resolveControl 判定（状态/枚举/值集 → 下拉，数值 → 数字框，日期/时间 → 对应选择器） */
  function batchRowControlType(row: BatchEditRow): ControlType {
    const f = batchRowMeta(row);
    return f ? resolveControl(f) : 'input';
  }

  /** 是否为下拉类控件（枚举/值集/字典/lov/级联）；多选字段仍按单选设置（BatchUpdateFields 值为单值） */
  function batchRowIsSelect(row: BatchEditRow): boolean {
    return ['select', 'selectMulti', 'lov', 'lovMulti', 'cascader'].includes(
      batchRowControlType(row),
    );
  }

  /** 按行字段元数据填充下拉选项（GetPage 已物化 dataSource；无则走 lovCode 远程） */
  async function loadBatchRowOptions(row: BatchEditRow) {
    const field = batchRowMeta(row);
    row.optionsLoading = false;
    if (!field) {
      row.options = [];
      return;
    }
    // 静态字典（GetPage/GetFields 已物化）优先：key=存储值、label=显示名
    if (field.dataSource && Object.keys(field.dataSource).length > 0) {
      row.options = Object.entries(field.dataSource).map(([value, label]) => ({ value, label }));
      return;
    }
    if (field.options && field.options.length > 0) {
      row.options = field.options.map((o) => ({ value: String(o.value), label: o.label }));
      return;
    }
    // lovCode 远程值集：Enum.* 或 Lov 定义
    if (field.lovCode) {
      row.optionsLoading = true;
      try {
        const meta = await fetchLovMeta(field.lovCode);
        const item = meta.meta?.find((m) => m.lovCode === field.lovCode);
        const opts =
          item && item.type === 'ENUM'
            ? item.options
            : meta.inlineEnums?.[field.lovCode] ?? [];
        row.options = opts.map((o) => ({ value: String(o.value), label: o.label }));
      } catch {
        row.options = [];
      } finally {
        row.optionsLoading = false;
      }
      return;
    }
    row.options = [];
  }

  /** 行字段切换：重置值并加载对应控件选项 */
  function onBatchRowFieldChange(row: BatchEditRow) {
    row.value = '';
    void loadBatchRowOptions(row);
  }

  function addBatchEditRow() {
    batchEditRows.value.push(newBatchEditRow());
  }

  function removeBatchEditRow(idx: number) {
    batchEditRows.value.splice(idx, 1);
  }

  function openBatchEdit() {
    batchEditRows.value = [newBatchEditRow(batchEditFieldOptions.value[0]?.value ?? '')];
    void loadBatchRowOptions(batchEditRows.value[0]);
    batchEditVisible.value = true;
  }

  async function confirmBatchEdit() {
    const keys = ctx.selectedKeys.value.join(',');
    // 有效行：字段已选且值非空（空值行跳过，避免误设空）
    const fields = batchEditRows.value
      .filter((r) => r.field && r.value !== '')
      .map((r) => ({ field: r.field, value: String(r.value) }));
    if (!keys || !fields.length) return;
    try {
      const res = await cubeApi.page.batchUpdateFields(ctx.typePath.value, { keys, fields });
      const { ok = 0, fail = 0, errors = [] } = res.data ?? {};
      if (fail > 0) {
        // 展示首条失败明细，便于定位（如必填/类型转换失败）
        const first = errors[0];
        Message.warning(
          first
            ? `批量修改：成功 ${ok} 条，失败 ${fail} 条（${first.message}）`
            : `批量修改：成功 ${ok} 条，失败 ${fail} 条`,
        );
      } else {
        Message.success(`已批量修改 ${ok} 条`);
      }
      batchEditVisible.value = false;
      ctx.selectedKeys.value = [];
      await query.loadData();
    } catch (err) {
      Message.error(formatApiError(err, '批量修改失败'));
    }
  }

  async function bootstrap() {
    await query.loadFields();
    await views.loadProfile();
    views.syncLocalState();
    views.applyWorkspacePrefs();
    // 初始回填 URL→已保存基准条件到搜索表单（OSC-0012）
    query.applySearchToForm(ctx.baseSearch.value);
    // 部件下钻：URL viewFilter 覆盖本地筛选
    applyUrlViewFilter();
    await query.loadData();
  }

  function applyUrlViewFilter() {
    const vf = parseUrlViewFilter(ctx.route.query as Record<string, unknown>);
    if (vf?.conditions?.length) ctx.localFilter.value = vf;
  }

  watch(ctx.typePath, () => {
    ctx.pagination.current = 1;
    ctx.selectedKeys.value = [];
    bootstrap();
  });

  // URL 参数变化（同页面路由 query 变更）时重新派生基准条件
  watch(
    () => ctx.route.query,
    () => {
      ctx.searchTouched.value = false;
      query.applySearchToForm(ctx.baseSearch.value);
      applyUrlViewFilter();
      ctx.pagination.current = 1;
      query.loadData();
    },
    { deep: true },
  );

  // 洞察图表开关变化时刷新图表区（不影响列表）
  watch(
    () => ctx.insight.value.showChart,
    () => {
      void query.loadChart();
    },
  );

  // 视图/高度模式切换后重测表格可用高度（分页器与外壳底部保持在首屏内）。
  // 视图组件（VTable/CardList 等）为异步加载，渲染完成后延迟多次重测直至高度收敛。
  watch(
    () => [ctx.viewState.value?.view, ctx.chrome.value.heightMode] as const,
    () => {
      nextTick(ctx.measureTableHeight);
      window.setTimeout(ctx.measureTableHeight, 200);
      window.setTimeout(ctx.measureTableHeight, 600);
    },
  );

  function applyAiFill(values: Record<string, unknown>) {
    if (!ctx.drawerVisible.value || (ctx.drawerMode.value !== 'add' && ctx.drawerMode.value !== 'edit')) {
      Message.info('请先打开添加或编辑');
      return;
    }
    const fields = resolveFieldsForKind(ctx.drawerMode.value, ctx.fieldParts.value);
    const filled = mergeFillFormValues(ctx.formModel, values, fields);
    if (filled.length) {
      Message.success(`AI 已预填 ${filled.length} 个字段（${filled.join('、')}），请检查后保存`);
    }
  }

  function syncAiContext() {
    const drawer = ctx.drawerVisible.value ? ctx.drawerMode.value : null;
    const idRaw = getValueByKey(ctx.formModel, ctx.pkField.value);
    const idNum = Number(idRaw);
    appStore.patchAiContext({
      page: mapPageKindToAiPage('entity', drawer),
      mode: ctx.drawerMode.value === 'edit' ? 'edit' : 'add',
      id: Number.isFinite(idNum) ? idNum : 0,
      typePath: ctx.typePath.value,
      queryB64: encodeQueryB64(ctx.effectiveSearch.value as Record<string, unknown>),
      applyFill: applyAiFill,
    });
  }

  watch(
    () => [
      ctx.drawerVisible.value,
      ctx.drawerMode.value,
      ctx.typePath.value,
      ctx.effectiveSearch.value,
      ctx.pkField.value,
    ],
    syncAiContext,
    { immediate: true, deep: true },
  );

  onMounted(() => {
    bootstrap();
    // Esc 退出全屏
    window.addEventListener('keydown', views.onKeydown);
    // 初始渲染完成后测量表格可用高度，并监听 scroll 容器尺寸变化（窗口/侧栏/布局调整）
    nextTick(() => {
      ctx.measureTableHeight();
      views.observeTableHeight();
    });
  });

  onBeforeUnmount(() => {
    ctx.tableResizeObserver.value?.disconnect();
    window.removeEventListener('keydown', views.onKeydown);
    appStore.clearAiPageContext();
  });

  return {
    ...ctx,
    ...query,
    ...crud,
    onTableAction,
    onCellLink,
    onOpsLinkClick,
    moreMenu,
    closeMoreMenu,
    batchEditVisible,
    batchEditRows,
    batchEditFieldOptions,
    batchRowControlType,
    batchRowIsSelect,
    onBatchRowFieldChange,
    addBatchEditRow,
    removeBatchEditRow,
    openBatchEdit,
    confirmBatchEdit,
    ...auto,
    ...views,
    ...nav,
    PAGE_SIZE_OPTIONS,
    getActiveView,
  };
}

export type DefaultListBindings = ReturnType<typeof useDefaultList>;
