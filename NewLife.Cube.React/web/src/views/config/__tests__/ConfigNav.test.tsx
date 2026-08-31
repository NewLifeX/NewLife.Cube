/**
 * ConfigNav 配置中心导航单元测试
 *
 * 覆盖：
 * - findConfigCenter：精确/前缀匹配、大小写不敏感、防误匹配（/Admin/CubeXxx）
 * - resolveConfigTitle / isConfigCenterPath：命中与回退
 * - ConfigNav 渲染：ReactSetting.configNavFlat=true 一字排开全部配置页；false 时 Segmented 核心 + 更多下拉
 */
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import ConfigNav, {
  CONFIG_NAV,
  MORE_NAV,
  ALL_CONFIG_NAV,
  findConfigCenter,
  isConfigCenterPath,
  resolveConfigTitle,
} from '../ConfigNav';

vi.mock('react-router-dom', () => ({
  useNavigate: () => vi.fn(),
}));

/** React 皮肤设置 mock：测试中可切换 configNavFlat */
const flatMock = vi.fn().mockReturnValue({ formStyle: 'inline', descMode: 1, configNavFlat: true });
vi.mock('@/stores/reactSetting', () => ({
  useReactSetting: () => flatMock(),
}));

describe('findConfigCenter 配置路径匹配', () => {
  it('精确匹配核心配置', () => {
    expect(findConfigCenter('/Admin/Cube')?.label).toBe('魔方设置');
    expect(findConfigCenter('/Admin/Core')?.label).toBe('基本设置');
    expect(findConfigCenter('/Admin/Star')?.label).toBe('星尘设置');
    expect(findConfigCenter('/Admin/React')?.label).toBe('React设置');
  });

  it('精确匹配更多配置', () => {
    expect(findConfigCenter('/Admin/SmsConfig')?.label).toBe('短信设置');
    expect(findConfigCenter('/Admin/OAuthConfig')?.label).toBe('OAuth设置');
    expect(findConfigCenter('/Admin/AccessRule')?.label).toBe('访问规则');
  });

  it('前缀匹配表单类子路径', () => {
    expect(findConfigCenter('/Admin/SmsConfig/Edit')?.label).toBe('短信设置');
    expect(findConfigCenter('/Admin/Cube/GetPage')?.label).toBe('魔方设置');
  });

  it('大小写不敏感匹配', () => {
    expect(findConfigCenter('/admin/cube')?.label).toBe('魔方设置');
    expect(findConfigCenter('/ADMIN/MAILCONFIG')?.label).toBe('邮件设置');
  });

  it('不误匹配相似路径', () => {
    expect(findConfigCenter('/Admin/CubeXxx')).toBeUndefined();
    expect(findConfigCenter('/Admin/SysConfig')).toBeUndefined();
  });

  it('非配置路径返回 undefined', () => {
    expect(findConfigCenter('/Admin/User')).toBeUndefined();
    expect(findConfigCenter('')).toBeUndefined();
  });
});

describe('resolveConfigTitle / isConfigCenterPath', () => {
  it('命中返回配置显示名', () => {
    expect(resolveConfigTitle('/Admin/XCode')).toBe('数据中间件');
    expect(resolveConfigTitle('/Admin/MailConfig')).toBe('邮件设置');
  });

  it('未命中返回 fallback', () => {
    expect(resolveConfigTitle('/Admin/User', '默认页面')).toBe('默认页面');
    expect(resolveConfigTitle('/Admin/User')).toBe('');
  });

  it('isConfigCenterPath 判断正确', () => {
    expect(isConfigCenterPath('/Admin/Cube')).toBe(true);
    expect(isConfigCenterPath('/Admin/SmsConfig/Edit')).toBe(true);
    expect(isConfigCenterPath('/Admin/User')).toBe(false);
  });
});

describe('ConfigNav 渲染', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('configNavFlat=true 时全部配置页一字排开（含更多配置，无下拉）', () => {
    flatMock.mockReturnValue({ formStyle: 'inline', descMode: 1, configNavFlat: true });
    render(<ConfigNav currentPath="/Admin/Cube" />);
    // 核心配置 + 更多配置全部平铺可见
    for (const item of ALL_CONFIG_NAV) {
      expect(screen.getByText(item.label)).toBeTruthy();
    }
    expect(screen.queryByText(/更多配置/)).toBeNull();
    expect(screen.queryByText(/短信设置/)).toBeTruthy();
  });

  it('configNavFlat=true 时当前项高亮（primary）', () => {
    flatMock.mockReturnValue({ formStyle: 'inline', descMode: 1, configNavFlat: true });
    const { container } = render(<ConfigNav currentPath="/Admin/Cube" />);
    const currentBtn = Array.from(container.querySelectorAll('.cube-config-nav-item')).find(
      (b) => b.textContent === '魔方设置',
    );
    expect(currentBtn?.className).toContain('ant-btn-primary');
  });

  it('configNavFlat=false 时渲染核心配置 Segmented 与更多下拉', () => {
    flatMock.mockReturnValue({ formStyle: 'inline', descMode: 1, configNavFlat: false });
    render(<ConfigNav currentPath="/Admin/Cube" />);
    for (const item of CONFIG_NAV) {
      expect(screen.getByText(item.label)).toBeTruthy();
    }
    expect(screen.getByText(/更多配置/)).toBeTruthy();
    for (const item of MORE_NAV) {
      expect(screen.queryByText(item.label)).toBeNull(); // 下拉未展开不渲染
    }
  });

  it('configNavFlat=false 时当前为核心配置高亮对应 Segmented 项', () => {
    flatMock.mockReturnValue({ formStyle: 'inline', descMode: 1, configNavFlat: false });
    const { container } = render(<ConfigNav currentPath="/Admin/Cube" />);
    const selected = container.querySelector('.ant-segmented-item-selected');
    expect(selected?.textContent).toContain('魔方设置');
  });

  it('configNavFlat=false 时当前为更多配置高亮更多下拉按钮', () => {
    flatMock.mockReturnValue({ formStyle: 'inline', descMode: 1, configNavFlat: false });
    const { container } = render(<ConfigNav currentPath="/Admin/SmsConfig" />);
    const moreBtn = container.querySelector('.cube-config-nav-more');
    expect(moreBtn?.className).toContain('active');
    expect(container.querySelector('.ant-segmented-item-selected')).toBeNull();
  });

  it('configNavFlat=false 时未知路径不高亮任何项', () => {
    flatMock.mockReturnValue({ formStyle: 'inline', descMode: 1, configNavFlat: false });
    const { container } = render(<ConfigNav currentPath="/Admin/User" />);
    expect(container.querySelector('.ant-segmented-item-selected')).toBeNull();
    expect(container.querySelector('.cube-config-nav-more')?.className).not.toContain('active');
  });
});
