import { ref, watchEffect } from 'vue';

export type ThemeFamily = 'cyber' | 'forest' | 'aurora' | 'industrial';
export type ThemeMode = 'dark' | 'light';

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

// 主题分组（每个 family 有 light/dark 两个变体）
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
    icon: '⚙',
    variants: [
      { id: 'industrial-dark', label: '工业深色', icon: '🌙', description: '工业科技深色主题', family: 'industrial', mode: 'dark' },
      { id: 'industrial-light', label: '工业浅色', icon: '☀️', description: '工业科技浅色主题', family: 'industrial', mode: 'light' },
    ],
  },
];

// 所有主题扁平列表（供 useDarkMode 等使用）
export const THEMES: ThemeOption[] = THEME_GROUPS.flatMap(g => g.variants);

// 用于下拉选择的主题选项（只显示家族层级）
export interface ThemeSelectOption {
  id: ThemeFamily;
  label: string;
  icon: string;
  description: string;
  family: ThemeFamily;
}

export const THEME_OPTIONS: ThemeSelectOption[] = THEME_GROUPS.map(g => ({
  id: g.id, label: g.label, icon: g.icon, description: g.label, family: g.id,
}));

// ── 独立状态：Mode ———————
const MODE_KEY = 'cube-mode';

function getInitialMode(): ThemeMode {
  const stored = localStorage.getItem(MODE_KEY) as ThemeMode | null;
  if (stored === 'dark' || stored === 'light') return stored;
  return 'dark';
}

const currentMode = ref<ThemeMode>(getInitialMode());

watchEffect(() => {
  document.documentElement.classList.toggle('dark', currentMode.value === 'dark');
  localStorage.setItem(MODE_KEY, currentMode.value);
});

// ── 独立状态：Theme ———————
const THEME_KEY = 'cube-theme';

function getInitialFamily(): ThemeFamily {
  const stored = localStorage.getItem(THEME_KEY) as ThemeFamily | null;
  if (stored && THEME_GROUPS.some(g => g.id === stored)) return stored;
  return 'cyber';
}

const currentFamily = ref<ThemeFamily>(getInitialFamily());

watchEffect(() => {
  document.documentElement.setAttribute('data-theme', currentFamily.value);
  localStorage.setItem(THEME_KEY, currentFamily.value);
});

// ── 计算属性 ———————
const currentTheme = ref<ThemeOption>(
  THEMES.find(t => t.family === currentFamily.value && t.mode === currentMode.value) || THEMES[0]
);
const currentGroup = ref<ThemeGroup>(
  THEME_GROUPS.find(g => g.id === currentFamily.value) || THEME_GROUPS[0]
);

// 同步 currentTheme / currentGroup
watchEffect(() => {
  const t = THEMES.find(t => t.family === currentFamily.value && t.mode === currentMode.value);
  if (t) currentTheme.value = t;
  const g = THEME_GROUPS.find(g => g.id === currentFamily.value);
  if (g) currentGroup.value = g;
});

export function useTheme() {
  function toggleMode() {
    currentMode.value = currentMode.value === 'dark' ? 'light' : 'dark';
  }

  function switchTheme(family: ThemeFamily) {
    currentFamily.value = family;
  }

  function setTheme(id: string) {
    // 兼容旧用法：id 可能为 "cyber-dark" 格式，解析后分别设置
    const theme = THEMES.find(t => t.id === id);
    if (theme) {
      currentFamily.value = theme.family;
      currentMode.value = theme.mode;
    }
  }

  return {
    currentTheme,
    currentGroup,
    currentMode,
    currentFamily,
    themeGroups: THEME_GROUPS,
    themes: THEMES,
    themeOptions: THEME_OPTIONS,
    toggleMode,
    switchTheme,
    setTheme,
  };
}

// 便利的组合式函数
export function useDarkMode() {
  const { currentMode, toggleMode } = useTheme();

  const isDark = ref(currentMode.value === 'dark');

  watchEffect(() => {
    isDark.value = currentMode.value === 'dark';
  });

  return { isDark, toggleMode };
}
