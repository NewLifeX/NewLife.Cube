// 多标签页纯逻辑单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  isHomePath,
  buildTabId,
  createTab,
  openTab,
  closeTab,
  closeOtherTabs,
  closeAllTabs,
} = require('../dist/index.cjs');

const home = () => createTab('/home', '/home', '首页');
const tab = (path, title, query) => createTab(path, query ? `${path}?${query}` : path, title);

test('isHomePath: 首页路径不可关闭', () => {
  assert.equal(isHomePath('/'), true);
  assert.equal(isHomePath('/home'), true);
  assert.equal(isHomePath('/Admin/User'), false);
  assert.equal(home().closable, false);
  assert.equal(tab('/Admin/User', '用户').closable, true);
});

test('buildTabId: query 参与 id，hash 不参与', () => {
  assert.equal(buildTabId('/Admin/User', '/Admin/User'), '/Admin/User');
  assert.equal(buildTabId('/Admin/User', '/Admin/User?name=x'), '/Admin/User?name=x');
  assert.equal(buildTabId('/Admin/User', '/Admin/User?name=x#top'), '/Admin/User?name=x');
});

test('openTab: 新标签追加到末尾', () => {
  const list = openTab([home()], tab('/Admin/User', '用户'));

  assert.deepEqual(list.map((t) => t.path), ['/home', '/Admin/User']);
});

test('openTab: 重复标签只更新标题，不重复追加', () => {
  const list = openTab(openTab([home()], tab('/Admin/User', '用户')), tab('/Admin/User', '用户管理'));

  assert.equal(list.length, 2);
  assert.equal(list[1].title, '用户管理');
});

test('openTab: 相同路径不同 query 视为不同标签', () => {
  const list = openTab(openTab([home()], tab('/Admin/User', '用户', 'name=a')), tab('/Admin/User', '用户', 'name=b'));

  assert.equal(list.length, 3);
});

test('closeTab: 关闭非激活标签不切换', () => {
  const tabs = openTab(openTab([home()], tab('/A', 'A')), tab('/B', 'B'));
  const result = closeTab(tabs, '/A', '/B');

  assert.deepEqual(result.tabs.map((t) => t.path), ['/home', '/B']);
  assert.equal(result.nextActiveId, null);
});

test('closeTab: 关闭激活标签切到相邻标签', () => {
  const tabs = openTab(openTab([home()], tab('/A', 'A')), tab('/B', 'B'));
  const result = closeTab(tabs, '/A', '/A');

  assert.deepEqual(result.tabs.map((t) => t.path), ['/home', '/B']);
  assert.equal(result.nextActiveId, '/B');
});

test('closeTab: 首页不可关闭', () => {
  const result = closeTab([home()], '/home', '/home');

  assert.equal(result.tabs.length, 1);
  assert.equal(result.nextActiveId, null);
});

test('closeOtherTabs / closeAllTabs: 保留首页', () => {
  const tabs = openTab(openTab(openTab([home()], tab('/A', 'A')), tab('/B', 'B')), tab('/C', 'C'));

  assert.deepEqual(closeOtherTabs(tabs, '/B').map((t) => t.path), ['/home', '/B']);
  assert.deepEqual(closeAllTabs(tabs).map((t) => t.path), ['/home']);
});
