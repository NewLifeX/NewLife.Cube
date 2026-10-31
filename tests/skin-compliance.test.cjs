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

  test(`${skin}: 列表页按字段名忽略大小写读取行值（SKIN-12）`, () => {
    expectReference(skin, 'getFieldValue', '列表页未接入共享行值读取（元数据字段名为 PascalCase，接口行数据为 camelCase）');
  });

  test(`${skin}: 工作台接真实聚合数据并支持卡片布局（SKIN-8/13）`, () => {
    expectReference(skin, 'createWorkbenchLoader', '工作台未接入共享数据加载器');
    expectReference(skin, 'createWidgetLayoutLoader', '工作台未接入卡片布局持久化');
    expectReference(skin, 'moveWidget', '工作台未接入卡片拖动排序');
  });

  test(`${skin}: 布局接入多标签页（SKIN-10）`, () => {
    expectReference(skin, 'createTab', '布局未接入共享多标签页逻辑');
    expectReference(skin, 'openTab', '布局未接入共享多标签页逻辑');
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

// ───────────────────── ItemType 控件全量对齐（MVC _Form_Item / _Form_String 口径） ─────────────────────

/** 每个皮肤的字段渲染组件（控件映射的唯一落点） */
const fieldInputFiles = {
  NaiveUI: 'NewLife.Cube.NaiveUI/web/src/components/FieldInput.vue',
  ArcoVue: 'NewLife.Cube.ArcoVue/web/src/components/FieldInput.vue',
  TDesign: 'NewLife.Cube.TDesign/web/src/components/FieldInput.vue',
  Vuetify: 'NewLife.Cube.Vuetify/web/src/components/FieldInput.vue',
  MUI: 'NewLife.Cube.MUI/web/src/components/FieldInput.tsx',
  Shadcn: 'NewLife.Cube.Shadcn/web/src/components/FieldInput.tsx',
  Svelte: 'NewLife.Cube.Svelte/web/src/lib/components/FieldInput.svelte',
  Angular: 'NewLife.Cube.Angular/web/src/app/components/field-input.component.ts',
};

/** 各皮肤字段渲染组件必须覆盖的 widget 分支（缺一即说明该 ItemType 会退化成文本框） */
const requiredWidgetCases = [
  'area',
  'timeSpan',
  'switch',
  'number',
  'mapInput',
  'date',
  'datetime',
  'textarea',
  'password',
  'color',
  'image',
  'file',
  'readonly',
];

for (const [skin, file] of Object.entries(fieldInputFiles)) {
  test(`${skin}: 字段渲染覆盖全部 ItemType 控件（SKIN-6 / ItemType 全量对齐）`, () => {
    const full = path.join(repoRoot, file);
    assert.ok(fs.existsSync(full), `${skin}: 未找到字段渲染组件 ${file}`);
    const text = fs.readFileSync(full, 'utf8');

    assert.ok(
      text.includes('resolveWidget') || text.includes('FieldMapping'),
      `${skin}: 字段控件未走共享映射 resolveWidget`,
    );
    for (const widget of requiredWidgetCases) {
      assert.ok(text.includes(`'${widget}'`), `${skin}: 字段渲染缺少 ${widget} 分支（该 ItemType 会退化成文本）`);
    }
    // 下拉类（select/lov/lovMulti/mapSelect）与地区/时长依赖共享能力
    assert.ok(text.includes('isSelect') || text.includes("'select'"), `${skin}: 下拉类控件缺失`);
    assert.ok(text.includes('createAreaCascade'), `${skin}: 地区级联未接入共享控制器`);
    assert.ok(text.includes('splitTimeSpan') && text.includes('joinTimeSpan'), `${skin}: 时长控件未接入共享换算`);
  });
}

/** 已提供找回密码的皮肤（八个 SPA 均已接入；冻结的 MVC 主题不参与对照） */
const forgotPasswordSkins = ['NaiveUI', 'ArcoVue', 'TDesign', 'Shadcn', 'Vuetify', 'MUI', 'Svelte', 'Angular'];

for (const skin of forgotPasswordSkins) {
  test(`${skin}: 找回密码入口与路由已接入（SKIN-3）`, () => {
    expectReference(skin, 'forgot-password', '未找到找回密码页面或路由');
  });
}

// Vue 参考皮肤不在 skins 表内（它是框架本体，契约项与 SPA 皮肤不同），单独校验
test('Vue: 找回密码入口与路由已接入（SKIN-3）', () => {
  const files = readSkinSources('NewLife.Cube.Vue/web/core');
  const hit = files.some((f) => f.text.includes('/forgot-password'));

  assert.ok(hit, '未找到找回密码页面或路由');
});
