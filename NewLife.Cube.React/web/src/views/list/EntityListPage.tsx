/**
 * 实体列表页（内层组件，无探测/早退逻辑）
 *
 * 由 DefaultListPage 包装组件探测后调用：确认当前页面为实体 CRUD 页后渲染。
 * 本组件与原始 DefaultListPage 行为一致：从后端 GetPage 拉取 list/search/addForm/editForm
 * 字段元数据，由 @cube/page-logic zustand store 驱动，分片渲染：
 *   - SearchBar      动态搜索控件（按钮与条件同行）
 *   - Toolbar        新增/删除选中/刷新 + 表格图表视图切换 + 高级菜单
 *   - TableContent   动态列渲染
 *   - ListPagination 分页 + 统计行
 *   - FormDialog     命令式新增/编辑弹窗（FieldControl）
 *   - ChartView      图表视图（表格/图表 Segmented 切换，规范 §7.6）
 */
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { App, Card } from 'antd';
import { getValueByKey } from '@/utils/url';
import { api } from '@/api';
import { usePageStore } from '@/hooks/usePageStore';
import SearchBar from './components/SearchBar';
import Toolbar, { type ListViewMode } from './components/Toolbar';
import TableContent from './components/TableContent';
import ListPagination from './components/ListPagination';
import ChartView from './components/ChartView';
import FormDialog from '@/views/form/FormDialog';
import DetailDialog from '@/views/form/DetailDialog';

export interface EntityListPageProps {
  /** 实体路径前缀，如 '/Admin/User'、'/Cube/App' */
  type: string;
}

export default function EntityListPage({ type }: EntityListPageProps) {
  const { message, modal } = App.useApp();
  const store = usePageStore(type);

  // 订阅 store 状态
  const listFields = store((s) => s.listFields);
  const searchFields = store((s) => s.searchFields);
  const addFields = store((s) => s.addFields);
  const editFields = store((s) => s.editFields);
  const detailFields = store((s) => s.detailFields);
  const pageSetting = store((s) => s.pageSetting);
  const tableData = store((s) => s.tableData);
  const statData = store((s) => s.statData);
  const pagination = store((s) => s.pagination);
  const loading = store((s) => s.loading);
  const formLoading = store((s) => s.formLoading);
  const pkField = store((s) => s.pkField);
  const canAdd = store((s) => s.canAdd);
  const canEdit = store((s) => s.canEdit);
  const canDelete = store((s) => s.canDelete);
  const canExport = store((s) => s.canExport);
  const canImport = store((s) => s.canImport);

  // 查看权限：只读控制器或无可编辑权限时提供「查看」（规范 §7.9）
  const canView = !!pageSetting?.isReadOnly || !canEdit;

  // 软删除字段：列表字段含 Deleted/IsDelete/IsDeleted 布尔字段时启用「恢复」（规范 §7.9）
  const softDeleteField = useMemo(
    () =>
      listFields.find(
        (f) => /^(deleted|isdelete|isdeleted)$/i.test(f.field.name) && f.field.typeName === 'Boolean',
      )?.field.name,
    [listFields],
  );

  const [searchParams, setSearchParams] = useState<Record<string, unknown>>({});
  const [selectedKeys, setSelectedKeys] = useState<React.Key[]>([]);
  // 当前排序状态：驱动表头排序箭头受控显示（仅排序列显示）
  const [sortState, setSortState] = useState<{ field?: string; desc: boolean }>({ desc: false });
  const [dialog, setDialog] = useState<{ open: boolean; mode: 'add' | 'edit'; row?: Record<string, unknown> | null }>({
    open: false,
    mode: 'add',
  });
  const [detailDialog, setDetailDialog] = useState<{ open: boolean; row?: Record<string, unknown> | null }>({
    open: false,
    row: null,
  });
  const [view, setView] = useState<ListViewMode>('table');
  const [canChart, setCanChart] = useState(false);
  const importInputRef = useRef<HTMLInputElement>(null);

  // 页面加载：拉字段元数据 + 列表数据
  useEffect(() => {
    let cancelled = false;
    store
      .getState()
      .loadFields()
      .then(() => {
        if (!cancelled) return store.getState().loadData();
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [type]);

  // 图表能力探测：加载一次，无图表数据的页面不显示 表格/图表 视图切换（§7.6）
  useEffect(() => {
    let cancelled = false;
    store
      .getState()
      .loadChart()
      .then((list) => {
        if (!cancelled) setCanChart(list.length > 0);
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [type]);

  const refresh = useCallback(() => {
    // 保留当前排序参数，刷新/删除后列表排序与表头箭头保持一致
    return store
      .getState()
      .loadData({ ...searchParams, sort: sortState.field, desc: sortState.desc })
      .catch(() => {});
  }, [store, searchParams, sortState]);

  // ── 事件处理 ─────────────────────────────────────────
  const handleSearch = (params: Record<string, unknown>) => {
    setSearchParams(params);
    // 搜索/重置时清空排序状态，避免表头箭头与后端排序参数不一致
    setSortState({ desc: false });
    store.getState().setPagination(1);
    void store.getState().loadData(params);
  };

  const handleReset = () => {
    setSearchParams({});
    setSortState({ desc: false });
    store.getState().setPagination(1);
    void store.getState().loadData();
  };

  const handleNew = () => setDialog({ open: true, mode: 'add' });

  const handleEditRow = (row: Record<string, unknown>) => setDialog({ open: true, mode: 'edit', row });

  const handleDeleteRow = (row: Record<string, unknown>) => {
    const id = getValueByKey(row, pkField) ?? getValueByKey(row, 'id');
    void store
      .getState()
      .remove(String(id))
      .then(() => {
        message.success('删除成功');
        void refresh();
      })
      .catch((err) => {
        // 业务错误已由 api-core 统一提示，这里兜底
        message.error((err as Error)?.message || '删除失败');
      });
  };

  const handleViewRow = (row: Record<string, unknown>) => setDetailDialog({ open: true, row });

  const handleRestoreRow = (row: Record<string, unknown>) => {
    const id = getValueByKey(row, pkField) ?? getValueByKey(row, 'id');
    void store
      .getState()
      .restore(String(id))
      .then(() => {
        message.success('恢复成功');
        void refresh();
      })
      .catch((err: unknown) => {
        message.error((err as Error)?.message || '恢复失败');
      });
  };

  const handleDeleteSelected = () => {
    const keys = selectedKeys.map(String);
    if (!keys.length) return;
    void store
      .getState()
      .deleteSelect(keys)
      .then(() => {
        message.success('删除成功');
        setSelectedKeys([]);
        void refresh();
      })
      .catch((err) => {
        message.error((err as Error)?.message || '删除失败');
      });
  };

  /** 导出：axios 携带 token 获取文件流后触发浏览器下载（对齐 Vue exportData，window.open 无法携带 Bearer 头） */
  const handleExport = async (format: string) => {
    try {
      const res = await api.client.get(`${type}/ExportFile`, {
        params: { format },
        responseType: 'blob',
      });
      const blob = res.data as Blob;
      const name = type.split('/').filter(Boolean).pop() || 'export';
      downloadBlob(blob, `${name}_${dateStamp()}${blobExt(blob.type)}`);
      message.success('导出成功');
    } catch (err) {
      message.error((err as Error)?.message || '导出失败');
    }
  };

  const handleImport = () => importInputRef.current?.click();

  const onImportFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file) return;
    void store
      .getState()
      .importFile(file)
      .then(() => {
        message.success('导入成功');
        void refresh();
      });
  };

  /** 切换表格/图表视图：切到图表时重新拉取图表数据 */
  const handleViewChange = (v: ListViewMode) => {
    setView(v);
    if (v === 'chart') {
      void store.getState().loadChart().then((list) => {
        if (list.length) setCanChart(true);
        else setCanChart(false);
      });
    }
  };

  /** 删除当前查询条件下的全部数据（高级菜单，二次确认） */
  const handleDeleteAll = () => {
    modal.confirm({
      title: '删除全部',
      content: '确定删除当前查询条件下的所有数据吗？此操作不可恢复！',
      okText: '删除',
      okButtonProps: { danger: true },
      cancelText: '取消',
      onOk: async () => {
        try {
          await store.getState().deleteAll(searchParams);
          message.success('删除成功');
          setSelectedKeys([]);
          void refresh();
        } catch (err) {
          message.error((err as Error)?.message || '删除失败');
        }
      },
    });
  };

  const handlePageChange = (page: number, pageSize?: number) => {
    store.getState().setPagination(page, pageSize);
    // 翻页保留当前排序参数，列表数据与表头箭头保持一致
    void store.getState().loadData({ ...searchParams, sort: sortState.field, desc: sortState.desc });
  };

  const handleSortChange = (sort?: string, desc?: boolean) => {
    setSortState({ field: sort, desc: !!desc });
    void store.getState().loadData({ ...searchParams, sort, desc });
  };

  const handleFormSubmit = async (data: Record<string, unknown>) => {
    try {
      if (dialog.mode === 'edit') {
        await store.getState().update(data);
      } else {
        await store.getState().add(data);
      }
      message.success(dialog.mode === 'edit' ? '更新成功' : '新增成功');
      setDialog({ open: false, mode: 'add', row: null });
      void refresh();
    } catch (err) {
      message.error((err as Error)?.message || '保存失败');
    }
  };

  const editFieldsForDialog = editFields.length ? editFields : addFields;
  const formFields = dialog.mode === 'edit' ? editFieldsForDialog : addFields;

  return (
    // 页面名由顶栏面包屑/多标签承担，Card 不再重复标题（避免搜索区上方冗余占位）
    <Card className="cube-entity-card" size="small" styles={{ body: { paddingTop: 12 } }}>
      <SearchBar fields={searchFields} onSearch={handleSearch} onReset={handleReset} />
      <Toolbar
        canAdd={canAdd}
        canDelete={canDelete}
        canExport={canExport}
        canImport={canImport}
        canChart={canChart}
        view={view}
        selectedCount={selectedKeys.length}
        onNew={handleNew}
        onDelete={handleDeleteSelected}
        onDeleteAll={handleDeleteAll}
        onExport={handleExport}
        onImport={handleImport}
        onViewChange={handleViewChange}
        onRefresh={() => void refresh()}
      />
      {view === 'chart' ? (
        <ChartView charts={store((s) => s.chartList)} />
      ) : (
        <>
          <TableContent
            fields={listFields}
            data={tableData}
            loading={loading}
            pkField={pkField}
            canView={canView}
            canEdit={canEdit}
            canDelete={canDelete}
            softDeleteField={softDeleteField}
            selectedKeys={selectedKeys}
            onSelectChange={setSelectedKeys}
            onView={handleViewRow}
            onEdit={handleEditRow}
            onDelete={handleDeleteRow}
            onRestore={handleRestoreRow}
            onSortChange={handleSortChange}
            sortField={sortState.field}
            sortDesc={sortState.desc}
          />
          <div className="cube-table-footer">
            <ListPagination
              total={pagination.totalCount}
              current={pagination.pageIndex}
              pageSize={pagination.pageSize}
              statData={statData}
              onChange={handlePageChange}
            />
          </div>
        </>
      )}

      {/* 导入：隐藏文件选择框 */}
      <input ref={importInputRef} type="file" accept=".xls,.xlsx,.csv,.json,.zip" style={{ display: 'none' }} onChange={onImportFileChange} />

      {/* 新增/编辑弹窗 */}
      <FormDialog
        open={dialog.open}
        title={dialog.mode === 'edit' ? '编辑' : '新增'}
        mode={dialog.mode}
        fields={formFields}
        row={dialog.row}
        apiPrefix={type}
        submitting={formLoading}
        onSubmit={handleFormSubmit}
        onCancel={() => setDialog({ open: false, mode: 'add', row: null })}
      />

      {/* 详情弹窗（操作列「查看」） */}
      <DetailDialog
        open={detailDialog.open}
        apiPrefix={type}
        id={
          detailDialog.row
            ? ((getValueByKey(detailDialog.row, pkField) ?? getValueByKey(detailDialog.row, 'id')) as string | number | null)
            : null
        }
        fields={detailFields.length ? detailFields : listFields}
        row={detailDialog.row}
        onClose={() => setDetailDialog({ open: false, row: null })}
      />
    </Card>
  );
}

// ── 导出辅助（对齐 Vue core/views/index.vue）──────────────────────

/** 触发浏览器下载 Blob */
function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

/** 根据响应 content-type 推断导出文件扩展名 */
function blobExt(contentType: string): string {
  if (contentType.includes('csv')) return '.csv';
  if (contentType.includes('json')) return '.json';
  if (contentType.includes('xml')) return '.xml';
  if (contentType.includes('excel')) return '.xlsx';
  return '.bin';
}

/** 生成时间戳文件名片段（yyyyMMddHHmmss） */
function dateStamp(): string {
  const d = new Date();
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}${p(d.getMonth() + 1)}${p(d.getDate())}${p(d.getHours())}${p(d.getMinutes())}${p(d.getSeconds())}`;
}
