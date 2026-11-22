// 工作台卡片布局单测（node:test + 已构建 dist）
const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  mergeWidgetsWithLayout,
  buildWidgetLayout,
  moveWidget,
  createWidgetLayoutLoader,
} = require('../dist/index.cjs');

const w = (name) => ({ name, title: name });

test('mergeWidgetsWithLayout: 按 sort 排序，未登记卡片保持原序排后', () => {
  const widgets = [w('A'), w('B'), w('C'), w('D')];
  const { visible, hidden } = mergeWidgetsWithLayout(widgets, { C: { sort: 0 }, A: { sort: 1 } });

  assert.deepEqual(visible.map((x) => x.name), ['C', 'A', 'B', 'D']);
  assert.deepEqual(hidden, []);
});

test('mergeWidgetsWithLayout: hide 卡片进入 hidden，空布局保持原序', () => {
  const widgets = [w('A'), w('B'), w('C')];
  const { visible, hidden } = mergeWidgetsWithLayout(widgets, { B: { hide: true } });

  assert.deepEqual(visible.map((x) => x.name), ['A', 'C']);
  assert.deepEqual(hidden.map((x) => x.name), ['B']);

  const plain = mergeWidgetsWithLayout(widgets, null);
  assert.deepEqual(plain.visible.map((x) => x.name), ['A', 'B', 'C']);
});

test('buildWidgetLayout: 可见在前、隐藏在后，各自按顺序编号', () => {
  const layout = buildWidgetLayout([w('A'), w('B')], [w('C')]);

  assert.deepEqual(layout, {
    A: { sort: 0, hide: false },
    B: { sort: 1, hide: false },
    C: { sort: 2, hide: true },
  });
});

test('buildWidgetLayout 与 mergeWidgetsWithLayout 往返一致', () => {
  const visible = [w('B'), w('A')];
  const hidden = [w('C')];
  const merged = mergeWidgetsWithLayout([w('A'), w('B'), w('C')], buildWidgetLayout(visible, hidden));

  assert.deepEqual(merged.visible.map((x) => x.name), ['B', 'A']);
  assert.deepEqual(merged.hidden.map((x) => x.name), ['C']);
});

test('moveWidget: 向前拖动落在目标之前，向后拖动落在目标之后', () => {
  const list = [w('A'), w('B'), w('C')];

  assert.deepEqual(moveWidget(list, 'C', 'A').map((x) => x.name), ['C', 'A', 'B']);
  assert.deepEqual(moveWidget(list, 'A', 'C').map((x) => x.name), ['B', 'C', 'A']);
});

test('moveWidget: 名称找不到或位置相同时保持原序，且不修改入参', () => {
  const list = [w('A'), w('B')];
  const copy = [...list];

  assert.deepEqual(moveWidget(list, 'X', 'A').map((x) => x.name), ['A', 'B']);
  assert.deepEqual(moveWidget(list, 'A', 'A').map((x) => x.name), ['A', 'B']);
  assert.deepEqual(list, copy);
});

test('createWidgetLayoutLoader: 读取成功返回布局，失败回退空对象', async () => {
  const calls = [];
  const api = {
    page: {
      getAction: async (type, action) => {
        calls.push({ type, action });
        return action === 'GetWidgetLayout' ? { code: 0, data: { A: { sort: 1 } } } : { code: 1 };
      },
      postAction: async () => ({ code: 0 }),
    },
  };

  const loader = createWidgetLayoutLoader(api);
  assert.deepEqual(await loader.load(), { A: { sort: 1 } });
  assert.deepEqual(calls[0], { type: 'Admin/Index', action: 'GetWidgetLayout' });
});

test('createWidgetLayoutLoader: 保存与重置按成功码返回布尔', async () => {
  const posts = [];
  const api = {
    page: {
      getAction: async () => {
        throw new Error('offline');
      },
      postAction: async (type, action, data) => {
        posts.push({ type, action, data });
        return { code: action === 'SaveWidgetLayout' ? 0 : 500 };
      },
    },
  };

  const loader = createWidgetLayoutLoader(api);

  assert.deepEqual(await loader.load(), {});
  assert.equal(await loader.save({ A: { sort: 0 } }), true);
  assert.equal(await loader.reset(), false);
  assert.deepEqual(posts[0], { type: 'Admin/Index', action: 'SaveWidgetLayout', data: { A: { sort: 0 } } });
  assert.deepEqual(posts[1], { type: 'Admin/Index', action: 'ResetWidgetLayout', data: undefined });
});
