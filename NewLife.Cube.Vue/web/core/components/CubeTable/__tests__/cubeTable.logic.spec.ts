/**
 * `cubeTable.logic.ts` 纯函数单测
 *
 * 覆盖：搜索控件显示态（含 LOV 降级与内联字典升级）、条件折叠、列视图与主键列、
 * 工具栏/表格开关优先级、弹窗分档、单元格文本与值集多值、溢出提示判定。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/cubeTable.logic.spec.ts
 */
import { describe, expect, it } from 'vitest';
import {
  BOOLEAN_FILTER_OPTIONS,
  FILE_EXISTS_OPTIONS,
  applyLinkTemplate,
  buildColumnViews,
  buildSearchControl,
  buildToolbarPayload,
  cellBoolean,
  cellLinkUrl,
  cellParts,
  cellRawValue,
  cellText,
  cellTitle,
  fieldHeader,
  formatDecimalCell,
  formatMoneyCell,
  formatPercentCell,
  normalizeTextAlign,
  showSearchToggle,
  defaultColumnsStorageKey,
  dialogShapeFor,
  isExpandableCell,
  isOverflowTooltipCell,
  layoutRowActions,
  normalizeMulti,
  resolveGridFlags,
  resolveToolbarFlags,
  splitRowActions,
  toolbarKeyField,
  toolbarQueryString,
  truncate,
} from '../cubeTable.logic';
import { FIELDS } from './fakeCtx';
import type { FieldMeta } from '../../../types/field';
import type { CubeColumnView } from '../types';

const view = (field: FieldMeta): CubeColumnView => buildColumnViews([field])[0];

describe('CubeTable/logic 搜索控件显示态', () => {
  it('应该用字段名作键 当普通文本条件时', () => {
    const control = buildSearchControl(FIELDS.name);
    expect(control.kind).toBe('text');
    expect(control.key).toBe('Name');
    expect(control.maxKey).toBeNull();
    expect(control.options).toEqual([]);
    expect(control.span).toBe(1);
  });

  it('应该拆成 min/max 两个键 当数值与时间范围条件时', () => {
    expect(buildSearchControl(FIELDS.count)).toMatchObject({
      kind: 'numberRange',
      key: 'Count_min',
      maxKey: 'Count_max',
    });
    const span = buildSearchControl({ ...FIELDS.name, typeName: 'TimeSpan' });
    expect(span.kind).toBe('timeRange');
    expect(span.key).toBe('Name_min');
    expect(span.maxKey).toBe('Name_max');
  });

  it('应该保持单键数组值 当日期与日期时间范围时', () => {
    expect(buildSearchControl(FIELDS.time)).toMatchObject({ kind: 'datetimeRange', key: 'Time', maxKey: null });
    const day = buildSearchControl({ ...FIELDS.time, itemType: 'date' });
    expect(day.kind).toBe('dateRange');
  });

  it('应该按后端 range 项使用显式起止参数', () => {
    const control = buildSearchControl({
      name: 'dtStart',
      displayName: '创建时间',
      typeName: 'DateTime',
      kind: 'range',
      end: 'dtEnd',
      field: 'CreateTime',
    });
    expect(control).toMatchObject({
      kind: 'range',
      key: 'dtStart',
      maxKey: 'dtEnd',
      rangeType: 'datetime',
      span: 2,
    });
  });

  it('应该给是/否与有/无两套固定选项 当布尔与附件存在性条件时', () => {
    expect(buildSearchControl(FIELDS.enable)).toMatchObject({ kind: 'switch', options: BOOLEAN_FILTER_OPTIONS });
    expect(buildSearchControl(FIELDS.avatar)).toMatchObject({ kind: 'fileExists', options: FILE_EXISTS_OPTIONS });
  });

  it('应该用值集码值并把多选占整行 当字段带 lovCode 或 multipleSelect 时', () => {
    expect(buildSearchControl(FIELDS.kind)).toMatchObject({ kind: 'lov', key: 'Kind' });
    expect(buildSearchControl({ ...FIELDS.kind, multiple: true }).kind).toBe('lovMulti');
    expect(buildSearchControl({ ...FIELDS.kind, itemType: 'multipleSelect' }).span).toBe(2);
  });

  it('应该降级成文本框 当 Enum 字段没有码值时', () => {
    // resolveSearchControl 对 typeName==='Enum' 返回 lov，但没有 lovCode 就无从选择
    const control = buildSearchControl({ name: 'Stage', displayName: '阶段', typeName: 'Enum' });
    expect(control.kind).toBe('text');
  });

  it('应该升级为下拉并数字化键值 当字段带内联字典时', () => {
    const control = buildSearchControl(FIELDS.dict);
    expect(control.kind).toBe('dict');
    expect(control.options).toEqual([
      { label: '小时', value: 1 },
      { label: '班次', value: 2 },
    ]);
  });

  it('应该保留非数字字典键原样 当内联字典是名称型时', () => {
    const control = buildSearchControl({
      name: 'Level',
      displayName: '级别',
      typeName: 'String',
      dataSource: { A: '甲', B: '乙' },
    });
    expect(control.options).toEqual([
      { label: '甲', value: 'A' },
      { label: '乙', value: 'B' },
    ]);
  });
});

describe('CubeTable/logic 展开入口判定', () => {
  it('应该给展开按钮 当元数据里有搜索条件时', () => {
    expect(showSearchToggle(1)).toBe(true);
    expect(showSearchToggle(6)).toBe(true);
  });

  it('应该不给展开按钮 当没有任何搜索条件时', () => {
    expect(showSearchToggle(0)).toBe(false);
  });
});

describe('CubeTable/logic 列视图', () => {
  it('应该默认全显示并保留元数据顺序 当未启用设置列时', () => {
    const columns = buildColumnViews([FIELDS.id, FIELDS.name, FIELDS.count]);
    expect(columns.map((c) => c.key)).toEqual(['Id', 'Name', 'Count']);
  });

  it('应该过滤取消勾选的列 当传入列显隐结果时', () => {
    const columns = buildColumnViews([FIELDS.id, FIELDS.name, FIELDS.count], { Name: false });
    expect(columns.map((c) => c.key)).toEqual(['Id', 'Count']);
  });

  it('应该整列去掉主键列 当 showKeyColumn 为 false 时', () => {
    const columns = buildColumnViews([FIELDS.id, FIELDS.name], null, { showKeyColumn: false });
    expect(columns.map((c) => c.key)).toEqual(['Name']);
  });

  it('应该按控件类型给宽与对齐 当生成各类列时', () => {
    // 数值优先于主键（与 ListTableContent.columnAlign 同口径）：Int32 主键是右对齐
    expect(view(FIELDS.id)).toMatchObject({ width: 80, align: 'right', isKey: true });
    expect(view({ name: 'Guid', displayName: '标识', typeName: 'Guid', primaryKey: true })).toMatchObject({
      control: 'readonly',
      align: 'center',
    });
    expect(view(FIELDS.count)).toMatchObject({ control: 'number', align: 'right', width: 'auto' });
    expect(view(FIELDS.enable)).toMatchObject({ control: 'boolean', width: 90, align: 'center' });
    expect(view(FIELDS.avatar)).toMatchObject({ control: 'image', width: 100 });
    expect(view(FIELDS.time)).toMatchObject({ control: 'date', width: 165 });
    expect(view({ ...FIELDS.time, itemType: 'date' })).toMatchObject({ width: 120 });
    expect(view(FIELDS.name)).toMatchObject({ control: 'text', align: 'left', width: 'auto' });
  });

  it('应该回退字段名当显示名缺失时', () => {
    expect(view({ name: 'Weird', typeName: 'String' }).label).toBe('Weird');
  });
});

describe('CubeTable/logic 工具栏与表格开关', () => {
  it('应该默认开新增导入导出刷新、模板默认关 当后端没给开关时', () => {
    expect(resolveToolbarFlags({})).toEqual({
      add: true,
      batchDelete: true,
      importFile: true,
      exportFile: true,
      downloadTemplate: false,
      refresh: true,
      readOnly: false,
    });
  });

  it('应该跟随后端 enableAdd 与 enableSelect 当未显式指定时', () => {
    const flags = resolveToolbarFlags({}, { enableAdd: false, enableSelect: false });
    expect(flags.add).toBe(false);
    expect(flags.batchDelete).toBe(false);
  });

  it('应该让页面意图优先于后端开关 当两侧都给值时', () => {
    expect(resolveToolbarFlags({ add: true }, { enableAdd: false }).add).toBe(true);
  });

  it('应该标出整页只读 当后端 isReadOnly 时', () => {
    expect(resolveToolbarFlags({}, { isReadOnly: true }).readOnly).toBe(true);
  });

  it('应该在无更新和删除权限时收起勾选列', () => {
    expect(resolveGridFlags({}, {}, { update: false, delete: false })).toMatchObject({ selection: false });
  });

  it('应该让勾选列与批量删除同一口径 当 enableSelect 未下发时', () => {
    // 否则会出现「有批量删除按钮却没有勾选框」
    expect(resolveGridFlags({}, {})).toMatchObject({ selection: true, rowActions: true });
    expect(resolveGridFlags({}, { enableSelect: false })).toMatchObject({ selection: false });
  });

  it('应该在只读时关掉操作列 当未显式指定 rowActions 时', () => {
    expect(resolveGridFlags({}, { isReadOnly: true }).rowActions).toBe(false);
    expect(resolveGridFlags({ rowActions: true }, { isReadOnly: true }).rowActions).toBe(true);
  });

  it('应该默认关索引列与双击编辑 当后端没声明时', () => {
    expect(resolveGridFlags({}, {})).toMatchObject({ indexColumn: false, doubleClickEdit: false });
    expect(resolveGridFlags({}, { enableTableDoubleClick: true }).doubleClickEdit).toBe(true);
  });
});

describe('CubeTable/logic 操作列自适应与收起', () => {
  it.each([
    [1, false, 80],
    [3, false, 208],
    [4, false, 272],
  ])('应该按个数撑宽且不收起 当动作数为 %i 时', (count, folded, width) => {
    expect(layoutRowActions(count)).toEqual({ folded, width });
  });

  it('应该按 4 个单元封顶宽度并标收起 当动作数超过 4 时', () => {
    // 5 个和 8 个同宽：后加的动作一律进「更多」，不再撑宽列
    expect(layoutRowActions(5)).toEqual({ folded: true, width: 272 });
    expect(layoutRowActions(8).width).toBe(layoutRowActions(5).width);
  });

  it('应该按一个单元保底 当动作数为 0 时', () => {
    // 空列表也要给操作列一个稳定宽度，不能退化成 0 宽把表头挤变形
    expect(layoutRowActions(0).width).toBe(80);
  });

  it('应该最多 4 个全部直接显示 当未超过收起阈值时', () => {
    const items = ['a', 'b', 'c', 'd'];
    expect(splitRowActions(items)).toEqual({ direct: items, overflow: [] });
  });

  it('应该前 3 个直接显示、其余收进更多 当有 5 个动作时', () => {
    const items = ['a', 'b', 'c', 'd', 'e'];
    expect(splitRowActions(items)).toEqual({ direct: ['a', 'b', 'c'], overflow: ['d', 'e'] });
  });
});

describe('CubeTable/logic 弹窗分档', () => {
  it.each([
    [5, '700px', 2],
    [12, '860px', 2],
    [20, '960px', 3],
  ])('应该按字段数给宽与列数 当字段数为 %i 时', (count, width, columns) => {
    expect(dialogShapeFor(count)).toEqual({ width, columns });
  });
});

describe('CubeTable/logic 单元格渲染', () => {
  it('应该翻译值集并用顿号连接 当值集列有码值时', () => {
    const column = view(FIELDS.kind);
    const options = { lovLabel: (_code: string, value: unknown) => `类型${value}` };
    expect(cellText(column, '1', options)).toBe('类型1');
    expect(cellText(column, '1,2', options)).toBe('类型1、类型2');
    expect(cellParts(column, '[1,2]', options)).toEqual(['类型1', '类型2']);
  });

  it('应该原样连接多值 当值集列没有 lovCode 时', () => {
    // itemType=singleSelect → 值集列，但后端没给 lovCode：只做拆分，不查标签
    const noCode: FieldMeta = { name: 'Tag', displayName: '标签', typeName: 'String', itemType: 'singleSelect' };
    expect(cellParts(view(noCode), 'a,b')).toEqual(['a', 'b']);
    // 普通文本列不拆分，逗号是内容本身
    expect(cellParts(view(FIELDS.name), 'a,b')).toEqual(['a,b']);
  });

  it('应该归一日期时间与只取日期 当列是日期类时', () => {
    expect(cellText(view(FIELDS.time), '2026-10-19T08:30:00')).toBe('2026-10-19 08:30:00');
    expect(cellText(view({ ...FIELDS.time, itemType: 'date' }), '2026-10-19T08:30:00')).toBe('2026-10-19');
    expect(cellText(view({ ...FIELDS.time, typeName: 'TimeSpan', itemType: 'time' }), '08:30')).toBe('08:30:00');
  });

  it('应该显示是否而不是强转布尔 当布尔列取值时', () => {
    // Boolean('false') === true 是经典坑：字符串必须显式比较
    expect(cellText(view(FIELDS.enable), 'false')).toBe('否');
    expect(cellText(view(FIELDS.enable), true)).toBe('是');
    expect(cellText(view(FIELDS.enable), 1)).toBe('是');
    expect(cellBoolean('0')).toBe(false);
    expect(cellBoolean(2)).toBe(true);
  });

  it('应该截断长文本并保留全文判定 当列是 Json 或富文本时', () => {
    const long = 'x'.repeat(60);
    expect(cellText(view(FIELDS.payload), long)).toHaveLength(41);
    expect(cellText(view(FIELDS.payload), long).endsWith('…')).toBe(true);
    expect(isExpandableCell(view(FIELDS.payload).control)).toBe(true);
  });

  it('应该把空值判成无片段而不是字符串 null 当单元格为空时', () => {
    for (const raw of [null, undefined, '']) {
      expect(cellParts(view(FIELDS.name), raw)).toEqual([]);
      expect(cellText(view(FIELDS.name), raw)).toBe('');
    }
    // 0 与 false 是有效值，必须显示出来
    expect(cellParts(view(FIELDS.count), 0)).toEqual(['0']);
  });

  it('应该只给纯文本类列开溢出提示 当列形态各异时', () => {
    expect(isOverflowTooltipCell(view(FIELDS.name).control)).toBe(true);
    expect(isOverflowTooltipCell(view(FIELDS.count).control)).toBe(true);
    expect(isOverflowTooltipCell(view(FIELDS.avatar).control)).toBe(false);
    expect(isOverflowTooltipCell(view(FIELDS.enable).control)).toBe(false);
  });

  it('应该折叠空白后截断 当摘要富文本时', () => {
    expect(truncate('a\n\n b')).toBe('a b');
    expect(truncate('')).toBe('');
  });

  it('应该把三种外形都归一成字符串数组 当值集多值下发时', () => {
    expect(normalizeMulti(['1', 2])).toEqual(['1', '2']);
    expect(normalizeMulti('[1,2]')).toEqual(['1', '2']);
    expect(normalizeMulti(' 1 , 2 ')).toEqual(['1', '2']);
    expect(normalizeMulti('[坏 JSON')).toEqual(['[坏 JSON']);
    expect(normalizeMulti(3)).toEqual(['3']);
    expect(normalizeMulti('')).toEqual([]);
  });
});

describe('CubeTable/logic 存储键', () => {
  it('应该按实体前缀分桶 当生成列显隐存储键时', () => {
    expect(defaultColumnsStorageKey('/api/Equipments/Equipment')).toBe(
      'cube-table:columns:/api/Equipments/Equipment',
    );
  });
});
describe('工具栏按钮参数组装', () => {
  it('include 含 keys 时提交选中行主键（逗号拼接，与批量删除一致）', () => {
    const payload = buildToolbarPayload({
      include: ['keys'],
      selected: [{ Id: 1 }, { Id: 2 }],
      keyField: 'Id',
      query: {},
    });
    expect(payload).toEqual({ keys: '1,2' });
  });

  it('未选中时不提交 keys，避免后端拿到空串误判', () => {
    const payload = buildToolbarPayload({ include: ['keys'], selected: [], keyField: 'Id', query: {} });
    expect(payload).toEqual({});
  });

  it('其它名称取当前查询值，空值跳过', () => {
    const payload = buildToolbarPayload({
      include: ['ds', 'sourceType', 'dtStart'],
      selected: [],
      keyField: 'Id',
      query: { ds: '20250101', sourceType: 2, dtStart: '' },
    });
    expect(payload).toEqual({ ds: '20250101', sourceType: 2 });
  });

  it('数组查询值按逗号拼接', () => {
    const payload = buildToolbarPayload({
      include: ['status'],
      selected: [],
      keyField: 'Id',
      query: { status: [1, 2] },
    });
    expect(payload).toEqual({ status: '1,2' });
  });

  it('主键字段取自元数据，缺省 id', () => {
    expect(toolbarKeyField([{ name: 'Code', typeName: 'String' }, { name: 'Id', typeName: 'Int32', primaryKey: true }])).toBe('Id');
    expect(toolbarKeyField([])).toBe('id');
  });

  it('选中行主键大小写不一致时仍能取值', () => {
    const payload = buildToolbarPayload({
      include: ['keys'],
      selected: [{ ID: 7 }, { id: 8 }],
      keyField: 'Id',
      query: {},
    });
    expect(payload).toEqual({ keys: '7,8' });
  });

  it('link 的查询串携带关键字与搜索模型', () => {
    const qs = toolbarQueryString('abc', { ds: '20250101', status: undefined, tags: ['a', 'b'] });
    expect(qs).toContain('q=abc');
    expect(qs).toContain('ds=20250101');
    expect(qs).toContain('tags=a%2Cb');
    expect(qs).not.toContain('status');
  });
});

describe('CubeTable/logic 列表呈现属性', () => {
  it('列头应优先使用后端 header', () => {
    expect(fieldHeader({ name: 'OrderNo', displayName: '订单号', header: '单号', typeName: 'String' })).toBe('单号');
    expect(fieldHeader({ name: 'OrderNo', displayName: '订单号', typeName: 'String' })).toBe('订单号');
    expect(fieldHeader({ name: 'OrderNo', typeName: 'String' })).toBe('OrderNo');
  });

  it('应识别后端 TextAligns 数值与常见字符串', () => {
    expect(normalizeTextAlign(1)).toBe('left');
    expect(normalizeTextAlign(2)).toBe('center');
    expect(normalizeTextAlign(3)).toBe('right');
    expect(normalizeTextAlign('text-center')).toBe('center');
    expect(normalizeTextAlign(0)).toBeUndefined();
    expect(normalizeTextAlign(5)).toBeUndefined();
  });

  it('列视图应采用 header 与 textAlign', () => {
    const column = view({ ...FIELDS.name, header: '客户名', textAlign: 2 });
    expect(column.label).toBe('客户名');
    expect(column.align).toBe('center');
  });

  it('valueField 有值时优先，空值回退本字段', () => {
    const column = view({ ...FIELDS.name, valueField: 'NickName' });
    expect(cellRawValue({ Name: '张三', NickName: '小三' }, column)).toBe('小三');
    expect(cellRawValue({ Name: '张三', NickName: '' }, column)).toBe('张三');
    expect(cellRawValue({ name: '李四' }, column)).toBe('李四');
  });

  it('链接模板应替换行字段、多级路径与页面参数', () => {
    const url = applyLinkTemplate(
      '/School/StudentDetail?studentId={Id}&org={Parent.Name}&page={page:pageIndex}',
      { Id: 7, Parent: { Name: '一班' } },
      { pageIndex: 3 },
    );
    expect(url).toBe('/School/StudentDetail?studentId=7&org=一班&page=3');
  });

  it('链接与提示文字应走同一套占位符', () => {
    const column = view({
      ...FIELDS.name,
      url: '/Detail?Id={Id}',
      title: '查看 {Name}',
      headerTitle: '查看 {Name}',
    });
    const row = { Id: 9, Name: '泵体' };
    expect(cellLinkUrl(column, row)).toBe('/Detail?Id=9');
    expect(cellTitle(column, row)).toBe('查看 泵体');
  });

  it('text 模板优先于原始值，并按 maxWidth 截断', () => {
    const column = view({ ...FIELDS.name, text: '打开{Name}', maxWidth: 4 });
    expect(cellText(column, '原值', { row: { Name: '设备档案' } })).toBe('打开设备…');
  });

  it('percent 按比例乘 100 并按描述单位缩放', () => {
    const field: FieldMeta = { name: 'Rate', typeName: 'Double', itemType: 'percent', scale: 2 };
    // 与 MVC "p{scale-2}" 同口径：scale=2 → 0 位小数
    expect(formatPercentCell(field, 0.1234)).toBe('12%');
    // 描述中的“百分之一”表示存储值为百分比数的 1/100
    expect(formatPercentCell({ ...field, description: '百分之一' }, 12.34)).toBe('12.34%');
  });

  it('money 默认两位小数并带千分位', () => {
    const field: FieldMeta = { name: 'Amount', typeName: 'Decimal', itemType: 'money' };
    expect(formatMoneyCell(field, 1234.5)).toBe('¥1,234.50');
  });

  it('decimal(p,s) 按声明位数补零', () => {
    const field: FieldMeta = { name: 'Price', typeName: 'Decimal', itemType: 'decimal(18,3)' };
    expect(formatDecimalCell(field, 12.3)).toBe('12.300');
  });
});