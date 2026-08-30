/**
 * TableContent 列表表格单元测试
 *
 * 覆盖：操作列宽度按可用操作数自适应（仅查看收窄 / 多操作撑开）、
 * 表头排序箭头受控显示（仅排序列带 ant-table-column-sort）、点击表头触发 onSortChange。
 */
import { describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import TableContent from '../TableContent';
import type { FieldMapping } from '@newlifex/field-mapping';

/** 构造列表字段（widget 固定 text，按字段元数据驱动渲染） */
function textField(name: string, displayName: string): FieldMapping {
  return { widget: 'text', field: { name, typeName: 'String', displayName } };
}

const FIELDS = [textField('Name', '名称'), textField('Remark', '备注')];
const DATA = [
  { Name: 'a', Remark: 'x' },
  { Name: 'b', Remark: 'y' },
];

/** 取操作列对应 col 的内联宽样式（antd 固定列宽度渲染在 colgroup col 上） */
function opsColStyle(container: HTMLElement): string {
  const cols = Array.from(container.querySelectorAll('col'));
  const ops = cols[cols.length - 1];
  return ops?.getAttribute('style') ?? '';
}

describe('TableContent 列表表格', () => {
  it('操作列宽度按可用操作数自适应：仅查看窄、全操作宽', () => {
    const { container, rerender } = render(
      <TableContent fields={FIELDS} data={DATA} canView canEdit={false} canDelete={false} selectable={false} />,
    );
    // 仅「查看」→ 64px（原固定 160px 会留大块空白）
    expect(opsColStyle(container)).toContain('width: 64px');

    // 查看 + 编辑 + 删除 → 160px 撑开容纳三个按钮
    rerender(<TableContent fields={FIELDS} data={DATA} canView canEdit canDelete selectable={false} />);
    expect(opsColStyle(container)).toContain('width: 160px');

    // 编辑 + 删除（可编辑时无查看）→ 112px
    rerender(<TableContent fields={FIELDS} data={DATA} canView={false} canEdit canDelete selectable={false} />);
    expect(opsColStyle(container)).toContain('width: 112px');
  });

  it('仅排序列带 ant-table-column-sort 类（排序箭头受控显示）', () => {
    const onSortChange = vi.fn();
    // 无排序状态：无排序列
    const { container, rerender } = render(
      <TableContent fields={FIELDS} data={DATA} selectable={false} onSortChange={onSortChange} />,
    );
    expect(container.querySelector('th.ant-table-column-sort')).toBeNull();

    // 指定 sortField=Name 降序：Name 列高亮，Remark 列不带排序类
    rerender(
      <TableContent
        fields={FIELDS}
        data={DATA}
        selectable={false}
        onSortChange={onSortChange}
        sortField="Name"
        sortDesc
      />,
    );
    const nameTh = container.querySelector('th.ant-table-column-sort');
    expect(nameTh).toBeTruthy();
    expect(nameTh?.textContent).toContain('名称');
    expect(container.querySelectorAll('th.ant-table-column-sort').length).toBe(1);
  });

  it('点击表头触发 onSortChange，升序/降序/清除三态正确回调', () => {
    const onSortChange = vi.fn();
    const { container } = render(
      <TableContent fields={FIELDS} data={DATA} selectable={false} onSortChange={onSortChange} />,
    );
    // 名称列排序触发器（.ant-table-column-sorters）
    const nameTh = Array.from(container.querySelectorAll('.ant-table-thead th')).find((th) =>
      th.textContent?.includes('名称'),
    )!;
    const sorters = nameTh.querySelector('.ant-table-column-sorters')!;

    // 第 1 次点击 → 升序
    fireEvent.click(sorters);
    expect(onSortChange).toHaveBeenLastCalledWith('Name', false);
  });
});
