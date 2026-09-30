/**
 * ECharts 主题：按魔方设置 EChartsTheme 注册并供 init 使用。
 * default / 空 → 不传主题名；dark 走 ESM；其余官方主题经 UMD 侧载注册。
 * 库本身在首次 init / ensure 时动态 import，列表页不提前加载。
 */

const OFFICIAL = new Set(['dark', 'vintage', 'macarons', 'infographic', 'shine', 'roma']);
const registered = new Set<string>();

type EChartsModule = typeof import('echarts');
export type EChartsHandle = import('echarts').ECharts;

let loading: Promise<EChartsModule> | null = null;

function loadEcharts(): Promise<EChartsModule> {
  loading ??= import('echarts');
  return loading;
}

function normalizeThemeName(raw: string | null | undefined): string {
  return (raw ?? '').trim();
}

/** 确保主题已 registerTheme；返回可传给 echarts.init 的主题名（default→undefined） */
export async function ensureEchartsTheme(
  raw: string | null | undefined,
): Promise<string | undefined> {
  const name = normalizeThemeName(raw);
  if (!name || name.toLowerCase() === 'default') return undefined;
  if (!OFFICIAL.has(name)) return undefined;
  if (registered.has(name)) return name;

  const echarts = await loadEcharts();
  if (name === 'dark') {
    // echarts 未导出 theme 的类型声明
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-expect-error no types for echarts/lib/theme/dark.js
    const mod = await import('echarts/lib/theme/dark.js');
    echarts.registerTheme('dark', (mod as { default: object }).default);
  } else {
    // UMD 主题依赖 globalThis.echarts
    const g = globalThis as typeof globalThis & { echarts?: EChartsModule };
    g.echarts = echarts;
    await import(/* @vite-ignore */ `echarts/theme/${name}.js`);
  }
  registered.add(name);
  return name;
}

/** 同步取已注册主题名（未 ensure 过则回落 undefined，避免阻塞首帧） */
export function peekEchartsTheme(raw: string | null | undefined): string | undefined {
  const name = normalizeThemeName(raw);
  if (!name || name.toLowerCase() === 'default') return undefined;
  return registered.has(name) ? name : undefined;
}

export async function initEcharts(
  el: HTMLElement,
  themeName?: string | null,
): Promise<EChartsHandle> {
  const echarts = await loadEcharts();
  const t = peekEchartsTheme(themeName);
  return echarts.init(el, t);
}
