/// <reference types="cypress" />
// ***********************************************
// 自定义 Cypress 命令
// ***********************************************

/**
 * 公共登录命令：直接 POST /Auth/Login 获取 token 写入 localStorage
 *
 * 使用 cy.request 直接调用后端 API（不走前端 UI），
 * 在 before 钩子中调用一次即可为整个 describe 块建立登录态。
 *
 * Token 统一写入 localStorage['token']，与 core/utils/token.ts 一致，
 * 路由守卫通过 getAccessToken() 读取此 key 判断登录态。
 *
 * @param username 用户名，默认 'admin'
 * @param password 密码，默认 'admin'
 */
Cypress.Commands.add('login', (username = 'admin', password = 'admin') => {
  // 从 Cypress 环境变量获取后端地址，默认 https://localhost:7116
  const apiUrl = Cypress.env('apiUrl') || 'https://localhost:7116';

  cy.request({
    method: 'POST',
    url: `${apiUrl}/Auth/Login`,
    body: { username, password },
    failOnStatusCode: false,
  }).then((response) => {
    // 验证 HTTP 状态码
    expect(response.status, '登录 HTTP 状态码').to.eq(200);

    const body = response.body;

    // 验证业务状态码
    expect(body.code, '登录业务状态码').to.eq(0);

    // 提取 accessToken（兼容 snake_case: access_token、PascalCase: Token）
    const token = body.data?.accessToken || body.data?.access_token || body.data?.Token;
    expect(token, '登录 token').to.be.a('string');
    expect(token, '登录 token 非空').to.have.length.greaterThan(0);

    // 写入 localStorage，与 core/utils/token.ts 的 setAccessToken() 一致
    localStorage.setItem('token', token);

    // 可选：写入 refresh_token（兼容 snake_case 和 PascalCase）
    const refreshToken =
      body.data?.refreshToken || body.data?.refresh_token || body.data?.RefreshToken;
    if (refreshToken) {
      localStorage.setItem('refresh_token', refreshToken);
    }
  });
});

// TypeScript 类型声明：扩展 Cypress.Chainable 接口
declare global {
  namespace Cypress {
    interface Chainable {
      /**
       * 公共登录命令
       * 直接 POST /Auth/Login 获取 token 写入 localStorage['token']
       * @param username 用户名，默认 'admin'
       * @param password 密码，默认 'admin'
       */
      login(username?: string, password?: string): Chainable<void>;
    }
  }
}

export {};
