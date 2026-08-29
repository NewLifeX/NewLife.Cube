/**
 * LovSelectTable 表格弹窗选择器单元测试（E1）
 *
 * 覆盖：toValueArray 值归一、入口展示、打开弹窗加载数据、
 * 单选行点击回填、多选勾选确定回填。
 */
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import LovSelectTable, { toValueArray } from '../LovSelectTable';
import { fetchLovMeta, fetchLovListData } from '@/api/lov';
import type { LovListMeta } from '@/types/lov';

vi.mock('@/api/lov', () => ({
  fetchLovMeta: vi.fn(),
  fetchLovListData: vi.fn(),
}));

const mockMeta: LovListMeta = {
  lovCode: 'List.User',
  type: 'LIST',
  name: '用户',
  valueField: 'id',
  labelField: 'name',
  listConfig: {
    requestUrl: '',
    method: 'POST',
    pageable: true,
    pageNumField: 'pageNum',
    pageSizeField: 'pageSize',
    dataPath: null,
    totalPath: null,
    fixedParams: null,
    proxyRequest: true,
  },
  searchFields: [
    { field: 'name', title: '名称', componentType: 'input', paramType: 'BODY', required: false, defaultValue: null, refLovCode: null },
  ],
  tableColumns: [
    { field: 'name', title: '名称', width: 200, align: 'left', sortable: false, refLovCode: null, formatType: null },
  ],
};

const mockRows = [
  { id: 1, name: '张三' },
  { id: 2, name: '李四' },
];

describe('toValueArray 值归一', () => {
  it('null/undefined → 空数组', () => {
    expect(toValueArray(undefined)).toEqual([]);
    expect(toValueArray(null)).toEqual([]);
  });

  it('数组 → 字符串数组', () => {
    expect(toValueArray([1, 2])).toEqual(['1', '2']);
    expect(toValueArray(['a', 'b'])).toEqual(['a', 'b']);
  });

  it('逗号分隔字符串 → 拆分为数组并清理空白', () => {
    expect(toValueArray('1, 2 ,3')).toEqual(['1', '2', '3']);
  });

  it('数字 → 单元素数组', () => {
    expect(toValueArray(42)).toEqual(['42']);
  });
});

describe('LovSelectTable 表格弹窗', () => {
  beforeEach(() => {
    vi.mocked(fetchLovMeta).mockResolvedValue([mockMeta]);
    vi.mocked(fetchLovListData).mockResolvedValue({ data: mockRows, total: 2 });
  });

  it('渲染只读入口并展示当前值', () => {
    render(<LovSelectTable value="1" lovCode="List.User" />);
    expect(screen.getByRole('textbox')).toHaveValue('1');
  });

  it('点击入口打开弹窗并加载表格数据', async () => {
    render(<LovSelectTable lovCode="List.User" />);
    fireEvent.click(screen.getByRole('textbox'));
    await waitFor(() => {
      expect(screen.getByText('选择用户')).toBeInTheDocument();
    });
    await waitFor(() => {
      expect(screen.getByText('张三')).toBeInTheDocument();
      expect(screen.getByText('李四')).toBeInTheDocument();
    });
    // 搜索栏按元数据渲染
    expect(screen.getByPlaceholderText('名称')).toBeInTheDocument();
  });

  it('单选：点击行触发 onChange 并关闭弹窗', async () => {
    const onChange = vi.fn();
    render(<LovSelectTable lovCode="List.User" onChange={onChange} />);
    fireEvent.click(screen.getByRole('textbox'));
    await waitFor(() => expect(screen.getByText('张三')).toBeInTheDocument());
    fireEvent.click(screen.getByText('张三'));
    await waitFor(() => {
      expect(onChange).toHaveBeenCalledWith('1');
    });
  });

  it('多选：勾选后确定触发 onChange 数组', async () => {
    const onChange = vi.fn();
    render(<LovSelectTable lovCode="List.User" multiple onChange={onChange} />);
    fireEvent.click(screen.getByRole('textbox'));
    await waitFor(() => expect(screen.getByText('张三')).toBeInTheDocument());
    // 点击表头全选框（第 1 个 checkbox）选中当前页全部行
    const checkboxes = screen.getAllByRole('checkbox');
    fireEvent.click(checkboxes[0]);
    fireEvent.click(screen.getByRole('button', { name: /确\s*定/ }));
    await waitFor(() => {
      expect(onChange).toHaveBeenCalledWith(['1', '2']);
    });
  });

  it('多选：无选中时确定传空数组', async () => {
    const onChange = vi.fn();
    render(<LovSelectTable lovCode="List.User" multiple onChange={onChange} />);
    fireEvent.click(screen.getByRole('textbox'));
    await waitFor(() => expect(screen.getByText('张三')).toBeInTheDocument());
    fireEvent.click(screen.getByRole('button', { name: /确\s*定/ }));
    await waitFor(() => {
      expect(onChange).toHaveBeenCalledWith([]);
    });
  });
});
