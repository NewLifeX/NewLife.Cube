/**
 * 根组件
 *
 * - ConfigProvider：antd 主题（4 主题家族 × 明暗）+ 中文语言包
 * - AntApp：message/notification/modal 上下文
 * - RouterProvider：路由系统
 */
import { App as AntApp, ConfigProvider, theme as antdTheme } from 'antd';
import zhCN from 'antd/locale/zh_CN';
import { RouterProvider } from 'react-router-dom';
import { router } from '@/router';
import { useThemeStore } from '@/stores/theme';
import { buildThemeConfig } from '@/themes';
import '@/i18n';

export default function App() {
  const family = useThemeStore((s) => s.family);
  const mode = useThemeStore((s) => s.mode);

  const themeConfig = buildThemeConfig(family, mode);

  return (
    <ConfigProvider
      locale={zhCN}
      theme={{
        ...themeConfig,
        algorithm: mode === 'dark' ? antdTheme.darkAlgorithm : antdTheme.defaultAlgorithm,
      }}
    >
      <AntApp>
        <RouterProvider router={router} />
      </AntApp>
    </ConfigProvider>
  );
}
