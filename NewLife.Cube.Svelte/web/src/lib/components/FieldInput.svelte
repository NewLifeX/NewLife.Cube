<script lang="ts">
  /**
   * 字段渲染组件（Svelte 皮肤）
   *
   * 控件类型一律由共享规则引擎 `resolveWidget` 决定，本组件只负责把它们映射到原生控件：
   * 与 Vue/React/Angular 皮肤同一套 ItemType 口径（密码/颜色/时长/地区/映射下拉/多选/日期…）。
   */
  import type { AreaOption, DataField, TimeSpanParts, TimeSpanUnit } from '@newlifex/cube';
  import { createAreaCascade, joinTimeSpan, resolveWidget, splitTimeSpan, timeSpanUnitOfDescription } from '@newlifex/cube';
  import { getApi } from '$lib/api';

  interface Props {
    field: DataField;
    value: any;
    onchange?: (v: any) => void;
  }

  let { field, value = $bindable(), onchange }: Props = $props();

  const mapping = $derived(resolveWidget(field));
  const wt = $derived(mapping.widget);
  const label = $derived(field.displayName || field.name);

  const isSelect = $derived(['select', 'lov', 'lovMulti', 'mapSelect'].includes(wt));
  const isMulti = $derived(wt === 'lovMulti' || !!mapping.props?.multiple);
  const isTextarea = $derived(['textarea', 'html', 'markdown', 'code', 'json'].includes(wt));

  const options = $derived.by(() => {
    const ds = (mapping.props?.dataSource ?? field.dataSource) as Record<string, string> | undefined;
    if (!ds) return [] as { value: string; label: string }[];
    return Object.entries(ds).map(([v, l]) => ({ value: v, label: l }));
  });

  const inputType = $derived(
    wt === 'password' ? 'password' : wt === 'email' ? 'email' : wt === 'phone' ? 'tel' : wt === 'url' ? 'url' : 'text',
  );

  const nativeDateType = $derived(wt === 'datetime' ? 'datetime-local' : wt === 'time' ? 'time' : 'date');

  function emit(v: any) {
    value = v;
    onchange?.(v);
  }

  /** 多选值归一：数组 / 逗号串 / JSON 串 */
  function toMultiArray(v: any): string[] {
    if (v == null || v === '') return [];
    if (Array.isArray(v)) return v.map(String);
    const text = String(v).trim();
    if (!text) return [];
    try {
      const parsed = JSON.parse(text);
      if (Array.isArray(parsed)) return parsed.map(String);
    } catch {
      /* 非 JSON，按逗号串处理 */
    }
    return text.split(',').filter(Boolean);
  }

  const multiValue = $derived(toMultiArray(value));

  function onMultiChange(event: Event) {
    const select = event.currentTarget as HTMLSelectElement;
    const picked = Array.from(select.selectedOptions).map((o) => o.value);
    emit(picked.join(','));
  }

  // ── 时长 ──
  const spanUnit: TimeSpanUnit = $derived(
    (mapping.props?.unit as TimeSpanUnit) ?? timeSpanUnitOfDescription(field.description),
  );
  let spanParts = $state<TimeSpanParts>(splitTimeSpan(value, spanUnit));

  $effect(() => {
    spanParts = splitTimeSpan(value, spanUnit);
  });

  const spanPartsList = $derived([
    { key: 'hours' as const, value: spanParts.hours, max: undefined, step: 1, placeholder: '时' },
    { key: 'minutes' as const, value: spanParts.minutes, max: 59, step: 1, placeholder: '分' },
    {
      key: 'seconds' as const,
      value: spanParts.seconds,
      max: 59,
      step: spanUnit === 'milliseconds' ? 0.001 : 1,
      placeholder: '秒',
    },
  ]);

  function onSpanChange(key: keyof TimeSpanParts, event: Event) {
    const next = { ...spanParts, [key]: Number((event.currentTarget as HTMLInputElement).value || 0) };
    spanParts = next;
    emit(joinTimeSpan(next, spanUnit));
  }

  // ── 地区级联 ──
  let areaOptions = $state<AreaOption[][]>([]);
  let areaPath = $state<number[]>([]);
  let areaCascade: ReturnType<typeof createAreaCascade> | null = null;

  function applyArea(state: { options: AreaOption[][]; path: number[] }) {
    areaOptions = state.options;
    areaPath = state.path;
  }

  async function initArea(initial: any) {
    areaCascade ??= createAreaCascade(getApi().area, Number(mapping.props?.areaLevel ?? 3) || 3);
    const n = Number(initial ?? 0);
    applyArea(await areaCascade.init(Number.isFinite(n) ? n : 0));
  }

  async function onAreaChange(level: number, event: Event) {
    if (!areaCascade) return;
    const raw = (event.currentTarget as HTMLSelectElement).value;
    const state = await areaCascade.selectLevel(level, raw === '' ? null : Number(raw));
    applyArea(state);
    emit(state.value ?? '');
  }

  $effect(() => {
    if (wt !== 'area') return;
    const initial = value;
    if (!areaCascade) void initArea(initial);
  });
</script>

{#if wt === 'area'}
  <div class="flex flex-col gap-1">
    {#each areaOptions as levelOptions, level}
      <select class="input" value={areaPath[level] ?? ''} onchange={(e) => onAreaChange(level, e)}>
        <option value="">{level === 0 ? '请选择省份' : '请选择下级'}</option>
        {#each levelOptions as opt}
          <option value={opt.id}>{opt.name}</option>
        {/each}
      </select>
    {/each}
  </div>
{:else if wt === 'timeSpan'}
  <div class="flex gap-1">
    {#each spanPartsList as part}
      <input
        type="number"
        class="input"
        min="0"
        max={part.max}
        step={part.step}
        value={part.value}
        placeholder={part.placeholder}
        oninput={(e) => onSpanChange(part.key, e)}
      />
    {/each}
  </div>
{:else if wt === 'switch'}
  <label class="relative inline-flex items-center cursor-pointer">
    <input type="checkbox" checked={!!value} onchange={(e) => emit(e.currentTarget.checked)} class="sr-only peer" />
    <div class="w-10 h-5 bg-gray-300 rounded-full peer peer-checked:bg-indigo-500 after:content-[''] after:absolute after:top-0.5 after:left-[2px] after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:after:translate-x-5"></div>
  </label>
{:else if isSelect && isMulti}
  <select class="input" multiple size={Math.min(6, Math.max(2, options.length))} value={multiValue} onchange={onMultiChange}>
    {#each options as opt}
      <option value={opt.value} selected={multiValue.includes(opt.value)}>{opt.label}</option>
    {/each}
  </select>
{:else if isSelect}
  <select class="input" value={value ?? ''} onchange={(e) => emit(e.currentTarget.value)}>
    <option value="">请选择{label}</option>
    {#each options as opt}
      <option value={opt.value}>{opt.label}</option>
    {/each}
  </select>
{:else if wt === 'number' || wt === 'mapInput'}
  <input
    type="number"
    step={wt === 'number' && field.scale ? 10 ** -(field.scale ?? 0) : 1}
    class="input"
    value={value ?? ''}
    oninput={(e) => emit(Number(e.currentTarget.value))}
    placeholder="请输入{label}" />
{:else if wt === 'date' || wt === 'datetime' || wt === 'time'}
  <input type={nativeDateType} class="input" value={value ?? ''} onchange={(e) => emit(e.currentTarget.value)} />
{:else if isTextarea}
  <textarea class="input min-h-[80px]" value={value ?? ''} oninput={(e) => emit(e.currentTarget.value)}
    placeholder="请输入{label}"></textarea>
{:else if wt === 'color'}
  <div class="flex items-center gap-2">
    <input type="color" value={value || '#000000'} oninput={(e) => emit(e.currentTarget.value)} class="h-8 w-10" />
    <input type="text" class="input" value={value ?? ''} oninput={(e) => emit(e.currentTarget.value)}
      placeholder="请输入颜色值，如 #4f46e5" />
  </div>
{:else if wt === 'image'}
  <div class="flex items-center gap-2">
    {#if value}
      <img src={value} alt="" class="h-12 w-12 rounded border object-cover" />
    {/if}
    <input type="text" class="input" value={value ?? ''} oninput={(e) => emit(e.currentTarget.value)}
      placeholder="图片地址" />
  </div>
{:else if wt === 'file'}
  <input type="text" class="input" value={value ?? ''} oninput={(e) => emit(e.currentTarget.value)}
    placeholder="附件地址" />
{:else if wt === 'readonly' || wt === 'link'}
  <span class="text-sm" style="color: var(--text-secondary)">{value ?? '-'}</span>
{:else}
  <input type={inputType} class="input" value={value ?? ''} oninput={(e) => emit(e.currentTarget.value)}
    placeholder="请输入{label}" />
{/if}
