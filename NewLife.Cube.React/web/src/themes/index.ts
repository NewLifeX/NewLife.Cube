/**
 * antd 主题 token 配置（antd6 单一主题版）
 *
 * antd6 升级后收敛为单一主题：主色沿用 antd6 默认蓝，
 * 明暗两套由 antd defaultAlgorithm / darkAlgorithm 自动派生，
 * 此处仅保留必要的自定义 token 与组件级微调，尽可能贴近 antd6 原生观感。
 */
import type { ThemeConfig } from 'antd';
import type { ThemeMode } from '@/stores/theme';

/** 单一主题色板（明暗仅覆盖 antd 算法默认值之外需要微调的部分） */
interface ThemePalette {
  /** 主色 */
  primary: string;
  /** 页面底色（Layout bodyBg / footerBg） */
  bg: string;
  /** 容器底色（侧栏 / 页头） */
  container: string;
  /** 表格冻结列背景（不透明，避免横向滚动时其它列透出） */
  tableHeaderBg: string;
  tableRowHoverBg: string;
}

const PALETTES: Record<ThemeMode, ThemePalette> = {
  light: {
    primary: '#1677ff',
    bg: '#f5f5f5',
    container: '#ffffff',
    tableHeaderBg: '#fafafa',
    tableRowHoverBg: '#fafafa',
  },
  dark: {
    primary: '#1668dc',
    bg: '#000000',
    container: '#141414',
    tableHeaderBg: '#1d1d1d',
    tableRowHoverBg: '#1d1d1d',
  },
};

/**
 * 生成主题配置
 *
 * @param mode 明暗模式
 * @returns antd ThemeConfig
 */
export function buildThemeConfig(mode: ThemeMode): ThemeConfig {
  const p = PALETTES[mode];

  return {
    token: {
      colorPrimary: p.primary,
      colorInfo: p.primary,
      colorLink: p.primary,
      colorLinkHover: p.primary,
    },
    components: {
      // 布局级：侧栏 / 页头 / 内容区跟随容器与页面底色，整体贴近 antd6 原生中后台
      Layout: {
        siderBg: p.container,
        headerBg: p.container,
        bodyBg: p.bg,
        footerBg: p.bg,
      },
    },
  };
}

/**
 * 应用表格冻结列不透明背景的 CSS 变量（随明暗切换）
 *
 * antd 表格固定列（操作列）默认叠用半透明 headerBg/rowHoverBg，
 * 横向滚动时其它列会从半透明背景中透出。这里注入不透明底色到
 * --cube-table-* 供 CSS 覆盖固定列使用。
 */
export function applyTableCellCssVars(mode: ThemeMode) {
  const p = PALETTES[mode];
  const root = document.documentElement;
  root.style.setProperty('--cube-table-header-bg', p.tableHeaderBg);
  root.style.setProperty('--cube-table-row-hover-bg', p.tableRowHoverBg);
}
