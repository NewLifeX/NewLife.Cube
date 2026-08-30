/**
 * 用户菜单（头像下拉：租户切换 / 安全中心 / 退出登录）
 *
 * 多租户开启（userInfo.enableTenant）且用户有可切换项（所属租户或系统管理员）时，
 * 下拉顶部显示当前租户 + 租户列表（含"系统管理后台"），点击切换后刷新页面
 * （菜单/数据随租户变化，刷新最稳妥）。
 */
import { useState } from 'react';
import { App, Avatar, Dropdown, type MenuProps } from 'antd';
import { ApartmentOutlined, LogoutOutlined, SafetyOutlined, UserOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useUserStore, logoutAndRedirect } from '@/stores/user';
import { api } from '@/api';

export default function UserMenu() {
  const { message } = App.useApp();
  const userInfo = useUserStore((s) => s.userInfo);
  const navigate = useNavigate();
  const [switching, setSwitching] = useState(false);

  // 多租户开启且用户有可切换项（所属租户列表非空，或系统管理员可进管理后台）时才显示租户区
  const showTenant = !!userInfo?.enableTenant && ((userInfo?.tenants?.length ?? 0) > 0 || !!userInfo?.isSystemAdmin);
  // 当前租户显示名：管理后台（tenantMode=1）显示"系统管理后台"，租户模式显示租户名
  const currentTenantName = userInfo?.tenantMode === 1 ? '系统管理后台' : userInfo?.tenantName || '系统后台';

  const switchTenant = async (tenantId: number) => {
    if (switching) return;
    setSwitching(true);
    try {
      await api.user.switchTenant(tenantId);
      message.success('租户切换成功');
      // 菜单/数据/页面缓存均随租户变化，刷新页面最稳妥
      window.location.reload();
    } catch {
      // 错误提示已由全局请求拦截统一弹出
    } finally {
      setSwitching(false);
    }
  };

  const items: MenuProps['items'] = [];

  if (showTenant) {
    items.push({
      key: 'tenant-current',
      label: `当前租户：${currentTenantName}`,
      disabled: true,
    });
    items.push({ type: 'divider' });

    if (userInfo?.isSystemAdmin) {
      items.push({
        key: 'tenant-0',
        icon: <ApartmentOutlined />,
        label: '系统管理后台',
        onClick: () => void switchTenant(0),
      });
    }

    for (const t of userInfo?.tenants ?? []) {
      items.push({
        key: `tenant-${t.id}`,
        icon: <ApartmentOutlined />,
        label: t.name || t.code || `租户${t.id}`,
        onClick: () => void switchTenant(t.id),
      });
    }

    items.push({ type: 'divider' });
  }

  items.push(
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: '个人中心',
      onClick: () => navigate('/profile'),
    },
    {
      key: 'security',
      icon: <SafetyOutlined />,
      label: '安全中心',
      onClick: () => navigate('/profile/security'),
    },
    { type: 'divider' },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: '退出登录',
      danger: true,
      onClick: () => {
        void logoutAndRedirect();
      },
    },
  );

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
