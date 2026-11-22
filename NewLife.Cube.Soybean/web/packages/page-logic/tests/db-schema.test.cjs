// 数据库管理页归一化单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  normalizeDbList,
  normalizeSchemaList,
  normalizeDiffList,
  columnFlag,
  precisionText,
} = require('../dist/index.cjs');

test('normalizeDbList: 兼容 ApiResponse 包裹与裸数组', () => {
  const rows = [
    { name: 'Cube', type: 6, version: '3.50.4', driver: 'SQLite', driverVersion: '2.0.2', entities: 13, tables: 29, backups: 2 },
  ];

  assert.equal(normalizeDbList({ code: 0, data: rows })[0].name, 'Cube');
  assert.equal(normalizeDbList(rows)[0].type, '6');
  assert.equal(normalizeDbList(rows)[0].entities, 13);
  assert.deepEqual(normalizeDbList(null), []);
});

test('normalizeDbList: 缺失字段兜底', () => {
  const [row] = normalizeDbList([{ name: 'Log' }]);

  assert.equal(row.version, '');
  assert.equal(row.entities, 0);
  assert.equal(row.backups, 0);
});

test('normalizeDbList: 兼容数字键序列化列表', () => {
  const payload = { 0: { name: 'Cube' }, 1: { name: 'Log' } };

  assert.deepEqual(
    normalizeDbList(payload).map((row) => row.name),
    ['Cube', 'Log'],
  );
  assert.deepEqual(
    normalizeDbList({ code: 0, data: payload }).map((row) => row.name),
    ['Cube', 'Log'],
  );
});

test('normalizeSchemaList: 列结构与布尔/数值兜底', () => {
  const [table] = normalizeSchemaList({
    data: [
      {
        name: 'User',
        tableName: 'User',
        displayName: '用户',
        count: '12',
        columns: [
          { name: 'Id', type: 'Int32', length: '4', primaryKey: 1, identity: true, nullable: false, unique: 'true' },
          { name: 'Name', type: 'String', length: 50 },
        ],
      },
    ],
  });

  assert.equal(table.count, 12);
  assert.equal(table.columns.length, 2);
  assert.equal(table.columns[0].primaryKey, true);
  assert.equal(table.columns[0].identity, true);
  assert.equal(table.columns[0].unique, true);
  assert.equal(table.columns[1].length, 50);
  assert.equal(table.columns[1].nullable, false);
  assert.equal(table.columns[1].description, '');
});

test('normalizeDiffList: 额外列与 XML 片段', () => {
  const [diff] = normalizeDiffList([
    { name: 'User', tableName: 'User', hasEntityModel: false, extraColumns: ['Remark', 3], xmlFragment: '<Table/>' },
  ]);

  assert.equal(diff.hasEntityModel, false);
  assert.deepEqual(diff.extraColumns, ['Remark', '3']);
  assert.equal(diff.xmlFragment, '<Table/>');
});

test('columnFlag: 自增优先于主键与唯一索引', () => {
  assert.equal(columnFlag({ identity: true, primaryKey: true, unique: true }), 'AI');
  assert.equal(columnFlag({ identity: false, primaryKey: true, unique: true }), 'PK');
  assert.equal(columnFlag({ identity: false, primaryKey: false, unique: true }), 'UQ');
  assert.equal(columnFlag({ identity: false, primaryKey: false, unique: false }), '');
});

test('precisionText: 仅在有精度或小数位时输出', () => {
  assert.equal(precisionText({ precision: 18, scale: 2 }), '(18, 2)');
  assert.equal(precisionText({ precision: 0, scale: 2 }), '(0, 2)');
  assert.equal(precisionText({ precision: 0, scale: 0 }), '');
});
