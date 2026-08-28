/**
 * 首页（站点信息 + 欢迎卡片 + 常用菜单入口）
 */
import { useEffect, useMemo, useState } from 'react';
import { Card, Col, List, Row, Skeleton, Tag } from 'antd';
import { useNavigate } from 'react-router-dom';
import { api } from '@/api';
import { useUserStore } from '@/stores/user';
import { getConfig } from '@/configure';
import type { LoginConfig, MenuItem } from '@cube/api-core';

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

  // 常用菜单入口：递归收集任意层级的叶子菜单（children 为空且有 url），取前 12 个
  // 注：不能只取"无 children 的顶层菜单"——顶层通常都是分组（含子菜单），会导致入口永远为空
  // 注：过滤后端区域根菜单（~/Ai 等，前端无对应页面，点击会 404）
  const topMenus = useMemo(() => {
    const leaves: MenuItem[] = [];
    const walk = (items: MenuItem[]) => {
      for (const item of items) {
        if (item.children?.length) {
          walk(item.children);
        } else if (item.visible !== false && item.url && item.url !== '~' && !item.url.startsWith('~/')) {
          leaves.push(item);
        }
      }
    };
    walk(menus);
    return leaves.slice(0, 12);
  }, [menus]);

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
