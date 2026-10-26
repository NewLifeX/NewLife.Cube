// 响应式布局判定单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { MOBILE_BREAKPOINT, shouldAutoCollapseSidebar } = require('../dist/index.cjs');

test('MOBILE_BREAKPOINT: 断点为 768', () => {
  assert.equal(MOBILE_BREAKPOINT, 768);
});

test('shouldAutoCollapseSidebar: 窄屏折叠、宽屏不折叠', () => {
  assert.equal(shouldAutoCollapseSidebar(375), true);
  assert.equal(shouldAutoCollapseSidebar(767), true);
  assert.equal(shouldAutoCollapseSidebar(768), false);
  assert.equal(shouldAutoCollapseSidebar(1920), false);
});

test('shouldAutoCollapseSidebar: 非法宽度按宽屏处理', () => {
  assert.equal(shouldAutoCollapseSidebar(0), false);
  assert.equal(shouldAutoCollapseSidebar(-1), false);
  assert.equal(shouldAutoCollapseSidebar(Number.NaN), false);
  assert.equal(shouldAutoCollapseSidebar(Number.POSITIVE_INFINITY), false);
});
