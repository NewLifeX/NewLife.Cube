<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { page } from '$app/state';
  import { collapsed, darkMode, siteInfo, siteTitle } from '$lib/stores/app';
  import { user, menus, isLoggedIn, displayName, fetchUserInfo, fetchMenus, logout } from '$lib/stores/user';
  import { getApi } from '$lib/api';
  import type { MenuItem } from '@cube/api-core';
  import '../app.css';

  let openKeys: Record<string, boolean> = $state({});
  let showUser = $state(false);

  onMount(async () => {
    try {
      const [siteRes] = await Promise.all([getApi().config.getSiteInfo()]);
      if (siteRes?.data) siteInfo.set(siteRes.data);
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
</script>

{#if page.url.pathname.startsWith('/login')}
  <slot />
{:else}
  <div class="flex h-screen overflow-hidden">
    <!-- Sidebar -->
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

    <!-- Main -->
    <div class="flex-1 flex flex-col overflow-hidden">
      <!-- Header -->
      <header class="flex items-center h-14 px-4 border-b" style="border-color: var(--border); background: var(--bg)">
        <button class="p-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-800" onclick={() => collapsed.update(v => !v)}>
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
      <main class="flex-1 overflow-auto p-6" style="background: var(--bg-secondary)">
        <slot />
      </main>
    </div>
  </div>
{/if}
