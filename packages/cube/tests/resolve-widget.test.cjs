// 字段映射规则单元测试（node:test + 已构建 dist，零额外依赖）
// 运行：在 packages/field-mapping 目录 `node --test tests/`（需先 `pnpm build` 生成 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  resolveWidget,
  resolveWidgets,
  toCamelCase,
  decomposePowerOfTwo,
  timeSpanUnitOfDescription,
  splitTimeSpan,
  joinTimeSpan,
  mapValueKey,
  isNumericMapKey,
} = require('../dist/index.cjs');

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

test('itemType 文本类别名：password/phone/fax/email/textarea/richtext', () => {
  const w = (itemType) => resolveWidget({ name: 'F', typeName: 'String', itemType }).widget;
  assert.equal(w('password'), 'password');
  assert.equal(w('phone'), 'phone');
  assert.equal(w('fax'), 'phone');
  assert.equal(w('email'), 'email');
  assert.equal(w('textarea'), 'textarea');
  assert.equal(w('richtext'), 'html');
  assert.equal(w('avatar'), 'image');
  assert.equal(w('attachment'), 'file');
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

test('dataSource + 字段名以 s 结尾 → lovMulti（对齐 MVC 约定）', () => {
  const r = resolveWidget({ name: 'RoleIds', typeName: 'String', dataSource: { '2': '管理员' } });
  assert.equal(r.widget, 'lovMulti');
  assert.equal(r.props.multiple, true);
});

test('dataSource + multiple 显式标记 → lovMulti', () => {
  const r = resolveWidget({ name: 'Tags', typeName: 'String', dataSource: { '1': 'A' }, multiple: true });
  assert.equal(r.widget, 'lovMulti');
  assert.equal(r.props.multiple, true);
});

test('DateTime 字段名以 Date 结尾 → 纯日期 date', () => {
  assert.equal(resolveWidget({ name: 'Birthday', typeName: 'DateTime' }).widget, 'date');
  assert.equal(resolveWidget({ name: 'LastLoginDay', typeName: 'DateTime' }).widget, 'date');
});

test('DateTime itemType=date → 纯日期 date，否则 datetime', () => {
  assert.equal(resolveWidget({ name: 'T', typeName: 'DateTime', itemType: 'date' }).widget, 'date');
  assert.equal(resolveWidget({ name: 'CreateTime', typeName: 'DateTime' }).widget, 'datetime');
});

test('真实枚举类型名（非基元）→ lov', () => {
  assert.equal(resolveWidget({ name: 'Sex', typeName: 'SexKinds' }).widget, 'lov');
  assert.equal(resolveWidget({ name: 'Status', typeName: 'OrderStatus' }).widget, 'lov');
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

// ───────────────────────── ItemType 全量对齐（MVC _Form_Item / _Form_String / _Form_Editor） ─────────────────────────

test('ItemType=TimeSpan → timeSpan 时长控件，单位按描述推断，格式原样透传', () => {
  const sec = resolveWidget({ name: 'OnlineTime', typeName: 'Int32', itemType: 'TimeSpan', description: '累计在线总时间，单位秒' });
  assert.equal(sec.widget, 'timeSpan');
  assert.equal(sec.props.unit, 'seconds');
  assert.equal(sec.props.format, undefined);

  const ms = resolveWidget({ name: 'Cost', typeName: 'Int32', itemType: 'TimeSpan', description: '耗时，单位毫秒' });
  assert.equal(ms.props.unit, 'milliseconds');

  const fmt = resolveWidget({ name: 'OnlineTime', typeName: 'Int32', itemType: "TimeSpan:d'.'hh':'mm':'ss", description: '秒' });
  assert.equal(fmt.widget, 'timeSpan');
  assert.equal(fmt.props.format, "d'.'hh':'mm':'ss");
});

test('ItemType=area/area1~4 → area 级联并带层级（area 默认三级）', () => {
  assert.equal(resolveWidget({ name: 'AreaId', typeName: 'Int32', itemType: 'area' }).props.areaLevel, 3);
  assert.equal(resolveWidget({ name: 'AreaId', typeName: 'Int32', itemType: 'area1' }).props.areaLevel, 1);
  assert.equal(resolveWidget({ name: 'AreaId', typeName: 'Int32', itemType: 'area2' }).props.areaLevel, 2);
  assert.equal(resolveWidget({ name: 'AreaId', typeName: 'Int32', itemType: 'area3' }).props.areaLevel, 3);
  assert.equal(resolveWidget({ name: 'AreaId', typeName: 'Int32', itemType: 'area4' }).props.areaLevel, 4);
});

test('映射字段：有数据源 → mapSelect，整型键无数据源 → mapInput，非整型无数据源 → readonly', () => {
  const withDs = resolveWidget({
    name: 'RoleName',
    typeName: 'String',
    mapField: 'RoleID',
    mapTypeName: 'Int32',
    dataSource: { '1': '管理员' },
  });
  assert.equal(withDs.widget, 'mapSelect');
  assert.equal(withDs.props.mapField, 'RoleID');

  const noDs = resolveWidget({ name: 'RoleName', typeName: 'String', mapField: 'RoleID', mapTypeName: 'Int32' });
  assert.equal(noDs.widget, 'mapInput');

  const text = resolveWidget({ name: 'DeptName', typeName: 'String', mapField: 'DeptCode' });
  assert.equal(text.widget, 'readonly');

  // 映射字段本身只读，但必须在只读判定之前处理（否则下拉永远不出现）
  const readonlyMap = resolveWidget({
    name: 'RoleName',
    typeName: 'String',
    readOnly: true,
    mapField: 'RoleID',
    mapTypeName: 'Int32',
    dataSource: { '1': '管理员' },
  });
  assert.equal(readonlyMap.widget, 'mapSelect');
});

test('映射字段 + lovCode → lov（大数据量关联表走远程值集）', () => {
  const r = resolveWidget({
    name: 'RoleName',
    typeName: 'String',
    mapField: 'RoleID',
    mapTypeName: 'Int32',
    lovCode: 'Role',
    dataSource: { '1': '管理员' },
  });
  assert.equal(r.widget, 'lov');
  assert.equal(r.props.lovCode, 'Role');
});

test('数值类 ItemType：percent/money/decimal(p,s)/number/gmk', () => {
  assert.deepEqual(resolveWidget({ name: 'Rate', typeName: 'Double', itemType: 'percent' }).props.percent, true);
  assert.deepEqual(resolveWidget({ name: 'Rate', typeName: 'Double', itemType: 'Percentage' }).props.percent, true);
  assert.equal(resolveWidget({ name: 'Price', typeName: 'Decimal', itemType: 'money' }).props.money, true);
  assert.equal(resolveWidget({ name: 'Size', typeName: 'Int64', itemType: 'GMK' }).props.gmk, true);
  assert.equal(resolveWidget({ name: 'N', typeName: 'String', itemType: 'number' }).widget, 'number');

  const dec = resolveWidget({ name: 'Amt', typeName: 'Decimal', itemType: 'decimal(18,2)' });
  assert.equal(dec.widget, 'number');
  assert.equal(dec.props.precision, 18);
  assert.equal(dec.props.scale, 2);
});

test('附件前缀：image-* / file-* / files / attachment', () => {
  assert.equal(resolveWidget({ name: 'A', typeName: 'String', itemType: 'image-jpg' }).widget, 'image');
  assert.equal(resolveWidget({ name: 'B', typeName: 'String', itemType: 'file-zip' }).widget, 'file');
  assert.equal(resolveWidget({ name: 'C', typeName: 'String', itemType: 'files' }).widget, 'file');
  assert.equal(resolveWidget({ name: 'D', typeName: 'String', itemType: 'attachment' }).widget, 'file');
});

test('MVC FormField.Type 同义 ItemType：select/radio/checkbox/date/datetime/text', () => {
  assert.equal(resolveWidget({ name: 'A', typeName: 'String', itemType: 'select', dataSource: { '1': 'x' } }).widget, 'select');
  assert.equal(resolveWidget({ name: 'B', typeName: 'Int32', itemType: 'radio', dataSource: { '1': 'x' } }).widget, 'select');
  assert.equal(resolveWidget({ name: 'C', typeName: 'String', itemType: 'checkbox', dataSource: { '1': 'x' } }).widget, 'lovMulti');
  assert.equal(resolveWidget({ name: 'D', typeName: 'DateTime', itemType: 'date' }).widget, 'date');
  assert.equal(resolveWidget({ name: 'E', typeName: 'DateTime', itemType: 'datetime' }).widget, 'datetime');
  assert.equal(resolveWidget({ name: 'F', typeName: 'String', itemType: 'text' }).widget, 'text');
});

test('lov/lovMulti 附带 dataSource，供没有远程 LOV 的皮肤直接渲染本地选项', () => {
  const r = resolveWidget({ name: 'Kind', typeName: 'Int32', lovCode: 'Enum.Kind', dataSource: { '1': '一' } });
  assert.equal(r.widget, 'lov');
  assert.deepEqual(r.props.dataSource, { '1': '一' });
});

test('时长换算助手：单位推断、分段与合成互逆、毫秒进位', () => {
  assert.equal(timeSpanUnitOfDescription('累计在线总时间，单位秒'), 'seconds');
  assert.equal(timeSpanUnitOfDescription('耗时，单位毫秒'), 'milliseconds');
  assert.equal(timeSpanUnitOfDescription('Timeout in milliseconds'), 'milliseconds');
  assert.equal(timeSpanUnitOfDescription('时长，单位分钟'), 'minutes');
  assert.equal(timeSpanUnitOfDescription('Duration in hour'), 'hours');
  assert.equal(timeSpanUnitOfDescription('累计时长'), 'seconds');

  assert.deepEqual(splitTimeSpan(22515, 'seconds'), { hours: 6, minutes: 15, seconds: 15 });
  assert.equal(joinTimeSpan({ hours: 6, minutes: 15, seconds: 15 }, 'seconds'), 22515);
  assert.deepEqual(splitTimeSpan(90061, 'seconds'), { hours: 25, minutes: 1, seconds: 1 });
  assert.deepEqual(splitTimeSpan(1500, 'milliseconds'), { hours: 0, minutes: 0, seconds: 1.5 });
  assert.deepEqual(splitTimeSpan(59999.6, 'milliseconds'), { hours: 0, minutes: 1, seconds: 0 });
  assert.deepEqual(splitTimeSpan(-90, 'seconds'), { hours: 0, minutes: 0, seconds: 0 });
  assert.equal(joinTimeSpan({ hours: 1, minutes: 30, seconds: 0 }, 'minutes'), 90);
  assert.equal(joinTimeSpan({ hours: -1, minutes: 0, seconds: 0 }, 'seconds'), 0);

  for (const total of [0, 59, 60, 3599, 3600, 22515, 90061]) {
    assert.equal(joinTimeSpan(splitTimeSpan(total, 'seconds'), 'seconds'), total);
  }
});

test('映射键助手：mapValueKey 与 isNumericMapKey', () => {
  assert.equal(mapValueKey({ name: 'RoleName', mapField: 'RoleID' }), 'RoleID');
  assert.equal(mapValueKey({ name: 'Name' }), 'Name');
  assert.equal(isNumericMapKey({ name: 'RoleName', mapTypeName: 'Int32' }), true);
  assert.equal(isNumericMapKey({ name: 'RoleName', mapTypeName: 'String' }), false);
  assert.equal(isNumericMapKey({ name: 'RoleName' }), false);
});
