import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import { useRouter } from 'vue-router';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { getActiveView } from '@/core/utils/viewProfile';
import { PAGE_SIZE_OPTIONS } from '@/core/utils/viewMapping';
import {
  buildOpsPartsWithLinks,
  isOpsLinkKey,
  parseOpsLinkKey,
  type OpsCustomLink,
} from '@/core/utils/opsAction';
import { OPS_LINK_INLINE_MAX } from '@/core/utils/listLinkFields';
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
        canViewDetail: ctx.chrome.value.allowViewDetail,
        canEdit: ctx.flags.value.canEdit,
        canDelete: ctx.flags.value.canDelete && ctx.chrome.value.allowDelete,
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

  /** 高级菜单「批量修改」弹窗状态（OSC-260819e483 P3.5）：BatchUpdateFields 对选中 keys 改单个字段 */
  const batchEditVisible = ref(false);
  const batchEditField = ref('');
  const batchEditValue = ref('');

  /** 批量修改可选字段：EditFormFields ∩ !PK ∩ !只读 ∩ 非布尔（布尔仍走 EnableSelect 徽标） */
  const batchEditFieldOptions = computed(() =>
    ctx.editFields.value
      .filter((f) => !f.primaryKey && !f.readOnly && f.typeName !== 'Boolean')
      .map((f) => ({ label: f.displayName || f.name, value: f.name })),
  );

  function openBatchEdit() {
    batchEditField.value = batchEditFieldOptions.value[0]?.value ?? '';
    batchEditValue.value = '';
    batchEditVisible.value = true;
  }

  async function confirmBatchEdit() {
    const keys = ctx.selectedKeys.value.join(',');
    if (!keys || !batchEditField.value) return;
    try {
      const res = await cubeApi.page.batchUpdateFields(ctx.typePath.value, {
        keys,
        field: batchEditField.value,
        value: batchEditValue.value,
      });
      const { ok = 0, fail = 0 } = res.data ?? {};
      if (fail > 0) {
        Message.warning(`批量修改完成：成功 ${ok} 条，失败 ${fail} 条`);
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

  /** 单元格编辑弹窗（OSC-260819e483 P3.5）：双击可编辑普通字段 → PatchFields 单字段更新，避免整单 PUT 打脏未提交列 */
  const cellEditVisible = ref(false);
  const cellEditRow = ref<Record<string, unknown> | null>(null);
  const cellEditField = ref('');
  const cellEditValue = ref('');

  const cellEditFieldLabel = computed(() => {
    const meta = ctx.editFields.value.find((f) => f.name === cellEditField.value);
    return meta?.displayName || cellEditField.value;
  });

  function onCellEdit(payload: { row: Record<string, unknown>; field: string; value: unknown }) {
    // 仅 EditFormFields 可编辑字段（非 PK/只读/布尔）进入编辑；布尔仍走 EnableSelect 徽标；其余回落详情
    const meta = ctx.editFields.value.find((f) => f.name === payload.field);
    if (!meta || meta.primaryKey || meta.readOnly || meta.typeName === 'Boolean') {
      void nav.openDetail(payload.row);
      return;
    }
    cellEditRow.value = payload.row;
    cellEditField.value = payload.field;
    cellEditValue.value = (payload.value ?? '') as string;
    cellEditVisible.value = true;
  }

  async function confirmCellEdit() {
    const row = cellEditRow.value;
    const field = cellEditField.value;
    if (!row || !field) return;
    const key = ctx.pkField.value;
    const id = row[key] ?? row.id ?? row.ID;
    if (id == null) return;
    try {
      await cubeApi.page.patchFields(ctx.typePath.value, {
        id: String(id),
        values: { [field]: cellEditValue.value },
      });
      Message.success('已保存');
      cellEditVisible.value = false;
      await query.loadData();
    } catch (err) {
      Message.error(formatApiError(err, '保存失败'));
    }
  }

  async function bootstrap() {
    await query.loadFields();
    await views.loadProfile();
    views.syncLocalState();
    views.applyWorkspacePrefs();
    // 初始回填 URL→已保存基准条件到搜索表单（OSC-0012）
    query.applySearchToForm(ctx.baseSearch.value);
    await query.loadData();
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
    batchEditField,
    batchEditValue,
    batchEditFieldOptions,
    openBatchEdit,
    confirmBatchEdit,
    cellEditVisible,
    cellEditField,
    cellEditFieldLabel,
    cellEditValue,
    onCellEdit,
    confirmCellEdit,
    ...auto,
    ...views,
    ...nav,
    PAGE_SIZE_OPTIONS,
    getActiveView,
  };
}

export type DefaultListBindings = ReturnType<typeof useDefaultList>;
