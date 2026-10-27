// 皮肤接入契约测试（node:test，零额外依赖）
//
// 八个 SPA 皮肤共用同一套共享包，但接入点散落在各自的组件里。本测试以静态契约的方式，
// 校验每个皮肤仍然接入 SKIN-5/6/8/11/12/14 与 SPA-15/17 要求的共享能力，
// 防止重构时某个皮肤悄悄退回自实现或漏接规范（皮肤 UI 行为级 E2E 见收口记录）。
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const repoRoot = path.resolve(__dirname, '..');

/** 皮肤源码根目录（相对仓库根） */
const skins = {
  NaiveUI: 'NewLife.Cube.NaiveUI/web/src',
  ArcoVue: 'NewLife.Cube.ArcoVue/web/src',
  TDesign: 'NewLife.Cube.TDesign/web/src',
  Vuetify: 'NewLife.Cube.Vuetify/web/src',
  MUI: 'NewLife.Cube.MUI/web/src',
  Shadcn: 'NewLife.Cube.Shadcn/web/src',
  Svelte: 'NewLife.Cube.Svelte/web/src',
  Angular: 'NewLife.Cube.Angular/web/src',
};

const sourceExtensions = ['.ts', '.tsx', '.vue', '.svelte'];
const skipDirectories = new Set(['node_modules', 'dist', 'wwwroot', '__tests__', '.svelte-kit', '.angular']);

/** 读取皮肤源码文本（跳过测试、构建产物与声明文件） */
function readSkinSources(dir) {
  const files = [];
  const walk = (current) => {
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      if (entry.isDirectory()) {
        if (!skipDirectories.has(entry.name)) walk(path.join(current, entry.name));
        continue;
      }
      if (!sourceExtensions.some((ext) => entry.name.endsWith(ext))) continue;
      if (entry.name.endsWith('.d.ts') || entry.name.endsWith('.vue.js')) continue;
      const full = path.join(current, entry.name);
      files.push({ path: full, text: fs.readFileSync(full, 'utf8') });
    }
  };
  walk(path.join(repoRoot, dir));
  return files;
}

const skinSources = Object.fromEntries(Object.entries(skins).map(([name, dir]) => [name, readSkinSources(dir)]));

/** 断言某皮肤存在引用指定标识符的源文件 */
function expectReference(skin, needle, reason) {
  const files = skinSources[skin];
  assert.ok(files.length > 0, `${skin}: 未找到源码文件`);
  const hits = files.filter((f) => f.text.includes(needle)).map((f) => path.relative(repoRoot, f.path).replace(/\\/g, '/'));
  assert.ok(hits.length > 0, `${skin}: ${reason}（未找到 ${needle} 的引用）`);
}

for (const skin of Object.keys(skins)) {
  test(`${skin}: 布局接入窄屏自动折叠（SKIN-11/14）`, () => {
    expectReference(skin, 'shouldAutoCollapseSidebar', '布局未接入共享窄屏折叠判定');
  });

  test(`${skin}: 列表页接入列宽与对齐规范（SKIN-12）`, () => {
    expectReference(skin, 'resolveColumnWidth', '列表页未接入共享列宽解析');
    expectReference(skin, 'resolveColumnAlign', '列表页未接入共享列对齐解析');
  });

  test(`${skin}: 工作台接真实聚合数据（SKIN-8）`, () => {
    expectReference(skin, 'createWorkbenchLoader', '工作台未接入共享数据加载器');
  });

  test(`${skin}: 表单页按字段类型渲染并拦截必填（SKIN-6）`, () => {
    expectReference(skin, 'FieldInput', '未使用字段渲染组件 FieldInput');
    expectReference(skin, 'validateRequiredFields', '表单提交前未接入共享必填校验');
  });
}

/** 提供图表视图的皮肤（Svelte 无图表数据源） */
const chartSkins = Object.keys(skins).filter((s) => s !== 'Svelte');

for (const skin of chartSkins) {
  test(`${skin}: 图表视图共用切换与数据归一化（SPA-15/17）`, () => {
    expectReference(skin, 'toggleListView', '图表视图未接入共享视图切换');
    expectReference(skin, 'normalizeChartList', '图表数据未接入共享归一化');
  });
}

/** 已提供找回密码的皮肤（Vue 与 Svelte 暂缺，按配置不阻塞） */
const forgotPasswordSkins = ['NaiveUI', 'ArcoVue', 'TDesign', 'Shadcn', 'Vuetify', 'MUI', 'Angular'];

for (const skin of forgotPasswordSkins) {
  test(`${skin}: 找回密码入口与路由已接入（SKIN-3）`, () => {
    expectReference(skin, 'forgot-password', '未找到找回密码页面或路由');
  });
}
