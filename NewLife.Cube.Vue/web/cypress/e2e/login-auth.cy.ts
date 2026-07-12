/**
 * 登录页 E2E 测试（带密码强度校验）
 *
 * 测试场景：
 * 1. 直接 API 登录（cy.login）验证后端登录正常
 * 2. 访问登录页 — 验证密码强度规则 UI 渲染
 * 3. admin/admin 通过登录页表单提交登录
 * 4. 截图登录页面
 */
describe('登录页 — 密码强度校验', () => {
  const adminUser = 'admin';
  const adminPwd = 'admin';

  // ── 场景 1：API 直连登录 ──
  it('cy.login() 直接 API 登录成功', () => {
    cy.login(adminUser, adminPwd);
    // 登录后跳转到首页（避免白屏）
    cy.visit('/');
    cy.get('.login-page').should('not.exist');
  });

  // ── 场景 2：登录页 UI 验证（未登录状态） ──
  it('登录页渲染密码强度规则 UI', () => {
    // 清除登录态，回到登录页
    cy.clearLocalStorage('token');
    cy.visit('/', { failOnStatusCode: false });

    // 登录页应该渲染
    cy.get('.login-page', { timeout: 10000 }).should('be.visible');

    // 截图：初始登录页
    cy.screenshot('login-page-initial', { capture: 'viewport' });

    // 品牌信息
    cy.get('.brand-name').should('be.visible');
  });

  // ── 场景 3：密码强度提示 UI ──
  it('输入密码时显示密码强度实时提示', () => {
    cy.clearLocalStorage('token');
    cy.visit('/', { failOnStatusCode: false });
    cy.get('.login-page', { timeout: 10000 }).should('be.visible');

    // 输入用户名
    cy.get('#login-username').type('admin');

    // 输入密码（不符合默认强密码规则）
    cy.get('#login-password').type('admin');

    // 检查密码强度提示面板是否出现
    cy.get('.password-hints', { timeout: 3000 }).should('be.visible');

    // 应该有提示项（规则解析结果）
    cy.get('.password-hint-item').should('have.length.at.least', 1);

    // 截图：密码输入 + 强度提示
    cy.screenshot('login-password-hints', { capture: 'viewport' });

    // 至少有一个规则展示了 ✓（已满足）
    cy.get('.password-hint-item.satisfied').should('have.length.at.least', 1);
  });

  // ── 场景 4：admin/admin 登录成功 ──
  it('admin/admin 通过登录页表单提交登录成功', () => {
    cy.clearLocalStorage('token');
    cy.visit('/', { failOnStatusCode: false });
    cy.get('.login-page', { timeout: 10000 }).should('be.visible');

    // 填写表单
    cy.get('#login-username').type(adminUser);
    cy.get('#login-password').type(adminPwd);

    // 截图：填写完成
    cy.screenshot('login-form-filled', { capture: 'viewport' });

    // 提交登录
    cy.get('.login-btn').click();

    // 登录成功 → 跳转首页
    cy.get('.login-page', { timeout: 15000 }).should('not.exist');

    // 截图：登录成功后的页面
    cy.screenshot('login-success-dashboard', { capture: 'viewport' });
  });

  // ── 场景 5：密码强度不符合时的提交拦截 ──
  it('密码不符合规则时表单提交被拦截', () => {
    cy.clearLocalStorage('token');
    cy.visit('/', { failOnStatusCode: false });
    cy.get('.login-page', { timeout: 10000 }).should('be.visible');

    // 输入用户名
    cy.get('#login-username').type('testuser');

    // 输入弱密码（长度不够）
    cy.get('#login-password').type('123');

    // 提交
    cy.get('.login-btn').click();

    // 应有错误提示（密码校验拦截）
    cy.get('.input-error').should('be.visible');

    // 截图：错误状态
    cy.screenshot('login-password-error', { capture: 'viewport' });
  });
});
