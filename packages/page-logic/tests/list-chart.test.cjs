// 列表页表格 / 图表视图切换单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { toggleListView, shouldDisposeCharts, normalizeChartList } = require('../dist/index.cjs');

test('toggleListView: 表格与图表视图来回切换', () => {
  assert.equal(toggleListView('table'), 'chart');
  assert.equal(toggleListView('chart'), 'table');
});

test('toggleListView: 连续切换两次回到原视图', () => {
  assert.equal(toggleListView(toggleListView('table')), 'table');
  assert.equal(toggleListView(toggleListView('chart')), 'chart');
});

test('shouldDisposeCharts: 仅离开图表视图时释放实例', () => {
  assert.equal(shouldDisposeCharts('table'), true);
  assert.equal(shouldDisposeCharts('chart'), false);
});

test('normalizeChartList: 数组原样返回', () => {
  const list = [{ series: [{ type: 'line', data: [1, 2] }] }];
  assert.deepEqual(normalizeChartList(list), list);
});

test('normalizeChartList: 非数组与空数组一律返回空列表', () => {
  assert.deepEqual(normalizeChartList(undefined), []);
  assert.deepEqual(normalizeChartList(null), []);
  assert.deepEqual(normalizeChartList([]), []);
  assert.deepEqual(normalizeChartList({ series: [] }), []);
  assert.deepEqual(normalizeChartList('option'), []);
});
