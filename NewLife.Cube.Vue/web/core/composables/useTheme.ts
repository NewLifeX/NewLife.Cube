import { ref, watchEffect } from 'vue';
import { loadThemeCss } from '../themes';
import i18n from '../i18n';

/** 主题名跟随语言：模块级常量用 getter，读到时才取当前 locale 的文案 */
const t = (key: string): string => i18n.global.t(key);

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
    get label() {
      return t('theme.cyber');
    },
    icon: '◉',
    variants: [
      {
        id: 'cyber-dark',
        get label() {
          return t('theme.cyberDark');
        },
        icon: '🌙',
        get description() {
          return t('theme.cyberDarkDesc');
        },
        family: 'cyber',
        mode: 'dark',
      },
      {
        id: 'cyber-light',
        get label() {
          return t('theme.cyberLight');
        },
        icon: '☀️',
        get description() {
          return t('theme.cyberLightDesc');
        },
        family: 'cyber',
        mode: 'light',
      },
    ],
  },
  {
    id: 'forest',
    get label() {
      return t('theme.forest');
    },
    icon: '🌿',
    variants: [
      {
        id: 'forest-dark',
        get label() {
          return t('theme.forestDark');
        },
        icon: '🌙',
        get description() {
          return t('theme.forestDarkDesc');
        },
        family: 'forest',
        mode: 'dark',
      },
      {
        id: 'forest-light',
        get label() {
          return t('theme.forestLight');
        },
        icon: '☀️',
        get description() {
          return t('theme.forestLightDesc');
        },
        family: 'forest',
        mode: 'light',
      },
    ],
  },
  {
    id: 'aurora',
    get label() {
      return t('theme.aurora');
    },
    icon: '🌌',
    variants: [
      {
        id: 'aurora-dark',
        get label() {
          return t('theme.auroraDark');
        },
        icon: '🌙',
        get description() {
          return t('theme.auroraDarkDesc');
        },
        family: 'aurora',
        mode: 'dark',
      },
      {
        id: 'aurora-light',
        get label() {
          return t('theme.auroraLight');
        },
        icon: '☀️',
        get description() {
          return t('theme.auroraLightDesc');
        },
        family: 'aurora',
        mode: 'light',
      },
    ],
  },
  {
    id: 'industrial',
    get label() {
      return t('theme.industrial');
    },
    icon: '⚙',
    variants: [
      {
        id: 'industrial-dark',
        get label() {
          return t('theme.industrialDark');
        },
        icon: '🌙',
        get description() {
          return t('theme.industrialDarkDesc');
        },
        family: 'industrial',
        mode: 'dark',
      },
      {
        id: 'industrial-light',
        get label() {
          return t('theme.industrialLight');
        },
        icon: '☀️',
        get description() {
          return t('theme.industrialLightDesc');
        },
        family: 'industrial',
        mode: 'light',
      },
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

export const THEME_OPTIONS: ThemeSelectOption[] = THEME_GROUPS.map((g) => ({
  id: g.id,
  get label() {
    return g.label;
  },
  icon: g.icon,
  get description() {
    return g.label;
  },
  family: g.id,
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

  async function switchTheme(family: ThemeFamily) {
    // 加载主题 CSS
    await loadThemeCss(family);
    currentFamily.value = family;
  }

  async function setTheme(id: string) {
    // 兼容旧用法：id 可能为 "cyber-dark" 格式，解析后分别设置
    const theme = THEMES.find(t => t.id === id);
    if (theme) {
      // 加载主题 CSS
      await loadThemeCss(theme.family);
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
