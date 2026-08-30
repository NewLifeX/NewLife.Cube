/**
 * SearchBar 搜索栏单元测试
 *
 * 覆盖：按字段渲染搜索控件、输入后搜索触发 onSearch 且携带清理后参数、
 * 重置触发 onReset 并清空输入。
 */
import { describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import SearchBar from '../SearchBar';
import type { FieldMapping } from '@newlifex/field-mapping';

/** 构造搜索字段 */
function textField(name: string, displayName: string): FieldMapping {
  return { widget: 'text', field: { name, typeName: 'String', displayName } };
}

function switchField(name: string, displayName: string): FieldMapping {
  return { widget: 'switch', field: { name, typeName: 'Boolean', displayName } };
}

describe('SearchBar 搜索栏', () => {
  it('按字段渲染搜索控件与标签', () => {
    render(<SearchBar fields={[textField('Name', '名称'), switchField('Enable', '启用')]} onSearch={() => {}} onReset={() => {}} />);
    expect(screen.getByText('名称')).toBeInTheDocument();
    expect(screen.getByText('启用')).toBeInTheDocument();
    // antd Button 中文自动插空格（"搜 索"），用空白正则匹配
    expect(screen.getByRole('button', { name: /搜\s*索/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /重\s*置/ })).toBeInTheDocument();
  });

  it('空字段列表时不渲染', () => {
    const { container } = render(<SearchBar fields={[]} onSearch={() => {}} onReset={() => {}} />);
    expect(container).toBeEmptyDOMElement();
  });

  it('输入关键词后点搜索，onSearch 收到清理后的参数', () => {
    const onSearch = vi.fn();
    render(<SearchBar fields={[textField('Name', '名称')]} onSearch={onSearch} onReset={() => {}} />);
    fireEvent.change(screen.getByPlaceholderText(/请输入名称/), { target: { value: 'admin' } });
    fireEvent.click(screen.getByRole('button', { name: /搜\s*索/ }));
    expect(onSearch).toHaveBeenCalledWith({ Name: 'admin' });
  });

  it('空输入点搜索不提交空值参数', () => {
    const onSearch = vi.fn();
    render(<SearchBar fields={[textField('Name', '名称')]} onSearch={onSearch} onReset={() => {}} />);
    fireEvent.click(screen.getByRole('button', { name: /搜\s*索/ }));
    expect(onSearch).toHaveBeenCalledWith({});
  });

  it('点重置触发 onReset 并清空输入', () => {
    const onReset = vi.fn();
    render(<SearchBar fields={[textField('Name', '名称')]} onSearch={() => {}} onReset={onReset} />);
    fireEvent.change(screen.getByPlaceholderText(/请输入名称/), { target: { value: 'admin' } });
    fireEvent.click(screen.getByRole('button', { name: /重\s*置/ }));
    expect(onReset).toHaveBeenCalledTimes(1);
    expect(screen.getByPlaceholderText(/请输入名称/)).toHaveValue('');
  });

  it('开关字段渲染是/否下拉', () => {
    render(<SearchBar fields={[switchField('Enable', '启用')]} onSearch={() => {}} onReset={() => {}} />);
    // Select 组件渲染为 combobox
    expect(screen.getByRole('combobox')).toBeInTheDocument();
  });

  it('字段过多时默认折叠，点击展开显示全部', () => {
    const fields = [
      textField('A', '甲'),
      textField('B', '乙'),
      textField('C', '丙'),
      textField('D', '丁'),
    ];
    render(<SearchBar fields={fields} onSearch={() => {}} onReset={() => {}} />);
    // jsdom 下 clientWidth=0 → 1 列 × 2 行，折叠默认只显示前 2 项
    expect(screen.getByText('甲')).toBeInTheDocument();
    expect(screen.getByText('乙')).toBeInTheDocument();
    expect(screen.queryByText('丙')).not.toBeInTheDocument();
    // 展开后全部显示
    fireEvent.click(screen.getByRole('button', { name: /展\s*开/ }));
    expect(screen.getByText('丙')).toBeInTheDocument();
    expect(screen.getByText('丁')).toBeInTheDocument();
    // 可再收起
    fireEvent.click(screen.getByRole('button', { name: /收\s*起/ }));
    expect(screen.queryByText('丙')).not.toBeInTheDocument();
  });
});
