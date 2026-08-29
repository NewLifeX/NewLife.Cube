/**
 * antd 主题 token 配置（AntD5 全面精修版）
 *
 * 4 个主题家族（cyber/forest/aurora/industrial）均提供 light/dark 两套完整 token，
 * 在保持 AntD 语义色体系的前提下，让每个家族都有可辨识的个性主色与页面底色。
 */
import type { ThemeConfig } from 'antd';
import type { ThemeFamily, ThemeMode } from '@/stores/theme';

/** 主题家族完整色板 */
interface FamilyPalette {
  light: {
    primary: string;
    primaryHover: string;
    bg: string;
    container: string;
    text: string;
    textSecondary: string;
    border: string;
    muted: string;
    sider: string;
    header: string;
  };
  dark: {
    primary: string;
    primaryHover: string;
    bg: string;
    container: string;
    text: string;
    textSecondary: string;
    border: string;
    muted: string;
    sider: string;
    header: string;
  };
}

const FAMILY_PALETTES: Record<ThemeFamily, FamilyPalette> = {
  cyber: {
    light: {
      primary: '#1677ff',
      primaryHover: '#4096ff',
      bg: '#f4f7fb',
      container: '#ffffff',
      text: '#0f172a',
      textSecondary: '#475569',
      border: '#e2e8f0',
      muted: 'rgba(15, 23, 42, 0.04)',
      sider: '#ffffff',
      header: '#ffffff',
    },
    dark: {
      primary: '#3b82f6',
      primaryHover: '#60a5fa',
      bg: '#0d1424',
      container: '#141c2e',
      text: '#e5edf7',
      textSecondary: '#94a3b8',
      border: '#1e2a3d',
      muted: 'rgba(148, 163, 184, 0.08)',
      sider: '#0d1424',
      header: '#0d1424',
    },
  },
  forest: {
    light: {
      primary: '#16a34a',
      primaryHover: '#22c55e',
      bg: '#f2f8f3',
      container: '#ffffff',
      text: '#0f1f15',
      textSecondary: '#3f5a48',
      border: '#dcebe1',
      muted: 'rgba(22, 101, 52, 0.05)',
      sider: '#ffffff',
      header: '#ffffff',
    },
    dark: {
      primary: '#22c55e',
      primaryHover: '#4ade80',
      bg: '#0d1912',
      container: '#12241a',
      text: '#e4f2e8',
      textSecondary: '#8fae99',
      border: '#1b3326',
      muted: 'rgba(74, 222, 128, 0.08)',
      sider: '#0d1912',
      header: '#0d1912',
    },
  },
  aurora: {
    light: {
      primary: '#0ea5e9',
      primaryHover: '#38bdf8',
      bg: '#f1f8fc',
      container: '#ffffff',
      text: '#0c1a23',
      textSecondary: '#3e5a6a',
      border: '#dbeaf2',
      muted: 'rgba(14, 165, 233, 0.05)',
      sider: '#ffffff',
      header: '#ffffff',
    },
    dark: {
      primary: '#38bdf8',
      primaryHover: '#7dd3fc',
      bg: '#0c1a24',
      container: '#102431',
      text: '#e2f1fa',
      textSecondary: '#8fb0c4',
      border: '#1a3342',
      muted: 'rgba(56, 189, 248, 0.08)',
      sider: '#0c1a24',
      header: '#0c1a24',
    },
  },
  industrial: {
    light: {
      primary: '#f97316',
      primaryHover: '#fb923c',
      bg: '#faf6f1',
      container: '#ffffff',
      text: '#23180d',
      textSecondary: '#6b5439',
      border: '#efe3d5',
      muted: 'rgba(249, 115, 22, 0.05)',
      sider: '#ffffff',
      header: '#ffffff',
    },
    dark: {
      primary: '#fb923c',
      primaryHover: '#fdba74',
      bg: '#19130d',
      container: '#201911',
      text: '#f7eee3',
      textSecondary: '#b9a184',
      border: '#332819',
      muted: 'rgba(251, 146, 60, 0.08)',
      sider: '#19130d',
      header: '#19130d',
    },
  },
};

/**
 * 生成主题配置
 *
 * @param family 主题家族
 * @param mode 明暗模式
 * @returns antd ThemeConfig
 */
export function buildThemeConfig(family: ThemeFamily, mode: ThemeMode): ThemeConfig {
  const p = FAMILY_PALETTES[family]?.[mode] ?? FAMILY_PALETTES.cyber[mode];

  return {
    token: {
      colorPrimary: p.primary,
      colorInfo: p.primary,
      colorLink: p.primary,
      colorLinkHover: p.primaryHover,
      colorSuccess: '#16a34a',
      colorWarning: '#f59e0b',
      colorError: '#ef4444',
      colorBgLayout: p.bg,
      colorBgContainer: p.container,
      colorText: p.text,
      colorTextSecondary: p.textSecondary,
      colorTextTertiary: p.textSecondary,
      colorBorder: p.border,
      colorBorderSecondary: p.border,
      colorSplit: p.border,
      borderRadius: 12,
      borderRadiusLG: 20,
      borderRadiusSM: 8,
      fontSize: 14,
      controlHeight: 38,
      controlHeightLG: 44,
      boxShadowSecondary: '0 18px 46px rgba(15, 23, 42, 0.10)',
      fontFamily:
        "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, 'Noto Sans SC', sans-serif",
    },
    components: {
      Layout: {
        siderBg: p.sider,
        headerBg: p.header,
        bodyBg: p.bg,
        footerBg: p.bg,
        headerHeight: 0,
        headerPadding: '0',
      },
      Menu: {
        itemBg: 'transparent',
        subMenuItemBg: 'transparent',
        itemHeight: 42,
        itemBorderRadius: 14,
        itemColor: p.textSecondary,
        itemSelectedColor: p.primary,
        itemSelectedBg: `${p.primary}1f`,
        itemHoverColor: p.text,
        itemHoverBg: p.muted,
        iconSize: 16,
        darkItemBg: 'transparent',
        darkSubMenuItemBg: 'transparent',
        darkItemColor: p.textSecondary,
        darkItemSelectedColor: p.primary,
        darkItemSelectedBg: `${p.primary}1f`,
      },
      Card: {
        borderRadiusLG: 24,
        headerBg: 'transparent',
        headerFontSize: 16,
      },
      Table: {
        headerBg: p.muted,
        headerColor: p.textSecondary,
        rowHoverBg: p.muted,
        borderColor: p.border,
        headerSplitColor: p.border,
        borderRadiusLG: 18,
      },
      Button: {
        borderRadius: 12,
        borderRadiusLG: 14,
        controlHeight: 38,
        controlHeightLG: 44,
        controlHeightSM: 30,
      },
      Tabs: {
        titleFontSize: 15,
        horizontalItemGutter: 16,
        itemSelectedColor: p.primary,
        inkBarColor: p.primary,
      },
      Modal: {
        borderRadiusLG: 24,
      },
      Tag: {
        borderRadiusSM: 8,
      },
      Input: {
        controlHeight: 38,
        controlHeightLG: 44,
      },
      InputNumber: {
        controlHeight: 38,
        controlHeightLG: 44,
      },
      Select: {
        controlHeight: 38,
        controlHeightLG: 44,
      },
      DatePicker: {
        controlHeight: 38,
        controlHeightLG: 44,
      },
      Pagination: {
        itemSize: 34,
        itemActiveBg: p.primary,
      },
      Breadcrumb: {
        itemColor: p.textSecondary,
        lastItemColor: p.text,
        linkColor: p.textSecondary,
        separatorColor: p.textSecondary,
      },
      Segmented: {
        itemSelectedBg: p.container,
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
