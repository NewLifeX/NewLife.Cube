/**
 * UserMenu 用户菜单单元测试（多租户切换器）
 *
 * 覆盖：
 * - 多租户关闭（enableTenant=false）→ 不显示租户区（当前租户/租户列表/管理后台均不出现）
 * - 多租户开启 + 普通用户有租户 → 显示当前租户 + 租户列表，点击租户项调用 switchTenant 并刷新
 * - 多租户开启 + 系统管理员 → 显示"系统管理后台"入口
 * - 多租户开启但用户无租户且非管理员 → 无租户区
 */
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { App as AntApp } from 'antd';
import UserMenu from '../UserMenu';

// ── 模块 Mock（vi.hoisted 保证 hoist 安全）────────────────────
const { mockUseUserStore, switchTenant, logoutAndRedirect } = vi.hoisted(() => ({
  mockUseUserStore: vi.fn(),
  switchTenant: vi.fn(),
  logoutAndRedirect: vi.fn(),
}));

vi.mock('@/stores/user', () => ({
  useUserStore: mockUseUserStore,
  logoutAndRedirect,
}));

vi.mock('@/api', () => ({
  api: { user: { switchTenant } },
}));

vi.mock('react-router-dom', () => ({
  useNavigate: () => vi.fn(),
}));

/** 设置 userInfo 快照：useUserStore((s) => s.userInfo) 走 selector */
function setUserInfo(userInfo: unknown) {
  mockUseUserStore.mockImplementation((selector: (s: { userInfo: unknown }) => unknown) =>
    selector({ userInfo }),
  );
}

/** 点击头像触发器打开下拉 */
function openMenu() {
  fireEvent.click(screen.getByText('张三'));
}

/** 渲染 UserMenu（包裹 antd App，提供 App.useApp() 上下文） */
function renderMenu() {
  return render(
    <AntApp>
      <UserMenu />
    </AntApp>,
  );
}

describe('UserMenu 用户菜单（多租户切换器）', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // jsdom 未实现 location.reload，替换为空实现
    Object.defineProperty(window, 'location', {
      value: { reload: vi.fn(), href: '' },
      writable: true,
    });
  });

  it('多租户关闭：下拉无租户区，仅安全中心/退出', async () => {
    setUserInfo({ displayName: '张三', enableTenant: false, tenantMode: 0 });
    renderMenu();
    openMenu();

    await waitFor(() => {
      expect(screen.getByText('安全中心')).toBeInTheDocument();
    });
    expect(screen.queryByText(/当前租户/)).not.toBeInTheDocument();
    expect(screen.queryByText('系统管理后台')).not.toBeInTheDocument();
    expect(screen.queryByText('测试租户')).not.toBeInTheDocument();
  });

  it('多租户开启：显示当前租户与租户列表，点击租户项调用 switchTenant 并刷新', async () => {
    setUserInfo({
      displayName: '张三',
      enableTenant: true,
      tenantMode: 2,
      tenantId: 1,
      tenantName: '测试租户',
      isSystemAdmin: false,
      tenants: [
        { id: 1, code: 't1', name: '测试租户' },
        { id: 2, code: 't2', name: '另一个租户' },
      ],
    });
    renderMenu();
    openMenu();

    await waitFor(() => {
      expect(screen.getByText('当前租户：测试租户')).toBeInTheDocument();
    });
    expect(screen.getByText('另一个租户')).toBeInTheDocument();
    // 普通用户不显示系统管理后台
    expect(screen.queryByText('系统管理后台')).not.toBeInTheDocument();

    fireEvent.click(screen.getByText('另一个租户'));
    await waitFor(() => {
      expect(switchTenant).toHaveBeenCalledWith(2);
      expect(window.location.reload).toHaveBeenCalled();
    });
  });

  it('多租户开启 + 系统管理员：显示系统管理后台入口，点击切到管理后台', async () => {
    setUserInfo({
      displayName: '张三',
      enableTenant: true,
      tenantMode: 1,
      tenantId: 0,
      isSystemAdmin: true,
      tenants: [
        { id: 1, code: 't1', name: '测试租户' },
        { id: 2, code: 't2', name: '另一个租户' },
      ],
    });
    renderMenu();
    openMenu();

    await waitFor(() => {
      expect(screen.getByText('当前租户：系统管理后台')).toBeInTheDocument();
    });
    expect(screen.getByText('系统管理后台')).toBeInTheDocument();

    fireEvent.click(screen.getByText('系统管理后台'));
    await waitFor(() => {
      expect(switchTenant).toHaveBeenCalledWith(0);
      expect(window.location.reload).toHaveBeenCalled();
    });
  });

  it('多租户开启但用户无租户且非管理员：不显示租户区', async () => {
    setUserInfo({ displayName: '张三', enableTenant: true, tenantMode: 0, tenantId: 0, isSystemAdmin: false, tenants: [] });
    renderMenu();
    openMenu();

    await waitFor(() => {
      expect(screen.getByText('安全中心')).toBeInTheDocument();
    });
    expect(screen.queryByText(/当前租户/)).not.toBeInTheDocument();
    expect(screen.queryByText('系统管理后台')).not.toBeInTheDocument();
  });
});
