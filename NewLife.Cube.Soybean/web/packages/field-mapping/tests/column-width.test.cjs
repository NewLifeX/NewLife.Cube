// 列宽与对齐规范单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  resolveColumnWidth,
  resolveColumnAlign,
  KEY_COLUMN_WIDTH,
  TIME_COLUMN_WIDTH,
  NUMBER_COLUMN_WIDTH,
  NARROW_COLUMN_WIDTH,
  MEDIA_COLUMN_WIDTH,
} = require('../dist/index.cjs');

test('resolveColumnWidth: 呈现配置 width 优先', () => {
  assert.equal(resolveColumnWidth({ name: 'Name', typeName: 'String', width: 220 }), 220);
  assert.equal(resolveColumnWidth({ name: 'Id', primaryKey: true, width: 120 }), 120);
  assert.equal(resolveColumnWidth({ name: 'Name', typeName: 'String', width: 'auto' }), undefined);
});

test('resolveColumnWidth: 主键与媒体列固定宽度', () => {
  assert.equal(resolveColumnWidth({ name: 'Id', primaryKey: true }), KEY_COLUMN_WIDTH);
  assert.equal(resolveColumnWidth({ name: 'Avatar', itemType: 'image' }), MEDIA_COLUMN_WIDTH);
  assert.equal(resolveColumnWidth({ name: 'Attachment', itemType: 'file' }), MEDIA_COLUMN_WIDTH);
  assert.equal(resolveColumnWidth({ name: 'Avatar', itemType: 'avatar' }), MEDIA_COLUMN_WIDTH);
});

test('resolveColumnWidth: 窄列与时间列', () => {
  assert.equal(resolveColumnWidth({ name: 'Enable', typeName: 'Boolean' }), NARROW_COLUMN_WIDTH);
  assert.equal(resolveColumnWidth({ name: 'Enable', itemType: 'switch' }), NARROW_COLUMN_WIDTH);
  assert.equal(resolveColumnWidth({ name: 'Color', itemType: 'color' }), NARROW_COLUMN_WIDTH);
  assert.equal(resolveColumnWidth({ name: 'CreateTime', typeName: 'DateTime' }), TIME_COLUMN_WIDTH);
  assert.equal(resolveColumnWidth({ name: 'Birthday', typeName: 'DateOnly' }), TIME_COLUMN_WIDTH);
  assert.equal(resolveColumnWidth({ name: 'Duration', typeName: 'TimeSpan' }), TIME_COLUMN_WIDTH);
});

test('resolveColumnWidth: 数值列与字符串自适应', () => {
  assert.equal(resolveColumnWidth({ name: 'Count', typeName: 'Int32' }), NUMBER_COLUMN_WIDTH);
  assert.equal(resolveColumnWidth({ name: 'Amount', typeName: 'Decimal' }), NUMBER_COLUMN_WIDTH);
  assert.equal(resolveColumnWidth({ name: 'Total', typeName: 'Int64' }), NUMBER_COLUMN_WIDTH);
  assert.equal(resolveColumnWidth({ name: 'Name', typeName: 'String' }), undefined);
  assert.equal(resolveColumnWidth({ name: 'Remark' }), undefined);
});

test('resolveColumnAlign: 数值右对齐、主键与布尔居中', () => {
  assert.equal(resolveColumnAlign({ name: 'Count', typeName: 'Int32' }), 'right');
  assert.equal(resolveColumnAlign({ name: 'Amount', typeName: 'Decimal' }), 'right');
  assert.equal(resolveColumnAlign({ name: 'Id', primaryKey: true }), 'center');
  assert.equal(resolveColumnAlign({ name: 'Enable', typeName: 'Boolean' }), 'center');
  assert.equal(resolveColumnAlign({ name: 'Avatar', itemType: 'image' }), 'center');
  assert.equal(resolveColumnAlign({ name: 'Name', typeName: 'String' }), 'left');
  assert.equal(resolveColumnAlign({ name: 'CreateTime', typeName: 'DateTime' }), 'left');
});
