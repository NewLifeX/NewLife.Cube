<script lang="ts">
  import { page } from '$app/state';
  import { onMount, onDestroy } from 'svelte';
  import { getApi } from '$lib/api';
  import { getMenuPermission } from '$lib/stores/user';
  import FieldInput from '$lib/components/FieldInput.svelte';
  import { FieldKind, Auth, type DataField, type PageResult } from '@cube/api-core';
  import { toCamelCase } from '@cube/field-mapping';
  import * as echarts from 'echarts';

  const exportOptions = [
    { label: '导出 Excel', value: 'Excel' },
    { label: '导出 CSV', value: 'Csv' },
    { label: '导出 JSON', value: 'Json' },
    { label: '导出 XML', value: 'Xml' },
    { label: '导出模板', value: 'ExcelTemplate' },
  ];

  // 当前类型路径
  const typePath = $derived('/' + page.params.type);

  // 权限
  const perms = $derived(getMenuPermission(typePath));
  const canAdd = $derived(String(Auth.ADD) in perms);
  const canEdit = $derived(String(Auth.EDIT) in perms);
  const canDelete = $derived(String(Auth.DELETE) in perms);
  const canExport = $derived(String(Auth.EXPORT) in perms);
  const canImport = $derived(String(Auth.IMPORT) in perms);

  // 字段
  let listFields: DataField[] = $state([]);
  let editFields: DataField[] = $state([]);
  let detailFields: DataField[] = $state([]);
  let searchFields: DataField[] = $state([]);

  // 数据
  let data: any[] = $state([]);
  let total = $state(0);
  let pageIndex = $state(1);
  let pageSize = $state(20);
  let keyword = $state('');
  let loading = $state(false);
  let statData: Record<string, unknown> | null = $state(null);

  // ECharts
  let chartList: any[] = $state([]);
  let chartEls: HTMLDivElement[] = [];
  let chartInstances: any[] = [];
  let showExportMenu = $state(false);

  // 选中
  let selectedIds: Set<number> = $state(new Set());
  let allSelected = $derived(data.length > 0 && data.every((r) => selectedIds.has(r.id)));

  // 对话框
  let showForm = $state(false);
  let showDetail = $state(false);
  let showDeleteConfirm = $state(false);
  let deleteTargetId = $state<number | null>(null);
  let formData: Record<string, any> = $state({});
  let detailData: Record<string, any> = $state({});
  let isEdit = $state(false);

  // 加载字段
  async function loadFields() {
    const api = getApi();
    try {
      const [listRes, editRes, detailRes, searchRes] = await Promise.all([
        api.page.getFields(typePath, FieldKind.List),
        api.page.getFields(typePath, FieldKind.Edit),
        api.page.getFields(typePath, FieldKind.Detail),
        api.page.getFields(typePath, FieldKind.Search),
      ]);
      listFields = listRes?.data ?? [];
      editFields = editRes?.data ?? [];
      detailFields = detailRes?.data ?? [];
      searchFields = searchRes?.data ?? [];
    } catch { /* ignore */ }
  }

  // 加载数据
  async function loadData() {
    loading = true;
    try {
      const params: Record<string, any> = { pageIndex: pageIndex - 1, pageSize };
      if (keyword) params.key = keyword;
      const res = await getApi().page.getList(typePath, params);
      data = (res?.data as any[]) ?? [];
      statData = (res as any)?.stat ?? null;
      if ((res as any)?.page) total = (res as any).page.totalCount ?? 0;
    } catch { /* ignore */ }
    loading = false;
  }

  // ECharts
  async function loadChartData() {
    try {
      const res = await getApi().page.getChartData(typePath);
      chartList = Array.isArray(res?.data) && res.data.length > 0 ? res.data : [];
    } catch {
      chartList = [];
    }
  }

  function initCharts() {
    disposeCharts();
    chartList.forEach((opt, idx) => {
      const el = chartEls[idx];
      if (!el) return;
      const instance = echarts.init(el);
      instance.setOption(opt);
      chartInstances.push(instance);
    });
  }

  function disposeCharts() {
    for (const inst of chartInstances) { inst?.dispose(); }
    chartInstances = [];
  }

  function handleResize() {
    for (const inst of chartInstances) { inst?.resize(); }
  }

  // 监听路由变化
  $effect(() => {
    void typePath;
    pageIndex = 1;
    keyword = '';
    selectedIds = new Set();
    loadFields();
    loadData();
    loadChartData();
  });

  // 图表初始化（监听 chartList 变化）
  $effect(() => {
    if (chartList.length > 0) {
      // 延迟一帧确保 DOM 已渲染
      requestAnimationFrame(() => initCharts());
    }
  });

  onMount(() => { window.addEventListener('resize', handleResize); });
  onDestroy(() => { window.removeEventListener('resize', handleResize); disposeCharts(); });

  // 创建
  function handleAdd() {
    formData = {};
    isEdit = false;
    showForm = true;
  }

  // 编辑
  async function handleEdit(id: number) {
    try {
      const res = await getApi().page.getDetail(typePath, id);
      formData = res?.data ? { ...res.data } : {};
      isEdit = true;
      showForm = true;
    } catch { /* ignore */ }
  }

  // 查看详情
  async function handleDetail(id: number) {
    try {
      const res = await getApi().page.getDetail(typePath, id);
      detailData = res?.data ?? {};
      showDetail = true;
    } catch { /* ignore */ }
  }

  // 保存
  async function handleSave() {
    try {
      if (isEdit) {
        await getApi().page.update(typePath, formData);
      } else {
        await getApi().page.create(typePath, formData);
      }
      showForm = false;
      await loadData();
    } catch { /* ignore */ }
  }

  // 删除确认
  function confirmDelete(id: number) {
    deleteTargetId = id;
    showDeleteConfirm = true;
  }

  async function handleDelete() {
    if (deleteTargetId == null) return;
    try {
      await getApi().page.delete(typePath, deleteTargetId);
      showDeleteConfirm = false;
      deleteTargetId = null;
      await loadData();
    } catch { /* ignore */ }
  }

  // 批量删除
  async function handleBatchDelete() {
    if (selectedIds.size === 0) return;
    try {
      await getApi().page.deleteSelect(typePath, [...selectedIds]);
      selectedIds = new Set();
      await loadData();
    } catch { /* ignore */ }
  }

  // 翻页
  function goPage(p: number) {
    pageIndex = p;
    loadData();
  }

  // 全选
  function toggleAll() {
    if (allSelected) {
      selectedIds = new Set();
    } else {
      selectedIds = new Set(data.map((r) => r.id));
    }
  }

  // 单选
  function toggleOne(id: number) {
    const next = new Set(selectedIds);
    if (next.has(id)) next.delete(id); else next.add(id);
    selectedIds = next;
  }

  // 导出
  function handleExport(fmt: string) {
    window.open(getApi().page.getExportUrl(typePath, fmt), '_blank');
    showExportMenu = false;
  }

  // 导入
  async function handleImport(e: Event) {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;
    try {
      await getApi().page.importFile(typePath, file);
      await loadData();
    } catch { /* ignore */ }
  }

  // 字段取值
  function getValue(row: any, field: DataField): any {
    return row[toCamelCase(field.name)];
  }

  const totalPages = $derived(Math.ceil(total / pageSize) || 1);
</script>

<!-- ECharts 图表 -->
{#if chartList.length > 0}
  <div class="card p-4 mb-4">
    {#each chartList as _, idx}
      <div bind:this={chartEls[idx]} style="width: 100%; height: 400px;"></div>
    {/each}
  </div>
{/if}

<!-- Toolbar -->
<div class="flex flex-wrap items-center gap-3 mb-4">
  {#if canAdd}
    <button class="btn btn-primary btn-sm" onclick={handleAdd}>新增</button>
  {/if}
  {#if canDelete}
    <button class="btn btn-danger btn-sm" disabled={selectedIds.size === 0} onclick={handleBatchDelete}>
      批量删除({selectedIds.size})
    </button>
  {/if}
  {#if canExport}
    <div class="relative">
      <button class="btn btn-outline btn-sm" onclick={() => showExportMenu = !showExportMenu}>
        导出 ▾
      </button>
      {#if showExportMenu}
        <div class="absolute top-full left-0 mt-1 bg-white dark:bg-gray-800 border rounded shadow-lg z-10 min-w-32">
          {#each exportOptions as opt}
            <button class="block w-full text-left px-3 py-2 text-sm hover:bg-gray-100 dark:hover:bg-gray-700" onclick={() => handleExport(opt.value)}>{opt.label}</button>
          {/each}
        </div>
      {/if}
    </div>
  {/if}
  {#if canImport}
    <label class="btn btn-outline btn-sm cursor-pointer">
      导入 <input type="file" accept=".csv,.xls,.xlsx" class="hidden" onchange={handleImport} />
    </label>
  {/if}
  <div class="flex-1"></div>
  <input class="input w-56" placeholder="搜索..." bind:value={keyword}
    onkeydown={(e) => { if (e.key === 'Enter') { pageIndex = 1; loadData(); } }} />
  <button class="btn btn-primary btn-sm" onclick={() => { pageIndex = 1; loadData(); }}>查询</button>
</div>

<!-- Table -->
<div class="card p-0 overflow-auto">
  <table class="w-full text-sm">
    <thead>
      <tr class="border-b" style="border-color: var(--border); background: var(--bg-secondary)">
        <th class="w-10 px-3 py-3">
          <input type="checkbox" checked={allSelected} onchange={toggleAll} />
        </th>
        {#each listFields as f}
          <th class="px-3 py-3 text-left font-medium" style="color: var(--text-secondary)">{f.displayName || f.name}</th>
        {/each}
        <th class="px-3 py-3 text-left font-medium" style="color: var(--text-secondary)">操作</th>
      </tr>
    </thead>
    <tbody>
      {#if loading}
        <tr><td colspan={listFields.length + 2} class="text-center py-8" style="color: var(--text-secondary)">加载中...</td></tr>
      {:else if data.length === 0}
        <tr><td colspan={listFields.length + 2} class="text-center py-8" style="color: var(--text-secondary)">暂无数据</td></tr>
      {:else}
        {#each data as row}
          <tr class="border-b hover:bg-gray-50 dark:hover:bg-gray-800/50" style="border-color: var(--border)">
            <td class="px-3 py-2.5">
              <input type="checkbox" checked={selectedIds.has(row.id)} onchange={() => toggleOne(row.id)} />
            </td>
            {#each listFields as f}
              <td class="px-3 py-2.5">{getValue(row, f) ?? '-'}</td>
            {/each}
            <td class="px-3 py-2.5">
              <div class="flex gap-2">
                <button class="text-indigo-600 hover:underline text-xs" onclick={() => handleDetail(row.id)}>查看</button>
                {#if canEdit}
                  <button class="text-indigo-600 hover:underline text-xs" onclick={() => handleEdit(row.id)}>编辑</button>
                {/if}
                {#if canDelete}
                  <button class="text-red-600 hover:underline text-xs" onclick={() => confirmDelete(row.id)}>删除</button>
                {/if}
              </div>
            </td>
          </tr>
        {/each}
        <!-- 统计行 -->
        {#if statData}
          <tr class="border-t" style="border-color: var(--border); background: var(--bg-secondary); font-weight: bold;">
            <td class="px-3 py-2.5"></td>
            {#each listFields as f, idx}
              <td class="px-3 py-2.5">{idx === 0 ? '合计' : (statData[f.name] != null ? String(statData[f.name]) : '')}</td>
            {/each}
            <td class="px-3 py-2.5"></td>
          </tr>
        {/if}
      {/if}
    </tbody>
  </table>
</div>

<!-- Pagination -->
<div class="flex items-center justify-between mt-4">
  <span class="text-sm" style="color: var(--text-secondary)">共 {total} 条</span>
  <div class="flex gap-1">
    <button class="btn btn-outline btn-sm" disabled={pageIndex <= 1} onclick={() => goPage(pageIndex - 1)}>上一页</button>
    {#each Array.from({ length: Math.min(totalPages, 7) }, (_, i) => i + 1) as p}
      <button class="btn btn-sm" class:btn-primary={p === pageIndex} class:btn-outline={p !== pageIndex} onclick={() => goPage(p)}>{p}</button>
    {/each}
    <button class="btn btn-outline btn-sm" disabled={pageIndex >= totalPages} onclick={() => goPage(pageIndex + 1)}>下一页</button>
  </div>
</div>

<!-- Modal: Form -->
{#if showForm}
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onclick={(e) => { if (e.target === e.currentTarget) showForm = false; }}>
    <div class="card w-full max-w-lg max-h-[80vh] overflow-auto m-4">
      <h3 class="text-lg font-bold mb-4">{isEdit ? '编辑' : '新增'}</h3>
      <div class="space-y-4">
        {#each editFields as f}
          <div>
            <label class="block text-sm font-medium mb-1" style="color: var(--text-secondary)">{f.displayName || f.name}</label>
            <FieldInput field={f} bind:value={formData[toCamelCase(f.name)]} />
          </div>
        {/each}
      </div>
      <div class="flex justify-end gap-3 mt-6">
        <button class="btn btn-outline" onclick={() => showForm = false}>取消</button>
        <button class="btn btn-primary" onclick={handleSave}>保存</button>
      </div>
    </div>
  </div>
{/if}

<!-- Modal: Detail -->
{#if showDetail}
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onclick={(e) => { if (e.target === e.currentTarget) showDetail = false; }}>
    <div class="card w-full max-w-lg max-h-[80vh] overflow-auto m-4">
      <h3 class="text-lg font-bold mb-4">详情</h3>
      <div class="space-y-3">
        {#each detailFields as f}
          <div class="flex">
            <span class="w-32 text-sm flex-shrink-0" style="color: var(--text-secondary)">{f.displayName || f.name}</span>
            <span class="text-sm">{detailData[toCamelCase(f.name)] ?? '-'}</span>
          </div>
        {/each}
      </div>
      <div class="flex justify-end mt-6">
        <button class="btn btn-outline" onclick={() => showDetail = false}>关闭</button>
      </div>
    </div>
  </div>
{/if}

<!-- Modal: Delete Confirm -->
{#if showDeleteConfirm}
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
    <div class="card w-full max-w-sm m-4">
      <h3 class="text-lg font-bold mb-2">确认删除</h3>
      <p class="text-sm mb-6" style="color: var(--text-secondary)">此操作不可撤销，确定要删除吗？</p>
      <div class="flex justify-end gap-3">
        <button class="btn btn-outline" onclick={() => { showDeleteConfirm = false; deleteTargetId = null; }}>取消</button>
        <button class="btn btn-danger" onclick={handleDelete}>删除</button>
      </div>
    </div>
  </div>
{/if}
