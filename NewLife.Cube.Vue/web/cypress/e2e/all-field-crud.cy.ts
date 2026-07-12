/**
 * 字段类型 CRUD 截图测试（Cypress）
 *
 * 流程：
 * 1. cy.login() 直连 API 登录 → 写入 token
 * 2. 导航到 /Test/TestField → 截图列表页
 * 3. 导航到 /Test/TestField/Add → 截图表单（全字段控件展示）
 * 4. 填写数据 → 提交 → 截图结果
 *
 * 在 Cypress 中，cy.login() 通过 cy.request 直连后端（不受跨域限制），
 * 登录完成后 localStorage 已有 token，路由守卫认为是已登录状态。
 */
describe('字段类型 CRUD — 全部截图', () => {
  const adminUser = 'admin';
  const adminPwd = 'admin';

  before(() => {
    // 全局登录一次，后续测试全部共享登录态
    cy.login(adminUser, adminPwd);
  });

  it('1. 字段类型 — 列表页截图（含搜索条件）', () => {
    cy.visit('/Test/TestField');
    // 等待页面渲染：默认模板的列表页有 .table-container 或 .page-container
    cy.get('table, .el-table, .page-container, .vue-container', { timeout: 15000 }).should('be.visible');
    cy.wait(1000); // 等待数据加载完毕
    cy.screenshot('01-test-field-list', { capture: 'fullPage' });
  });

  it('2. 字段类型 — 新增表单截图（全部控件）', () => {
    cy.visit('/Test/TestField/Add');
    // 等待表单渲染：el-form 或动态生成的 field 控件
    cy.get('form, .el-form, .form-container', { timeout: 15000 }).should('be.visible');
    cy.wait(1500);
    cy.screenshot('02-test-field-form', { capture: 'fullPage' });
  });

  it('3. 字段类型 — 填写部分字段后提交', () => {
    cy.visit('/Test/TestField/Add');
    cy.get('form, .el-form, .form-container', { timeout: 15000 }).should('be.visible');
    cy.wait(1000);

    // 尝试填充可见的输入框（不同类型的输入可能结构不同）
    cy.get('input').first().then(($input) => {
      // 第一个输入框通常是必填字段（短文本）
      cy.wrap($input).type('Cypress 测试数据', { force: true });
    });

    // 点击提交/保存按钮
    cy.get('button[type="submit"], .el-button--primary, button:contains("保存"), button:contains("提交")')
      .first().click({ force: true });

    // 提交后应跳回列表页
    cy.get('table, .el-table, .page-container', { timeout: 15000 }).should('be.visible');
    cy.wait(1000);
    cy.screenshot('03-test-field-after-submit', { capture: 'fullPage' });
  });

  it('4. 字段类型 — 编辑表单截图', () => {
    cy.visit('/Test/TestField');
    cy.get('table, .el-table, .page-container', { timeout: 15000 }).should('be.visible');
    cy.wait(1000);

    // 点击第一行的编辑按钮
    cy.get('a, button').contains(/编辑|修改|Edit/i).first().click({ force: true }).then(() => {
      // 可能直接跳转到编辑页
      cy.wait(1000);
    }).catch(() => {
      // 如果文本找不到，尝试点击表格第一行的第一个操作链接
      cy.get('table a, .el-table a').first().click({ force: true });
      cy.wait(1000);
    });

    // 等待编辑表单渲染
    cy.get('form, .el-form, .form-container', { timeout: 15000 }).should('be.visible');
    cy.wait(1000);
    cy.screenshot('04-test-field-edit-form', { capture: 'fullPage' });
  });

  it('5. 字段类型 — 搜索区展开截图', () => {
    cy.visit('/Test/TestField');
    cy.get('table, .el-table, .page-container', { timeout: 15000 }).should('be.visible');
    cy.wait(1000);

    // 展开搜索区（如果有折叠按钮）
    cy.get('button, .el-button').contains(/搜索|查找|查询|展开|Search/i).first().click({ force: true }).catch(() => {
      // 搜索区可能已经展开
    });
    cy.wait(500);
    cy.screenshot('05-test-field-search', { capture: 'fullPage' });
  });
});
