/**
 * 个人中心（/profile）
 *
 * 工作台 hero 头像/昵称点击进入。展示当前用户资料（工作台接口 /Admin/Index/Dashboard 的
 * user 优先，降级 userInfo）+ 安全中心入口。数据均复用现有接口，无新增后端。
 */
import { Avatar, Button, Card, Descriptions, Space, Tag } from 'antd';
import { LogoutOutlined, SafetyOutlined, UserOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useUserStore, logoutAndRedirect } from '@/stores/user';
import { useDashboard } from '@/hooks/useDashboard';

export default function ProfilePage() {
  const navigate = useNavigate();
  const userInfo = useUserStore((s) => s.userInfo);
  const { data: wb } = useDashboard();

  const user = wb?.user;
  const profile = wb?.profile;
  const displayName = user?.displayName || userInfo?.displayName || userInfo?.name || '用户';
  const userName = user?.name || userInfo?.name || '—';
  const roles = user?.roles?.join(' / ') || userInfo?.roleName || '用户';
  const online = profile?.online;

  return (
    <div style={{ maxWidth: 720, margin: '0 auto' }}>
      {/* 用户卡：大头像 + 昵称 + 角色 + 在线状态 */}
      <Card style={{ marginBottom: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 20 }}>
          <Avatar size={72} src={userInfo?.avatar} icon={<UserOutlined />} />
          <div style={{ minWidth: 0 }}>
            <Space size={8}>
              <span style={{ fontSize: 20, fontWeight: 600 }}>{displayName}</span>
              {online !== undefined && (
                <Tag color={online ? 'success' : 'default'}>{online ? '在线' : '离线'}</Tag>
              )}
            </Space>
            <div style={{ marginTop: 6, display: 'flex', alignItems: 'center', gap: 8 }}>
              <Tag color="blue">{roles}</Tag>
              <span style={{ color: 'var(--cube-text-muted)' }}>{userName}</span>
            </div>
          </div>
        </div>
      </Card>

      {/* 基本信息：登录次数 / 最近登录 / 登录 IP / 注册时间 */}
      <Card title="基本信息" style={{ marginBottom: 16 }}>
        <Descriptions
          column={1}
          size="middle"
          items={[
            { key: 'logins', label: '登录次数', children: String(profile?.logins ?? '—') },
            { key: 'lastLogin', label: '最近登录', children: String(profile?.lastLogin ?? '—') },
            { key: 'lastLoginIP', label: '登录 IP', children: String(profile?.lastLoginIP ?? '—') },
            { key: 'registerTime', label: '注册时间', children: String(profile?.registerTime ?? '—') },
          ]}
        />
      </Card>

      {/* 账号操作：安全中心 / 退出登录 */}
      <Card title="账号操作">
        <Space wrap>
          <Button type="primary" icon={<SafetyOutlined />} onClick={() => navigate('/profile/security')}>
            安全中心
          </Button>
          <Button danger icon={<LogoutOutlined />} onClick={() => void logoutAndRedirect()}>
            退出登录
          </Button>
        </Space>
      </Card>
    </div>
  );
}
