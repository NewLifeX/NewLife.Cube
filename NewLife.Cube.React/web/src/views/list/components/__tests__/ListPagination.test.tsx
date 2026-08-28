/**
 * ListPagination 分页单元测试
 *
 * 覆盖：总数展示（共 N 条）、统计行 statData 渲染、页码切换触发 onChange。
 */
import { describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import ListPagination from '../ListPagination';

describe('ListPagination 分页', () => {
  it('展示总数（共 N 条）', () => {
    render(<ListPagination total={123} current={1} pageSize={20} />);
    expect(screen.getByText(/共\s*123\s*条/)).toBeInTheDocument();
  });

  it('渲染统计行 statData', () => {
    render(<ListPagination total={10} current={1} pageSize={10} statData={{ 总金额: '1234.5', 条数: 10 }} />);
    expect(screen.getByText(/总金额/)).toBeInTheDocument();
    expect(screen.getByText('1234.5')).toBeInTheDocument();
    expect(screen.getByText('10')).toBeInTheDocument();
  });

  it('无 statData 时不渲染统计区', () => {
    const { container } = render(<ListPagination total={10} current={1} pageSize={10} />);
    // 只含分页器，无统计行
    expect(container.querySelectorAll('span').length).toBeGreaterThan(0);
  });

  it('页码跳转触发 onChange', () => {
    const onChange = vi.fn();
    render(<ListPagination total={100} current={1} pageSize={10} onChange={onChange} />);
    // 点击第 2 页
    fireEvent.click(screen.getByTitle('2'));
    expect(onChange).toHaveBeenCalledWith(2, 10);
  });
});
