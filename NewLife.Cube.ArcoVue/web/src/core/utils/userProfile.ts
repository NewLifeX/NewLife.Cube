import type { UserProfileModel } from '@cube/api-core';
import { normalizeAiFab, normalizeAiPanel, type AiFabPos, type AiPanelPref } from './aiFab';

export type LayoutMode = 'side' | 'top' | 'mix';
/** 内容区宽度：标准 / 较宽 / 流式（适配不同分辨率） */
export type ContentWidth = 'standard' | 'wide' | 'fluid';
export type Appearance = 'light' | 'dark' | 'system';
export type Density = 'default' | 'compact';

export interface LayoutPrefs {
  mode: LayoutMode;
  siderCollapsed: boolean;
  siderWidth: number;
  showTabs: boolean;
  contentWidth: ContentWidth;
}

export interface ThemePrefs {
  appearance: Appearance;
  primaryColor: string;
  radius: number;
  density: Density;
  fontScale: number;
}

export interface WorkspacePrefs {
  defaultView: string;
  pageSize: number;
  /** AI 悬浮球视口坐标（left/top）；缺省则右下角 */
  aiFab?: AiFabPos;
  /** AI 卡片浮窗位置与尺寸；缺省贴悬浮球 / 默认卡片 */
  aiPanel?: AiPanelPref;
}

export interface UserProfilePrefs {
  layout: LayoutPrefs;
  theme: ThemePrefs;
  workspace: WorkspacePrefs;
}

export const SYSTEM_DEFAULT_PROFILE: UserProfilePrefs = {
  layout: {
    mode: 'side',
    siderCollapsed: false,
    siderWidth: 220,
    showTabs: true,
    contentWidth: 'fluid',
  },
  theme: {
    appearance: 'light',
    primaryColor: '#165DFF',
    radius: 4,
    density: 'default',
    fontScale: 1,
  },
  workspace: {
    defaultView: 'table',
    pageSize: 20,
  },
};

const LAYOUT_MODES: LayoutMode[] = ['side', 'top', 'mix'];
const CONTENT_WIDTHS: ContentWidth[] = ['standard', 'wide', 'fluid'];
const APPEARANCES: Appearance[] = ['light', 'dark', 'system'];
const DENSITIES: Density[] = ['default', 'compact'];

export function parseJsonObject(raw: string | null | undefined): Record<string, unknown> | null {
  if (!raw || typeof raw !== 'string') return null;
  try {
    const v = JSON.parse(raw) as unknown;
    return v && typeof v === 'object' && !Array.isArray(v) ? (v as Record<string, unknown>) : null;
  } catch {
    return null;
  }
}

export function resolveLayoutMode(mode: unknown): LayoutMode {
  if (typeof mode === 'string' && LAYOUT_MODES.includes(mode as LayoutMode)) {
    return mode as LayoutMode;
  }
  return 'side';
}

/** 旧值 `fixed` 迁移为 `standard`；非法回落 fluid */
export function resolveContentWidth(raw: unknown): ContentWidth {
  if (raw === 'fixed') return 'standard';
  if (typeof raw === 'string' && CONTENT_WIDTHS.includes(raw as ContentWidth)) {
    return raw as ContentWidth;
  }
  return SYSTEM_DEFAULT_PROFILE.layout.contentWidth;
}

function resolveAppearance(v: unknown): Appearance {
  if (typeof v === 'string' && APPEARANCES.includes(v as Appearance)) return v as Appearance;
  return SYSTEM_DEFAULT_PROFILE.theme.appearance;
}

function resolveDensity(v: unknown): Density {
  if (v === 'comfortable') return 'default';
  if (typeof v === 'string' && DENSITIES.includes(v as Density)) return v as Density;
  return SYSTEM_DEFAULT_PROFILE.theme.density;
}

function resolveRadius(v: unknown): number {
  if (typeof v === 'number' && Number.isFinite(v)) return v;
  if (v === 'sm') return 2;
  if (v === 'md') return 4;
  if (v === 'lg') return 8;
  return SYSTEM_DEFAULT_PROFILE.theme.radius;
}

function resolveFontScale(v: unknown): number {
  if (typeof v === 'number' && Number.isFinite(v) && v > 0) return v;
  if (v === 'normal') return 1;
  if (v === 'large') return 1.125;
  return SYSTEM_DEFAULT_PROFILE.theme.fontScale;
}

function pickIgnoreCase(obj: Record<string, unknown>, name: string): unknown {
  const want = name.toLowerCase();
  const hit = Object.keys(obj).find((k) => k.toLowerCase() === want);
  return hit !== undefined ? obj[hit] : undefined;
}

function asRecord(v: unknown): Record<string, unknown> | null {
  if (v && typeof v === 'object' && !Array.isArray(v)) return v as Record<string, unknown>;
  return null;
}

function asJsonObject(v: unknown): Record<string, unknown> | null {
  if (typeof v === 'string') return parseJsonObject(v);
  return asRecord(v);
}

function hasProfileJsonKey(obj: Record<string, unknown>): boolean {
  return Object.keys(obj).some((k) => {
    const n = k.toLowerCase();
    return n === 'layoutjson' || n === 'themejson' || n === 'workspacejson';
  });
}

/** 兼容 Axios / ApiResponse 嵌套，以及 FastJson 首字母小写（ThemeJson → tHemeJson） */
function unwrapProfileWire(raw: unknown): Record<string, unknown> | null {
  const seen = new Set<unknown>();
  let cur: unknown = raw;
  for (let i = 0; i < 4; i++) {
    const rec = asRecord(cur);
    if (!rec || seen.has(rec)) break;
    seen.add(rec);
    if (hasProfileJsonKey(rec)) return rec;
    if ('data' in rec) {
      cur = rec.data;
      continue;
    }
    return rec;
  }
  return asRecord(cur);
}

function mergeLayout(partial?: Partial<LayoutPrefs> | Record<string, unknown> | null): LayoutPrefs {
  const d = SYSTEM_DEFAULT_PROFILE.layout;
  const p = (partial || {}) as Record<string, unknown>;
  const siderWidth = pickIgnoreCase(p, 'siderWidth');
  const siderCollapsed = pickIgnoreCase(p, 'siderCollapsed');
  const showTabs = pickIgnoreCase(p, 'showTabs');
  return {
    mode: resolveLayoutMode(pickIgnoreCase(p, 'mode') ?? d.mode),
    siderCollapsed: typeof siderCollapsed === 'boolean' ? siderCollapsed : d.siderCollapsed,
    siderWidth: typeof siderWidth === 'number' && siderWidth >= 48 ? siderWidth : d.siderWidth,
    showTabs: typeof showTabs === 'boolean' ? showTabs : d.showTabs,
    contentWidth: resolveContentWidth(pickIgnoreCase(p, 'contentWidth') ?? d.contentWidth),
  };
}

function mergeTheme(partial?: Partial<ThemePrefs> | Record<string, unknown> | null): ThemePrefs {
  const d = SYSTEM_DEFAULT_PROFILE.theme;
  const p = (partial || {}) as Record<string, unknown>;
  const primaryColor = pickIgnoreCase(p, 'primaryColor');
  return {
    appearance: resolveAppearance(pickIgnoreCase(p, 'appearance')),
    primaryColor: typeof primaryColor === 'string' && primaryColor ? primaryColor : d.primaryColor,
    radius: resolveRadius(pickIgnoreCase(p, 'radius')),
    density: resolveDensity(pickIgnoreCase(p, 'density')),
    fontScale: resolveFontScale(pickIgnoreCase(p, 'fontScale')),
  };
}

function mergeWorkspace(
  partial?: Partial<WorkspacePrefs> | Record<string, unknown> | null,
): WorkspacePrefs {
  const d = SYSTEM_DEFAULT_PROFILE.workspace;
  const p = (partial || {}) as Record<string, unknown>;
  const aiFab = normalizeAiFab(pickIgnoreCase(p, 'aiFab'));
  const aiPanel = normalizeAiPanel(pickIgnoreCase(p, 'aiPanel'));
  const defaultView = pickIgnoreCase(p, 'defaultView');
  const pageSize = pickIgnoreCase(p, 'pageSize');
  return {
    defaultView: typeof defaultView === 'string' && defaultView ? defaultView : d.defaultView,
    pageSize: typeof pageSize === 'number' && pageSize > 0 ? pageSize : d.pageSize,
    ...(aiFab ? { aiFab } : {}),
    ...(aiPanel ? { aiPanel } : {}),
  };
}

/** 将局部偏好与系统默认 deep-merge；非法 mode 回落 side */
export function mergeProfile(
  partial?: Partial<UserProfilePrefs> | null,
): UserProfilePrefs {
  return {
    layout: mergeLayout(partial?.layout as unknown as Record<string, unknown>),
    theme: mergeTheme(partial?.theme as unknown as Record<string, unknown>),
    workspace: mergeWorkspace(partial?.workspace as unknown as Record<string, unknown>),
  };
}

/** 线缆模型 → 前端偏好（JSON 列 parse + 默认回落）。可传入 Axios/ApiResponse 整包。 */
export function prefsFromWire(model: UserProfileModel | null | undefined | unknown): UserProfilePrefs {
  const obj = unwrapProfileWire(model);
  if (!obj) return mergeProfile(null);
  return {
    layout: mergeLayout(asJsonObject(pickIgnoreCase(obj, 'layoutJson'))),
    theme: mergeTheme(asJsonObject(pickIgnoreCase(obj, 'themeJson'))),
    workspace: mergeWorkspace(asJsonObject(pickIgnoreCase(obj, 'workspaceJson'))),
  };
}

/** 前端偏好 → PUT body（仅 Json 字段） */
export function prefsToWirePayload(prefs: UserProfilePrefs): Partial<UserProfileModel> {
  return {
    layoutJson: JSON.stringify(prefs.layout),
    themeJson: JSON.stringify(prefs.theme),
    workspaceJson: JSON.stringify(prefs.workspace),
  };
}

export function cloneProfile(prefs: UserProfilePrefs): UserProfilePrefs {
  return {
    layout: { ...prefs.layout },
    theme: { ...prefs.theme },
    workspace: { ...prefs.workspace },
  };
}

export const PROFILE_STORAGE_KEY = 'cube.arco.userProfile';

export function loadLocalProfile(): UserProfilePrefs | null {
  try {
    const raw = localStorage.getItem(PROFILE_STORAGE_KEY);
    if (!raw) return null;
    return mergeProfile(JSON.parse(raw) as Partial<UserProfilePrefs>);
  } catch {
    return null;
  }
}

export function saveLocalProfile(prefs: UserProfilePrefs): void {
  try {
    localStorage.setItem(PROFILE_STORAGE_KEY, JSON.stringify(prefs));
  } catch {
    /* ignore quota */
  }
}

export function clearLocalProfile(): void {
  try {
    localStorage.removeItem(PROFILE_STORAGE_KEY);
  } catch {
    /* ignore */
  }
}
