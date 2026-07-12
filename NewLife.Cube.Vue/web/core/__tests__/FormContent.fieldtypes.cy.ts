/**
 * 表单控件渲染矩阵组件测试（穷举字段类型 → 控件）
 *
 * 不依赖后端：直接挂载 FormContent，构造覆盖全矩阵的 FieldMeta[]，
 * 断言每种字段类型渲染出正确的控件（按 ARCH §3.2 映射总表）。
 *
 * 运行：pnpm exec cypress run --component
 */
import FormContent from '../views/components/FormContent.vue';
import type { FieldMeta } from '../types/field';

const LOV = 'Enum.CubeDemo.Areas.Test.测试枚举';

/** 覆盖全矩阵的字段元数据（24 类，超出 15 类要求） */
const FIELDS: FieldMeta[] = [
  { name: 'ShortText', displayName: '短文本', typeName: 'String', length: 50 },
  { name: 'LongText', displayName: '大文本', typeName: 'String', length: 500 },
  { name: 'Int32Val', displayName: '整数', typeName: 'Int32' },
  { name: 'Int64Val', displayName: '长整数', typeName: 'Int64' },
  { name: 'DecimalVal', displayName: '金额', typeName: 'Decimal' },
  { name: 'DoubleVal', displayName: '双精度', typeName: 'Double' },
  { name: 'FloatVal', displayName: '单精度', typeName: 'Single' },
  { name: 'Enable', displayName: '启用', typeName: 'Boolean' },
  { name: 'CreateTime', displayName: '创建时间', typeName: 'DateTime' },
  { name: 'Span', displayName: '时间段', typeName: 'TimeSpan' },
  { name: 'Kind', displayName: '种类', typeName: '测试枚举', lovCode: LOV },
  { name: 'GuidVal', displayName: '唯一标识', typeName: 'Guid' },
  { name: 'FileUrl', displayName: '附件', typeName: 'String', itemType: 'file' },
  { name: 'Avatar', displayName: '头像', typeName: 'String', itemType: 'image' },
  { name: 'JsonVal', displayName: 'Json', typeName: 'String', itemType: 'json' },
  { name: 'HtmlVal', displayName: '富文本', typeName: 'String', itemType: 'html' },
  { name: 'MarkdownVal', displayName: 'Markdown', typeName: 'String', itemType: 'markdown' },
  { name: 'ColorVal', displayName: '颜色', typeName: 'String', itemType: 'color' },
  { name: 'IconVal', displayName: '图标', typeName: 'String', itemType: 'icon' },
  { name: 'MailVal', displayName: '邮箱', typeName: 'String', itemType: 'mail' },
  { name: 'MobileVal', displayName: '手机', typeName: 'String', itemType: 'mobile' },
  { name: 'UrlVal', displayName: '网址', typeName: 'String', itemType: 'url' },
  { name: 'SingleVal', displayName: '单选', typeName: 'Int32', itemType: 'singleSelect', lovCode: LOV },
  { name: 'MultiVal', displayName: '多选', typeName: 'String', itemType: 'multipleSelect', lovCode: LOV, multiple: true },
];

/** 每个字段期望渲染的控件断言 */
type Expectation = (displayName: string) => void;

const EXPECTATIONS: Record<string, Expectation> = {
  短文本: (n) => cy.contains('.fmc-field', n).find('input').should('have.attr', 'type', 'text'),
  大文本: (n) => cy.contains('.fmc-field', n).find('.el-textarea').should('exist'),
  整数: (n) => cy.contains('.fmc-field', n).find('.el-input-number').should('exist'),
  长整数: (n) => cy.contains('.fmc-field', n).find('.el-input-number').should('exist'),
  金额: (n) => cy.contains('.fmc-field', n).find('.el-input-number').should('exist'),
  双精度: (n) => cy.contains('.fmc-field', n).find('.el-input-number').should('exist'),
  单精度: (n) => cy.contains('.fmc-field', n).find('.el-input-number').should('exist'),
  启用: (n) => cy.contains('.fmc-field', n).find('.el-switch').should('exist'),
  创建时间: (n) => cy.contains('.fmc-field', n).find('.el-date-editor--datetime').should('exist'),
  时间段: (n) => cy.contains('.fmc-field', n).find('.el-time-picker').should('exist'),
  种类: (n) => cy.contains('.fmc-field', n).find('.lov-select').should('exist'),
  唯一标识: (n) => cy.contains('.fmc-field', n).find('input').should('be.disabled'),
  附件: (n) => cy.contains('.fmc-field', n).find('.uploader').should('exist'),
  头像: (n) => cy.contains('.fmc-field', n).find('.uploader').should('exist'),
  Json: (n) => cy.contains('.fmc-field', n).find('.json-editor').should('exist'),
  富文本: (n) => cy.contains('.fmc-field', n).find('.rich-editor').should('exist'),
  Markdown: (n) => cy.contains('.fmc-field', n).find('.rich-editor').should('exist'),
  颜色: (n) => cy.contains('.fmc-field', n).find('.color-picker').should('exist'),
  图标: (n) => cy.contains('.fmc-field', n).find('.icon-selector').should('exist'),
  邮箱: (n) => cy.contains('.fmc-field', n).find('input').should('have.attr', 'type', 'email'),
  手机: (n) => cy.contains('.fmc-field', n).find('input').should('have.attr', 'type', 'tel'),
  网址: (n) => cy.contains('.fmc-field', n).find('input').should('have.attr', 'type', 'url'),
  单选: (n) => cy.contains('.fmc-field', n).find('.lov-select').should('exist'),
  多选: (n) => cy.contains('.fmc-field', n).find('.lov-select').should('exist'),
};

describe('FormContent 字段类型 → 控件渲染矩阵', { testTimeout: 30000 }, () => {
  beforeEach(() => {
    // 重型编辑器（wangEditor / md-editor / vanilla-jsoneditor / IconSelector）首次挂载较慢，放宽命令超时
    Cypress.config('defaultCommandTimeout', 15000);
    cy.mount(FormContent, {
      props: {
        title: '字段类型全覆盖',
        fields: FIELDS,
        modelValue: {},
      },
    });
    // 全表单渲染截图（每轮 beforeEach 都会重拍，overwrite 保证只保留一张）
    cy.screenshot('all-fields-form', { overwrite: true });
  });

  it('应渲染全部 24 个字段容器', () => {
    cy.get('.fmc-field').should('have.length', FIELDS.length);
  });

  FIELDS.forEach((f) => {
    const label = f.displayName as string;
    it(`字段「${label}」(${f.typeName}${f.itemType ? '/' + f.itemType : ''}) → 正确控件`, () => {
      const assert = EXPECTATIONS[label];
      expect(assert, `缺少「${label}」的控件断言`).to.exist;
      assert(label);
      // 单字段渲染截图（24 类各一张，displayName 唯一不冲突）
      cy.screenshot(`field-${label}`);
    });
  });

  it('附件控件按钮文案为「上传文件」、图片控件为「上传图片」', () => {
    cy.contains('.fmc-field', '附件').find('.uploader .el-button').should('contain.text', '上传文件');
    cy.contains('.fmc-field', '头像').find('.uploader .el-button').should('contain.text', '上传图片');
  });

  it('Json 编辑器应渲染 vanilla-jsoneditor 容器', () => {
    cy.contains('.fmc-field', 'Json').find('.json-editor__body').should('exist');
  });

  it('颜色控件应渲染取色器', () => {
    cy.contains('.fmc-field', '颜色').find('.el-color-picker').should('exist');
  });
});
