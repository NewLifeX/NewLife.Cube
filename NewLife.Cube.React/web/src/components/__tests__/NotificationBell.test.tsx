/**
 * NotificationBell 通知铃铛单元测试（E3）
 *
 * 覆盖：铃铛按钮渲染、点击触发 onClick、showLabel 文字标签、count 未读角标。
 */
import { describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import NotificationBell from '../NotificationBell';

describe('NotificationBell 通知铃铛', () => {
  it('渲染铃铛按钮', () => {
    render(<NotificationBell />);
    expect(screen.getByRole('button', { name: /通知/ })).toBeInTheDocument();
  });

  it('点击触发 onClick', () => {
    const onClick = vi.fn();
    render(<NotificationBell onClick={onClick} />);
    fireEvent.click(screen.getByRole('button', { name: /通知/ }));
    expect(onClick).toHaveBeenCalledTimes(1);
  });

  it('showLabel 时显示文字标签', () => {
    render(<NotificationBell showLabel label="消息" />);
    // antd Button 中文自动插空格（"消 息"）
    expect(screen.getByRole('button', { name: /消\s*息/ })).toHaveTextContent(/消\s*息/);
  });

  it('count>0 时显示未读角标', () => {
    render(<NotificationBell count={5} />);
    expect(screen.getByText('5')).toBeInTheDocument();
  });

  it('count=0 时不显示角标数字', () => {
    render(<NotificationBell count={0} />);
    expect(screen.queryByText('0')).not.toBeInTheDocument();
  });
});
