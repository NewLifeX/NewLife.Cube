/**
 * Toolbar 工具栏单元测试
 *
 * 覆盖：权限控制（canAdd/canDelete/canExport/canImport/canChart）、
 * 删除选中仅选中时显示、表格/图表视图切换、高级菜单（导出5格式/导入/删除全部）。
 */
import { describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import Toolbar from '../Toolbar';

describe('Toolbar 工具栏', () => {
  it('默认权限展示新增/刷新/高级，无选中时不显示删除选中，canChart 时显示视图切换', () => {
    render(<Toolbar canChart />);
    expect(screen.getByRole('button', { name: /新\s*增/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /刷新/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /高\s*级/ })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /删除/ })).not.toBeInTheDocument();
    // 表格/图表视图切换（Segmented）
    expect(screen.getByText('表格')).toBeInTheDocument();
    expect(screen.getByText('图表')).toBeInTheDocument();
  });

  it('canAdd=false 时隐藏新增按钮', () => {
    render(<Toolbar canAdd={false} />);
    expect(screen.queryByRole('button', { name: /新\s*增/ })).not.toBeInTheDocument();
  });

  it('canDelete=false 时隐藏删除选中，高级菜单无删除全部', async () => {
    render(<Toolbar canDelete={false} selectedCount={3} />);
    expect(screen.queryByRole('button', { name: /删除/ })).not.toBeInTheDocument();
    fireEvent.mouseEnter(screen.getByRole('button', { name: /高\s*级/ }));
    await waitFor(() => {
      expect(screen.getByText('导出 Excel')).toBeInTheDocument();
    });
    expect(screen.queryByText('删除全部')).not.toBeInTheDocument();
  });

  it('canExport=false / canImport=false 时高级菜单无导出/导入项', async () => {
    render(<Toolbar canExport={false} canImport={false} />);
    fireEvent.mouseEnter(screen.getByRole('button', { name: /高\s*级/ }));
    await waitFor(() => {
      expect(screen.getByText('删除全部')).toBeInTheDocument();
    });
    expect(screen.queryByText('导出 Excel')).not.toBeInTheDocument();
    expect(screen.queryByText('导入 Excel/Json/Zip')).not.toBeInTheDocument();
  });

  it('选中行时显示删除选中并带数量，确认后触发 onDelete', async () => {
    const onDelete = vi.fn();
    render(<Toolbar selectedCount={3} onDelete={onDelete} />);
    expect(screen.getByRole('button', { name: /删除选中\s*\(3\)/ })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /删除选中\s*\(3\)/ }));
    await waitFor(() => {
      expect(screen.getByText('确定删除选中的 3 条数据吗？')).toBeInTheDocument();
    });
    // Popconfirm 确认按钮渲染在 portal（.ant-popover）内
    const okBtn = screen.getAllByRole('button', { name: /删\s*除/ }).find((el) => el.closest('.ant-popover'));
    expect(okBtn).toBeTruthy();
    fireEvent.click(okBtn!);
    await waitFor(() => {
      expect(onDelete).toHaveBeenCalledTimes(1);
    });
  });

  it('高级菜单包含全部 5 种导出格式，点击触发 onExport', async () => {
    const onExport = vi.fn();
    render(<Toolbar onExport={onExport} />);
    // antd Dropdown 默认 hover 触发，jsdom 用 mouseEnter 打开
    fireEvent.mouseEnter(screen.getByRole('button', { name: /高\s*级/ }));
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

  it('高级菜单导入触发 onImport', async () => {
    const onImport = vi.fn();
    render(<Toolbar onImport={onImport} />);
    fireEvent.mouseEnter(screen.getByRole('button', { name: /高\s*级/ }));
    await waitFor(() => {
      expect(screen.getByText('导入 Excel/Json/Zip')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('导入 Excel/Json/Zip'));
    await waitFor(() => {
      expect(onImport).toHaveBeenCalledTimes(1);
    });
  });

  it('高级菜单删除全部触发 onDeleteAll', async () => {
    const onDeleteAll = vi.fn();
    render(<Toolbar onDeleteAll={onDeleteAll} />);
    fireEvent.mouseEnter(screen.getByRole('button', { name: /高\s*级/ }));
    await waitFor(() => {
      expect(screen.getByText('删除全部')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('删除全部'));
    await waitFor(() => {
      expect(onDeleteAll).toHaveBeenCalledTimes(1);
    });
  });

  it('canChart=false 时不显示表格/图表视图切换', () => {
    render(<Toolbar canChart={false} />);
    expect(screen.queryByText('图表')).not.toBeInTheDocument();
  });

  it('切换视图触发 onViewChange', async () => {
    const onViewChange = vi.fn();
    render(<Toolbar canChart onViewChange={onViewChange} />);
    fireEvent.click(screen.getByText('图表'));
    await waitFor(() => {
      expect(onViewChange).toHaveBeenCalledWith('chart');
    });
  });

  it('新增按钮点击触发 onNew', () => {
    const onNew = vi.fn();
    render(<Toolbar onNew={onNew} />);
    fireEvent.click(screen.getByRole('button', { name: /新\s*增/ }));
    expect(onNew).toHaveBeenCalledTimes(1);
  });
});
