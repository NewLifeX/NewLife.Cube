/**
 * antd 主题 token 配置（对齐 Vue 皮肤 themes/，用 antd token 表达）
 *
 * 4 个主题家族，每个提供 light/dark 两套 token。
 */
import type { ThemeConfig } from 'antd';
import type { ThemeFamily, ThemeMode } from '@/stores/theme';

/** 各主题家族主色 */
const FAMILY_PRIMARY: Record<ThemeFamily, { light: string; dark: string }> = {
  cyber: { light: '#1677ff', dark: '#3b82f6' }, // 赛博蓝
  forest: { light: '#16a34a', dark: '#22c55e' }, // 森林绿
  aurora: { light: '#0ea5e9', dark: '#38bdf8' }, // 极光蓝绿
  industrial: { light: '#f97316', dark: '#fb923c' }, // 工业橙
};

/** 各主题家族明暗背景色 */
const FAMILY_BG: Record<ThemeFamily, { light: string; dark: string }> = {
  cyber: { light: '#f5f7fa', dark: '#141a26' },
  forest: { light: '#f4f8f4', dark: '#131f17' },
  aurora: { light: '#f2f8fc', dark: '#0f1c24' },
  industrial: { light: '#faf7f3', dark: '#1a1611' },
};

/**
 * 生成主题配置
 *
 * @param family 主题家族
 * @param mode 明暗模式
 * @returns antd ThemeConfig
 */
export function buildThemeConfig(family: ThemeFamily, mode: ThemeMode): ThemeConfig {
  const primary = FAMILY_PRIMARY[family]?.[mode] ?? '#1677ff';
  const bg = FAMILY_BG[family]?.[mode] ?? (mode === 'dark' ? '#141a26' : '#f5f7fa');

  return {
    algorithm: mode === 'dark' ? undefined : undefined, // 由 App 内 algorithm 统一控制，见 App.tsx
    token: {
      colorPrimary: primary,
      borderRadius: 6,
      colorBgLayout: bg,
    },
    components: {
      Layout: {
        siderBg: mode === 'dark' ? '#0d1117' : '#ffffff',
        headerBg: mode === 'dark' ? '#0d1117' : '#ffffff',
        headerHeight: 56,
      },
      Menu: {
        darkItemBg: '#0d1117',
        darkSubMenuItemBg: '#0d1117',
      },
    },
  };
}

/** 主题家族中文标签 */
export const FAMILY_LABELS: Record<ThemeFamily, string> = {
  cyber: '赛博',
  forest: '森林',
  aurora: '极光',
  industrial: '工业',
};
