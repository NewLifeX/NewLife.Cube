<script lang="ts">
  import type { DataField } from '@newlifex/cube';
  import { resolveWidget } from '@newlifex/cube';

  interface Props {
    field: DataField;
    value: any;
    onchange?: (v: any) => void;
  }

  let { field, value = $bindable(), onchange }: Props = $props();

  const wt = $derived(resolveWidget(field).widget);

  const options = $derived.by(() => {
    if (!field.dataSource) return [];
    return Object.entries(field.dataSource).map(([v, l]) => ({ value: v, label: l }));
  });

  function emit(v: any) {
    value = v;
    onchange?.(v);
  }
</script>

{#if wt === 'switch'}
  <label class="relative inline-flex items-center cursor-pointer">
    <input type="checkbox" checked={!!value} onchange={(e) => emit(e.currentTarget.checked)} class="sr-only peer" />
    <div class="w-10 h-5 bg-gray-300 rounded-full peer peer-checked:bg-indigo-500 after:content-[''] after:absolute after:top-0.5 after:left-[2px] after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:after:translate-x-5"></div>
  </label>
{:else if wt === 'select' || wt === 'lovMulti'}
  <select class="input" value={value ?? ''} onchange={(e) => emit(e.currentTarget.value)}>
    <option value="">请选择{field.displayName || field.name}</option>
    {#each options as opt}
      <option value={opt.value}>{opt.label}</option>
    {/each}
  </select>
{:else if wt === 'number'}
  <input type="number" step={field.precision ? '0.01' : undefined} class="input" value={value ?? ''} oninput={(e) => emit(Number(e.currentTarget.value))}
    placeholder="请输入{field.displayName || field.name}" />
{:else if wt === 'datetime'}
  <input type="datetime-local" class="input" value={value ?? ''} onchange={(e) => emit(e.currentTarget.value)}
    placeholder="请选择{field.displayName || field.name}" />
{:else if wt === 'date'}
  <input type="date" class="input" value={value ?? ''} onchange={(e) => emit(e.currentTarget.value)}
    placeholder="请选择{field.displayName || field.name}" />
{:else if wt === 'textarea' || wt === 'html'}
  <textarea class="input min-h-[80px]" value={value ?? ''} oninput={(e) => emit(e.currentTarget.value)}
    placeholder="请输入{field.displayName || field.name}"></textarea>
{:else if wt === 'password'}
  <input type="password" class="input" value={value ?? ''} oninput={(e) => emit(e.currentTarget.value)}
    placeholder="请输入{field.displayName || field.name}" />
{:else if wt === 'readonly'}
  <span class="text-sm" style="color: var(--text-secondary)">{value ?? '-'}</span>
{:else}
  <input type="text" class="input" value={value ?? ''} oninput={(e) => emit(e.currentTarget.value)}
    placeholder="请输入{field.displayName || field.name}" />
{/if}
