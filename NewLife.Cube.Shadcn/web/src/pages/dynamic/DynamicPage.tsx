import { useState, useEffect, useCallback, type ChangeEvent } from 'react';
import { useLocation } from 'react-router-dom';
import {
  Plus, Trash2, Download, Upload, Search, RefreshCw, Eye, Pencil,
} from 'lucide-react';
import { FieldKind, type DataField } from '@cube/api-core';
import { resolveWidgets, type FieldMapping } from '@cube/field-mapping';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Table, TableHeader, TableBody, TableRow, TableHead, TableCell,
} from '@/components/ui/table';
import {
  Dialog, DialogContent, DialogHeader, DialogFooter, DialogTitle, DialogDescription,
} from '@/components/ui/dialog';
import FieldInput from '@/components/FieldInput';
import api from '@/api';

export default function DynamicPage() {
  const location = useLocation();
  const type = location.pathname;

  const [listFields, setListFields] = useState<FieldMapping[]>([]);
  const [searchFields, setSearchFields] = useState<FieldMapping[]>([]);
  const [addFields, setAddFields] = useState<FieldMapping[]>([]);
  const [editFields, setEditFields] = useState<FieldMapping[]>([]);
  const [detailFields, setDetailFields] = useState<FieldMapping[]>([]);
  const [pkField, setPkField] = useState('id');

  const [data, setData] = useState<Record<string, any>[]>([]);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [total, setTotal] = useState(0);
  const [selected, setSelected] = useState<Set<string | number>>(new Set());
  const [searchForm, setSearchForm] = useState<Record<string, any>>({});

  const [formOpen, setFormOpen] = useState(false);
  const [isEdit, setIsEdit] = useState(false);
  const [editForm, setEditForm] = useState<Record<string, any>>({});
  const [detailOpen, setDetailOpen] = useState(false);
  const [detailData, setDetailData] = useState<Record<string, any>>({});

  // 加载字段
  useEffect(() => {
    Promise.all([
      api.page.getFields(type, FieldKind.List),
      api.page.getFields(type, FieldKind.Search),
      api.page.getFields(type, FieldKind.Add),
      api.page.getFields(type, FieldKind.Edit),
      api.page.getFields(type, FieldKind.Detail),
    ]).then(([listRes, searchRes, addRes, editRes, detailRes]) => {
      setListFields(resolveWidgets(listRes.data ?? []));
      setSearchFields(resolveWidgets(searchRes.data ?? []));
      setAddFields(resolveWidgets(addRes.data ?? []));
      setEditFields(resolveWidgets(editRes.data ?? []));
      setDetailFields(resolveWidgets(detailRes.data ?? []));
      const pk = (listRes.data ?? []).find((f) => f.primaryKey);
      if (pk) setPkField(pk.name);
    });
  }, [type]);

  const loadData = useCallback(async (p?: number) => {
    const pageIndex = p ?? page;
    const res = await api.page.getList(type, { ...searchForm, pageIndex, pageSize });
    setData((res.data as any[]) ?? []);
    if (res.page) setTotal(res.page.totalCount);
    if (p !== undefined) setPage(p);
  }, [type, searchForm, page, pageSize]);

  useEffect(() => { loadData(0); }, [type, pageSize]);

  // 选择
  const toggleSelect = (id: string | number) => {
    setSelected((prev) => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  };
  const toggleSelectAll = () => {
    if (selected.size === data.length) {
      setSelected(new Set());
    } else {
      setSelected(new Set(data.map((r) => r[pkField])));
    }
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
    if (isEdit) await api.page.update(type, editForm);
    else await api.page.add(type, editForm);
    setFormOpen(false);
    loadData();
  };
  const handleDelete = async (id: string | number) => {
    await api.page.remove(type, id);
    loadData();
  };
  const handleDeleteSelect = async () => {
    await api.page.deleteSelect(type, [...selected]);
    setSelected(new Set());
    loadData();
  };
  const handleExport = (fmt: string) => { window.open(api.page.getExportUrl(type, fmt), '_blank'); };
  const handleImport = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    await api.page.importFile(type, file);
    loadData();
    e.target.value = '';
  };

  const totalPages = Math.ceil(total / pageSize);
  const formFields = isEdit ? editFields : addFields;

  return (
    <div className="space-y-4">
      {/* 搜索栏 */}
      {searchFields.length > 0 && (
        <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-4">
          {searchFields.map((f) => (
            <div key={f.field.name} className="min-w-40">
              <FieldInput
                mapping={f}
                value={searchForm[f.field.name] ?? ''}
                onChange={(v) => setSearchForm((prev) => ({ ...prev, [f.field.name]: v }))}
              />
            </div>
          ))}
          <Button size="sm" onClick={() => loadData(0)}><Search className="mr-1 h-3 w-3" />搜索</Button>
          <Button variant="ghost" size="sm" onClick={() => { setSearchForm({}); loadData(0); }}>重置</Button>
        </div>
      )}

      {/* 工具栏 */}
      <div className="flex flex-wrap items-center gap-2">
        <Button size="sm" onClick={showAdd}><Plus className="mr-1 h-3 w-3" />新增</Button>
        <Button variant="outline" size="sm" onClick={() => handleExport('Csv')}><Download className="mr-1 h-3 w-3" />CSV</Button>
        <Button variant="outline" size="sm" onClick={() => handleExport('Excel')}><Download className="mr-1 h-3 w-3" />Excel</Button>
        <Button variant="outline" size="sm" asChild>
          <label>
            <Upload className="mr-1 h-3 w-3" />导入
            <input type="file" className="hidden" accept=".csv,.xlsx,.xls" onChange={handleImport} />
          </label>
        </Button>
        {selected.size > 0 && (
          <Button variant="destructive" size="sm" onClick={handleDeleteSelect}>
            <Trash2 className="mr-1 h-3 w-3" />删除 ({selected.size})
          </Button>
        )}
        <div className="flex-1" />
        <Button variant="ghost" size="icon" onClick={() => loadData()}><RefreshCw className="h-4 w-4" /></Button>
      </div>

      {/* 数据表格 */}
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-10">
                <Checkbox
                  checked={data.length > 0 && selected.size === data.length}
                  onCheckedChange={toggleSelectAll}
                />
              </TableHead>
              {listFields.map((f) => (
                <TableHead key={f.field.name}>{f.field.displayName ?? f.field.name}</TableHead>
              ))}
              <TableHead className="text-right">操作</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data.map((row) => {
              const id = row[pkField];
              return (
                <TableRow key={id} data-state={selected.has(id) ? 'selected' : undefined}>
                  <TableCell>
                    <Checkbox checked={selected.has(id)} onCheckedChange={() => toggleSelect(id)} />
                  </TableCell>
                  {listFields.map((f) => (
                    <TableCell key={f.field.name}>{renderCell(row, f)}</TableCell>
                  ))}
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => showDetail(row)}>
                        <Eye className="h-3 w-3" />
                      </Button>
                      <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => showEdit(row)}>
                        <Pencil className="h-3 w-3" />
                      </Button>
                      <Button variant="ghost" size="icon" className="h-7 w-7 text-destructive" onClick={() => handleDelete(id)}>
                        <Trash2 className="h-3 w-3" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              );
            })}
            {data.length === 0 && (
              <TableRow>
                <TableCell colSpan={listFields.length + 2} className="h-24 text-center text-muted-foreground">
                  暂无数据
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      {/* 分页 */}
      <div className="flex items-center justify-between text-sm text-muted-foreground">
        <span>共 {total} 条</span>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" disabled={page <= 0} onClick={() => loadData(page - 1)}>上一页</Button>
          <span>{page + 1} / {totalPages || 1}</span>
          <Button variant="outline" size="sm" disabled={page + 1 >= totalPages} onClick={() => loadData(page + 1)}>下一页</Button>
        </div>
      </div>

      {/* 编辑/新增弹窗 */}
      <Dialog open={formOpen} onOpenChange={setFormOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{isEdit ? '编辑' : '新增'}</DialogTitle>
            <DialogDescription>{isEdit ? '修改记录信息' : '填写新记录信息'}</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-2">
            {formFields.map((f) => (
              <FieldInput
                key={f.field.name}
                mapping={f}
                value={editForm[f.field.name] ?? ''}
                onChange={(v) => setEditForm((prev) => ({ ...prev, [f.field.name]: v }))}
              />
            ))}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setFormOpen(false)}>取消</Button>
            <Button onClick={handleSubmit}>{isEdit ? '保存' : '确定'}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* 详情弹窗 */}
      <Dialog open={detailOpen} onOpenChange={setDetailOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>详情</DialogTitle>
            <DialogDescription>查看记录详细信息</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            {detailFields.map((f) => (
              <div key={f.field.name} className="flex gap-4 border-b py-2 last:border-0">
                <span className="w-28 shrink-0 font-medium text-muted-foreground">{f.field.displayName ?? f.field.name}</span>
                <span className="break-all">{String(detailData[f.field.name] ?? '')}</span>
              </div>
            ))}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDetailOpen(false)}>关闭</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function renderCell(row: Record<string, any>, m: FieldMapping) {
  const val = row[m.field.name];
  if (m.widget === 'switch') {
    return <Badge variant={val ? 'default' : 'secondary'}>{val ? '是' : '否'}</Badge>;
  }
  if (m.field.dataSource && Object.keys(m.field.dataSource).length) {
    return m.field.dataSource[String(val ?? '')] ?? val;
  }
  if (m.field.url) {
    const href = m.field.url.replace(/\{(\w+)\}/g, (_, k) => row[k] ?? '');
    return <a href={href} target={m.field.target ?? '_self'} className="text-primary underline">{val}</a>;
  }
  return String(val ?? '');
}
