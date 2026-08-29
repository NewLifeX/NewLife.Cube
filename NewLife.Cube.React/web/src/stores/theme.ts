/**
 * 主题 Store（对齐 Vue 皮肤 composables/useTheme.ts）
 *
 * 4 个主题家族 × 明暗模式，持久化到 localStorage。
 */
import { create } from 'zustand';
import { getConfig } from '@/configure';

/** 主题家族 */
export type ThemeFamily = 'cyber' | 'forest' | 'aurora' | 'industrial';
export type ThemeMode = 'light' | 'dark';

export interface ThemeOption {
  id: string;
  label: string;
  icon: string;
  description: string;
  family: ThemeFamily;
  mode: ThemeMode;
}

export interface ThemeGroup {
  id: ThemeFamily;
  label: string;
  icon: string;
  variants: ThemeOption[];
}

/** 主题分组（每个 family 有 light/dark 两个变体，对齐 Vue THEME_GROUPS） */
export const THEME_GROUPS: ThemeGroup[] = [
  {
    id: 'cyber',
    label: 'Cyber 赛博',
    icon: '◉',
    variants: [
      { id: 'cyber-dark', label: '赛博深色', icon: '🌙', description: '赛博深色主题', family: 'cyber', mode: 'dark' },
      { id: 'cyber-light', label: '赛博浅色', icon: '☀️', description: '赛博浅色主题', family: 'cyber', mode: 'light' },
    ],
  },
  {
    id: 'forest',
    label: 'Forest 森林绿',
    icon: '🌿',
    variants: [
      { id: 'forest-dark', label: '森林深色', icon: '🌙', description: '森林绿深色主题', family: 'forest', mode: 'dark' },
      { id: 'forest-light', label: '森林浅色', icon: '☀️', description: '森林绿浅色主题', family: 'forest', mode: 'light' },
    ],
  },
  {
    id: 'aurora',
    label: 'Aurora 极光蓝绿',
    icon: '🌌',
    variants: [
      { id: 'aurora-dark', label: '极光深色', icon: '🌙', description: '极光蓝绿深色主题', family: 'aurora', mode: 'dark' },
      { id: 'aurora-light', label: '极光浅色', icon: '☀️', description: '极光蓝绿浅色主题', family: 'aurora', mode: 'light' },
    ],
  },
  {
    id: 'industrial',
    label: 'Industrial 工业科技',
    icon: '⚙️',
    variants: [
      { id: 'industrial-dark', label: '工业深色', icon: '🌙', description: '工业科技深色主题', family: 'industrial', mode: 'dark' },
      { id: 'industrial-light', label: '工业浅色', icon: '☀️', description: '工业科技浅色主题', family: 'industrial', mode: 'light' },
    ],
  },
];

export const THEMES: ThemeOption[] = THEME_GROUPS.flatMap((g) => g.variants);

const THEME_KEY = 'cube-react-theme';

interface ThemeState {
  family: ThemeFamily;
  mode: ThemeMode;
  setFamily: (family: ThemeFamily) => void;
  setMode: (mode: ThemeMode) => void;
  toggleMode: () => void;
}

function loadTheme(): { family: ThemeFamily; mode: ThemeMode } {
  const cfg = getConfig().theme;
  try {
    const saved = localStorage.getItem(THEME_KEY);
    if (saved) {
      const parsed = JSON.parse(saved) as { family: ThemeFamily; mode: ThemeMode };
      return {
        family: parsed.family ?? (cfg.defaultTheme as ThemeFamily),
        mode: parsed.mode ?? cfg.defaultMode,
      };
    }
  } catch {
    // ignore
  }
  return { family: cfg.defaultTheme as ThemeFamily, mode: cfg.defaultMode };
}

export const useThemeStore = create<ThemeState>((set, get) => ({
  ...loadTheme(),
  setFamily: (family) => {
    set({ family });
    localStorage.setItem(THEME_KEY, JSON.stringify({ family, mode: get().mode }));
  },
  setMode: (mode) => {
    set({ mode });
    localStorage.setItem(THEME_KEY, JSON.stringify({ family: get().family, mode }));
  },
  toggleMode: () => {
    const mode = get().mode === 'dark' ? 'light' : 'dark';
    set({ mode });
    localStorage.setItem(THEME_KEY, JSON.stringify({ family: get().family, mode }));
  },
}));

export default useThemeStore;
