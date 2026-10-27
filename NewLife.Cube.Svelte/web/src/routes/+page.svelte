<script lang="ts">
  import { onDestroy, onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import {
    appendMonitorPoint,
    buildMonitorChart,
    buildWorkbenchView,
    buildWidgetLayout,
    createWidgetLayoutLoader,
    createWorkbenchLoader,
    mergeWidgetsWithLayout,
    moveWidget,
    MONITOR_POLL_INTERVAL,
    type MonitorPoint,
    type WidgetLayoutMap,
    type WorkbenchData,
    type WorkbenchWidgetView,
  } from '@newlifex/page-logic';
  import { getApi } from '$lib/api';
  import { siteTitle } from '$lib/stores/app';
  import { displayName } from '$lib/stores/user';

  /** 个人信息卡行标签，顺序与共享视图模型一致 */
  const profileLabels = ['登录次数', '最后登录', '登录地址', '注册时间'];

  /** KPI 语义色 → 色值 */
  const KPI_COLORS: Record<string, string> = {
    blue: '#2563eb',
    green: '#16a34a',
    cyan: '#0891b2',
    orange: '#ea580c',
    red: '#dc2626',
    purple: '#7c3aed',
    grey: '#6b7280',
  };

  function kpiColor(color: string): string {
    return KPI_COLORS[color] ?? KPI_COLORS.blue;
  }

  const loader = createWorkbenchLoader(getApi());
  const layoutLoader = createWidgetLayoutLoader(getApi());

  let raw: WorkbenchData | null = null;
  let loading = false;
  let error = '';
  let monitorPoints: MonitorPoint[] = [];

  // --- 卡片布局：拖动排序 / 隐藏 / 持久化 ---
  let layout: WidgetLayoutMap = {};
  let visibleWidgets: WorkbenchWidgetView[] = [];
  let hiddenWidgets: WorkbenchWidgetView[] = [];
  let layoutEditing = false;
  let savingLayout = false;
  let layoutTip = '';
  let draggingName = '';
  let timer: ReturnType<typeof setInterval> | null = null;
  let polling = false;

  $: view = raw ? buildWorkbenchView(raw) : null;
  $: chart = buildMonitorChart(monitorPoints, 320, 120);
  $: latestMonitor = monitorPoints[monitorPoints.length - 1] ?? null;
  $: greeting = greetingOf(new Date().getHours());
  $: today = new Date().toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    weekday: 'long',
  });

  function greetingOf(hour: number): string {
    if (hour < 6) return '夜深了';
    if (hour < 12) return '早上好';
    if (hour < 14) return '中午好';
    if (hour < 18) return '下午好';
    return '晚上好';
  }

  function applyLayout(widgets: WorkbenchWidgetView[]) {
    const merged = mergeWidgetsWithLayout(widgets, layout);
    visibleWidgets = merged.visible;
    hiddenWidgets = merged.hidden;
  }

  function startLayoutEdit() {
    layoutEditing = true;
    layoutTip = '拖动卡片调整顺序，点击隐藏按钮可隐藏卡片';
  }

  function cancelLayoutEdit() {
    layoutEditing = false;
    layoutTip = '';
    if (view) applyLayout(view.widgets);
  }

  function onDropOn(target: string) {
    if (!layoutEditing || !draggingName) return;
    visibleWidgets = moveWidget(visibleWidgets, draggingName, target);
    draggingName = '';
  }

  function hideWidget(name: string) {
    const index = visibleWidgets.findIndex((item) => item.name === name);
    if (index < 0) return;
    const [widget] = visibleWidgets.splice(index, 1);
    visibleWidgets = [...visibleWidgets];
    hiddenWidgets = [...hiddenWidgets, widget];
  }

  function restoreWidget(name: string) {
    const index = hiddenWidgets.findIndex((item) => item.name === name);
    if (index < 0) return;
    const [widget] = hiddenWidgets.splice(index, 1);
    hiddenWidgets = [...hiddenWidgets];
    visibleWidgets = [...visibleWidgets, widget];
  }

  async function saveLayoutChanges() {
    savingLayout = true;
    try {
      const map = buildWidgetLayout(visibleWidgets, hiddenWidgets);
      if (await layoutLoader.save(map)) {
        layout = map;
        layoutTip = '布局已保存';
      } else {
        layoutTip = '布局保存失败';
      }
    } finally {
      savingLayout = false;
    }
  }

  async function resetLayoutChanges() {
    savingLayout = true;
    try {
      if (await layoutLoader.reset()) {
        layout = {};
        layoutTip = '布局已重置';
        if (view) applyLayout(view.widgets);
      } else {
        layoutTip = '布局重置失败';
      }
    } finally {
      savingLayout = false;
    }
  }

  async function load() {
    loading = true;
    error = '';
    try {
      const result = await loader.load();
      if (result.data) {
        raw = result.data;
        if (!layoutEditing) {
          layout = await layoutLoader.load();
          applyLayout(buildWorkbenchView(result.data).widgets);
        }
        syncPolling();
      } else {
        error = result.error || '工作台加载失败';
      }
    } catch (ex) {
      error = ex instanceof Error ? ex.message : String(ex);
    } finally {
      loading = false;
    }
  }

  async function pollMonitor() {
    if (polling) return;
    polling = true;
    try {
      monitorPoints = appendMonitorPoint(monitorPoints, await loader.pollMonitor());
    } catch {
      // 监控曲线属于增强信息，单次失败不打断工作台主流程
    } finally {
      polling = false;
    }
  }

  function startPolling() {
    if (timer) return;
    void pollMonitor();
    timer = setInterval(() => void pollMonitor(), MONITOR_POLL_INTERVAL);
  }

  function stopPolling() {
    if (!timer) return;
    clearInterval(timer);
    timer = null;
  }

  function syncPolling() {
    if (view?.hasChartWidget) startPolling();
    else stopPolling();
  }

  onMount(() => {
    void load();
    return stopPolling;
  });

  onDestroy(stopPolling);
</script>

<svelte:head>
  <title>首页 - {$siteTitle}</title>
</svelte:head>

{#if loading && !view}
  <div class="py-16 text-center text-sm" style="color: var(--text-secondary)">正在加载工作台…</div>
{:else if error && !view}
  <div class="card p-6">
    <p class="text-sm font-medium" style="color: #dc2626">工作台加载失败：{error}</p>
    <button class="mt-3 rounded border px-3 py-1.5 text-sm" style="border-color: var(--border)" onclick={load}>重试</button>
  </div>
{:else}
  <div class="mb-6 flex items-center justify-between gap-4">
    <div>
      <h2 class="text-2xl font-bold">{greeting}，{view?.displayName || $displayName || '-'}</h2>
      <p class="mt-1 text-sm" style="color: var(--text-secondary)">
        {today}{view?.roleText ? ` · ${view.roleText}` : ''}
      </p>
    </div>
    <div class="flex gap-2">
      {#if layoutEditing}
        <button class="rounded border px-3 py-1.5 text-sm" style="border-color: var(--border)" disabled={savingLayout} onclick={saveLayoutChanges}>保存</button>
        <button class="rounded border px-3 py-1.5 text-sm" style="border-color: var(--border)" disabled={savingLayout} onclick={resetLayoutChanges}>重置</button>
        <button class="rounded px-3 py-1.5 text-sm" onclick={cancelLayoutEdit}>完成</button>
      {:else}
        <button class="rounded border px-3 py-1.5 text-sm" style="border-color: var(--border)" onclick={startLayoutEdit}>编辑布局</button>
      {/if}
      <button class="rounded border px-3 py-1.5 text-sm" style="border-color: var(--border)" disabled={loading} onclick={load}>
        刷新
      </button>
    </div>
  </div>

  {#if layoutTip}
    <p class="mb-4 rounded border p-3 text-sm" style="border-color: #3b82f6; color: #1d4ed8">{layoutTip}</p>
  {/if}

  {#if layoutEditing && hiddenWidgets.length}
    <div class="mb-4">
      <p class="mb-2 text-sm" style="color: var(--text-secondary)">已隐藏的卡片</p>
      <div class="flex flex-wrap gap-2">
        {#each hiddenWidgets as widget}
          <button class="rounded-full border px-3 py-1 text-sm" style="border-color: var(--border)" onclick={() => restoreWidget(widget.name)}>
            {widget.title}（点击恢复）
          </button>
        {/each}
      </div>
    </div>
  {/if}

  {#if error}
    <p class="mb-4 rounded border p-3 text-sm" style="border-color: #f59e0b; color: #b45309">{error}</p>
  {/if}

  {#if view?.kpis.length}
    <div class="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      {#each view.kpis as kpi}
        <div
          class="card p-4 {kpi.url ? 'cursor-pointer' : ''}"
          style="border-left: 4px solid {kpiColor(kpi.color)}"
          onclick={() => kpi.url && goto(kpi.url)}
          role={kpi.url ? 'link' : undefined}
        >
          <p class="text-sm" style="color: var(--text-secondary)">{kpi.label}</p>
          <p class="mt-1 text-3xl font-bold">{kpi.value}</p>
          {#if kpi.trend}
            <p class="mt-1 text-xs" style="color: var(--text-secondary)">{kpi.trend}</p>
          {/if}
        </div>
      {/each}
    </div>
  {/if}

  {#if visibleWidgets.length}
    <div class="grid grid-cols-12 gap-4">
      {#each visibleWidgets as widget}
        <div
          class="card col-span-12 p-4"
          style="grid-column: span {widget.cols} / span {widget.cols}"
          data-widget={widget.name}
          draggable={layoutEditing}
          ondragstart={() => (draggingName = widget.name)}
          ondragover={(e) => e.preventDefault()}
          ondrop={() => onDropOn(widget.name)}
        >
          <div class="mb-3 flex items-center gap-2">
            <h3 class="text-base font-semibold">{widget.title}</h3>
            {#if layoutEditing}
              <button class="ml-auto text-sm underline" onclick={() => hideWidget(widget.name)}>隐藏</button>
            {/if}
          </div>

          {#if widget.kind === 'monitor'}
            {#if monitorPoints.length}
              <svg viewBox="0 0 {chart.width} {chart.height}" class="h-32 w-full" preserveAspectRatio="none">
                <polyline points={chart.cpuPoints} fill="none" stroke="#4f46e5" stroke-width="2" />
                <polyline points={chart.memPoints} fill="none" stroke="#059669" stroke-width="2" />
              </svg>
              <div class="flex gap-4 text-xs" style="color: var(--text-secondary)">
                <span>CPU {latestMonitor?.cpu ?? 0}%</span>
                <span>内存 {latestMonitor?.memory ?? 0}%</span>
                {#if latestMonitor?.memUsed != null && latestMonitor?.memTotal}
                  <span>{latestMonitor.memUsed} / {latestMonitor.memTotal} MB</span>
                {/if}
              </div>
            {:else}
              <p class="text-sm" style="color: var(--text-secondary)">正在采集监控数据…</p>
            {/if}
          {:else if widget.kind === 'profile' && widget.profile}
            <div class="mb-3 flex items-center gap-3">
              <div class="flex h-11 w-11 items-center justify-center rounded-full bg-indigo-600 font-semibold text-white">
                {widget.profile.initial}
              </div>
              <div>
                <p class="font-medium">{widget.profile.displayName}</p>
                <div class="mt-1 flex flex-wrap gap-1 text-xs">
                  <span class="rounded px-1.5 py-0.5" style="background: var(--bg-secondary)">
                    {widget.profile.online ? '在线' : '离线'}
                  </span>
                  {#each widget.profile.roles as role}
                    <span class="rounded px-1.5 py-0.5" style="background: var(--bg-secondary)">{role}</span>
                  {/each}
                </div>
              </div>
            </div>
            <dl class="grid grid-cols-2 gap-2 text-sm">
              {#each widget.profile.rows as row, i}
                <div>
                  <dt class="text-xs" style="color: var(--text-secondary)">{profileLabels[i] || row.key}</dt>
                  <dd>{row.text}</dd>
                </div>
              {/each}
            </dl>
          {:else if widget.kind === 'quickLinks'}
            {#if widget.links?.length}
              <div class="flex flex-wrap gap-2">
                {#each widget.links as link}
                  <a class="rounded border px-3 py-1.5 text-sm" style="border-color: var(--border)" href={link.url}>{link.name}</a>
                {/each}
              </div>
            {:else}
              <p class="text-sm" style="color: var(--text-secondary)">暂无快捷入口</p>
            {/if}
          {:else if widget.kind === 'loginLog'}
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <section>
                <p class="mb-1 text-xs" style="color: var(--text-secondary)">最近登录</p>
                {#if widget.logins?.length}
                  <ul>
                    {#each widget.logins as item, i}
                      <li class="flex justify-between gap-2 border-b py-1 text-sm" style="border-color: var(--border)">
                        <span>{item.title}</span>
                        <span class="text-xs" style="color: var(--text-secondary)">{item.subtitle} · {item.time}</span>
                      </li>
                    {/each}
                  </ul>
                {:else}
                  <p class="text-sm" style="color: var(--text-secondary)">暂无登录记录</p>
                {/if}
              </section>
              <section>
                <p class="mb-1 text-xs" style="color: var(--text-secondary)">在线用户</p>
                {#if widget.onlines?.length}
                  <ul>
                    {#each widget.onlines as item, i}
                      <li class="flex justify-between gap-2 border-b py-1 text-sm" style="border-color: var(--border)">
                        <span>{item.title}</span>
                        <span class="text-xs" style="color: var(--text-secondary)">{item.subtitle} · {item.time}</span>
                      </li>
                    {/each}
                  </ul>
                {:else}
                  <p class="text-sm" style="color: var(--text-secondary)">暂无在线用户</p>
                {/if}
              </section>
            </div>
          {:else if widget.kind === 'kv' && widget.rows?.length}
            <dl>
              {#each widget.rows as row}
                <div class="flex gap-4 border-b py-1.5 text-sm" style="border-color: var(--border)">
                  <dt class="min-w-24" style="color: var(--text-secondary)">{row.key}</dt>
                  <dd>
                    {#if row.link}
                      <a href={row.link.href} target={row.link.external ? '_blank' : undefined} rel="noopener noreferrer">{row.link.text}</a>
                    {:else}
                      {row.text}
                    {/if}
                  </dd>
                </div>
              {/each}
            </dl>
          {:else if widget.kind === 'text'}
            <p class="text-sm">{widget.text}</p>
          {:else}
            <p class="text-sm" style="color: var(--text-secondary)">暂无数据</p>
          {/if}
        </div>
      {/each}
    </div>
  {/if}

  {#if view && !view.hasContent}
    <p class="py-16 text-center text-sm" style="color: var(--text-secondary)">暂无工作台数据</p>
  {/if}
{/if}

