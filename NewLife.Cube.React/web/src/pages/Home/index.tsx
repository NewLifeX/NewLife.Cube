/**
 * 首页（站点信息 + 欢迎卡片 + 常用菜单入口）
 */
import { useEffect, useState } from 'react';
import { Card, Col, List, Row, Skeleton, Tag } from 'antd';
import { useNavigate } from 'react-router-dom';
import { api } from '@/api';
import { useUserStore } from '@/stores/user';
import { getConfig } from '@/configure';
import type { LoginConfig } from '@cube/api-core';

export default function HomePage() {
  const navigate = useNavigate();
  const userInfo = useUserStore((s) => s.userInfo);
  const menus = useUserStore((s) => s.menus);
  const [config, setConfig] = useState<LoginConfig | null>(null);

  useEffect(() => {
    api.user
      .getLoginConfig()
      .then((res) => setConfig(res.data ?? null))
      .catch(() => {});
  }, []);

  // 顶层菜单（常用入口）
  const topMenus = menus.filter((m) => m.visible !== false && m.children?.length === 0 && m.url);

  return (
    <div style={{ maxWidth: 960, margin: '0 auto' }}>
      <Row gutter={16}>
        <Col span={24}>
          <Card>
            <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
              {config?.loginLogo || config?.logo ? (
                <img src={config.loginLogo || config.logo} alt="logo" style={{ height: 56 }} />
              ) : (
                <div style={{ fontSize: 48 }}>🧊</div>
              )}
              <div>
                <h2 style={{ margin: 0 }}>{config?.name || getConfig().base.title}</h2>
                <p style={{ color: '#888', margin: '4px 0 0' }}>
                  欢迎回来，{userInfo?.displayName || userInfo?.name || '用户'}！
                  {userInfo?.roleName && <Tag color="blue" style={{ marginLeft: 8 }}>{userInfo.roleName}</Tag>}
                </p>
              </div>
            </div>
            {config?.loginTip && <p style={{ color: '#666', marginTop: 8 }}>{config.loginTip}</p>}
          </Card>
        </Col>
      </Row>

      <Row gutter={16} style={{ marginTop: 16 }}>
        <Col span={24}>
          <Card title="常用菜单">
            <Skeleton loading={!topMenus.length && menus.length > 0} active>
              <List
                grid={{ gutter: 16, xs: 1, sm: 2, md: 3 }}
                dataSource={topMenus}
                locale={{ emptyText: '暂无可用菜单' }}
                renderItem={(item) => (
                  <List.Item>
                    <Card
                      size="small"
                      hoverable
                      onClick={() => item.url && navigate(item.url)}
                      styles={{ body: { padding: '12px 16px' } }}
                    >
                      <div style={{ fontWeight: 500 }}>{item.displayName || item.name}</div>
                      <div style={{ fontSize: 12, color: '#999', marginTop: 4 }}>{item.url}</div>
                    </Card>
                  </List.Item>
                )}
              />
            </Skeleton>
          </Card>
        </Col>
      </Row>
    </div>
  );
}
