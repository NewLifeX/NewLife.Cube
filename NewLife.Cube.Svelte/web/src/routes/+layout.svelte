<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { goto } from '$app/navigation';
  import { page } from '$app/state';
  import { closeAllTabs, closeTab, createTab, openTab, shouldAutoCollapseSidebar, type TabItem } from '@newlifex/page-logic';
  import { collapsed, darkMode, siteConfig, siteTitle } from '$lib/stores/app';
  import { user, menus, isLoggedIn, displayName, fetchUserInfo, fetchMenus, logout } from '$lib/stores/user';
  import { getApi } from '$lib/api';
  import type { MenuItem } from '@newlifex/api-core';
  import '../app.css';

  let openKeys: Record<string, boolean> = $state({});
  let showUser = $state(false);
  let isNarrow = $state(false);
  let mobileOpen = $state(false);

  // 路由切换后关闭抽屉
  $effect(() => {
    page.url.pathname;
    mobileOpen = false;
  });

  // --- 多标签页 ---
  let tabs: TabItem[] = $state([]);
  let activeTabId = $state('');

  $effect(() => {
    const path = page.url.pathname;
    const search = page.url.search;
    const tab = createTab(path, path + search, path);
    tabs = openTab(tabs, tab);
    activeTabId = tab.id;
  });

  function activateTab(tab: TabItem) {
    if (tab.id === activeTabId) return;
    goto(tab.fullPath);
  }

  function closeTabAndNavigate(id: string) {
    const result = closeTab(tabs, id, activeTabId);
    tabs = result.tabs;
    if (result.nextActiveId) {
      activeTabId = result.nextActiveId;
      const next = result.tabs.find((t) => t.id === result.nextActiveId);
      if (next) goto(next.fullPath);
    }
  }

  function closeAll() {
    tabs = closeAllTabs(tabs);
    const home = tabs[0];
    if (home) {
      activeTabId = home.id;
      goto(home.fullPath);
    }
  }

  onMount(async () => {
    try {
      const [siteRes] = await Promise.all([getApi().site.getSite()]);
      if (siteRes?.data) siteConfig.set(siteRes.data);
    } catch { /* ignore */ }

    // 不在登录页时加载用户信息
    if (!page.url.pathname.startsWith('/login')) {
      await fetchUserInfo();
      if (!$isLoggedIn) { goto('/login'); return; }
      await fetchMenus();
    }
  });

  function toggleKey(key: string) {
    openKeys[key] = !openKeys[key];
  }

  async function handleLogout() {
    await logout();
    goto('/login');
  }

  function buildUrl(item: MenuItem): string {
    return item.url?.startsWith('/') ? item.url : `/${item.url ?? ''}`;
  }

  // 窄屏使用抽屉菜单；宽屏恢复用户手动展开
  let autoCollapsed = false;
  function syncResponsive() {
    const narrow = shouldAutoCollapseSidebar(window.innerWidth);
    isNarrow = narrow;
    if (narrow) {
      if (!$collapsed) {
        collapsed.update((value) => !value);
        autoCollapsed = true;
      }
    } else {
      mobileOpen = false;
      if (autoCollapsed) {
        if ($collapsed) collapsed.update((value) => !value);
        autoCollapsed = false;
      }
    }
  }

  onMount(() => {
    syncResponsive();
    window.addEventListener('resize', syncResponsive);
    return () => window.removeEventListener('resize', syncResponsive);
  });

  onDestroy(() => window.removeEventListener('resize', syncResponsive));
</script>

{#if page.url.pathname.startsWith('/login')}
  <slot />
{:else}
  <div class="flex h-screen overflow-hidden">
    <!-- Sidebar（宽屏） -->
    {#if !isNarrow}
      <aside class="flex flex-col transition-all duration-300 overflow-y-auto"
        style="width: {$collapsed ? '64px' : '240px'}; background: var(--bg-sidebar)">
        <div class="flex items-center h-14 px-4 text-white font-bold text-lg border-b border-white/10">
          {#if !$collapsed}<span>{$siteTitle}</span>{/if}
        </div>
        <nav class="flex-1 py-2">
          {#each $menus as item}
            {#if item.children?.length}
              <div>
                <button class="flex items-center w-full px-4 py-2.5 text-sm text-gray-300 hover:bg-white/10 transition-colors"
                  onclick={() => toggleKey(item.name ?? '')}>
                  <span class="flex-1 text-left">{$collapsed ? '' : (item.displayName || item.name)}</span>
                  {#if !$collapsed}
                    <svg class="w-4 h-4 transition-transform" class:rotate-90={openKeys[item.name ?? '']} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                    </svg>
                  {/if}
                </button>
                {#if openKeys[item.name ?? ''] && !$collapsed}
                  {#each item.children as child}
                    <a href={buildUrl(child)} class="flex items-center pl-10 pr-4 py-2 text-sm text-gray-400 hover:bg-white/10 hover:text-white transition-colors {page.url.pathname === buildUrl(child) ? 'text-white bg-white/10' : ''}">
                      {child.displayName || child.name}
                    </a>
                  {/each}
                {/if}
              </div>
            {:else}
              <a href={buildUrl(item)} class="flex items-center px-4 py-2.5 text-sm text-gray-300 hover:bg-white/10 transition-colors {page.url.pathname === buildUrl(item) ? 'text-white bg-white/10' : ''}">
                {$collapsed ? '' : (item.displayName || item.name)}
              </a>
            {/if}
          {/each}
        </nav>
      </aside>
    {/if}

    <!-- Sidebar（窄屏抽屉） -->
    {#if isNarrow && mobileOpen}
      <div class="fixed inset-0 z-40 flex">
        <button type="button" aria-label="关闭菜单" class="absolute inset-0 bg-black/40" onclick={() => (mobileOpen = false)}></button>
        <aside class="relative z-50 flex w-60 flex-col overflow-y-auto shadow-lg" style="background: var(--bg-sidebar)">
          <div class="flex items-center justify-between h-14 px-4 text-white font-bold text-lg border-b border-white/10">
            <span>{$siteTitle}</span>
            <button type="button" class="text-white/80 hover:text-white" onclick={() => (mobileOpen = false)}>✕</button>
          </div>
          <nav class="flex-1 py-2">
            {#each $menus as item}
              {#if item.children?.length}
                <div>
                  <button class="flex items-center w-full px-4 py-2.5 text-sm text-gray-300 hover:bg-white/10 transition-colors"
                    onclick={() => toggleKey(item.name ?? '')}>
                    <span class="flex-1 text-left">{item.displayName || item.name}</span>
                    <svg class="w-4 h-4 transition-transform" class:rotate-90={openKeys[item.name ?? '']} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                    </svg>
                  </button>
                  {#if openKeys[item.name ?? '']}
                    {#each item.children as child}
                      <a href={buildUrl(child)} class="flex items-center pl-10 pr-4 py-2 text-sm text-gray-400 hover:bg-white/10 hover:text-white transition-colors">
                        {child.displayName || child.name}
                      </a>
                    {/each}
                  {/if}
                </div>
              {:else}
                <a href={buildUrl(item)} class="flex items-center px-4 py-2.5 text-sm text-gray-300 hover:bg-white/10 transition-colors">
                  {item.displayName || item.name}
                </a>
              {/if}
            {/each}
          </nav>
        </aside>
      </div>
    {/if}

    <!-- Main -->
    <div class="flex-1 flex flex-col overflow-hidden">
      <!-- Header -->
      <header class="flex items-center h-14 px-4 border-b" style="border-color: var(--border); background: var(--bg)">
        <button class="p-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-800" onclick={() => isNarrow ? (mobileOpen = true) : collapsed.update(v => !v)}>
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>

        <div class="flex-1"></div>

        <!-- Dark mode toggle -->
        <button class="p-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-800 mr-3" onclick={() => darkMode.update(v => !v)}>
          {#if $darkMode}
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"/></svg>
          {:else}
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"/></svg>
          {/if}
        </button>

        <!-- User -->
        <div class="relative">
          <button class="flex items-center gap-2 px-3 py-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-800"
            onclick={() => showUser = !showUser}>
            <span class="text-sm">{$displayName}</span>
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
          </button>
          {#if showUser}
            <div class="absolute right-0 top-full mt-1 w-40 rounded-md shadow-lg border z-50" style="background: var(--bg); border-color: var(--border)">
              <button class="w-full text-left px-4 py-2 text-sm hover:bg-gray-100 dark:hover:bg-gray-800"
                onclick={handleLogout}>退出登录</button>
            </div>
          {/if}
        </div>
      </header>

      <!-- Content -->
      <main class="flex-1 overflow-auto p-3 sm:p-6" style="background: var(--bg-secondary)">
        {#if tabs.length > 1}
          <div class="mb-3 flex flex-wrap items-center gap-2">
            {#each tabs as t}
              <span
                class="inline-flex items-center gap-1 rounded-full border px-3 py-1 text-sm"
                style={t.id === activeTabId ? 'border-color: var(--primary); background: var(--primary); color: #fff' : 'border-color: var(--border)'}
              >
                <button type="button" onclick={() => activateTab(t)}>{t.title}</button>
                {#if t.closable}
                  <button type="button" class="text-xs" onclick={() => closeTabAndNavigate(t.id)}>×</button>
                {/if}
              </span>
            {/each}
            <button type="button" class="text-sm underline" onclick={closeAll}>关闭全部</button>
          </div>
        {/if}
        <slot />
      </main>
    </div>
  </div>
{/if}
