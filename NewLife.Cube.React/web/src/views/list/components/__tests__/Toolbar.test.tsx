/**
 * Toolbar 工具栏单元测试
 *
 * 覆盖：权限控制（canAdd/canDelete/canExport/canImport 为 false 时按钮隐藏）、
 * 批量删除计数展示、导出下拉项。
 */
import { describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import Toolbar from '../Toolbar';

describe('Toolbar 工具栏', () => {
  it('默认权限下展示全部操作按钮', () => {
    render(<Toolbar />);
    expect(screen.getByRole('button', { name: /新增/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /删除/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /导出/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /导入/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /图表/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /刷新/ })).toBeInTheDocument();
  });

  it('canAdd=false 时隐藏新增按钮', () => {
    render(<Toolbar canAdd={false} />);
    expect(screen.queryByRole('button', { name: /新增/ })).not.toBeInTheDocument();
  });

  it('canDelete=false 时隐藏删除按钮', () => {
    render(<Toolbar canDelete={false} />);
    expect(screen.queryByRole('button', { name: /删除/ })).not.toBeInTheDocument();
  });

  it('canExport=false / canImport=false 时隐藏对应按钮', () => {
    render(<Toolbar canExport={false} canImport={false} />);
    expect(screen.queryByRole('button', { name: /导出/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /导入/ })).not.toBeInTheDocument();
  });

  it('选中条数展示在删除按钮上', () => {
    render(<Toolbar selectedCount={3} />);
    expect(screen.getByRole('button', { name: /删除\s*\(3\)/ })).toBeInTheDocument();
  });

  it('导出下拉包含全部 5 种格式，点击触发 onExport', async () => {
    const onExport = vi.fn();
    render(<Toolbar onExport={onExport} />);
    // antd Dropdown 默认 hover 触发，jsdom 用 mouseEnter 打开
    fireEvent.mouseEnter(screen.getByRole('button', { name: /导出/ }));
    await waitFor(() => {
      expect(screen.getByText('导出 Excel')).toBeInTheDocument();
      expect(screen.getByText('导出 CSV')).toBeInTheDocument();
      expect(screen.getByText('导出 JSON')).toBeInTheDocument();
      expect(screen.getByText('导出 XML')).toBeInTheDocument();
      expect(screen.getByText('导出模板')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('导出 CSV'));
    await waitFor(() => {
      expect(onExport).toHaveBeenCalledWith('Csv');
    });
  });

  it('新增按钮点击触发 onNew', () => {
    const onNew = vi.fn();
    render(<Toolbar onNew={onNew} />);
    fireEvent.click(screen.getByRole('button', { name: /新增/ }));
    expect(onNew).toHaveBeenCalledTimes(1);
  });
});
