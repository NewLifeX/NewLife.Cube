/**
 * 用户菜单（头像下拉：安全中心 / 退出登录）
 */
import { Avatar, Dropdown } from 'antd';
import { LogoutOutlined, SafetyOutlined, UserOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useUserStore, logoutAndRedirect } from '@/stores/user';

export default function UserMenu() {
  const userInfo = useUserStore((s) => s.userInfo);
  const navigate = useNavigate();

  const items = [
    {
      key: 'security',
      icon: <SafetyOutlined />,
      label: '安全中心',
      onClick: () => navigate('/profile/security'),
    },
    { type: 'divider' as const },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: '退出登录',
      danger: true,
      onClick: () => {
        void logoutAndRedirect();
      },
    },
  ];

  return (
    <Dropdown menu={{ items }} placement="bottomRight" trigger={['click']}>
      <div className="cube-user-trigger">
        <Avatar size="small" src={userInfo?.avatar} icon={<UserOutlined />} />
        <span className="cube-user-trigger-copy">
          <span className="cube-user-trigger-name">{userInfo?.displayName || userInfo?.name || '用户'}</span>
          {userInfo?.roleName ? <span className="cube-user-trigger-role">{userInfo.roleName}</span> : null}
        </span>
      </div>
    </Dropdown>
  );
}
