<script lang="ts">
  import type { DataField } from '@cube/api-core';
  import { resolveWidget, type WidgetType } from '@cube/field-mapping';

  interface Props {
    field: DataField;
    value: any;
    onchange?: (v: any) => void;
  }

  let { field, value = $bindable(), onchange }: Props = $props();

  const wt = $derived(resolveWidget(field).widget);

  const options = $derived.by(() => {
    if (!field.dataSource) return [];
    try {
      const map = JSON.parse(field.dataSource) as Record<string, string>;
      return Object.entries(map).map(([v, l]) => ({ value: v, label: l }));
    } catch {
      return [];
    }
  });

  function emit(v: any) {
    value = v;
    onchange?.(v);
  }
</script>

{#if wt === WidgetType.Switch}
  <label class="relative inline-flex items-center cursor-pointer">
    <input type="checkbox" checked={!!value} onchange={(e) => emit(e.currentTarget.checked)} class="sr-only peer" />
    <div class="w-10 h-5 bg-gray-300 rounded-full peer peer-checked:bg-indigo-500 after:content-[''] after:absolute after:top-0.5 after:left-[2px] after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:after:translate-x-5"></div>
  </label>
{:else if wt === WidgetType.Select || wt === WidgetType.TagList}
  <select class="input" value={value ?? ''} onchange={(e) => emit(e.currentTarget.value)}>
    <option value="">请选择{field.displayName || field.name}</option>
    {#each options as opt}
      <option value={opt.value}>{opt.label}</option>
    {/each}
  </select>
{:else if wt === WidgetType.Number}
  <input type="number" class="input" value={value ?? ''} oninput={(e) => emit(Number(e.currentTarget.value))}
    placeholder="请输入{field.displayName || field.name}" />
{:else if wt === WidgetType.Decimal}
  <input type="number" step="0.01" class="input" value={value ?? ''} oninput={(e) => emit(Number(e.currentTarget.value))}
    placeholder="请输入{field.displayName || field.name}" />
{:else if wt === WidgetType.DateTime}
  <input type="datetime-local" class="input" value={value ?? ''} onchange={(e) => emit(e.currentTarget.value)}
    placeholder="请选择{field.displayName || field.name}" />
{:else if wt === WidgetType.Date}
  <input type="date" class="input" value={value ?? ''} onchange={(e) => emit(e.currentTarget.value)}
    placeholder="请选择{field.displayName || field.name}" />
{:else if wt === WidgetType.TextArea || wt === WidgetType.RichText}
  <textarea class="input min-h-[80px]" value={value ?? ''} oninput={(e) => emit(e.currentTarget.value)}
    placeholder="请输入{field.displayName || field.name}"></textarea>
{:else if wt === WidgetType.Password}
  <input type="password" class="input" value={value ?? ''} oninput={(e) => emit(e.currentTarget.value)}
    placeholder="请输入{field.displayName || field.name}" />
{:else if wt === WidgetType.ReadOnly}
  <span class="text-sm" style="color: var(--text-secondary)">{value ?? '-'}</span>
{:else}
  <input type="text" class="input" value={value ?? ''} oninput={(e) => emit(e.currentTarget.value)}
    placeholder="请输入{field.displayName || field.name}" />
{/if}
