/**
 * 主题预置色（OSC-0017）
 *
 * Arco Design 官方 13 个品牌色（不含中性灰 gray），色值来自 arco-design-vue `colors.less`（`-6` 即主色）。
 * 「外观设置」主色表单项以预置色板形式展示，选择即写入 `ThemePrefs.primaryColor`。
 */
export interface PresetThemeColor {
  /** 唯一键（Arco 色板 key） */
  key: string;
  /** 官方中文命名 */
  name: string;
  /** 主色 hex */
  color: string;
}

/** 13 个官方品牌色（官方中文命名 + hex），默认主题色「极客蓝」#165DFF 在其中 */
export const PRESET_THEME_COLORS: readonly PresetThemeColor[] = [
  { key: 'red', name: '浪漫红', color: '#F53F3F' },
  { key: 'orangered', name: '晚秋红', color: '#F77234' },
  { key: 'orange', name: '日暮黄', color: '#FF7D00' },
  { key: 'gold', name: '明黄金', color: '#F7BA1E' },
  { key: 'yellow', name: '柠檬黄', color: '#FADC19' },
  { key: 'lime', name: '青柠绿', color: '#9FDB1D' },
  { key: 'green', name: '极光绿', color: '#00B42A' },
  { key: 'cyan', name: '青春绿', color: '#14C9C9' },
  { key: 'blue', name: '碧涛蓝', color: '#3491FA' },
  { key: 'arcoblue', name: '极客蓝', color: '#165DFF' },
  { key: 'purple', name: '贵族紫', color: '#722ED1' },
  { key: 'pinkpurple', name: '浪漫紫', color: '#D91AD9' },
  { key: 'magenta', name: '法式洋红', color: '#F5319D' },
] as const;

/** 默认主题主色（极客蓝） */
export const DEFAULT_PRIMARY_COLOR = '#165DFF';
