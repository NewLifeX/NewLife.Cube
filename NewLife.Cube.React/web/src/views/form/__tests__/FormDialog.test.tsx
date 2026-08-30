/**
 * FormDialog 表单弹窗单元测试
 *
 * 覆盖：按字段 Category 决定是否分组——有分类渲染 Tabs 标签页，
 * 全部无分类保持平铺；编辑模式行数据回填不受分组影响。
 *
 * 注：FieldControl 不透传 id，Form.Item 标签无法用 getByLabelText 关联定位，
 * 因此用 role/displayValue 等不依赖 label-for 关联的查询。
 */
import { describe, expect, it } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import FormDialog from '../FormDialog';
import type { DataField } from '@cube/api-core';
import type { FieldMapping } from '@cube/field-mapping';

/** 构造字段映射（widget 固定 text，按字段元数据驱动渲染） */
function mapping(name: string, partial: Partial<DataField> = {}): FieldMapping {
  return { widget: 'text', field: { name, typeName: 'String', ...partial } };
}

describe('FormDialog 表单弹窗', () => {
  it('字段带 Category → 渲染分类 Tabs 标签页', () => {
    render(
      <FormDialog
        open
        mode="add"
        fields={[
          mapping('Name', { category: '基本信息' }),
          mapping('Secret', { category: '扩展' }),
          mapping('Remark'),
        ]}
      />,
    );
    // 分类标签 + 默认组标签都在
    expect(screen.getByRole('tab', { name: '基本信息' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: '扩展' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: '常规设置' })).toBeInTheDocument();
    // 字段标签仍在（forceRender 全量渲染，激活 Tab 展示 Name）
    expect(screen.getByText('Name')).toBeInTheDocument();
  });

  it('字段全部无 Category → 平铺渲染，无 Tabs', () => {
    render(
      <FormDialog
        open
        mode="add"
        fields={[mapping('Name'), mapping('Mail', { itemType: 'mail' })]}
      />,
    );
    expect(screen.queryByRole('tab')).not.toBeInTheDocument();
    expect(screen.getByText('Name')).toBeInTheDocument();
    expect(screen.getByText('Mail')).toBeInTheDocument();
    expect(screen.getAllByRole('textbox')).toHaveLength(2);
  });

  it('编辑模式回填行数据，分组不影响取值', () => {
    render(
      <FormDialog
        open
        mode="edit"
        fields={[mapping('Name', { category: '基本信息' }), mapping('Enable', { category: '基本信息', typeName: 'Boolean' })]}
        row={{ Name: '张三', Enable: 'true' }}
      />,
    );
    expect(screen.getByDisplayValue('张三')).toBeInTheDocument();
  });

  it('保存触发 onSubmit 并携带表单值', async () => {
    let submitted: Record<string, unknown> | null = null;
    render(
      <FormDialog
        open
        mode="add"
        fields={[mapping('Name', { category: '基本信息' }), mapping('Secret', { category: '扩展' })]}
        onSubmit={async (data) => {
          submitted = data;
        }}
      />,
    );
    fireEvent.change(screen.getAllByRole('textbox')[0], { target: { value: '李四' } });
    fireEvent.click(screen.getByRole('button', { name: /保\s*存/ }));
    // onSubmit 为异步，等待完成后再断言
    await waitFor(() => {
      expect(submitted).toEqual({ Name: '李四' });
    });
  });
});
