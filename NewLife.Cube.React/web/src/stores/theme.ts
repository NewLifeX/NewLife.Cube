/**
 * 主题 Store（单一主题 × 明暗模式，持久化到 localStorage）
 *
 * antd6 升级后收敛为单一主题：移除 4 主题家族维度，
 * 明暗模式由 antd defaultAlgorithm / darkAlgorithm 驱动。
 */
import { create } from 'zustand';
import { getConfig } from '@/configure';

/** 明暗模式 */
export type ThemeMode = 'light' | 'dark';

const THEME_KEY = 'cube-react-theme';

interface ThemeState {
  mode: ThemeMode;
  setMode: (mode: ThemeMode) => void;
  toggleMode: () => void;
}

function loadTheme(): ThemeMode {
  const cfg = getConfig().theme;
  try {
    const saved = localStorage.getItem(THEME_KEY);
    if (saved) {
      // 兼容旧版持久化：忽略 family，仅取 mode
      const parsed = JSON.parse(saved) as { family?: string; mode?: ThemeMode };
      if (parsed.mode === 'light' || parsed.mode === 'dark') return parsed.mode;
    }
  } catch {
    // ignore
  }
  return cfg.defaultMode;
}

export const useThemeStore = create<ThemeState>((set, get) => ({
  mode: loadTheme(),
  setMode: (mode) => {
    set({ mode });
    localStorage.setItem(THEME_KEY, JSON.stringify({ mode }));
  },
  toggleMode: () => {
    const mode = get().mode === 'dark' ? 'light' : 'dark';
    set({ mode });
    localStorage.setItem(THEME_KEY, JSON.stringify({ mode }));
  },
}));

export default useThemeStore;
