// 字段映射规则单元测试（node:test + 已构建 dist，零额外依赖）
// 运行：在 packages/field-mapping 目录 `node --test tests/`（需先 `pnpm build` 生成 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { resolveWidget, resolveWidgets, toCamelCase, decomposePowerOfTwo } = require('../dist/index.cjs');

test('只读字段与主键映射为 readonly', () => {
  assert.equal(resolveWidget({ name: 'Id', typeName: 'Int32', primaryKey: true }).widget, 'readonly');
  assert.equal(resolveWidget({ name: 'X', typeName: 'String', readOnly: true }).widget, 'readonly');
});

test('itemType 精确映射：json/icon/markdown/time', () => {
  assert.equal(resolveWidget({ name: 'F', typeName: 'String', itemType: 'json' }).widget, 'json');
  assert.equal(resolveWidget({ name: 'F', typeName: 'String', itemType: 'icon' }).widget, 'icon');
  assert.equal(resolveWidget({ name: 'F', typeName: 'String', itemType: 'markdown' }).widget, 'markdown');
  assert.equal(resolveWidget({ name: 'F', typeName: 'String', itemType: 'time' }).widget, 'time');
});

test('itemType singleSelect + lovCode → lov（透传 lovCode）', () => {
  const r = resolveWidget({ name: 'Kind', typeName: 'Int32', itemType: 'singleSelect', lovCode: 'Enum.Kind' });
  assert.equal(r.widget, 'lov');
  assert.equal(r.props.lovCode, 'Enum.Kind');
  assert.equal(r.props.multiple, false);
});

test('itemType multipleSelect → lovMulti（多选转逗号）', () => {
  const r = resolveWidget({ name: 'Tags', typeName: 'String', itemType: 'multipleSelect', lovCode: 'Enum.Tags' });
  assert.equal(r.widget, 'lovMulti');
  assert.equal(r.props.multiple, true);
});

test('lovCode 优先于 dataSource', () => {
  const r = resolveWidget({ name: 'S', typeName: 'Int32', lovCode: 'Enum.S', dataSource: { '1': '一' } });
  assert.equal(r.widget, 'lov');
  assert.equal(r.props.lovCode, 'Enum.S');
});

test('lovCode + multiple → lovMulti', () => {
  const r = resolveWidget({ name: 'S', typeName: 'String', lovCode: 'Enum.S', multiple: true });
  assert.equal(r.widget, 'lovMulti');
});

test('dataSource → select', () => {
  const r = resolveWidget({ name: 'Kind', typeName: 'Int32', dataSource: { '1': '甲' } });
  assert.equal(r.widget, 'select');
});

test('typeName 推断：Boolean/DateTime/Date/TimeSpan/数值', () => {
  assert.equal(resolveWidget({ name: 'Enable', typeName: 'Boolean' }).widget, 'switch');
  assert.equal(resolveWidget({ name: 'T', typeName: 'DateTime' }).widget, 'datetime');
  assert.equal(resolveWidget({ name: 'D', typeName: 'Date' }).widget, 'date');
  assert.equal(resolveWidget({ name: 'Ts', typeName: 'TimeSpan' }).widget, 'time');
  assert.equal(resolveWidget({ name: 'N', typeName: 'Decimal' }).widget, 'number');
});

test('typeName Guid → readonly', () => {
  assert.equal(resolveWidget({ name: 'G', typeName: 'Guid' }).widget, 'readonly');
});

test('typeName Enum → lov', () => {
  assert.equal(resolveWidget({ name: 'E', typeName: 'Enum' }).widget, 'lov');
});

test('数值 scale 透传为精度', () => {
  const r = resolveWidget({ name: 'Price', typeName: 'Decimal', scale: 2 });
  assert.equal(r.widget, 'number');
  assert.equal(r.props.precision, 2);
});

test('名称模式：password/email/mobile/image/url', () => {
  assert.equal(resolveWidget({ name: 'Password', typeName: 'String' }).widget, 'password');
  assert.equal(resolveWidget({ name: 'Mail', typeName: 'String' }).widget, 'email');
  assert.equal(resolveWidget({ name: 'Mobile', typeName: 'String' }).widget, 'phone');
  assert.equal(resolveWidget({ name: 'Avatar', typeName: 'String' }).widget, 'image');
  assert.equal(resolveWidget({ name: 'Url', typeName: 'String' }).widget, 'url');
});

test('长文本 → textarea，默认 → text', () => {
  assert.equal(resolveWidget({ name: 'Remark', typeName: 'String', length: 500 }).widget, 'textarea');
  assert.equal(resolveWidget({ name: 'Content', typeName: 'String', length: 300 }).widget, 'textarea');
  assert.equal(resolveWidget({ name: 'Name', typeName: 'String', length: 50 }).widget, 'text');
});

test('resolveWidgets 批量映射保持顺序', () => {
  const fields = [
    { name: 'Name', typeName: 'String' },
    { name: 'Enable', typeName: 'Boolean' },
    { name: 'Kind', typeName: 'Int32', lovCode: 'Enum.Kind' },
  ];
  const list = resolveWidgets(fields);
  assert.deepEqual(list.map((x) => x.widget), ['text', 'switch', 'lov']);
});

test('toCamelCase 与 decomposePowerOfTwo 工具', () => {
  assert.equal(toCamelCase('User_Name'), 'userName');
  assert.equal(toCamelCase('UserName'), 'userName');
  assert.deepEqual(decomposePowerOfTwo(15), [1, 2, 4, 8]);
});
