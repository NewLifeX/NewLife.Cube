import { ref, watchEffect } from 'vue';

export type ThemeId =
  | 'cyber-light'    // Cyber 浅色（默认）
  | 'cyber-dark'     // Cyber 深色
  | 'forest-dark'    // 森林绿深色
  | 'forest-light'   // 森林绿浅色
  | 'aurora-light'   // 极光蓝绿浅色
  | 'aurora-dark';   // 极光蓝绿深色

export type ThemeFamily = 'cyber' | 'forest' | 'aurora';
export type ThemeMode = 'dark' | 'light';

export interface ThemeOption {
  id: ThemeId;
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

// 主题分组
export const THEME_GROUPS: ThemeGroup[] = [
  {
    id: 'cyber',
    label: 'Cyber 赛博',
    icon: '◉',
    variants: [
      {
        id: 'cyber-dark',
        label: '赛博深色',
        icon: '🌙',
        description: '赛博深色主题',
        family: 'cyber',
        mode: 'dark',
      },
      {
        id: 'cyber-light',
        label: '赛博浅色',
        icon: '☀️',
        description: '赛博浅色主题',
        family: 'cyber',
        mode: 'light',
      },
    ],
  },
  {
    id: 'forest',
    label: 'Forest 森林绿',
    icon: '🌿',
    variants: [
      {
        id: 'forest-dark',
        label: '森林深色',
        icon: '🌙',
        description: '森林绿深色主题',
        family: 'forest',
        mode: 'dark',
      },
      {
        id: 'forest-light',
        label: '森林浅色',
        icon: '☀️',
        description: '森林绿浅色主题',
        family: 'forest',
        mode: 'light',
      },
    ],
  },
  {
    id: 'aurora',
    label: 'Aurora 极光蓝绿',
    icon: '🌌',
    variants: [
      {
        id: 'aurora-dark',
        label: '极光深色',
        icon: '🌙',
        description: '极光蓝绿深色主题',
        family: 'aurora',
        mode: 'dark',
      },
      {
        id: 'aurora-light',
        label: '极光浅色',
        icon: '☀️',
        description: '极光蓝绿浅色主题',
        family: 'aurora',
        mode: 'light',
      },
    ],
  },
];

// 扁平化主题列表（完整，包含深色/浅色变体）
export const THEMES: ThemeOption[] = THEME_GROUPS.flatMap(group => group.variants);

// 用于下拉选择的主题选项（只显示主题家族，不带深色/浅色后缀）
export interface ThemeSelectOption {
  id: ThemeFamily;
  label: string;
  icon: string;
  description: string;
  family: ThemeFamily;
}

export const THEME_OPTIONS: ThemeSelectOption[] = THEME_GROUPS.map(group => ({
  id: group.id,
  label: group.label,
  icon: group.icon,
  description: group.label,
  family: group.id,
}));

const STORAGE_KEY = 'cube-theme';

function getInitialTheme(): ThemeId {
  const stored = localStorage.getItem(STORAGE_KEY) as ThemeId | null;
  if (stored && THEMES.some((t) => t.id === stored)) return stored;
  return 'cyber-light';
}

const currentThemeId = ref<ThemeId>(getInitialTheme());

// 当前主题信息
const currentTheme = ref<ThemeOption>(
  THEMES.find(t => t.id === currentThemeId.value) || THEMES[0]
);

// 当前主题分组
const currentGroup = ref<ThemeGroup>(
  THEME_GROUPS.find(g => g.id === currentTheme.value.family) || THEME_GROUPS[0]
);

// 监听主题变化，更新 document 属性
watchEffect(() => {
  const themeId = currentThemeId.value;
  const theme = THEMES.find(t => t.id === themeId);

  if (theme) {
    currentTheme.value = theme;
    currentGroup.value = THEME_GROUPS.find(g => g.id === theme.family) || THEME_GROUPS[0];

    if (themeId === 'cyber-dark') {
      // 默认值，移除属性
      document.documentElement.removeAttribute('data-theme');
    } else if (themeId === 'aurora-light') {
      // 极光浅色兼容已有 :root[data-theme="aurora"]
      document.documentElement.setAttribute('data-theme', 'aurora');
    } else {
      document.documentElement.setAttribute('data-theme', themeId);
    }

    localStorage.setItem(STORAGE_KEY, themeId);
  }
});

export function useTheme() {
  function setTheme(id: ThemeId) {
    currentThemeId.value = id;
  }

  function setThemeByGroupAndMode(family: ThemeFamily, mode: ThemeMode) {
    const themeId = `${family}-${mode}` as ThemeId;
    if (THEMES.some(t => t.id === themeId)) {
      currentThemeId.value = themeId;
    }
  }

  function toggleMode() {
    const currentMode = currentTheme.value.mode;
    const newMode: ThemeMode = currentMode === 'dark' ? 'light' : 'dark';
    setThemeByGroupAndMode(currentTheme.value.family, newMode);
  }

  function switchTheme(family: ThemeFamily) {
    const mode = currentTheme.value.mode;
    setThemeByGroupAndMode(family, mode);
  }

  return {
    currentTheme,
    currentGroup,
    currentThemeId,
    themeGroups: THEME_GROUPS,
    themes: THEMES,
    themeOptions: THEME_OPTIONS,
    setTheme,
    setThemeByGroupAndMode,
    toggleMode,
    switchTheme,
  };
}

// 便利的组合式函数
export function useDarkMode() {
  const { currentTheme, toggleMode } = useTheme();

  const isDark = ref(currentTheme.value.mode === 'dark');

  watchEffect(() => {
    isDark.value = currentTheme.value.mode === 'dark';
  });

  return {
    isDark,
    toggleMode,
  };
}
