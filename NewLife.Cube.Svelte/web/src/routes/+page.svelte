<script lang="ts">
  import { siteTitle } from '$lib/stores/app';
  import { displayName } from '$lib/stores/user';
</script>

<div class="mb-6">
  <h2 class="text-2xl font-bold">欢迎回来，{$displayName}</h2>
  <p class="mt-1 text-sm" style="color: var(--text-secondary)">{$siteTitle} 管理控制台</p>
</div>

<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
  {#each [
    { label: '用户数', value: '-', color: 'text-indigo-600' },
    { label: '角色数', value: '-', color: 'text-emerald-600' },
    { label: '在线数', value: '-', color: 'text-amber-600' },
    { label: '今日访问', value: '-', color: 'text-rose-600' },
  ] as stat}
    <div class="card">
      <p class="text-sm" style="color: var(--text-secondary)">{stat.label}</p>
      <p class="text-3xl font-bold mt-2 {stat.color}">{stat.value}</p>
    </div>
  {/each}
</div>
