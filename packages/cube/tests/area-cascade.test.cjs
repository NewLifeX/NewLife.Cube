// 地区级联控制器单元测试（node:test + 已构建 dist，零额外依赖）
// 运行：在 packages/cube 目录 `pnpm build && node --test tests/`
const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  AreaCascade,
  normalizeAreaOptions,
  normalizeAreaPath,
  AREA_CHILDREN_PATH,
  AREA_PARENTS_PATH,
} = require('../dist/index.cjs');

/** 假地区树：1 省 → 11 市 → 111 区；2 省 → 21 市 → 211 区 */
const tree = {
  0: [
    { id: 1, name: '省一' },
    { id: 2, name: '省二' },
  ],
  1: [{ id: 11, name: '市一' }],
  2: [{ id: 21, name: '市二' }],
  11: [{ id: 111, name: '区一' }],
  21: [{ id: 211, name: '区二' }],
};

const parents = {
  1: [1],
  2: [2],
  11: [1, 11],
  111: [1, 11, 111],
  211: [2, 21, 211],
};

function deps() {
  return {
    loadChildren: async (parentId) => tree[parentId] ?? [],
    loadPath: async (id) => parents[id] ?? [],
  };
}

test('接口路径常量与 MVC/Vue 口径一致', () => {
  assert.equal(AREA_CHILDREN_PATH, '/Cube/AreaChilds');
  assert.equal(AREA_PARENTS_PATH, '/Cube/AreaParents');
});

test('normalizeAreaOptions 兼容裸数组与 {data:[...]} 包装', () => {
  assert.deepEqual(normalizeAreaOptions([{ id: 1, name: 'A' }]), [{ id: 1, name: 'A' }]);
  assert.deepEqual(normalizeAreaOptions({ data: [{ id: 2, name: 'B' }] }), [{ id: 2, name: 'B' }]);
  assert.deepEqual(normalizeAreaOptions({ items: [{ id: 3, displayName: 'C' }] }), [{ id: 3, name: 'C' }]);
  assert.deepEqual(normalizeAreaOptions(null), []);
});

test('normalizeAreaPath 按响应顺序返回 id 路径', () => {
  assert.deepEqual(normalizeAreaPath({ data: [{ id: 1 }, { id: 11 }, { id: 111 }] }), [1, 11, 111]);
  assert.deepEqual(normalizeAreaPath('bad'), []);
});

test('init 按已有值回显三级路径与选项', async () => {
  const cascade = new AreaCascade(deps(), 3);
  const state = await cascade.init(111);

  assert.deepEqual(state.path, [1, 11, 111]);
  assert.equal(state.value, 111);
  assert.equal(state.options.length, 3);
  assert.deepEqual(state.options[0].map((x) => x.id), [1, 2]);
  assert.deepEqual(state.options[1].map((x) => x.id), [11]);
  assert.deepEqual(state.options[2].map((x) => x.id), [111]);
});

test('init 未选值只加载省级', async () => {
  const cascade = new AreaCascade(deps(), 3);
  const state = await cascade.init(0);

  assert.deepEqual(state.path, []);
  assert.equal(state.value, null);
  assert.deepEqual(state.options[0].map((x) => x.id), [1, 2]);
  assert.deepEqual(state.options[1], []);
  assert.deepEqual(state.options[2], []);
});

test('selectLevel 切省后清空下级并加载新市', async () => {
  const cascade = new AreaCascade(deps(), 3);
  await cascade.init(111);
  const state = await cascade.selectLevel(0, 2);

  assert.deepEqual(state.path, [2]);
  assert.equal(state.value, 2);
  assert.deepEqual(state.options[1].map((x) => x.id), [21]);
  assert.deepEqual(state.options[2], []);
});

test('selectLevel 清空某级时该级及以下归零', async () => {
  const cascade = new AreaCascade(deps(), 3);
  await cascade.init(111);
  const state = await cascade.selectLevel(1, null);

  assert.deepEqual(state.path, [1]);
  assert.equal(state.value, 1);
  assert.deepEqual(state.options[2], []);
});

test('area2 只渲染两级，area1 只渲染一级', async () => {
  const two = new AreaCascade(deps(), 2);
  const state2 = await two.init(11);
  assert.equal(state2.options.length, 2);
  assert.equal(state2.value, 11);

  const one = new AreaCascade(deps(), 1);
  const state1 = await one.init(1);
  assert.equal(state1.options.length, 1);
  assert.deepEqual(state1.path, [1]);
  assert.equal(state1.value, 1);
});

test('目标层级越界时收敛到 1~4', async () => {
  const nine = new AreaCascade(deps(), 9);
  assert.equal((await nine.init(0)).options.length, 4);
  const zero = new AreaCascade(deps(), 0);
  assert.equal((await zero.init(0)).options.length, 3);
});

test('超深层级按目标层级截断，提交值与可见层级一致', async () => {
  const cascade = new AreaCascade(deps(), 2);
  const state = await cascade.init(111);
  assert.deepEqual(state.path, [1, 11]);
  assert.equal(state.value, 11);
});
