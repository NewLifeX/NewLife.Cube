// 表单必填校验单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { isBlankValue, validateRequiredFields } = require('../dist/index.cjs');

const mapping = (name, required, displayName) => ({
  widget: 'text',
  field: { name, displayName, required },
});

test('isBlankValue: 空串、空白、空数组与 null 视为空', () => {
  assert.equal(isBlankValue(''), true);
  assert.equal(isBlankValue('   '), true);
  assert.equal(isBlankValue(null), true);
  assert.equal(isBlankValue(undefined), true);
  assert.equal(isBlankValue([]), true);
});

test('isBlankValue: 0、false 与有内容的值不算空', () => {
  assert.equal(isBlankValue(0), false);
  assert.equal(isBlankValue(false), false);
  assert.equal(isBlankValue('0'), false);
  assert.equal(isBlankValue([1]), false);
});

test('validateRequiredFields: 必填为空时报错，提示带字段显示名', () => {
  const fields = [mapping('name', true, '名称'), mapping('remark', false, '备注')];

  assert.deepEqual(validateRequiredFields(fields, { remark: 'x' }), [{ name: 'name', message: '名称 不能为空' }]);
});

test('validateRequiredFields: 缺少显示名时回退字段名', () => {
  assert.deepEqual(validateRequiredFields([mapping('name', true)], { name: ' ' }), [
    { name: 'name', message: 'name 不能为空' },
  ]);
});

test('validateRequiredFields: 多个必填项一次全部报出', () => {
  const fields = [mapping('name', true, '名称'), mapping('age', true, '年龄'), mapping('remark', false, '备注')];

  assert.deepEqual(validateRequiredFields(fields, {}), [
    { name: 'name', message: '名称 不能为空' },
    { name: 'age', message: '年龄 不能为空' },
  ]);
});

test('validateRequiredFields: 全部填齐时无错误', () => {
  const fields = [mapping('name', true, '名称'), mapping('tags', true, '标签')];

  assert.deepEqual(validateRequiredFields(fields, { name: 'stone', tags: ['a'] }), []);
});

test('validateRequiredFields: 支持自定义字段名到表单键的映射', () => {
  const fields = [mapping('User_Name', true, '用户名')];
  // 与 page-utils 的 toCamelCase 口径一致：下划线后的字符统一转大写
  const keyOf = (name) => name.replace(/[-_](\w)/g, (_, c) => c.toUpperCase()).replace(/^(\w)/, (c) => c.toLowerCase());

  assert.deepEqual(validateRequiredFields(fields, { userName: 'stone' }, keyOf), []);
  assert.deepEqual(validateRequiredFields(fields, {}, keyOf), [{ name: 'userName', message: '用户名 不能为空' }]);
});
