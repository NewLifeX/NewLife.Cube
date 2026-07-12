/**
 * 字段类型全覆盖 — Cypress E2E 冒烟测试（真实 CRUD）
 *
 * 前置条件：
 *   1. 启动后端：D:/src/os-proj/X/NewLife.Cube/CubeDemo
 *        dotnet run --project CubeDemo.csproj -f net10.0 --urls "https://localhost:7116"
 *   2. 启动前端：D:/src/os-proj/X/NewLife.Cube/NewLife.Cube.Vue/web
 *        pnpm dev   （默认 http://localhost:5187，API 直连 https://localhost:7116，不走 Vite 代理）
 *   3. 浏览器可访问 http://localhost:5187
 *
 * 运行：
 *   cd NewLife.Cube.Vue/web
 *   pnpm exec cypress run --e2e --browser chrome --spec cypress/e2e/fieldtypes-crud.cy.ts
 *
 * 截图（输出到 cypress/screenshots/fieldtypes-crud.cy.ts/）：
 *   list        列表页加载
 *   form-new    点击「新建」后的表单对话框
 *   after-save  新建并保存成功后
 *   edit        编辑已有记录（对话框内已改值）
 *   after-delete 删除成功后
 */
describe('字段类型全覆盖 E2E（TestField 真实 CRUD）', () => {
  const BASE = '/Admin/Test/TestField';

  // 公共登录前置：直接 POST /Auth/Login 获取 token 写入 localStorage
  // 在所有测试用例之前执行一次，后续 beforeEach 的 cy.visit 自动带上登录态
  before(() => {
    cy.login();
  });

  beforeEach(() => {
    // localStorage 已有 token（由 before 写入），路由守卫放行
    cy.visit(BASE);
  });

  it('列表页加载且展示多类型字段列', () => {
    cy.get('.el-table').should('exist');
    // 覆盖 §3.2 中的代表性列：文本 / 数值 / 布尔 / 日期 / 枚举(LOV)
    cy.contains('th', '短文本').should('exist');
    cy.contains('th', '数值').should('exist');
    cy.contains('th', '是否启用').should('exist');
    cy.screenshot('list');
  });

  it('新建：表单按字段类型渲染正确控件', () => {
    cy.contains('button', '新建').click();
    cy.get('.el-dialog').should('exist');
    // 文本 → input[type=text]
    cy.get('input[type="text"]').should('exist');
    // 数值 → input-number
    cy.get('.el-input-number').should('exist');
    // 布尔 → switch
    cy.get('.el-switch').should('exist');
    // 日期 → date-picker（根节点类 el-date-editor）
    cy.get('.el-date-editor').should('exist');
    // 枚举 / 单选 → LOV 选择器
    cy.get('.lov-select').should('exist');
    // 大文本 → textarea
    cy.get('textarea').should('exist');
    cy.screenshot('form-new');
  });

  it('新建并保存一条记录', () => {
    cy.contains('button', '新建').click();
    cy.get('.el-dialog').within(() => {
      cy.get('input[type="text"]').first().type('E2E-测试记录');
      cy.get('.el-input-number input').first().type('123');
    });
    cy.contains('button', '保存').click();
    cy.get('.el-message--success').should('exist');
    cy.contains('E2E-测试记录').should('exist');
    cy.screenshot('after-save');
  });

  it('编辑已有记录', () => {
    cy.contains('tr', 'E2E-测试记录').find('.el-button--primary').click();
    cy.get('.el-dialog').within(() => {
      cy.get('input[type="text"]').first().clear().type('E2E-已编辑');
    });
    cy.screenshot('edit');
    cy.contains('button', '保存').click();
    cy.get('.el-message--success').should('exist');
  });

  it('删除记录', () => {
    cy.contains('tr', 'E2E-已编辑').find('.el-button--danger').click();
    cy.contains('button', '确定').click();
    cy.get('.el-message--success').should('exist');
    cy.contains('E2E-已编辑').should('not.exist');
    cy.screenshot('after-delete');
  });
});
