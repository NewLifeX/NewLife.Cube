import { useState, useEffect, useCallback, useRef, type ChangeEvent, type MouseEvent } from 'react';
import { useLocation } from 'react-router-dom';
import {
  Box, Button, Card, CardContent, Checkbox, Chip,
  Dialog, DialogTitle, DialogContent, DialogActions,
  IconButton, Menu, MenuItem as MuiMenuItem, Paper, Stack, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, TablePagination, TextField, Toolbar, Tooltip, Typography,
} from '@mui/material';
import { Add, Delete, Download, Upload, Search, Refresh, Visibility, Edit as EditIcon, ArrowDropDown } from '@mui/icons-material';
import { Auth, type DataField } from '@cube/api-core';
import { resolveWidgets, type FieldMapping } from '@cube/field-mapping';
import * as echarts from 'echarts';
import api from '@/api';
import { useUserStore } from '@/stores/user';
import FieldInput from '@/components/FieldInput';

const exportOptions = [
  { label: '导出 Excel', value: 'Excel' },
  { label: '导出 CSV', value: 'Csv' },
  { label: '导出 JSON', value: 'Json' },
  { label: '导出 XML', value: 'Xml' },
  { label: '导出模板', value: 'ExcelTemplate' },
];

export default function DynamicPage() {
  const location = useLocation();
  const type = location.pathname;
  const getMenuPermission = useUserStore((s) => s.getMenuPermission);
  const perms = getMenuPermission(type);
  const canAdd = String(Auth.ADD) in perms;
  const canEdit = String(Auth.EDIT) in perms;
  const canDelete = String(Auth.DELETE) in perms;
  const canExport = String(Auth.EXPORT) in perms;
  const canImport = String(Auth.IMPORT) in perms;

  const [listFields, setListFields] = useState<FieldMapping[]>([]);
  const [searchFields, setSearchFields] = useState<FieldMapping[]>([]);
  const [addFields, setAddFields] = useState<FieldMapping[]>([]);
  const [editFields, setEditFields] = useState<FieldMapping[]>([]);
  const [detailFields, setDetailFields] = useState<FieldMapping[]>([]);
  const [pkField, setPkField] = useState('id');

  const [data, setData] = useState<Record<string, any>[]>([]);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [total, setTotal] = useState(0);
  const [selected, setSelected] = useState<(string | number)[]>([]);
  const [searchForm, setSearchForm] = useState<Record<string, any>>({});
  const [statData, setStatData] = useState<Record<string, unknown> | null>(null);

  // ECharts
  const [chartList, setChartList] = useState<any[]>([]);
  const chartRefs = useRef<(HTMLDivElement | null)[]>([]);
  const chartInstances = useRef<any[]>([]);

  // 导出下拉菜单
  const [exportAnchor, setExportAnchor] = useState<null | HTMLElement>(null);

  // 弹窗
  const [formOpen, setFormOpen] = useState(false);
  const [isEdit, setIsEdit] = useState(false);
  const [editForm, setEditForm] = useState<Record<string, any>>({});
  const [detailOpen, setDetailOpen] = useState(false);
  const [detailData, setDetailData] = useState<Record<string, any>>({});

  // 加载字段
  useEffect(() => {
    api.page.getPage(type).then((pageRes) => {
      const pageMeta = pageRes.data ?? {};
      const listData = pageMeta.list ?? pageMeta.fields?.list ?? [];
      const searchData = pageMeta.search ?? pageMeta.fields?.search ?? [];
      const addData = pageMeta.addForm ?? pageMeta.fields?.form?.addForm ?? [];
      const editData = pageMeta.editForm ?? pageMeta.fields?.form?.editForm ?? [];
      const detailData = pageMeta.detail ?? pageMeta.fields?.form?.detail ?? [];

      setListFields(resolveWidgets(listData));
      setSearchFields(resolveWidgets(searchData));
      setAddFields(resolveWidgets(addData));
      setEditFields(resolveWidgets(editData));
      setDetailFields(resolveWidgets(detailData));

      const pk = listData.find((f) => f.primaryKey);
      if (pk) setPkField(pk.name);
    });
  }, [type]);

  // 加载数据
  const loadData = useCallback(async (p?: number) => {
    const pageIndex = p ?? page;
    setLoading(true);
    try {
      const res = await api.page.getList(type, { ...searchForm, pageIndex, pageSize });
      setData((res.data as any[]) ?? []);
      setStatData((res as any).stat ?? null);
      if (res.page) {
        setTotal(res.page.totalCount);
      }
      if (p !== undefined) setPage(p);
    } finally {
      setLoading(false);
    }
  }, [type, searchForm, page, pageSize]);

  useEffect(() => { loadData(0); }, [type, pageSize]);

  // ECharts
  const loadChartData = useCallback(async () => {
    try {
      const res = await api.page.getChartData(type);
      setChartList(Array.isArray(res.data) && res.data.length > 0 ? res.data : []);
    } catch {
      setChartList([]);
    }
  }, [type]);

  useEffect(() => { loadChartData(); }, [type]);

  // 初始化/更新图表
  useEffect(() => {
    chartList.forEach((opt, idx) => {
      const el = chartRefs.current[idx];
      if (!el) return;
      if (chartInstances.current[idx]) chartInstances.current[idx].dispose();
      const instance = echarts.init(el);
      instance.setOption(opt);
      chartInstances.current[idx] = instance;
    });

    const handleResize = () => {
      for (const inst of chartInstances.current) { inst?.resize(); }
    };
    window.addEventListener('resize', handleResize);
    return () => {
      window.removeEventListener('resize', handleResize);
      for (const inst of chartInstances.current) { inst?.dispose(); }
      chartInstances.current = [];
    };
  }, [chartList]);

  // 选择
  const isSelected = (id: string | number) => selected.includes(id);
  const handleSelectAll = (e: ChangeEvent<HTMLInputElement>) => {
    setSelected(e.target.checked ? data.map((r) => r[pkField]) : []);
  };
  const handleSelectOne = (id: string | number) => {
    setSelected((prev) => prev.includes(id) ? prev.filter((v) => v !== id) : [...prev, id]);
  };

  // CRUD
  const showAdd = () => { setIsEdit(false); setEditForm({}); setFormOpen(true); };
  const showEdit = async (row: Record<string, any>) => {
    const res = await api.page.getDetail(type, row[pkField]);
    setIsEdit(true);
    setEditForm({ ...(res.data as any) });
    setFormOpen(true);
  };
  const showDetail = async (row: Record<string, any>) => {
    const res = await api.page.getDetail(type, row[pkField]);
    setDetailData((res.data as any) ?? {});
    setDetailOpen(true);
  };
  const handleSubmit = async () => {
    if (isEdit) {
      await api.page.update(type, editForm);
    } else {
      await api.page.add(type, editForm);
    }
    setFormOpen(false);
    loadData();
  };
  const handleDelete = async (id: string | number) => {
    await api.page.remove(type, id);
    loadData();
  };
  const handleDeleteSelect = async () => {
    await api.page.deleteSelect(type, selected);
    setSelected([]);
    loadData();
  };
  const handleExport = (fmt: string) => {
    window.open(api.page.getExportUrl(type, fmt), '_blank');
    setExportAnchor(null);
  };
  const handleImport = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    await api.page.importFile(type, file);
    loadData();
    e.target.value = '';
  };

  const formFields = isEdit ? editFields : addFields;

  return (
    <Box>
      {/* ECharts 图表区域 */}
      {chartList.length > 0 && (
        <Card sx={{ mb: 2 }}>
          <CardContent>
            {chartList.map((_, idx) => (
              <div
                key={idx}
                ref={(el) => { chartRefs.current[idx] = el; }}
                style={{ width: '100%', height: 400 }}
              />
            ))}
          </CardContent>
        </Card>
      )}

      {/* 搜索栏 */}
      {searchFields.length > 0 && (
        <Card sx={{ mb: 2 }}>
          <CardContent>
            <Stack direction="row" spacing={2} flexWrap="wrap" alignItems="center">
              {searchFields.map((f) => (
                <FieldInput
                  key={f.field.name}
                  mapping={f}
                  value={searchForm[f.field.name] ?? ''}
                  onChange={(v) => setSearchForm((prev) => ({ ...prev, [f.field.name]: v }))}
                  label={f.field.displayName ?? f.field.name}
                  size="small"
                />
              ))}
              <Button variant="contained" startIcon={<Search />} onClick={() => loadData(0)}>搜索</Button>
              <Button onClick={() => { setSearchForm({}); loadData(0); }}>重置</Button>
            </Stack>
          </CardContent>
        </Card>
      )}

      {/* 工具栏 */}
      <Toolbar disableGutters sx={{ gap: 1, mb: 1 }}>
        {canAdd && <Button variant="contained" startIcon={<Add />} onClick={showAdd}>新增</Button>}
        {canExport && (
          <>
            <Button startIcon={<Download />} endIcon={<ArrowDropDown />} onClick={(e) => setExportAnchor(e.currentTarget)}>导出</Button>
            <Menu anchorEl={exportAnchor} open={Boolean(exportAnchor)} onClose={() => setExportAnchor(null)}>
              {exportOptions.map((opt) => (
                <MuiMenuItem key={opt.value} onClick={() => handleExport(opt.value)}>{opt.label}</MuiMenuItem>
              ))}
            </Menu>
          </>
        )}
        {canImport && (
          <Button component="label" startIcon={<Upload />}>
            导入
            <input type="file" hidden accept=".csv,.xlsx,.xls" onChange={handleImport} />
          </Button>
        )}
        {canDelete && (
          <Button color="error" startIcon={<Delete />} disabled={!selected.length} onClick={handleDeleteSelect}>
            批量删除 ({selected.length})
          </Button>
        )}
        <Box flexGrow={1} />
        <IconButton onClick={() => loadData()}><Refresh /></IconButton>
      </Toolbar>

      {/* 数据表格 */}
      <TableContainer component={Paper}>
        <Table size="small" stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell padding="checkbox">
                <Checkbox
                  indeterminate={selected.length > 0 && selected.length < data.length}
                  checked={data.length > 0 && selected.length === data.length}
                  onChange={handleSelectAll}
                />
              </TableCell>
              {listFields.map((f) => (
                <TableCell key={f.field.name}>{f.field.displayName ?? f.field.name}</TableCell>
              ))}
              <TableCell align="right">操作</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data.map((row) => {
              const id = row[pkField];
              return (
                <TableRow key={id} hover selected={isSelected(id)}>
                  <TableCell padding="checkbox">
                    <Checkbox checked={isSelected(id)} onChange={() => handleSelectOne(id)} />
                  </TableCell>
                  {listFields.map((f) => (
                    <TableCell key={f.field.name}>
                      {renderCell(row, f)}
                    </TableCell>
                  ))}
                  <TableCell align="right">
                    <Tooltip title="查看"><IconButton size="small" onClick={() => showDetail(row)}><Visibility fontSize="small" /></IconButton></Tooltip>
                    {canEdit && <Tooltip title="编辑"><IconButton size="small" onClick={() => showEdit(row)}><EditIcon fontSize="small" /></IconButton></Tooltip>}
                    {canDelete && <Tooltip title="删除"><IconButton size="small" color="error" onClick={() => handleDelete(id)}><Delete fontSize="small" /></IconButton></Tooltip>}
                  </TableCell>
                </TableRow>
              );
            })}
            {/* 统计行 */}
            {statData && (
              <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                <TableCell padding="checkbox" />
                {listFields.map((f, idx) => (
                  <TableCell key={f.field.name} sx={{ fontWeight: 'bold' }}>
                    {idx === 0 ? '合计' : (statData[f.field.name] != null ? String(statData[f.field.name]) : '')}
                  </TableCell>
                ))}
                <TableCell />
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
      <TablePagination
        component="div"
        count={total}
        page={page}
        rowsPerPage={pageSize}
        rowsPerPageOptions={[10, 20, 50, 100]}
        onPageChange={(_, p) => loadData(p)}
        onRowsPerPageChange={(e) => { setPageSize(parseInt(e.target.value, 10)); setPage(0); }}
      />

      {/* 编辑/新增弹窗 */}
      <Dialog open={formOpen} onClose={() => setFormOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{isEdit ? '编辑' : '新增'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {formFields.map((f) => (
              <FieldInput
                key={f.field.name}
                mapping={f}
                value={editForm[f.field.name] ?? ''}
                onChange={(v) => setEditForm((prev) => ({ ...prev, [f.field.name]: v }))}
                label={f.field.displayName ?? f.field.name}
              />
            ))}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setFormOpen(false)}>取消</Button>
          <Button variant="contained" onClick={handleSubmit}>{isEdit ? '保存' : '确定'}</Button>
        </DialogActions>
      </Dialog>

      {/* 详情弹窗 */}
      <Dialog open={detailOpen} onClose={() => setDetailOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>详情</DialogTitle>
        <DialogContent>
          <Table size="small">
            <TableBody>
              {detailFields.map((f) => (
                <TableRow key={f.field.name}>
                  <TableCell sx={{ fontWeight: 'bold', width: 120 }}>{f.field.displayName ?? f.field.name}</TableCell>
                  <TableCell>{String(detailData[f.field.name] ?? '')}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDetailOpen(false)}>关闭</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

/** 表格单元格渲染 */
function renderCell(row: Record<string, any>, m: FieldMapping) {
  const val = row[m.field.name];
  if (m.widget === 'switch') {
    return <Chip label={val ? '是' : '否'} color={val ? 'success' : 'default'} size="small" />;
  }
  if (m.field.dataSource && Object.keys(m.field.dataSource).length) {
    return m.field.dataSource[String(val ?? '')] ?? val;
  }
  if (m.field.url) {
    const href = m.field.url.replace(/\{(\w+)\}/g, (_, k) => row[k] ?? '');
    return <a href={href} target={m.field.target ?? '_self'}>{val}</a>;
  }
  return String(val ?? '');
}
