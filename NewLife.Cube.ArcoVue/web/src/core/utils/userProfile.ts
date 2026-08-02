import type { UserProfileModel } from '@cube/api-core';

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

function mergeLayout(partial?: Partial<LayoutPrefs> | Record<string, unknown> | null): LayoutPrefs {
  const d = SYSTEM_DEFAULT_PROFILE.layout;
  const p = (partial || {}) as Record<string, unknown>;
  return {
    mode: resolveLayoutMode(p.mode ?? d.mode),
    siderCollapsed: typeof p.siderCollapsed === 'boolean' ? p.siderCollapsed : d.siderCollapsed,
    siderWidth:
      typeof p.siderWidth === 'number' && p.siderWidth >= 48 ? p.siderWidth : d.siderWidth,
    showTabs: typeof p.showTabs === 'boolean' ? p.showTabs : d.showTabs,
    contentWidth: resolveContentWidth(p.contentWidth ?? d.contentWidth),
  };
}

function mergeTheme(partial?: Partial<ThemePrefs> | Record<string, unknown> | null): ThemePrefs {
  const d = SYSTEM_DEFAULT_PROFILE.theme;
  const p = (partial || {}) as Record<string, unknown>;
  return {
    appearance: resolveAppearance(p.appearance),
    primaryColor: typeof p.primaryColor === 'string' && p.primaryColor ? p.primaryColor : d.primaryColor,
    radius: resolveRadius(p.radius),
    density: resolveDensity(p.density),
    fontScale: resolveFontScale(p.fontScale),
  };
}

function mergeWorkspace(
  partial?: Partial<WorkspacePrefs> | Record<string, unknown> | null,
): WorkspacePrefs {
  const d = SYSTEM_DEFAULT_PROFILE.workspace;
  const p = (partial || {}) as Record<string, unknown>;
  return {
    defaultView: typeof p.defaultView === 'string' && p.defaultView ? p.defaultView : d.defaultView,
    pageSize: typeof p.pageSize === 'number' && p.pageSize > 0 ? p.pageSize : d.pageSize,
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

/** 线缆模型 → 前端偏好（JSON 列 parse + 默认回落） */
export function prefsFromWire(model: UserProfileModel | null | undefined): UserProfilePrefs {
  if (!model) return mergeProfile(null);
  return {
    layout: mergeLayout(parseJsonObject(model.layoutJson)),
    theme: mergeTheme(parseJsonObject(model.themeJson)),
    workspace: mergeWorkspace(parseJsonObject(model.workspaceJson)),
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
