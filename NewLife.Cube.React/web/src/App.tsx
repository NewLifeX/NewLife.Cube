/**
 * 根组件
 *
 * - ConfigProvider：antd 主题（单一主题 × 明暗）+ 中文语言包
 * - AntApp：message/notification/modal 上下文（供 App.useApp() 使用）
 * - AntdAppBinder：把 useApp 实例托管给非组件模块（api 拦截器等）
 * - RouterProvider：路由系统
 */
import { useEffect } from 'react';
import { App as AntApp, ConfigProvider, theme as antdTheme } from 'antd';
import zhCN from 'antd/locale/zh_CN';
import { RouterProvider } from 'react-router-dom';
import { router } from '@/router';
import { useThemeStore } from '@/stores/theme';
import { applyTableCellCssVars, buildThemeConfig } from '@/themes';
import { bindAntdApp } from '@/utils/antdApp';
import '@/i18n';

/** 在 <App> 上下文内绑定 useApp 实例，供非组件模块（api 拦截器等）复用 */
function AntdAppBinder() {
  const app = AntApp.useApp();
  useEffect(() => {
    bindAntdApp(app);
  }, [app]);
  return null;
}

export default function App() {
  const mode = useThemeStore((s) => s.mode);

  // 同步 body 明暗属性，供全局 CSS 深浅色设计令牌切换；并注入表格冻结列不透明背景变量
  useEffect(() => {
    document.body.dataset.cubeMode = mode;
    applyTableCellCssVars(mode);
  }, [mode]);

  const themeConfig = buildThemeConfig(mode);

  return (
    <ConfigProvider
      locale={zhCN}
      theme={{
        ...themeConfig,
        algorithm: mode === 'dark' ? antdTheme.darkAlgorithm : antdTheme.defaultAlgorithm,
      }}
    >
      <AntApp>
        <AntdAppBinder />
        <div className="cube-app">
          <RouterProvider router={router} />
        </div>
      </AntApp>
    </ConfigProvider>
  );
}
