/**
 * 表单控件渲染矩阵组件测试（验收核心）
 *
 * 挂载 FormContent，喂入 §3.2 映射总表覆盖的每一种字段类型 FieldMeta，
 * 断言 resolveControl 分发后渲染出正确的控件：
 *   input / textarea / inputNumber / switch / datePicker / timePicker /
 *   lov / lovMulti / upload / image / json / richHtml / richMarkdown /
 *   color / icon / email / tel / url / readonly（共 19 类，>15）
 *
 * 不依赖后端；用 @vue/test-utils 挂载，断言通过率 100%。
 *
 * 运行：pnpm test:unit core/__tests__/FormContent.fieldtypes.spec.ts
 */
import { describe, it, expect, vi, beforeAll } from 'vitest';
import { h } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import type { FieldMeta } from '../types/field';

// ── jsdom 兼容垫片：Element Plus 弹层（el-select/el-popover/el-date-picker 等）依赖 ResizeObserver / matchMedia / scrollIntoView ──
class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}
if (!globalThis.ResizeObserver) {
  globalThis.ResizeObserver = ResizeObserverStub as unknown as typeof ResizeObserver;
}
if (typeof window !== 'undefined' && !window.matchMedia) {
  window.matchMedia = ((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener() {},
    removeListener() {},
    addEventListener() {},
    removeEventListener() {},
    dispatchEvent() {
      return false;
    },
  })) as unknown as typeof window.matchMedia;
}
if (typeof Element !== 'undefined' && !Element.prototype.scrollIntoView) {
  Element.prototype.scrollIntoView = () => {};
}

// ── 隔离第三方重型编辑器（Svelte/wangEditor/md-editor 在 jsdom 下无法稳定挂载）──
// 仅替换其内部实现，FormContent 仍会渲染真实的 JsonEditor/RichEditor 组件本身，
// 以此验证“某字段类型 → 对应控件组件”的分发逻辑。
vi.mock('vanilla-jsoneditor', () => ({
  // JsonEditor.vue 使用具名导入 createJSONEditor（非 createEditor）
  createJSONEditor: () => ({ get: () => ({ content: { text: '' } }), set: () => {}, destroy: () => {} }),
  createEditor: () => ({ get: () => ({ content: { text: '' } }), set: () => {}, destroy: () => {} }),
}));
vi.mock('@wangeditor/editor-for-vue', async () => ({
  Editor: { name: 'WEditorMock', render: () => h('div', { class: 'wangeditor-mock' }) },
  Toolbar: { name: 'WToolbarMock', render: () => h('div', { class: 'wtoolbar-mock' }) },
}));
vi.mock('md-editor-v3', async () => ({
  // RichEditor.vue 使用具名导入 MdEditor（非 default），两者都需 mock
  MdEditor: { name: 'MdEditorMock', render: () => h('div', { class: 'md-mock' }) },
  default: { name: 'MdEditorMock', render: () => h('div', { class: 'md-mock' }) },
}));

// 关键组件（用于 findComponent 断言）
import FormContent from '../views/components/FormContent';
import LovSelect from '../components/LovSelect';
import Uploader from '../components/Uploader';
import JsonEditor from '../components/JsonEditor';
import RichEditor from '../components/RichEditor';
import ColorPicker from '../components/ColorPicker';
import IconSelector from '../components/IconSelector';

const ENUM_LOV = 'Enum.CubeDemo.Areas.Test.测试枚举';

/** 构造 FieldMeta */
function fm(partial: Partial<FieldMeta> & { name: string; typeName: string }): FieldMeta {
  return { itemType: undefined, length: 0, ...partial } as FieldMeta;
}

/** 挂载单个字段的 FormContent */
function mountField(meta: FieldMeta, value: unknown) {
  return mount(FormContent, {
    props: {
      fields: [meta],
      modelValue: { [meta.name]: value },
    },
    global: {
      plugins: [ElementPlus],
    },
  });
}

beforeAll(() => {
  // 确保垫片在挂载前就位
  expect(globalThis.ResizeObserver).toBeDefined();
});

describe('FormContent 字段类型 → 控件渲染矩阵（§3.2 全矩阵）', () => {
  it('短文本 String(Length<300) → el-input（type=text）', () => {
    const w = mountField(fm({ name: 'ShortText', typeName: 'String', length: 50 }), 'hello');
    const input = w.find('input');
    expect(input.exists()).toBe(true);
    expect(input.attributes('type')).toBe('text');
    expect(w.find('.el-input.is-disabled').exists()).toBe(false);
  });

  it('大文本 String(Length>=300) → textarea', () => {
    const w = mountField(fm({ name: 'LongText', typeName: 'String', length: 500 }), '很长的内容');
    expect(w.find('textarea').exists()).toBe(true);
  });

  it('数值 Int32 → el-input-number', () => {
    const w = mountField(fm({ name: 'Int32Val', typeName: 'Int32' }), 42);
    expect(w.find('.el-input-number').exists()).toBe(true);
  });

  it('布尔 Boolean → el-switch', () => {
    const w = mountField(fm({ name: 'Enable', typeName: 'Boolean' }), true);
    expect(w.find('.el-switch').exists()).toBe(true);
  });

  it('日期 DateTime → el-date-picker（根节点类为 el-date-editor）', () => {
    const w = mountField(fm({ name: 'CreateTime', typeName: 'DateTime' }), '2026-07-11T10:00:00');
    // Element Plus 日期选择器根节点类为 el-date-editor（含 el-date-editor--datetime 修饰）
    expect(w.find('.el-date-editor').exists()).toBe(true);
  });

  it('时间 TimeSpan → el-time-picker（根节点类同 el-date-editor 命名空间）', () => {
    const w = mountField(fm({ name: 'Span', typeName: 'TimeSpan' }), '10:00:00');
    // Element Plus 时间选择器与日期选择器共用 el-date-editor 根节点类（含 --time 修饰）
    expect(w.find('.el-date-editor').exists()).toBe(true);
  });

  it('枚举(lovCode) → LovSelect（单选 multiple=false）', () => {
    const w = mountField(fm({ name: 'Kind', typeName: '测试枚举', lovCode: ENUM_LOV }), 1);
    const lov = w.findComponent(LovSelect);
    expect(lov.exists()).toBe(true);
    expect(lov.props('multiple')).toBe(false);
  });

  it('multipleSelect(lovCode) → LovSelect（多选 multiple=true）', () => {
    const w = mountField(
      fm({ name: 'MultiVal', typeName: 'String', itemType: 'multipleSelect', lovCode: ENUM_LOV }),
      ['1', '2'],
    );
    const lov = w.findComponent(LovSelect);
    expect(lov.exists()).toBe(true);
    expect(lov.props('multiple')).toBe(true);
  });

  it('ItemType=file → Uploader（kind=file）', () => {
    const w = mountField(fm({ name: 'FileUrl', typeName: 'String', itemType: 'file' }), '/upload/test/a.pdf');
    const up = w.findComponent(Uploader);
    expect(up.exists()).toBe(true);
    expect(up.props('kind')).toBe('file');
  });

  it('ItemType=image → Uploader（kind=image）', () => {
    const w = mountField(fm({ name: 'Avatar', typeName: 'String', itemType: 'image' }), '/upload/test/a.png');
    const up = w.findComponent(Uploader);
    expect(up.exists()).toBe(true);
    expect(up.props('kind')).toBe('image');
  });

  it('ItemType=json → JsonEditor', () => {
    const w = mountField(fm({ name: 'JsonVal', typeName: 'String', itemType: 'json' }), '{"a":1}');
    expect(w.findComponent(JsonEditor).exists()).toBe(true);
  });

  it('ItemType=html → RichEditor（mode=html）', () => {
    const w = mountField(fm({ name: 'HtmlVal', typeName: 'String', itemType: 'html' }), '<p>hi</p>');
    const re = w.findComponent(RichEditor);
    expect(re.exists()).toBe(true);
    expect(re.props('mode')).toBe('html');
  });

  it('ItemType=markdown → RichEditor（mode=markdown）', () => {
    const w = mountField(fm({ name: 'MarkdownVal', typeName: 'String', itemType: 'markdown' }), '# hi');
    const re = w.findComponent(RichEditor);
    expect(re.exists()).toBe(true);
    expect(re.props('mode')).toBe('markdown');
  });

  it('ItemType=color → el-color-picker（ColorPicker）', () => {
    const w = mountField(fm({ name: 'ColorVal', typeName: 'String', itemType: 'color' }), '#16703f');
    expect(w.findComponent(ColorPicker).exists()).toBe(true);
    expect(w.find('.el-color-picker').exists()).toBe(true);
  });

  it('ItemType=icon → IconSelector', () => {
    const w = mountField(fm({ name: 'IconVal', typeName: 'String', itemType: 'icon' }), 'Edit');
    expect(w.findComponent(IconSelector).exists()).toBe(true);
  });

  it('ItemType=mail → el-input（type=email）', () => {
    const w = mountField(fm({ name: 'MailVal', typeName: 'String', itemType: 'mail' }), 'a@b.com');
    const input = w.find('input');
    expect(input.exists()).toBe(true);
    expect(input.attributes('type')).toBe('email');
  });

  it('ItemType=mobile → el-input（type=tel）', () => {
    const w = mountField(fm({ name: 'MobileVal', typeName: 'String', itemType: 'mobile' }), '13800000000');
    const input = w.find('input');
    expect(input.exists()).toBe(true);
    expect(input.attributes('type')).toBe('tel');
  });

  it('ItemType=url → el-input（type=url）', () => {
    const w = mountField(fm({ name: 'UrlVal', typeName: 'String', itemType: 'url' }), 'https://x.com');
    const input = w.find('input');
    expect(input.exists()).toBe(true);
    expect(input.attributes('type')).toBe('url');
  });

  it('Guid → 只读 el-input（disabled）', () => {
    const w = mountField(fm({ name: 'GuidVal', typeName: 'Guid' }), '3f2504e0-4f89-41d3-9a0c-0305e82c3301');
    expect(w.find('.el-input.is-disabled').exists()).toBe(true);
  });

  it('全矩阵一次性挂载：19 类字段均产生唯一控件节点', () => {
    const metas: FieldMeta[] = [
      fm({ name: 'ShortText', typeName: 'String', length: 50 }),
      fm({ name: 'LongText', typeName: 'String', length: 500 }),
      fm({ name: 'Int32Val', typeName: 'Int32' }),
      fm({ name: 'Enable', typeName: 'Boolean' }),
      fm({ name: 'CreateTime', typeName: 'DateTime' }),
      fm({ name: 'Span', typeName: 'TimeSpan' }),
      fm({ name: 'Kind', typeName: '测试枚举', lovCode: ENUM_LOV }),
      fm({ name: 'MultiVal', typeName: 'String', itemType: 'multipleSelect', lovCode: ENUM_LOV }),
      fm({ name: 'FileUrl', typeName: 'String', itemType: 'file' }),
      fm({ name: 'Avatar', typeName: 'String', itemType: 'image' }),
      fm({ name: 'JsonVal', typeName: 'String', itemType: 'json' }),
      fm({ name: 'HtmlVal', typeName: 'String', itemType: 'html' }),
      fm({ name: 'MarkdownVal', typeName: 'String', itemType: 'markdown' }),
      fm({ name: 'ColorVal', typeName: 'String', itemType: 'color' }),
      fm({ name: 'IconVal', typeName: 'String', itemType: 'icon' }),
      fm({ name: 'MailVal', typeName: 'String', itemType: 'mail' }),
      fm({ name: 'MobileVal', typeName: 'String', itemType: 'mobile' }),
      fm({ name: 'UrlVal', typeName: 'String', itemType: 'url' }),
      fm({ name: 'GuidVal', typeName: 'Guid' }),
    ];
    const model: Record<string, unknown> = {};
    metas.forEach((m) => (model[m.name] = null));
    const w = mount(FormContent, {
      props: { fields: metas, modelValue: model },
      global: { plugins: [ElementPlus] },
    });
    expect(w.findAll('.fmc-field').length).toBe(19);
    // 关键控件组件数量
    expect(w.findComponent(LovSelect).exists()).toBe(true);
    expect(w.findComponent(Uploader).exists()).toBe(true);
    expect(w.findComponent(JsonEditor).exists()).toBe(true);
    expect(w.findComponent(RichEditor).exists()).toBe(true);
    expect(w.findComponent(ColorPicker).exists()).toBe(true);
    expect(w.findComponent(IconSelector).exists()).toBe(true);
  });
});
