<script lang="ts">
  import { goto } from '$app/navigation';
  import { login, fetchMenus } from '$lib/stores/user';
  import { siteInfo, siteTitle } from '$lib/stores/app';
  import { getApi } from '$lib/api';
  import { onMount } from 'svelte';
  import type { LoginConfig } from '@cube/api-core';

  let username = $state('');
  let password = $state('');
  let loading = $state(false);
  let error = $state('');
  let loginConfig = $state<LoginConfig | null>(null);

  onMount(async () => {
    try {
      const [siteRes, configRes] = await Promise.all([
        getApi().config.getSiteInfo(),
        getApi().config.getLoginConfig(),
      ]);
      if (siteRes?.data) siteInfo.set(siteRes.data);
      loginConfig = configRes?.data ?? null;
    } catch { /* ignore */ }
  });

  async function handleLogin() {
    if (!username || !password) { error = '请输入用户名和密码'; return; }
    loading = true;
    error = '';
    try {
      const ok = await login(username, password);
      if (ok) {
        await fetchMenus();
        goto('/');
      } else {
        error = '用户名或密码错误';
      }
    } catch {
      error = '登录失败，请检查网络';
    } finally {
      loading = false;
    }
  }
</script>

<div class="min-h-screen flex items-center justify-center" style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%)">
  <div class="w-full max-w-md mx-4">
    <div class="card">
      <h1 class="text-2xl font-bold text-center mb-6">{$siteTitle}</h1>

      {#if error}
        <div class="mb-4 p-3 rounded-md bg-red-50 text-red-600 text-sm dark:bg-red-900/30 dark:text-red-400">{error}</div>
      {/if}

      <form onsubmit={(e) => { e.preventDefault(); handleLogin(); }} class="space-y-4">
        <div>
          <label class="block text-sm font-medium mb-1" style="color: var(--text-secondary)">用户名</label>
          <input class="input" type="text" bind:value={username} placeholder="请输入用户名" autocomplete="username" />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1" style="color: var(--text-secondary)">密码</label>
          <input class="input" type="password" bind:value={password} placeholder="请输入密码" autocomplete="current-password" />
        </div>
        <button class="btn btn-primary w-full" type="submit" disabled={loading}>
          {loading ? '登录中...' : '登录'}
        </button>
      </form>

      {#if loginConfig?.oauthItems?.length}
        <div class="mt-6">
          <div class="relative">
            <div class="absolute inset-0 flex items-center"><div class="w-full border-t" style="border-color: var(--border)"></div></div>
            <div class="relative flex justify-center text-sm"><span class="px-2" style="background: var(--bg); color: var(--text-secondary)">第三方登录</span></div>
          </div>
          <div class="flex justify-center gap-3 mt-4">
            {#each loginConfig.oauthItems as item}
              <a href={item.url} class="btn btn-outline btn-sm">{item.name}</a>
            {/each}
          </div>
        </div>
      {/if}
    </div>
  </div>
</div>
