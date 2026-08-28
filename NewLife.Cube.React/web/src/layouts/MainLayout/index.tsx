/**
 * 主布局（Sider 动态菜单 + Header + 多标签 + Content + Footer）
 *
 * - 桌面端：左侧玻璃拟态 Sider 面板，可折叠
 * - 移动端（<lg）：Sider 自动隐藏，顶栏按钮打开 Drawer 导航
 * - Content 内容居中限制最大宽度，保证大屏观感
 */
import { useState, type ReactNode } from 'react';
import { Drawer, Layout } from 'antd';
import SiderMenu from './SiderMenu';
import HeaderBar from './HeaderBar';
import TabsView from '@/components/TabsView';
import AiAssistant from '@/components/ai/AiAssistant';
import { getConfig } from '@/configure';

const { Sider, Content, Footer } = Layout;

export default function MainLayout({ children }: { children: ReactNode }) {
  const cfg = getConfig().ui.layout;
  const [collapsed, setCollapsed] = useState(cfg.sider.defaultCollapsed);
  const [isMobile, setIsMobile] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);

  // 导航切换：移动端打开 Drawer，桌面端折叠/展开 Sider
  const toggleNav = () => {
    if (isMobile) setDrawerOpen(true);
    else setCollapsed((c) => !c);
  };

  const brand = getConfig().base;

  const siderPanel = (
    <div className="cube-shell-sider-panel">
      <div className="cube-shell-brand">
        <div className="cube-shell-brand-mark">
          {brand.logo ? <img src={brand.logo} alt="logo" /> : '🧊'}
        </div>
        <div className="cube-shell-brand-copy">
          <div className="cube-shell-brand-title">{brand.title}</div>
          <div className="cube-shell-brand-subtitle">React 皮肤 · AntD5</div>
        </div>
      </div>
      <div className="cube-shell-menu-wrap">
        <SiderMenu />
      </div>
      <div className="cube-shell-menu-footer">NewLife.Cube.React</div>
    </div>
  );

  return (
    <Layout className="cube-main-layout" style={{ height: '100%', overflow: 'hidden' }}>
      {cfg.sider.show && (
        <Sider
          className="cube-shell-sider"
          collapsible
          collapsed={collapsed}
          onCollapse={setCollapsed}
          trigger={null}
          breakpoint="lg"
          collapsedWidth={0}
          onBreakpoint={(broken) => setIsMobile(broken)}
          width={cfg.sider.width}
        >
          {siderPanel}
        </Sider>
      )}
      <Layout className="cube-main-content">
        {cfg.header.show && <HeaderBar collapsed={collapsed} onToggle={toggleNav} />}
        <TabsView />
        <Content className="cube-shell-body">
          <div className="cube-shell-page-container">{children}</div>
        </Content>
        {cfg.footer.show && (
          <Footer className="cube-shell-footer">
            <div className="cube-shell-footer-inner">{brand.footer || '魔方 React 皮肤'}</div>
          </Footer>
        )}
      </Layout>
      {/* AI 助手（全局悬浮） */}
      <AiAssistant />
      {/* 移动端抽屉导航 */}
      <Drawer
        className="cube-mobile-drawer"
        placement="left"
        width={cfg.sider.width}
        open={isMobile && drawerOpen}
        onClose={() => setDrawerOpen(false)}
        styles={{ body: { padding: 0 } }}
        title={brand.title}
      >
        <SiderMenu />
      </Drawer>
    </Layout>
  );
}
