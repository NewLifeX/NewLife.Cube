/**
 * 主布局（Sider 动态菜单 + Header + 多标签 + Content + Footer）
 */
import { useState, type ReactNode } from 'react';
import { Layout, theme } from 'antd';
import SiderMenu from './SiderMenu';
import HeaderBar from './HeaderBar';
import TabsView from '@/components/TabsView';
import { getConfig } from '@/configure';

const { Sider, Content, Footer } = Layout;

export default function MainLayout({ children }: { children: ReactNode }) {
  const [collapsed, setCollapsed] = useState(getConfig().ui.layout.sider.defaultCollapsed);
  const { token } = theme.useToken();
  const cfg = getConfig().ui.layout;

  return (
    <Layout style={{ height: '100%', overflow: 'hidden' }}>
      {cfg.sider.show && (
        <Sider
          collapsible
          collapsed={collapsed}
          onCollapse={setCollapsed}
          width={cfg.sider.width}
          collapsedWidth={cfg.sider.collapsedWidth}
          theme={cfg.sider.theme}
          style={{ overflow: 'auto' }}
        >
          <SiderMenu />
        </Sider>
      )}
      <Layout>
        {cfg.header.show && <HeaderBar />}
        <TabsView />
        <Content style={{ padding: 16, overflowY: 'auto', background: token.colorBgLayout }}>
          {children}
        </Content>
        {cfg.footer.show && (
          <Footer style={{ textAlign: 'center', padding: '12px 16px' }}>
            {getConfig().base.footer || '魔方 React 皮肤'}
          </Footer>
        )}
      </Layout>
    </Layout>
  );
}
